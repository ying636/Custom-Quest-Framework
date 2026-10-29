using System.Reflection;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CQF.MapLayoutRuntimeChecks;

public sealed class MapLayoutChecksDriver : MonoBehaviour
{
    private string Root => GenFilePaths.SaveDataFolderPath;
    private string Results => Path.Combine(Root, "MapLayoutChecks.txt");

    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 310f) throw new TimeoutException("Map layout runtime checks timed out at " + stage);
            if (LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Current.Game == null || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
            if (Time.realtimeSinceStartup - changedAt < 0.6f) return;
            Find.TickManager.Pause();
            if (stage == 0)
            {
                source = Find.CurrentMap;
                Directory.CreateDirectory(Path.Combine(Root, "Screenshots"));
                Check(CQFEditorBridge.IsLoaded, "editor module is loaded from staged candidate");
                QuestEditor_CustomQuestMap.GenerateMap(new IntVec3(120, 1, 120));
                Go(1);
            }
            else if (stage == 1)
            {
                if (Find.CurrentMap == source) return;
                map = Find.CurrentMap;
                Setup();
                regionWindow = new Dialog_CQFMapRegions(map, city);
                Find.WindowStack.Add(regionWindow);
                Go(2);
            }
            else if (stage == 2)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Root, "Screenshots", "01-Regions.png")); Go(3);
            }
            else if (stage == 3)
            {
                regionWindow!.Close();
                layoutWindow = new Dialog_CQFLayout(map!, settings);
                Find.WindowStack.Add(layoutWindow); Go(4);
            }
            else if (stage == 4)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Root, "Screenshots", "02-LayoutSettings.png")); Go(5);
            }
            else if (stage == 5)
            {
                layoutWindow!.Close();
                plan = settings!.Preview(map!);
                Check(plan.Parts.Count == 3 && plan.Roads.Count > 0, "actual map preview finds three linked settlements");
                Check(map!.listerThings.AllThings.Count == initialThingCount, "preview leaves actual map contents unchanged");
                Find.DesignatorManager.Select(new Designator_CQFLayoutPreview(map, settings, plan));
                Find.CameraDriver.SetRootPosAndSize(map.Center.ToVector3Shifted(), 66f); Go(6);
            }
            else if (stage == 6)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Root, "Screenshots", "03-Preview.png")); Go(7);
            }
            else if (stage == 7)
            {
                Find.DesignatorManager.Deselect();
                settings!.Generate(map!, null!, new CustomMapGenerationContext(null!));
                Check(wall != null && wall.Spawned && wall.HitPoints == wallHealth, "protected city wall keeps identity and health");
                Check(stock != null && stock.Spawned && stock.stackCount == 17, "protected stockpile item keeps identity and count");
                Check(city!.cells.All(c => map!.terrainGrid.TerrainAt(c) == TerrainDefOf.Concrete), "protected city terrain remains unchanged");
                Check(map!.roofGrid.RoofAt(new IntVec3(52, 0, 52)) == RoofDefOf.RoofConstructed, "protected city roof remains unchanged");
                Check(MapComponent_CustomMapData.GetComp(map).Regions.All.Count(r => r.tags.Contains("Settlement")) == 3, "generated settlements register queryable regions");
                Check(plan!.Parts.All(p => p.Bounds.Cells.Any(c => c.GetFirstBuilding(map) != null)), "normal CQF part pipeline places actual buildings");
                foreach (CQFLayoutPlacement part in plan.Parts)
                {
                    IntVec3 entry = part.Entrances().First().cell;
                    Check(map.reachability.CanReach(city.connections[0].cell, entry, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)), "actual doorway is reachable from city: " + part.Origin);
                }
                Check(!GameTools.isGeneratingMap && CQFLayoutExecution.Current == null && GenStep_CustomMap.generatedCount.Count == 0, "standalone generation restores transient state");
                string generationXml = settings.SaveToXElement("step").ToString();
                Check(generationXml.Contains("CQF_Runtime_House") && generationXml.Contains("CQF_MainCity"), "layout settings export references and candidate rules");
                CustomMapDataDef captured = new CustomMapDataDef { defName = "CQF_Runtime_Export" };
                captured.LoadData(map);
                Check(captured.regions.Count == 4 && captured.SaveToXElement("QuestEditor_Library.CustomMapDataDef").Element("regions")!.Elements().Count() == 4, "whole-map export includes author and generated regions");
                CheckProtection();
                map.fogGrid.ClearAllFog();
                ScreenCapture.CaptureScreenshot(Path.Combine(Root, "Screenshots", "04-Generated.png"));
                GameDataSaveLoader.SaveGame("CQF_MapLayoutRuntime"); Go(8);
            }
            else if (stage == 8)
            {
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("CQF_MapLayoutRuntime")), "native game save created");
                previousGame = Current.Game;
                GameDataSaveLoader.LoadGame("CQF_MapLayoutRuntime"); Go(9);
            }
            else if (stage == 9)
            {
                if (ReferenceEquals(previousGame, Current.Game)) return;
                map = Find.CurrentMap;
                CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(map).Regions;
                CQFMapRegion loaded = registry.Select("CQF_MainCity", Array.Empty<string>()).Single();
                Check(loaded.protect && loaded.cells.Count == 400 && loaded.connections.Count == 1, "native load restores city geometry protection and connection");
                Check(registry.Select("", new[] { "Settlement" }, requireSingle: false).Count == 3, "native load restores generated region identities and tags");
                Check(registry.NextScope("CQF_Runtime_Output") != "CQF_Runtime_Output/1", "native load preserves unique instance sequence");
                Find.WindowStack.Add(new Dialog_CQFMapRegions(map, loaded)); Go(10);
            }
            else if (stage == 10)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Root, "Screenshots", "05-ReloadedRegions.png")); Go(11);
            }
            else if (stage == 11)
            {
                Check(Directory.GetFiles(Path.Combine(Root, "Screenshots"), "*.png").Length == 5, "five fresh UI and map screenshots captured");
                source = map;
                QuestEditor_CustomQuestMap.GenerateMap(new IntVec3(120, 1, 120)); Go(12);
            }
            else if (stage == 12)
            {
                if (Find.CurrentMap == source) return;
                map = Find.CurrentMap;
                foreach (IntVec3 cell in map.AllCells) map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
                List<Thing> legacy = house!.Generate(new IntVec3(5, 0, 5), map, null!);
                Check(legacy != null && legacy.Any(t => t is Building) && MapComponent_CustomMapData.GetComp(map).Regions.All.Count == 0, "unmarked legacy template still generates through original entry");
                CustomMapDataDef parent = new CustomMapDataDef
                {
                    defName = "CQF_Runtime_Parent", size = new IntVec3(120, 1, 120), regions = new List<CQFMapRegion> { city!.Copy() },
                    terrainsRect = new Dictionary<string, List<CellRect>> { [TerrainDefOf.Concrete.defName] = new List<CellRect> { new CellRect(50, 50, 20, 20) } },
                    customSteps = new List<CustomMapStep> { settings! }
                };
                Check(parent.Generate(IntVec3.Zero, map, null!) != null, "existing map generation executes layout custom step");
                CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(map).Regions;
                Check(registry.Select("CQF_MainCity", Array.Empty<string>()).Single().protect && registry.All.Count(r => r.tags.Contains("Settlement")) == 3,
                    "template regions are registered before custom-step reference lookup");
                Check(city.cells.All(c => map.terrainGrid.TerrainAt(c) == TerrainDefOf.Concrete), "parent authored city terrain survives its layout custom step");
                Check(!GameTools.isGeneratingMap && CQFLayoutExecution.Current == null, "parent generation restores lifecycle after nested layout");
                CustomMapDataDef failure = new CustomMapDataDef
                {
                    defName = "CQF_Runtime_Failure",
                    terrainsRect = new Dictionary<string, List<CellRect>> { [TerrainDefOf.Soil.defName] = new List<CellRect> { new CellRect(0, 0, 1, 1) } },
                    customSteps = new List<CustomMapStep> { new CustomMapStep_Layout
                    { layout = settings!.layout, referenceKey = "CQF_Runtime_Missing", parts = settings.parts } }
                };
                bool rejected = false;
                try { failure.Generate(IntVec3.Zero, map, null!); }
                catch (CQFLayoutException) { rejected = true; }
                Check(rejected && !GameTools.isGeneratingMap && GenStep_CustomMap.generatedCount.Count == 0 && GenStep_CustomMap.requests.Count == 0,
                    "failed parent layout propagates its error and clears generation lifecycle");
                File.AppendAllText(Results, "COMPLETE\n"); finished = true; Application.Quit(0);
            }
        }
        catch (Exception exception)
        {
            File.AppendAllText(Results, "FAIL " + exception + "\n");
            Log.Error("CQF layout runtime failure: " + exception); finished = true; Application.Quit(1);
        }
    }

    private void Setup()
    {
        foreach (IntVec3 cell in map!.AllCells) map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
        CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(map).Regions;
        city = new CQFMapRegion { key = "CQF_MainCity", name = "CQF_MainCity", tags = new List<string> { "MainCity" }, protect = true };
        Designator_CQFRegionPaint painter = new Designator_CQFRegionPaint(map, city, false, false);
        painter.DesignateMultiCell(new CellRect(50, 50, 20, 20).Cells);
        Check(registry.All.Single() == city && city.cells.Count == 400, "production area tool paints a registered 20 by 20 city");
        new Designator_CQFRegionPaint(map, city, true, true).DesignateSingleCell(new IntVec3(50, 0, 50));
        Check(city.cells.Count == 399, "production erase tool removes one cell");
        painter.DesignateSingleCell(new IntVec3(50, 0, 50));
        city.connections.Add(new CQFMapRegionConnection { cell = new IntVec3(69, 0, 59), direction = Rot4.East, width = 3 });
        foreach (IntVec3 cell in city.cells) map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        wall = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog), new IntVec3(52, 0, 52), map);
        wallHealth = wall.HitPoints;
        stock = ThingMaker.MakeThing(ThingDefOf.WoodLog); stock.stackCount = 17; GenSpawn.Spawn(stock, new IntVec3(54, 0, 54), map);
        map.roofGrid.SetRoof(new IntVec3(52, 0, 52), RoofDefOf.RoofConstructed);
        house = new CustomMapDataDef
        {
            defName = "CQF_Runtime_House", label = "CQF_Runtime_House", isPart = true, rot = Rot4.North, size = new IntVec3(8, 1, 8),
            terrainsRect = new Dictionary<string, List<CellRect>> { [TerrainDefOf.Concrete.defName] = new List<CellRect> { new CellRect(0, 0, 8, 8) } },
            enterSpots = new List<IntVec3> { new IntVec3(3, 0, 0) }, enterDirection = Rot4.South
        };
        foreach (IntVec3 cell in new CellRect(0, 0, 8, 8).EdgeCells)
            house.thingDatas.Add(new ThingData { def = cell == new IntVec3(3, 0, 0) ? ThingDefOf.Door : ThingDefOf.Wall, stuff = ThingDefOf.WoodLog, position = cell });
        DefDatabase<CustomMapDataDef>.Add(house);
        CQFLayoutPartRule rule = new CQFLayoutPartRule { count = new IntRange(3, 3) };
        rule.pool.datas.Add(new CustomMapDataWithWeight { data = house, weight = 1 });
        settings = new CustomMapStep_Layout
        {
            layout = DefDatabase<CQFMapLayoutDef>.GetNamed("CQF_Layout_Village"), referenceKey = "CQF_MainCity", seed = 517,
            parts = new List<CQFLayoutPartRule> { rule }, connectRoads = true, roadTerrain = TerrainDefOf.Concrete,
            outputKey = "CQF_Runtime_Output", outputTags = new List<string> { "Settlement" }, distance = new IntRange(12, 35)
        };
        initialThingCount = map.listerThings.AllThings.Count;
        Find.CameraDriver.SetRootPosAndSize(map.Center.ToVector3Shifted(), 66f);
    }

    private void CheckProtection()
    {
        using (new CQFLayoutExecution(map!, new CellRect(0, 0, 120, 120).Cells, city!.cells))
        {
            bool rejected = false;
            try { CQFLayoutExecution.Current!.Validate(map!, house!, new IntVec3(51, 0, 51)); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected && wall!.Spawned, "actual-map guard rejects protected placement before clearing");
        }
    }

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        File.AppendAllText(Results, "PASS " + message + "\n");
    }

    private void Go(int value) { stage = value; changedAt = Time.realtimeSinceStartup; }

    private int stage;
    private float changedAt;
    private bool finished;
    private Map? source;
    private Map? map;
    private CQFMapRegion? city;
    private Thing? wall;
    private Thing? stock;
    private int wallHealth;
    private int initialThingCount;
    private CustomMapDataDef? house;
    private CustomMapStep_Layout? settings;
    private CQFLayoutPlan? plan;
    private Dialog_CQFMapRegions? regionWindow;
    private Dialog_CQFLayout? layoutWindow;
    private Game? previousGame;
}
