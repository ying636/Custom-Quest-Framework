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
