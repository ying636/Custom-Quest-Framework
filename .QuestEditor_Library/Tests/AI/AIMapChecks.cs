using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIMapChecks
{
    public static void Run(CQFAIModel model)
    {
        ThingDef building = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        building.defName = "CQF_Check_LargeBuilding"; building.stackLimit = 1; building.size = new IntVec2(2, 3); building.thingClass = typeof(Thing); building.category = ThingCategory.Building;
        ThingDef door = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        door.defName = "CQF_Check_CustomDoor"; door.stackLimit = 1; door.size = new IntVec2(2, 3); door.thingClass = typeof(CustomDoor); door.category = ThingCategory.Building;
        RoofDef roof = (RoofDef)RuntimeHelpers.GetUninitializedObject(typeof(RoofDef)); roof.defName = "CQF_Check_Roof";
        DefDatabase<ThingDef>.Add(building); DefDatabase<ThingDef>.Add(door); DefDatabase<RoofDef>.Add(roof);
        CQFAIChanges changes = new CQFAIChanges(model);
        CustomMapDataDef source = AICheckFixtures.Map();
        source.thingDatas.Add(new ThingData { def = building, position = IntVec3.Zero, allRect = new List<CellRect> { new CellRect(8, 8, 3, 1) } });
        CQFAIMapPlan.Validate(source);
        Check(CQFAIMapPlan.Positions(source.thingDatas[0]).Count() == 3, "compressed placements ignore unused zero anchor");
        CustomMapDataDef draft = (CustomMapDataDef)changes.Build(source, XElement.Parse("<changes><erase x='8' z='8'/></changes>"), "", false);
        Check(CQFAIMapPlan.Positions(draft.thingDatas.Single()).Count() == 2 && CQFAIMapPlan.Positions(source.thingDatas.Single()).Count() == 3, "erase edits compressed placements and preserves source");
        CustomMapDataDef custom = (CustomMapDataDef)changes.Build(AICheckFixtures.Map(), XElement.Parse("<changes><place def='CQF_Check_CustomDoor' Class='QuestEditor_Library.CustomThingData_CustomDoor' x='8' z='8'/></changes>"), "", false);
        Check(custom.customThings.Single() is CustomThingData_CustomDoor, "interactive object uses matching data class");
        CellRect occupied = GenAdj.OccupiedRect(custom.customThings.Single().position, custom.customThings.Single().rotation, door.size);
        IntVec3 edge = occupied.Cells.First(cell => cell != custom.customThings.Single().position);
        XElement eraseEdge = new XElement("changes", new XElement("erase", new XAttribute("x", edge.x), new XAttribute("z", edge.z)));
        CustomMapDataDef erasedCustom = (CustomMapDataDef)changes.Build(custom, eraseEdge, "", false);
        Check(erasedCustom.customThings.Count == 0 && custom.customThings.Count == 1, "erase intersects full interactive object footprint and preserves source");
        Reject(() => changes.Build(AICheckFixtures.Map(), XElement.Parse("<changes><place def='CQF_Check_CustomDoor' x='8' z='8'/></changes>"), "", false), "interactive object cannot use plain ThingData");
        Reject(() => changes.Build(AICheckFixtures.Map(), XElement.Parse("<changes><place def='CQF_Check_CustomDoor' Class='QuestEditor_Library.CustomThingData_LootBox' x='8' z='8'/></changes>"), "", false), "mismatched interactive data class rejected");
        Reject(() => changes.Build(AICheckFixtures.Map(), XElement.Parse("<changes><place def='CQF_Check_CustomDoor' Class='QuestEditor_Library.CustomThingData_CustomDoor' x='0' z='0'/></changes>"), "", false), "custom object footprint cannot cross map edge");
        source.roofRects.Add(roof, new List<CellRect> { new CellRect(62, 62, 3, 3) });
        Reject(() => CQFAIMapPlan.Validate(source), "roof rectangle bounds validated");
        source.roofRects.Clear(); source.routes.Add("CQF_Check_Route", new List<IntVec3> { new IntVec3(64, 0, 1) });
        Reject(() => CQFAIMapPlan.Validate(source), "route coordinates validated");
    }
    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); }
        catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
