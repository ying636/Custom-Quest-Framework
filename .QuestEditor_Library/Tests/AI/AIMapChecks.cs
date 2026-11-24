using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class AIMapChecks
{
    public static void Run(CQFAIModel model)
    {
        CheckOverview();
        CheckRegionEncoding();
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
        CustomMapDataDef turned = (CustomMapDataDef)changes.Build(AICheckFixtures.Map(), XElement.Parse("<changes><place def='CQF_Check_LargeBuilding' x='8' z='8' rotation='1'/></changes>"), "", false);
        Check(turned.thingDatas.Single().rotation == Rot4.East && GenAdj.OccupiedRect(turned.thingDatas.Single().position, turned.thingDatas.Single().rotation, building.size).Width == 3,
            "map draft placement preserves explicit rotation and rotates its native footprint");
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
    private static void CheckOverview()
    {
        CellRect bounds = new CellRect(20, 30, 1001, 701);
        HashSet<IntVec3> visited = new();
        CQFAIMapOverview map = new(cell =>
        {
            CheckSample(cell);
            return (cell.x < 520 ? "CQF_Check_Soil" : "CQF_Check_Stone", cell.z > 380, cell.x < 520);
        });
        void CheckSample(IntVec3 cell)
        {
            if (!bounds.Contains(cell) || !visited.Add(cell)) throw new InvalidOperationException("Sample outside bounds or repeated");
        }

        XElement result = map.Describe(bounds, 8);
        XElement[] tiles = result.Element("tiles")!.Elements("tile").ToArray();
        Check((bool?)result.Attribute("sampled") == true && tiles.Length == 64 && visited.Count == 576 && (int?)result.Attribute("samples") == visited.Count,
            "large map overview uses bounded unique samples and identifies its approximate output");
        Check(tiles.Sum(tile => (int)tile.Attribute("w")! * (int)tile.Attribute("h")!) == bounds.Area &&
            (int)tiles.First().Attribute("x")! == 20 && (int)tiles.First().Attribute("z")! == 30 &&
            (int)tiles.Last().Attribute("x")! + (int)tiles.Last().Attribute("w")! - 1 == bounds.maxX &&
            (int)tiles.Last().Attribute("z")! + (int)tiles.Last().Attribute("h")! - 1 == bounds.maxZ,
            "overview partitions an offset region including uneven edges");
        Check(result.Element("terrains")!.Elements().Count() == 2 && (int?)tiles.First().Attribute("blocked") == 0 &&
            (int?)tiles.Last().Attribute("blocked") == 9 && (int?)tiles.Last().Attribute("roofed") == 9 &&
            (int?)tiles.Last().Attribute("matching") == 9,
            "overview reports dominant terrain, non-walkable cells and roofs from real samples");
        visited.Clear();
        XElement compact = map.Describe(bounds, 8, true);
        XElement grid = compact.Element("grid")!;
        int[] xEdges = grid.Attribute("xEdges")!.Value.Split(',').Select(int.Parse).ToArray();
        int[] zEdges = grid.Attribute("zEdges")!.Value.Split(',').Select(int.Parse).ToArray();
        XElement[] rows = grid.Elements("row").ToArray();
        Check(xEdges.Length == 9 && zEdges.Length == 9 && rows.Length == 8 &&
            XNode.DeepEquals(result.Element("terrains"), compact.Element("terrains")),
            "compact map overview keeps the full grid and the exact terrain palette");
        for (int row = 0; row < rows.Length; row++)
        {
            string[] tuples = rows[row].Value.Split(' ');
            for (int column = 0; column < tuples.Length; column++)
            {
                var tile = tiles[row * 8 + column];
                int[] actual = tuples[column].Split(',').Select(int.Parse).ToArray();
                int[] expected = new[] { "terrain", "matching", "roofed", "blocked", "n" }
                    .Select(name => (int)tile.Attribute(name)!).ToArray();
                if (!actual.SequenceEqual(expected) || (int?)tile.Attribute("x") != xEdges[column] ||
                    (int?)tile.Attribute("z") != zEdges[row] || (int?)tile.Attribute("w") != xEdges[column + 1] - xEdges[column] ||
                    (int?)tile.Attribute("h") != zEdges[row + 1] - zEdges[row])
                    throw new InvalidOperationException("The compact overview changed tile bounds or sample data.");
            }
        }
        Check(visited.Count == 576 && compact.ToString(SaveOptions.DisableFormatting).Length * 2 < result.ToString(SaveOptions.DisableFormatting).Length,
            "compact overview re-encodes identical sample data in less than half the characters");
        int count = 0;
        CQFAIMapOverview small = new(_ => { count++; return ("CQF_Check_Soil", false, true); });
        var narrow = small.Describe(new CellRect(2, 3, 1, 2), 16);
        Check(count == 2 && (int?)narrow.Attribute("samples") == 2 && (int?)narrow.Attribute("columns") == 1,
            "narrow overviews avoid empty partitions and duplicate samples");
        Reject(() => small.Describe(new CellRect(0, 0, 1, 1), 17), "map overview rejects excess subdivisions");
    }

    private static void CheckRegionEncoding()
    {
        var first = (ThingWithComps)RuntimeHelpers.GetUninitializedObject(typeof(ThingWithComps));
        var second = (ThingWithComps)RuntimeHelpers.GetUninitializedObject(typeof(ThingWithComps));
        first.def = second.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        first.def.defName = "CQF_Check_EncodedThing";
        first.thingIDNumber = 1;
        second.thingIDNumber = 2;
        Thing[] crowded = new[] { first, second }.Concat(Enumerable.Repeat<Thing>(first, 21)).ToArray();
        var cells = new[] { new IntVec3(23, 0, 18), new IntVec3(24, 0, 18), new IntVec3(20, 0, 19) };
        XElement top = CQFAILiveMap.EncodeRegionPage(8, 32, 6, cells,
            cell => cell == cells[0] ? ("CQF_Check_Soil", "CQF_Check_Roof", crowded)
                : cell == cells[1] ? ("CQF_Check_Soil", null, new Thing[] { second })
                : ("CQF_Check_Stone", null, Array.Empty<Thing>()),
            thing => new XElement("thing", new XAttribute("id", ReferenceEquals(thing, first) ? "CQF_Check_First" : "CQF_Check_Second"),
                new XElement("footprint", new XAttribute("width", 2))));
        string[] tuples = top.Element("cells")!.Value.Split(' ');
        string[] crowdedParts = tuples[0].Split(',');
        Check((int?)top.Attribute("map") == 8 && (int?)top.Attribute("total") == 32 && (int?)top.Attribute("offset") == 6 &&
            tuples[1] == "24,18,0,-,1,1" && tuples[2] == "20,19,1,-,0,-" &&
            crowdedParts.Take(5).SequenceEqual(new[] { "23", "18", "0", "0", "23" }) &&
            crowdedParts[5].Split(':').Length == 20 && crowdedParts[5].StartsWith("0:1:", StringComparison.Ordinal),
            "compact map pages retain exact absolute cells, roof indices, true counts and capped references");
        Check(top.Element("terrains")!.Elements().Select(e => e.Value).SequenceEqual(new[] { "CQF_Check_Soil", "CQF_Check_Stone" }) &&
            top.Element("roofs")!.Elements().Single().Value == "CQF_Check_Roof" &&
            top.Element("things")!.Elements().Select(e => (string)e.Attribute("id")!).SequenceEqual(new[] { "CQF_Check_First", "CQF_Check_Second" }) &&
            top.Descendants("footprint").Count() == 2,
            "compact map pages preserve object identities and full footprint metadata once per object");
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
