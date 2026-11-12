using System.Reflection;
using Poolcore;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.PanoramaLightingChecks;

public sealed class PanoramaChecksDriver : MonoBehaviour
{
    public static List<MapDrawLayer> Layers(Map map) => (List<MapDrawLayer>)global.GetValue(map.mapDrawer);

    public static IEnumerable<object> Frames(int count)
    {
        int end = Time.frameCount + count;
        while (Time.frameCount < end) yield return null;
    }

    public static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        File.AppendAllText(Path.Combine(Bootstrap.OutputDirectory, "Checks.txt"), "PASS " + name + "\n");
    }

    public static void Position(Vector3 center, float size)
    {
        DrawCapture.Center = center;
        DrawCapture.Size = size;
        DrawCapture.CameraLocked = true;
        DrawCapture.LockCamera();
    }

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 1100f) throw new TimeoutException("CQF panorama runtime checks");
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
            checks ??= Run().GetEnumerator();
            if (checks.MoveNext()) return;
            File.AppendAllText(Path.Combine(Bootstrap.OutputDirectory, "Checks.txt"), "COMPLETE\n");
            finished = true;
            Time.timeScale = 1f;
            Application.Quit(0);
        }
        catch (Exception exception)
        {
            File.AppendAllText(Path.Combine(Bootstrap.OutputDirectory, "Checks.txt"), "FAIL " + exception + "\n");
            Log.Error("[CQF.PanoramaLightingChecks] " + exception);
            finished = true;
            Time.timeScale = 1f;
            Application.Quit(1);
        }
    }

    private void LateUpdate()
    {
        if (!finished) DrawCapture.LockCamera();
    }

    private IEnumerable<object> Run()
    {
        DrawCapture.Install();
        DebugSettings.enableStoryteller = false;
        Find.TickManager.Pause();
        Find.CameraDriver.config.sizeRange = new FloatRange(11f, 1200f);
        Find.WindowStack.WindowOfType<LudeonTK.EditWindow_Log>()?.Close();
        Check(typeof(MapDrawLayer_BrutalistBackdrop).Assembly.GetType("Poolcore.MapDrawLayer_PanoramaLighting") == null,
            "Poolcore temporary lighting layer removed from loaded assembly");
        Map normal = Find.CurrentMap;
        scenes.Add(("Normal", normal.uniqueID));
        foreach ((string name, string def) in new[] { ("Star", "CQF_PL_Star"), ("Pool", "PC_PoolcoreSceneMap"), ("Concrete", "PC_ConcreteSceneMap") })
        {
            Time.timeScale = 1f;
            if (name == "Star") DefDatabase<CustomMapDataDef>.GetNamed(def).generator = DefDatabase<CustomMapDataDef>.GetNamed("PC_PoolcoreSceneMap").generator;
            LongEventHandler.QueueLongEvent(() => PoolcorePreview.Create(normal, def), "GenerateMap", false, null);
            yield return null;
            scenes.Add((name, Find.CurrentMap.uniqueID));
            Check(Find.CurrentMap != normal, name + " native scene generated");
        }
        foreach ((string name, int id) in scenes)
        {
            Map map = Find.Maps.Single(m => m.uniqueID == id);
            Current.Game.CurrentMap = map;
            Find.TickManager.Pause();
            CustomMapBackgroundData background = map.GetComponent<MapComponent_CustomMapData>().background;
            if (name != "Normal") Check(background.Enabled && background.DrawOnCameraVisibleArea, name + " CameraVisible enabled");
            if (name is "Pool" or "Concrete") Check(background.texPath == "UI/Null", name + " other layers supply UI/Null panorama");
            map.weatherManager.TransitionTo(WeatherDefOf.Clear);
            map.mapDrawer.RegenerateEverythingNow();
            MapDrawLayer_CQFPanoramaLighting layer = Layers(map).OfType<MapDrawLayer_CQFPanoramaLighting>().Single();
            MapDrawLayer_ExteriorLightingOverlay original = Layers(map).OfType<MapDrawLayer_ExteriorLightingOverlay>().Single();
            Vector3[][] originalVertices = original.subMeshes.Select(sm => sm.verts.ToArray()).ToArray();
            Mesh[] cachedMeshes = layer.subMeshes.Select(sm => sm.mesh).ToArray();
            foreach ((string time, int tick) in new[] { ("Dawn", 15000), ("Noon", 30000), ("Dusk", 45000), ("Night", 0) })
            {
                int offset = (tick - GenLocalDate.DayTick(map) + 60000) % 60000;
                Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + offset);
                map.skyManager.SkyManagerUpdate();
                map.glowGrid.GlowGridUpdate_First();
                map.mapDrawer.RegenerateEverythingNow();
                Time.timeScale = 0f;
                foreach ((string view, Vector3 center, float size) in Views(map))
                {
                    Position(center, size);
                    foreach (object wait in Frames(6)) yield return wait;
                    ValidateGeometry(map, layer, name + "_" + time + "_" + view, name != "Normal");
                    if (view == "Wide" || time == "Dawn")
                    {
                        foreach (object wait in CapturePair(name + "_" + time + "_" + view, layer)) yield return wait;
                        if (name == "Pool")
                        {
                            MapComponent_Poolcore pool = map.GetComponent<MapComponent_Poolcore>();
                            float haze = pool.hazeStrength;
                            pool.hazeStrength = 0f;
                            foreach (object wait in Frames(3)) yield return wait;
                            ValidateGeometry(map, layer, name + "_" + time + "_" + view + "_NoHaze", true);
                            foreach (object wait in CapturePair(name + "_" + time + "_" + view + "_NoHaze", layer)) yield return wait;
                            pool.hazeStrength = haze;
                        }
                    }
                }
            }
            Check(originalVertices.Length == original.subMeshes.Count && originalVertices.Select((verts, i) => verts.SequenceEqual(original.subMeshes[i].verts)).All(v => v),
                name + " native exterior geometry unchanged");
            if (cachedMeshes.Length > 0) Check(cachedMeshes.SequenceEqual(layer.subMeshes.Select(sm => sm.mesh)), name + " mesh instances reused across views and regeneration");
            if (name != "Normal")
            {
                savedBackgrounds.Add(id, (background.texPath, background.drawScope, background.fitMode));
                foreach (object wait in ValidateModes(map, layer, name)) yield return wait;
            }
        }
        Time.timeScale = 1f;
        string save = "CQF_PL_" + Path.GetFileName(Bootstrap.OutputDirectory);
        GameDataSaveLoader.SaveGame(save);
        Bootstrap.WriteJson("Save.json", new { Name = save, Scenes = scenes, Backgrounds = savedBackgrounds });
        GameDataSaveLoader.LoadGame(save);
        yield return null;
        foreach ((string name, int id) in scenes)
        {
            Map map = Find.Maps.Single(m => m.uniqueID == id);
            Current.Game.CurrentMap = map;
            Find.TickManager.Pause();
            map.mapDrawer.RegenerateEverythingNow();
            CustomMapBackgroundData background = map.GetComponent<MapComponent_CustomMapData>().background;
            if (savedBackgrounds.TryGetValue(id, out var saved))
                Check(background.texPath == saved.Path && background.drawScope == saved.Scope && background.fitMode == saved.Fit,
                    name + " real game save/load preserves background");
            MapDrawLayer_CQFPanoramaLighting layer = Layers(map).OfType<MapDrawLayer_CQFPanoramaLighting>().Single();
            Position(map.Center.ToVector3Shifted(), 900f);
            foreach (object wait in Frames(6)) yield return wait;
            ValidateGeometry(map, layer, name + "_AfterLoad", name != "Normal");
            foreach (object wait in CapturePair(name + "_AfterLoad", layer)) yield return wait;
        }
        DrawCapture.RemoveDrawObserver();
        Map sampleMap = Find.Maps.Single(m => m.uniqueID == scenes.Single(s => s.Name == "Pool").Id);
        Current.Game.CurrentMap = sampleMap;
        foreach (object wait in PanoramaProfiler.Run(sampleMap)) yield return wait;
    }

    private static IEnumerable<(string Name, Vector3 Center, float Size)> Views(Map map)
    {
        float right = map.Size.x + 200f;
        float top = map.Size.z + 200f;
        Vector3 center = map.Center.ToVector3Shifted();
        yield return ("Inside", center, 20f);
        yield return ("Left", new Vector3(-200f, 0f, center.z), 55f);
        yield return ("Right", new Vector3(right, 0f, center.z), 55f);
        yield return ("Bottom", new Vector3(center.x, 0f, -200f), 55f);
        yield return ("Top", new Vector3(center.x, 0f, top), 55f);
        yield return ("BottomLeft", new Vector3(-200f, 0f, -200f), 55f);
        yield return ("BottomRight", new Vector3(right, 0f, -200f), 55f);
        yield return ("TopLeft", new Vector3(-200f, 0f, top), 55f);
        yield return ("TopRight", new Vector3(right, 0f, top), 55f);
        yield return ("Wide", center, 900f);
    }

    private static void ValidateGeometry(Map map, MapDrawLayer_CQFPanoramaLighting layer, string name, bool enabled)
    {
        Camera camera = Find.Camera;
        Check(Math.Abs(camera.transform.position.x - DrawCapture.Center.x) < 0.001f
            && Math.Abs(camera.transform.position.z - DrawCapture.Center.z) < 0.001f
            && Math.Abs(camera.orthographicSize - DrawCapture.Size) < 0.001f, name + " actual camera reached requested position and zoom");
        Rect viewport = Rect.MinMaxRect(camera.transform.position.x - camera.orthographicSize * camera.aspect,
            camera.transform.position.z - camera.orthographicSize, camera.transform.position.x + camera.orthographicSize * camera.aspect,
            camera.transform.position.z + camera.orthographicSize);
        Rect covered = Rect.MinMaxRect(-200f, -200f, map.Size.x + 200f, map.Size.z + 200f);
        Submission[] calls = DrawCapture.Frame >= Time.frameCount - 1 ? DrawCapture.Calls.ToArray() : Array.Empty<Submission>();
        if (!enabled || !layer.Visible) Check(calls.Length == 0, name + " no supplemental submissions");
        else
        {
            var light = calls.Where(c => c.Material == MatBases.LightOverlay).ToArray();
            var shadow = calls.Where(c => c.Material == MatBases.ShadowMask).ToArray();
            double expectedArea = viewport.width * (double)viewport.height - IntersectionArea(viewport, covered);
            Check(light.Length == shadow.Length && light.Length <= 4 && calls.Length == light.Length * 2, name + " two native material submissions per nonempty region");
            Check(Math.Abs(light.Sum(c => c.Area.width * (double)c.Area.height) - expectedArea) < Math.Max(0.01, expectedArea * 0.00001), name + " exact viewport minus native rectangle coverage");
            for (int i = 0; i < light.Length; i++)
            {
                Check(IntersectionArea(light[i].Area, covered) < 0.001 && light[i].Area == shadow[i].Area,
                    name + " no native-region overlap " + i);
                for (int j = i + 1; j < light.Length; j++)
                    Check(IntersectionArea(light[i].Area, light[j].Area) < 0.001, name + " disjoint regions " + i + "/" + j);
            }
            Check(calls.All(c => Math.Abs(c.Altitude - AltitudeLayer.LightingOverlay.AltitudeFor()) < 0.0001
                && c.Layer == LayerMask.NameToLayer("GravshipExclude")), name + " native altitude and render layer");
        }
        File.AppendAllText(Path.Combine(Bootstrap.OutputDirectory, "Cameras.jsonl"), System.Text.Json.JsonSerializer.Serialize(new
        {
            Name = name, Frame = Time.frameCount, Map = map.uniqueID, MapSizeX = map.Size.x, MapSizeZ = map.Size.z,
            X = camera.transform.position.x, Y = camera.transform.position.y, Z = camera.transform.position.z,
            Size = camera.orthographicSize, Aspect = camera.aspect, Near = camera.nearClipPlane, Far = camera.farClipPlane,
            Rotation = camera.transform.eulerAngles.ToString(), Width = Screen.width, Height = Screen.height, UIScale = Prefs.UIScale,
            ViewLeft = viewport.xMin, ViewRight = viewport.xMax, ViewBottom = viewport.yMin, ViewTop = viewport.yMax,
            LocalTick = GenLocalDate.DayTick(map), Visible = layer.Visible, Submissions = calls.Length,
            Areas = calls.Select(c => new { Left = c.Area.xMin, Right = c.Area.xMax, Bottom = c.Area.yMin, Top = c.Area.yMax, c.Altitude, c.Layer, Material = c.Material.name })
        }) + "\n");
    }

    private static double IntersectionArea(Rect a, Rect b)
    {
        return Math.Max(0f, Math.Min(a.xMax, b.xMax) - Math.Max(a.xMin, b.xMin))
            * (double)Math.Max(0f, Math.Min(a.yMax, b.yMax) - Math.Max(a.yMin, b.yMin));
    }

    private static IEnumerable<object> CapturePair(string name, MapDrawLayer_CQFPanoramaLighting layer)
    {
        foreach (LayerSubMesh mesh in layer.subMeshes) mesh.disabled = true;
        foreach (object wait in Frames(3)) yield return wait;
        foreach (object wait in Capture(name + "_Before")) yield return wait;
        foreach (LayerSubMesh mesh in layer.subMeshes) mesh.disabled = false;
        foreach (object wait in Frames(3)) yield return wait;
        foreach (object wait in Capture(name + "_After")) yield return wait;
    }

    private static IEnumerable<object> Capture(string name)
    {
        string path = Path.Combine(Bootstrap.OutputDirectory, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        float deadline = Time.realtimeSinceStartup + 15f;
        do
        {
            yield return null;
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(path);
        } while (!File.Exists(path) || new FileInfo(path).Length == 0);
        foreach (object wait in Frames(2)) yield return wait;
        Check(File.Exists(path), "native screenshot " + name);
    }

    private static IEnumerable<object> ValidateModes(Map map, MapDrawLayer_CQFPanoramaLighting layer, string name)
    {
        var background = map.GetComponent<MapComponent_CustomMapData>().background;
        string path = background.texPath;
        Position(map.Center.ToVector3Shifted(), 900f);
        foreach ((string mode, Action change, bool expected) in new[]
        {
            ("Map", (Action)(() => background.drawScope = CustomMapBackgroundDrawScope.Map), false),
            ("CameraVisible", () => background.drawScope = CustomMapBackgroundDrawScope.CameraVisible, true),
            ("Disabled", () => background.texPath = null, false),
            ("UI_Null", () => background.texPath = "UI/Null", true),
            ("LightingOff", () => DebugViewSettings.drawLightingOverlay = false, false),
            ("LightingOn", () => DebugViewSettings.drawLightingOverlay = true, true)
        })
        {
            change();
            foreach (object wait in Frames(5)) yield return wait;
            Check(layer.Visible == expected, name + " mode " + mode);
            ValidateGeometry(map, layer, name + "_Mode_" + mode, expected);
        }
        background.texPath = path;
        Check(background.Copy().drawScope == CustomMapBackgroundDrawScope.CameraVisible
            && background.SaveToXElement("background").Element("drawScope")?.Value == "CameraVisible", name + " copy and XML keep panorama mode");
    }

    private IEnumerator<object> checks;
    private bool finished;
    private readonly List<(string Name, int Id)> scenes = new();
    private readonly Dictionary<int, (string Path, CustomMapBackgroundDrawScope Scope, CustomMapBackgroundFitMode Fit)> savedBackgrounds = new();
    private static readonly FieldInfo global = typeof(MapDrawer).GetField("global", BindingFlags.NonPublic | BindingFlags.Instance);
}
