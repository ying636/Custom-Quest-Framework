using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Waylith;
using Waylith.Graphics;
using Waylith.UnityCompat;
using Time = UnityEngine.Time;

namespace CQF.ProjectionQualityChecks;

public sealed partial class QualityChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        try
        {
            routine ??= Run().GetEnumerator();
            if (routine.MoveNext()) return;
            routine.Dispose();
            finished = true;
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "COMPLETE\n");
            Application.Quit(0);
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "FAIL " + error + "\n");
            Log.Error("[CQF.ProjectionQualityChecks] " + error);
            finished = true;
            routine?.Dispose();
            Application.Quit(1);
        }
    }

    private IEnumerable<object?> Run()
    {
        Find.TickManager.Pause();
        DebugSettings.enableStoryteller = false;
        Find.WindowStack.WindowOfType<LudeonTK.EditWindow_Log>()?.Close();
        string save = Path.Combine(GenFilePaths.SaveDataFolderPath, "Saves", "CQF_ProjectionQuality.rws");
        if (!File.Exists(save))
        {
            host = Find.CurrentMap;
            EditorMapObject parent = (EditorMapObject)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("QE_CustomMap_Editor"));
            parent.Tile = TileFinder.RandomSettlementTileFor(Faction.OfPlayer);
            parent.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(parent);
            LongEventHandler.QueueLongEvent(() => source = MapGenerator.GenerateMap(new IntVec3(250, 1, 250), parent, parent.MapGeneratorDef),
                "GeneratingMap", false, null);
            yield return null;
            foreach (Map map in new[] { host!, source! })
            {
                foreach (Thing thing in map.spawnedThings.ToArray())
                    if (thing is not Pawn) { if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish); else thing.DeSpawn(); }
                foreach (IntVec3 cell in map.AllCells)
                {
                    map.terrainGrid.SetTerrain(cell, map == source && (cell.x / 5 + cell.z / 5) % 2 == 0 ? TerrainDefOf.WoodPlankFloor : TerrainDefOf.Soil);
                    map.roofGrid.SetRoof(cell, null);
                }
                map.fogGrid.ClearAllFog();
            }
            ThingDef oak = DefDatabase<ThingDef>.GetNamed("Plant_TreeOak");
            for (int x = 4; x < 247; x += 3)
                for (int z = 4; z < 247; z += 3)
                {
                    Plant tree = (Plant)ThingMaker.MakeThing(oak);
                    tree.Growth = 1f;
                    GenSpawn.Spawn(tree, new IntVec3(x, 0, z), source!);
                }
            for (int i = 0; i < 40; i++)
                GenSpawn.Spawn(PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer), new IntVec3(112 + i % 10 * 2, 0, 112 + i / 10 * 2), source!);
            Current.Game.CurrentMap = host;
            GameDataSaveLoader.SaveGame("CQF_ProjectionQuality");
            yield return null;
        }
        Game before = Current.Game;
        GameDataSaveLoader.LoadGame("CQF_ProjectionQuality");
        yield return null;
        Check(Current.Game != before, "fixed native save reload completes");
        host = Find.CurrentMap;
        source = Current.Game.Maps.Single(map => map != host && map.Parent is EditorMapObject);
        Find.TickManager.Pause();
        Find.MainTabsRoot.SetCurrentTab(null, playSound: false);
        Find.Selector.ClearSelection();
        host.mapDrawer.RegenerateEverythingNow();
        source.mapDrawer.RegenerateEverythingNow();
        projection = host.GetComponent<MapComponent_CQFMapProjections>().Add(new CQFMapProjection
        {
            id = "CQF_Quality", sourceMap = source, destination = CellRect.WholeMap(source), matchCellSize = true,
            mode = CQFMapProjectionMode.Display, resolution = 1024, refreshFrames = 1
        });
        Write("Scene.json", new
        {
            Save = save, SHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(save))),
            ProjectionSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(CQFMapProjection).Assembly.Location))),
            CoreSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(GameComponent_MapManager).Assembly.Location))),
            SourceSize = source.Size.ToString(), SourceThings = source.spawnedThings.Count, SourcePawns = source.mapPawns.AllPawnsSpawned.Count,
            Screen.width, Screen.height, Prefs.UIScale, Version = VersionControl.CurrentVersionStringWithRev,
            AutoResolutionAvailable = typeof(CQFMapProjection).GetField("autoResolution") != null
        });
        int oldLimit = Application.targetFrameRate;
        VSyncMode oldSync = UnityRuntime.Current!.Screen.VSyncMode;
        Application.targetFrameRate = -1;
        EngineWindow.SetVSyncMode(VSyncMode.Off);
        try
        {
            Current.Game.CurrentMap = source;
            SetCamera(24f);
            foreach (object? wait in Frames(30)) yield return wait;
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "Reference.png"));
            foreach (object? wait in Frames(4)) yield return wait;
            Current.Game.CurrentMap = host;
            SetCamera(24f);
            foreach (object? wait in Frames(30)) yield return wait;
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "Projected.png"));
            foreach (object? wait in Frames(4)) yield return wait;
            Check(projection.Texture != null, "large one-to-one projection produces native pixels");
            if (typeof(CQFMapProjection).GetProperty("TextureBounds") is { } captureProperty)
            {
                Rect captured = (Rect)captureProperty.GetValue(projection)!;
                Check(captured.width > 0f && captured.height > 0f && captured.width * captured.height < 250f * 250f / 2f,
                    "submods can query the actual cropped source rectangle for the exposed texture");
                using (new CQFMapProjectionContext(projection, Current.Camera))
                    Check(Find.CameraDriver.CurrentViewRect == projection.VisibleCells, "display cropping preserves the full input coordinate scope");
                Write("Capture.json", new { Bounds = captured.ToString(), captured.width, captured.height,
                    TextureWidth = projection.Texture!.width, TextureHeight = projection.Texture.height, ScreenWidth = Screen.width, ScreenHeight = Screen.height });
            }
            if (GenCommandLine.CommandLineArgPassed("cqfprojectionpaired"))
                foreach (object? wait in PairedFrames()) yield return wait;
            for (int round = 0; round < (GenCommandLine.CommandLineArgPassed("cqfprojectionpaired") ? 0 : 3); round++)
                foreach ((string name, bool enabled, float size, bool focused, int refresh) in Scenarios())
                {
                    Configure(enabled, size, focused, refresh);
                    foreach (object? wait in Frames(120)) yield return wait;
                    List<double> durations = new();
                    Bootstrap.RenderCalls = 0;
                    Bootstrap.Sampling = true;
                    long previous = Stopwatch.GetTimestamp(), started = previous;
                    for (int i = 0; i < 600 || (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency < 3d; i++)
                    {
                        yield return null;
                        long now = Stopwatch.GetTimestamp();
                        durations.Add((now - previous) * 1000d / Stopwatch.Frequency);
                        previous = now;
                    }
                    Bootstrap.Sampling = false;
                    double[] sorted = durations.Order().ToArray();
                    Write($"Frames-{round}-{name}.json", new
                    {
                        Scenario = name, Round = round, FrameCount = durations.Count, FPS = durations.Count * 1000d / durations.Sum(),
                        MedianMs = sorted[sorted.Length / 2], P95Ms = sorted[(int)(sorted.Length * 0.95)], FrameTimesMs = durations,
                        Bootstrap.RenderCalls, Paused = true, FrameLimit = Application.targetFrameRate, VSync = "Off",
                        TextureWidth = projection.Texture?.width, TextureHeight = projection.Texture?.height,
                        View = Find.CameraDriver.CurrentViewRect.ToString(), RefreshFrames = refresh, Focused = focused,
                        PixelRect = projection.ScreenRect(Current.Camera).ToString(), Instrumentation = false
                    });
                    Check(enabled ? Bootstrap.RenderCalls > 0 : Bootstrap.RenderCalls == 0, "actual frame sampling observes render workload: " + name + " round " + round);
                }
            foreach (object? wait in Profile()) yield return wait;
            Configure(true, 24f, true, 60);
            foreach (object? wait in Frames(65)) yield return wait;
            int frame = projection.RenderedFrame;
            projection.PanView(new Vector2(0f, 0f));
            foreach (object? wait in Frames(2)) yield return wait;
            Check(projection.RenderedFrame > frame, "viewport invalidation refreshes immediately with a sixty-frame interval");
            if (typeof(CQFMapProjection).GetField("autoResolution") is { } field)
            {
                field.SetValue(projection, false);
                foreach (object? wait in Frames(65)) yield return wait;
                Check(Math.Max(projection.Texture!.width, projection.Texture.height) == 1024, "manual render resolution remains available");
                field.SetValue(projection, true);
            }
            if (GenCommandLine.CommandLineArgPassed("cqfprojectionpoolpaired"))
                foreach (object? wait in CheckDrawingBuffers()) yield return wait;
        }
        finally
        {
            Bootstrap.Sampling = false;
            Application.targetFrameRate = oldLimit;
            EngineWindow.SetVSyncMode(oldSync);
            CQFMapProjectionContext.ReturnToHost();
        }
    }

    private void Configure(bool enabled, float size, bool focused, int refresh)
    {
        CQFMapProjectionContext.ReturnToHost();
        projection!.enabled = enabled;
        projection.mode = focused ? CQFMapProjectionMode.Control : CQFMapProjectionMode.Display;
        projection.refreshFrames = refresh;
        SetCamera(size);
        if (focused) CQFMapProjectionContext.Focus(host!, projection);
    }

    private static void SetCamera(float size) => Find.CameraDriver.SetRootPosAndSize(new Vector3(125f, 0f, 125f), size);

    private static (string, bool, float, bool, int)[] Scenarios() => new[]
    {
        ("None", false, 24f, false, 1), ("CloseDisplay", true, 24f, false, 1),
        ("ClosePlayable", true, 24f, true, 1), ("ClosePlayableEvery3", true, 24f, true, 3),
        ("CloseDisplayEvery6", true, 24f, false, 6), ("FullDisplay", true, 160f, false, 1)
    };

    private static IEnumerable<object?> Frames(int count)
    {
        int end = Time.frameCount + count;
        while (Time.frameCount < end) yield return null;
    }

    private static void Write(string name, object value) => File.WriteAllText(Path.Combine(Bootstrap.Output, name),
        JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "PASS " + name + "\n");
    }

    private IEnumerator<object?>? routine;
    private Map? host;
    private Map? source;
    private CQFMapProjection? projection;
    private bool finished;
}
