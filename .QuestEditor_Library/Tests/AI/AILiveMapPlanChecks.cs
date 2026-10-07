using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AILiveMapPlanChecks
{
    public static void Run(CQFAIModel model)
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.info = new MapInfo { Size = new IntVec3(64, 1, 64) };
        CQFAILiveMap backend = new(map);
        Reject(() => Plan("<place def='CQF_Check_LargeBuilding' x='63' z='63'/>"), "actual live planner rejects a building footprint crossing the map boundary");
        Reject(() => Plan("<place def='CQF_Check_LargeBuilding' x='1' z='1' rotation='4'/>"), "actual live planner rejects invalid rotation before creating a Thing");
        Reject(() => Plan("<place def='CQF_Check_Missing' x='1' z='1'/>"), "actual live planner requires a loaded ThingDef");
        Reject(() => Plan("<terrain def='CQF_Check_Floor' x='63' z='63' width='2' height='2'/>"), "actual live planner rejects terrain outside map bounds before accessing grids");
        Reject(() => Plan("<roof def='CQF_Check_Missing' x='1' z='1'/>"), "actual live planner requires a loaded RoofDef");
        Reject(() => Plan("<mapResize x='10' z='10'/>"), "actual live planner rejects resizing a running map");
        Reject(() => Plan("<place def='CQF_Check_LargeBuilding' x='one' z='1'/>"), "actual live planner reports invalid coordinate text");
        Reject(() => Plan("<erase x='1' z='1' category='Pawn'/>"), "actual live planner rejects erasing pawns");
        Reject(() => Plan("<roof def='CQF_Check_Roof' x='1' z='1' arbitrary='true'/>"), "actual live planner rejects invented operation attributes");
        TerrainDef foundation = (TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef)); foundation.defName = "CQF_Check_LiveFoundation"; foundation.isFoundation = true;
        DefDatabase<TerrainDef>.Add(foundation);
        Reject(() => Plan("<terrain def='CQF_Check_LiveFoundation' x='1' z='1'/>"), "actual live planner explicitly rejects unsupported foundation layers");
        var roof = Plan("<roof def='CQF_Check_Roof' x='1' z='1' width='2' height='2'/>");
        Check(roof.Count == 4 && roof.All(edit => edit.Before == null), "actual roof planning creates reversible cell operations without mutating native grids");
        Check(Plan("<roof def='' x='1' z='1'/>").Count == 1, "actual roof planner accepts explicit roof removal");
        Reject(() => Plan(string.Concat(Enumerable.Repeat("<roof def='CQF_Check_Roof' x='0' z='0' width='64' height='64'/>", 3))), "actual planner stops oversized cell batches before allocating the entire operation list");
        Reject(() => backend.ReadRegion(new CellRect(63, 63, 2, 2), 0, 10), "actual live backend rejects invalid region bounds");
        Reject(() => backend.ReadRegion(new CellRect(1, 1, 2, 2), 0, 401), "actual live backend enforces read page limits");
        Check(map.terrainGrid == null && map.roofGrid == null, "native planning checks run without initializing game maps or Unity UI");
        map.cellIndices = new CellIndices(map);
        map.thingGrid = new ThingGrid(map);
        ThingDef collisionDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        collisionDef.defName = "CQF_Check_Collision"; collisionDef.category = ThingCategory.Item; collisionDef.size = new IntVec2(2, 3); collisionDef.stackLimit = 1;
        DefDatabase<ThingDef>.Add(collisionDef);
        Thing planned = new() { def = collisionDef, Position = new IntVec3(10, 0, 10), Rotation = Rot4.East };
        CQFAILiveMapPlan collision = new(backend, model, "", false);
        var plannedCells = (Dictionary<IntVec3, List<Thing>>)typeof(CQFAILiveMapPlan).GetField("plannedCells", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(collision)!;
        foreach (IntVec3 cell in GenAdj.OccupiedRect(planned.Position, planned.Rotation, planned.def.size)) plannedCells[cell] = new List<Thing> { planned };
        try { collision.Build(XElement.Parse("<changes><place def='CQF_Check_Collision' x='10' z='10' rotation='0'/></changes>")); }
        catch (InvalidDataException error) when (error.Message.StartsWith("CQF_AI_LiveMapOverlap:"))
        {
            Check(error.Message.Contains("batch not applied") && error.Message.Contains("requested CQF_Check_Collision anchor=(10,10) rotation=0")
                && error.Message.Contains("planned CQF_Check_Collision anchor=(10,10) rotation=1") && error.Message.Contains("width=3,height=2"),
                "native planner returns both requested and conflicting planned footprints and rotations");
            Check(map.thingGrid.ThingsListAt(new IntVec3(10, 0, 10)).Count == 0, "conflicting native placement planning leaves the live map unchanged");
            map.terrainGrid = new TerrainGrid(map); map.roofGrid = new RoofGrid(map);
            TerrainDef floor = (TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef)); floor.defName = "CQF_Check_RegionFloor";
            var topGrid = (TerrainDef[])typeof(TerrainGrid).GetField("topGrid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.GetValue(map.terrainGrid)!;
            Array.Fill(topGrid, floor);
            map.thingGrid.Register(planned);
            XElement read = backend.ReadRegion(new CellRect(8, 8, 5, 5), 0, 25);
            Check(read.Element("things")!.Elements("thing").Count() == 1 && read.Descendants("thingRef").Count() == 6,
                "native map reads describe multi-cell objects once and preserve all occupied cell references");
            XElement described = read.Element("things")!.Element("thing")!;
            Check(described.Attribute("rotation")!.Value == "1" && described.Element("footprint")!.Attribute("width")!.Value == "3"
                && described.Attribute("maxHitPoints") == null && read.Elements("cell").All(cell => cell.Attribute("terrain")!.Value == floor.defName),
                "compact native map reads preserve terrain, anchors and actual rotated footprints");
            return;
        }
        throw new InvalidOperationException("native planned collision was not reported");
        IReadOnlyList<CQFAILiveMapEdit> Plan(string operations) => new CQFAILiveMapPlan(backend, model, "", false).Build(XElement.Parse("<changes>" + operations + "</changes>"));
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
