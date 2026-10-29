using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using QuestEditor_Library;
using Verse;

internal static class Program
{
    private static int Main()
    {
        try
        {
            FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
            prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
            Regions(); Selection(); Placement(); Roads(); Algorithms();
            Console.WriteLine("COMPLETE " + passed); return 0;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
    }

    private static void Regions()
    {
        CQFMapRegion region = MainCity();
        CQFMapRegionCollection registry = new CQFMapRegionCollection(); registry.Add(region);
        Check(registry.Select("CQF_MainCity", Array.Empty<string>()).Single() == region, "exact region reference");
        Check(registry.Select("", new[] { "MainCity" }).Single() == region, "region tag reference");
        Reject(() => registry.Select("missing", Array.Empty<string>()), "missing reference rejected");
        Reject(() => registry.Add(MainCity()), "duplicate key rejected");
        CQFMapRegion second = MainCity(); registry.Add(second, "CQF_Instance_2");
        Reject(() => registry.Select("", new[] { "MainCity" }), "ambiguous tag rejected");
        Check(registry.Select("", new[] { "MainCity" }, requireSingle: false).Count == 2, "tag can select every region explicitly");
        Check(registry.Select("CQF_MainCity", Array.Empty<string>(), "CQF_Instance_2").Single() == second, "local instance lookup");
        Reject(() => registry.Capture(new CellRect(50, 50, 2, 2).Cells, IntVec3.Zero), "partial region export rejected");
        List<CQFMapRegion> capture = registry.Capture(new CellRect(0, 0, 120, 120).Cells, new IntVec3(10, 0, 20));
        Check(capture.Count == 2 && capture.Select(r => r.key).Distinct().Count() == 2, "instance export keeps unique keys");
        Check(capture[0].cells.Contains(new IntVec3(40, 0, 30)) && capture[0].connections[0].cell == new IntVec3(59, 0, 39), "region export translates cells and connections together");
        Check(Math.Abs(region.DistanceTo(new IntVec3(75, 0, 59)) - 6) < 0.001, "distance measured from actual boundary");
        CustomMapDataDef data = Part("CQF_Check_Rotation"); data.regions.Add(new CQFMapRegion { key = "CQF_Entrance", cells = new List<IntVec3> { new IntVec3(2, 0, 3) }, connections = new List<CQFMapRegionConnection> { new CQFMapRegionConnection { cell = new IntVec3(2, 0, 3), direction = Rot4.North } } });
        CustomMapDataDef rotated = data.GetRotated(Rot4.East);
        Check(rotated.regions[0].cells[0] == new IntVec3(3, 0, -2) && rotated.regions[0].connections[0].direction == Rot4.East, "rotation transforms region cells and direction");
        Check(data.regions[0].cells[0] == new IntVec3(2, 0, 3) && data.regions[0].connections[0].direction == Rot4.North, "rotation preserves source template");
        CustomMapDataDef shifted = data.GetNewDataUseNewOrigih(new IntVec3(1, 0, 1), Rot4.North);
        Check(shifted.regions[0].cells[0] == new IntVec3(1, 0, 2), "core-origin transform includes regions");
        CustomMapDataDef wider = Part("CQF_Check_WideRegion");
        wider.regions.Add(new CQFMapRegion { key = "CQF_Outlying", cells = new List<IntVec3> { new IntVec3(12, 0, 12) } });
        Check(new CQFLayoutPlacement(wider, Rot4.North).LocalBounds.Contains(new IntVec3(12, 0, 12)), "region metadata participates in complete prefab footprint");
        XmlDocument document = new XmlDocument(); document.LoadXml(region.SaveToXElement("region").ToString());
        CQFMapRegion read = DirectXmlToObject.ObjectFromXml<CQFMapRegion>(document.DocumentElement!, false);
        Check(read.cells.SequenceEqual(region.cells) && read.protect && read.connections[0].cell == region.connections[0].cell && read.tags.Contains("MainCity"), "region native XML roundtrip");
        Check(registry.All[0].cells.Count == 400, "capturing and serializing do not mutate runtime region");
    }

    private static void Selection()
    {
        CustomMapDataDef a = Part("CQF_Check_A"); a.tags.AddRange(new[] { "City", "House" });
        CustomMapDataDef b = Part("CQF_Check_B"); b.tags.AddRange(new[] { "City", "House", "Ruined" });
        CustomMapDataDef c = Part("CQF_Check_C"); c.tags.Add("City");
        CQFLayoutPartRule rule = new CQFLayoutPartRule { requiredTags = new List<string> { "City", "House" }, excludedTags = new List<string> { "Ruined" } };
        rule.pool.tags.Add(new CustomMapDataTagWithWeight { tag = "City", weight = 1 });
        Check(rule.Candidates(new[] { a, b, c }).Single().Data == a, "all-tags and excluded-tags filter candidates");
        rule.excludedTags.Clear(); rule.anyTags.Add("Ruined");
        Check(rule.Candidates(new[] { a, b, c }).Single().Data == b, "any-tag constraint");
        rule.pool.datas.Add(new CustomMapDataWithWeight { data = b, weight = 5 });
        Check(rule.Candidates(new[] { a, b, c }).Single().Weight == 5, "explicit weight takes precedence without duplicate counting");
        rule.pool.datas.Add(new CustomMapDataWithWeight { data = b, weight = 1 });
        Reject(() => rule.Candidates(new[] { a, b }).ToList(), "duplicate explicit candidate rejected");
    }

    private static void Placement()
    {
        CustomMapDataDef part = Part("CQF_Check_Settlement");
        CustomMapStep_Layout settings = Settings(typeof(CQFLayoutWorker_Village), part, 4);
        CQFMapRegion main = MainCity();
        CQFLayoutPlanningContext context = Context(settings, part, main);
        CQFLayoutPlan plan = new CQFLayoutPlanner().Plan(context, new[] { main });
        Check(plan.Parts.Count == 4, "four satellite settlements generated");
        Check(plan.Parts.All(p => !p.Bounds.Cells.Any(main.cells.Contains)), "main city footprint excluded");
        Check(plan.Parts.All(p => p.Bounds.Cells.All(plan.Allowed.Contains)), "whole footprint stays inside generation area");
        Check(plan.Parts.SelectMany((a, i) => plan.Parts.Skip(i + 1).Select(b => !a.Bounds.ExpandedBy(settings.spacing).Overlaps(b.Bounds))).All(x => x), "settlements preserve minimum spacing");
        CQFLayoutPlan repeat = new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main });
        Check(plan.Parts.Select(p => p.Origin).SequenceEqual(repeat.Parts.Select(p => p.Origin)), "seed produces identical plan");
        Check(plan.Matches(repeat), "unchanged preview matches executable plan");
        repeat.Parts[0].MoveCenter(repeat.Parts[0].Center + IntVec3.North);
        Check(!plan.Matches(repeat), "changed placement invalidates preview");
        settings.directions.Add(Rot4.East);
        CQFLayoutPlan eastern = new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main });
        Check(eastern.Parts.All(p => p.Center.x > main.Bounds.CenterCell.x && Math.Abs(p.Center.x - main.Bounds.CenterCell.x) > Math.Abs(p.Center.z - main.Bounds.CenterCell.z)), "allowed direction selects eastern sector");
        settings.directions.Clear();
        CQFMapRegion irregular = new CQFMapRegion { key = "CQF_Irregular", cells = new CellRect(10, 10, 2, 20).Cells.Concat(new CellRect(10, 10, 20, 2).Cells).Distinct().ToList() };
        Check(irregular.Bounds.Contains(new IntVec3(25, 0, 25)) && irregular.DistanceTo(new IntVec3(25, 0, 25)) == 14, "irregular region distance excludes empty bounding-box interior");
        Check(main.cells.Count == 400 && GenStep_CustomMap.generatedCount.Count == 0, "planning does not mutate regions or generation counts");
        CQFMapRegion blocked = new CQFMapRegion { key = "CQF_Extra", id = "CQF_Extra", protect = true, cells = new CellRect(0, 0, 45, 120).Cells.ToList() };
        CQFLayoutPlan protectedPlan = new CQFLayoutPlanner().Plan(new CQFLayoutPlanningContext(new IntVec3(120, 1, 120), settings, new[] { part }, new[] { main, blocked }), new[] { main });
        Check(protectedPlan.Parts.All(p => !p.Bounds.Cells.Any(blocked.cells.Contains)), "additional protected region respected");
        part.generationLimit = 2;
        Reject(() => new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main }), "required count cannot exceed definition limit");
        settings.parts[0].optional = true;
        CQFLayoutPlan optional = new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main });
        Check(optional.Parts.Count == 2 && optional.Notices.Count == 1, "optional shortage reports explicit notice");
        part.generationLimit = 0; settings.parts[0].optional = false;
        settings.relation = "Inside"; main.protect = false;
        settings.parts[0].count = new IntRange(1, 1);
        CQFLayoutPlan inside = new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main });
        Check(inside.Parts.Single().Bounds.Cells.All(main.cells.Contains), "inside relation uses actual region mask");
        settings.allowedRegion = "CQF_MainCity"; settings.relation = "Anywhere";
        Check(new CQFLayoutPlanner().Plan(Context(settings, part, main), new CQFMapRegion?[] { null }).Parts.Single().Bounds.Cells.All(main.cells.Contains), "allowed region restricts generation without a reference");
        part.preCustomSteps.Add(new CustomMapStep_MapPart());
        Reject(() => CQFLayoutExecution.ValidateTemplate(part), "unsupported nested steps rejected before execution");
        Map map = new Map();
        using (new CQFLayoutExecution(map, new CellRect(0, 0, 10, 10).Cells, new[] { new IntVec3(2, 0, 2) }))
        {
            Check(!CQFLayoutExecution.Current!.Allows(map, new[] { new IntVec3(2, 0, 2) }), "execution guard blocks protected cell");
            using (new CQFLayoutExecution(map, new CellRect(0, 0, 20, 20).Cells, Array.Empty<IntVec3>()))
                Check(!CQFLayoutExecution.Current!.Allows(map, new[] { new IntVec3(15, 0, 15) }), "child scope cannot expand parent boundary");
            Check(CQFLayoutExecution.Current != null, "nested guard restores outer scope");
        }
        Check(CQFLayoutExecution.Current == null, "guard releases after completion");
        CustomMapDataDef targets = Part("CQF_Check_Targets");
        targets.thingDatas.Add(new ThingData { targetKeys = new List<string> { "CQF_TestDoor" } });
        CQFLayoutExecution.ValidateTargets(new[] { targets }, Array.Empty<string>());
        Reject(() => CQFLayoutExecution.ValidateTargets(new[] { targets, targets }, Array.Empty<string>()), "repeated parts cannot silently reuse a target key");
        Reject(() => CQFLayoutExecution.ValidateTargets(new[] { targets }, new[] { "CQF_TestDoor" }), "existing map target collision rejected before spawning");
    }

    private static void Roads()
    {
        CustomMapDataDef part = Part("CQF_Check_RoadPart");
        CustomMapStep_Layout settings = Settings(typeof(CQFLayoutWorker_Village), part, 3);
        settings.connectRoads = true; settings.roadTerrain = new TerrainDef { defName = "CQF_Check_Road" };
        CQFMapRegion main = MainCity();
        CQFLayoutPlan plan = new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main });
        Check(plan.Roads.Count > 0 && !plan.Roads.Any(main.cells.Contains), "roads connect outside protected city");
        Check(plan.Roads.All(plan.Allowed.Contains), "roads stay within allowed region");
        part.thingDatas.Add(new ThingData { def = new ThingDef { size = new IntVec2(1, 1), passability = Traversability.Impassable }, allPositions = new List<IntVec3> { new IntVec3(3, 0, 0) } });
        Reject(() => new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main }), "entrance blocked by batched template walls rejected before spawning");
        part.thingDatas.Clear();
        main.connections.Clear();
        Reject(() => new CQFLayoutPlanner().Plan(Context(settings, part, main), new[] { main }), "missing city connection prevents generation");
        main = MainCity();
        Reject(() => new CQFLayoutPlanner().Plan(new CQFLayoutPlanningContext(new IntVec3(120, 1, 120), settings, new[] { part }, new[] { main }, canRoad: _ => false), new[] { main }), "blocked roads fail during planning");
    }

    private static void Algorithms()
    {
        foreach (Type worker in new[] { typeof(CQFLayoutWorker_City), typeof(CQFLayoutWorker_Scatter), typeof(CQFLayoutWorker_Clusters), typeof(CQFLayoutWorker_Ruins) })
        {
            CustomMapDataDef part = Part("CQF_Check_" + worker.Name);
            CustomMapStep_Layout settings = Settings(worker, part, 4);
            settings.relation = "Anywhere";
            CQFLayoutPlan plan = new CQFLayoutPlanner().Plan(Context(settings, part), new CQFMapRegion?[] { null });
            Check(plan.Parts.Count == 4, worker.Name + " places required parts");
        }
        CustomMapDataDef interior = Part("CQF_Check_Interior");
        CustomMapStep_Layout tooManyClusters = Settings(typeof(CQFLayoutWorker_Clusters), interior, 1); tooManyClusters.clusterCount = 64;
        Reject(() => new CQFLayoutPlanner().Plan(Context(tooManyClusters, interior), new CQFMapRegion?[] { null }), "insufficient cluster center space is reported instead of reducing count");
        CustomMapDataDef perimeter = Part("CQF_Check_Perimeter"); perimeter.size = new IntVec3(24, 1, 24); perimeter.terrainsRect["Concrete"] = new List<CellRect> { new CellRect(0, 0, 24, 24) };
        CustomMapStep_Layout fortress = Settings(typeof(CQFLayoutWorker_Fortified), interior, 4); fortress.relation = "Anywhere";
        foreach (string role in new[] { "Corner", "Gate", "Wall" })
        {
            CQFLayoutPartRule rule = new CQFLayoutPartRule { role = role };
            rule.pool.datas.Add(new CustomMapDataWithWeight { data = perimeter, weight = 1 }); fortress.parts.Insert(0, rule);
        }
        CQFLayoutPlan fort = new CQFLayoutPlanner().Plan(new CQFLayoutPlanningContext(new IntVec3(120, 1, 120), fortress, new[] { interior, perimeter }, Array.Empty<CQFMapRegion>()), new CQFMapRegion?[] { null });
        Check(fort.Parts.Count(p => p.Role == "Content") == 4 && fort.Parts.Count(p => p.Role == "Gate") == 1 && fort.Parts.Count(p => p.Role == "Corner") == 4, "fortress fills perimeter and interior roles");
        Check(fort.Parts.Where(p => p.Role != "Content").SelectMany(p => p.Bounds.Cells).Distinct().Count() == 12 * 24 * 24, "fortress perimeter modules tile without overlap or missing slots");
    }

    private static CQFLayoutPlanningContext Context(CustomMapStep_Layout settings, CustomMapDataDef part, params CQFMapRegion[] regions) => new CQFLayoutPlanningContext(new IntVec3(120, 1, 120), settings, new[] { part }, regions);

    private static CQFMapRegion MainCity() => new CQFMapRegion
    {
        key = "CQF_MainCity", id = "CQF_MainCity", tags = new List<string> { "MainCity" }, protect = true,
        cells = new CellRect(50, 50, 20, 20).Cells.ToList(),
        connections = new List<CQFMapRegionConnection> { new CQFMapRegionConnection { cell = new IntVec3(69, 0, 59), direction = Rot4.East } }
    };

    private static CustomMapDataDef Part(string key) => new CustomMapDataDef
    {
        defName = key, label = key, isPart = true, size = new IntVec3(8, 1, 8), rot = Rot4.North,
        terrainsRect = new Dictionary<string, List<CellRect>> { ["Concrete"] = new List<CellRect> { new CellRect(0, 0, 8, 8) } },
        enterSpots = new List<IntVec3> { new IntVec3(3, 0, 0) }, enterDirection = Rot4.South
    };

    private static CustomMapStep_Layout Settings(Type worker, CustomMapDataDef part, int count)
    {
        CQFLayoutPartRule rule = new CQFLayoutPartRule { count = new IntRange(count, count) };
        rule.pool.datas.Add(new CustomMapDataWithWeight { data = part, weight = 1 });
        return new CustomMapStep_Layout { layout = new CQFMapLayoutDef { defName = "CQF_Check_Layout", workerClass = worker }, parts = new List<CQFLayoutPartRule> { rule }, distance = new IntRange(12, 35), seed = 517 };
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL " + name);
        passed++; Console.WriteLine("PASS " + name);
    }

    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidOperationException) { Check(true, name); return; }
        throw new InvalidOperationException("FAIL " + name);
    }

    private static int passed;
}
