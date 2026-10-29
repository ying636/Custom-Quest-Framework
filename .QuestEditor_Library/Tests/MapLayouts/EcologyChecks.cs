using System.Xml;
using QuestEditor_Library;
using RimWorld;
using Verse;

internal static class EcologyChecks
{
    public static void Run(Action<bool, string> check, Action<Action, string> reject)
    {
        TerrainDef soil = new TerrainDef { defName = "CQF_Check_Soil", fertility = 1f };
        TerrainDef water = new TerrainDef { defName = "CQF_Check_Water", tags = new List<string> { "Water" }, passability = Traversability.Impassable };
        ThingDef rock = new ThingDef { defName = "CQF_Check_Rock", size = new IntVec2(1, 1), building = new BuildingProperties { isNaturalRock = true }, passability = Traversability.Impassable };
        ThingDef grass = new ThingDef { defName = "CQF_Check_Grass", size = new IntVec2(1, 1), plant = new PlantProperties { fertilityMin = 0f } };
        BiomeDef biome = new BiomeDef { defName = "CQF_Check_Biome", waterShallowTerrain = water, plantDensity = 0.25f,
            terrainsByFertility = new List<TerrainThreshold> { new TerrainThreshold { terrain = soil } }, wildPlants = new List<BiomePlantRecord> { new BiomePlantRecord { plant = grass, commonality = 1f } } };
        Dictionary<IntVec3, TerrainDef> original = new CellRect(0, 0, 40, 40).Cells.ToDictionary(c => c, _ => soil);
        HashSet<IntVec3> protectedCells = new HashSet<IntVec3>(new CellRect(12, 12, 5, 5).Cells);
        CQFEcologySettings ecology = new CQFEcologySettings { rock = rock, waterCoverage = 0.1f, rockCoverage = 0.15f };
        CQFEcologyGenerator generator = new CQFEcologyGenerator();
        CQFEcologyPlan first = generator.Create(ecology, biome, 517, original, original.Keys.Where(c => !protectedCells.Contains(c)));
        CQFEcologyPlan repeat = generator.Create(ecology, biome, 517, original, original.Keys.Where(c => !protectedCells.Contains(c)));
        check(first.Terrains.Count == original.Count && first.Mutable.Count == original.Count - protectedCells.Count, "ecology covers whole map except protected cells");
        check(first.Matches(repeat), "ecology seed reproduces terrain plants and rocks");
        check(!first.Mutable.Any(protectedCells.Contains) && !first.Objects.Keys.Any(protectedCells.Contains), "ecology cannot write protected cells");
        check(original.Values.All(t => t == soil), "ecology planning preserves source terrain");
        check(first.Water.Count == (int)(first.Mutable.Count * 0.1f) && first.Rocks.Count == (int)(first.Mutable.Count * 0.15f), "water and rock coverage apply to available cells");
        check(first.Objects.Values.Contains(grass) && first.Objects.Values.Contains(rock), "biome plant pool and configured rocks are used");
        check(!first.Rocks.Any(first.Water.Contains), "rocks do not occupy water");
        check(!first.Matches(generator.Create(ecology, biome, 518, original, first.Mutable)), "new seed changes ecology plan");
        ecology.regenerate = false;
        CQFEcologyPlan existing = generator.Create(ecology, biome, 517, first.Terrains, first.Mutable, first.Rocks, first.Trees);
        check(existing.Mutable.Count == 0 && existing.Objects.Count == 0 && existing.Biome == null && existing.Terrains.All(p => first.Terrains[p.Key] == p.Value), "reuse mode preserves ecology without terrain or biome writes");
        ecology.regenerate = true; ecology.terrains.Add(new CQFEcologyTerrain { terrain = soil, weight = 1f }); ecology.plants.Add(new CQFEcologyPlant { plant = grass, weight = 1f });
        ecology.waterCoverage = 0f; ecology.rockCoverage = 0f;
        check(generator.Create(ecology, new BiomeDef { defName = "CQF_Check_Custom" }, 517, original, original.Keys).Terrains.Values.All(t => t == soil), "custom terrain and plant lists work without vanilla biome thresholds");
        ecology.terrains.Clear();
        reject(() => generator.Create(ecology, new BiomeDef { defName = "CQF_Check_Empty" }, 517, original, original.Keys), "unsupported biome without terrain rules is rejected before writing");
        ecology.waterCoverage = 0.8f; ecology.rockCoverage = 0.8f;
        reject(ecology.Validate, "invalid combined water and rock coverage rejected");
        ecology.waterCoverage = 0.1f; ecology.rockCoverage = 0.1f;
        XmlDocument xml = new XmlDocument(); xml.LoadXml(ecology.SaveToXElement("ecology").ToString());
        CQFEcologySettings read = DirectXmlToObject.ObjectFromXml<CQFEcologySettings>(xml.DocumentElement!, false);
        check(read.patchSize == ecology.patchSize && read.waterCoverage == ecology.waterCoverage, "ecology scalar settings survive native XML parsing");

        foreach (IntVec3 cell in original.Keys.ToList()) original[cell] = cell.x < 5 ? water : soil;
        CQFEcologyPlan shore = generator.Create(new CQFEcologySettings { regenerate = false }, biome, 1, original, Array.Empty<IntVec3>());
        CustomMapDataDef prefab = new CustomMapDataDef { defName = "CQF_Check_POI", isPart = true, rot = Rot4.North, size = new IntVec3(4, 1, 4),
            terrainsRect = new Dictionary<string, List<CellRect>> { ["Concrete"] = new List<CellRect> { new CellRect(0, 0, 4, 4) } } };
        CQFLayoutPartRule rule = new CQFLayoutPartRule { count = new IntRange(1, 1), pointOfInterest = new CQFPointOfInterestSettings { habitat = "Shore", featureDistance = new IntRange(2, 4) } };
        rule.pool.datas.Add(new CustomMapDataWithWeight { data = prefab, weight = 1f });
        CustomMapStep_Layout settings = new CustomMapStep_Layout { layout = new CQFMapLayoutDef { defName = "CQF_Check_Ecology", workerClass = typeof(CQFLayoutWorker_Ecology) }, parts = new List<CQFLayoutPartRule> { rule }, relation = "Anywhere", ecology = new CQFEcologySettings { regenerate = false } };
        CQFLayoutPlanningContext Context(IEnumerable<CQFMapRegion>? regions = null)
        {
            CQFLayoutPlanningContext context = new CQFLayoutPlanningContext(new IntVec3(40, 1, 40), settings, new[] { prefab }, regions ?? Array.Empty<CQFMapRegion>(),
                c => shore.CanWalk(c) && !shore.Water.Contains(c), c => shore.CanWalk(c), shore.CanWalk);
            context.Plan.Ecology = shore; return context;
        }
        CQFLayoutPlan points = new CQFLayoutPlanner().Plan(Context(), new CQFMapRegion?[] { null });
        check(points.Parts.Single().Bounds.minX - 4 >= 2 && points.Parts.Single().Bounds.minX - 4 <= 4 && points.Parts.Single().Bounds.Cells.All(c => !shore.Water.Contains(c)), "shore POI tests distance from complete dry footprint");
        rule.pointOfInterest.allowedTerrains.Add(water);
        reject(() => new CQFLayoutPlanner().Plan(Context(), new CQFMapRegion?[] { null }), "POI terrain whitelist applies to entire footprint");
        rule.pointOfInterest.allowedTerrains.Clear(); rule.pointOfInterest.habitat = "Any";
        CQFMapRegion island = new CQFMapRegion { key = "CQF_Island", id = "CQF_Island", cells = new CellRect(12, 12, 10, 10).Cells.ToList() };
        shore.Obstacles.UnionWith(new CellRect(10, 10, 14, 14).EdgeCells); settings.allowedRegion = island.key;
        reject(() => new CQFLayoutPlanner().Plan(Context(new[] { island }), new CQFMapRegion?[] { null }), "isolated POI rejected by preflight reachability");
        rule.pointOfInterest.requireReachable = false;
        check(new CQFLayoutPlanner().Plan(Context(new[] { island }), new CQFMapRegion?[] { null }).Parts.Count == 1, "explicitly disabled reachability permits isolated POI");
    }
}
