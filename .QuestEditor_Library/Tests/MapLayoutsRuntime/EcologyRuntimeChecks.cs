using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.MapLayoutRuntimeChecks;

public sealed class EcologyRuntimeChecks
{
    public EcologyRuntimeChecks(Map map, CustomMapDataDef house, Action<bool, string> check)
    {
        this.map = map; this.house = house; this.check = check;
    }

    public CustomMapStep_Layout Settings { get; private set; } = null!;

    public void Setup()
    {
        CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(map).Regions;
        foreach (CQFMapRegion region in registry.All) region.protect = true;
        CQFMapRegion entrance = new CQFMapRegion { key = "CQF_Runtime_Entrance", protect = true, cells = new CellRect(0, 55, 3, 4).Cells.ToList() };
        registry.Add(entrance, "", map);
        foreach (IntVec3 cell in entrance.cells) map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        bed = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog), new IntVec3(95, 0, 90), map);
        registry.Add(new CQFMapRegion { key = "CQF_Runtime_BedEdge", protect = true, cells = new List<IntVec3> { bed.Position } }, "", map);
        stock = ThingMaker.MakeThing(ThingDefOf.WoodLog); stock.stackCount = 23; GenSpawn.Spawn(stock, new IntVec3(30, 0, 30), map);
        GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.WoodLog), new IntVec3(94, 0, 90), map);
        map.roofGrid.SetRoof(new IntVec3(94, 0, 90), RoofDefOf.RoofConstructed);
        originalTerrain = map.AllCells.ToDictionary(c => c, c => c.GetTerrain(map));
        originalThings = map.listerThings.AllThings.ToDictionary(t => t.ThingID, t => (t, t.Position, t.HitPoints, t.stackCount));
        worldBiome = map.TileInfo.PrimaryBiome;
        otherMap = Find.Maps.First(m => m != map); otherBiome = otherMap.Biome;
        selectedBiome = DefDatabase<BiomeDef>.GetNamed(map.Biome.defName == "TemperateForest" ? "BorealForest" : "TemperateForest");
        house.tags.Add("CQF_Runtime_EcologyPart");
        Settings = new CustomMapStep_Layout
        {
            layout = DefDatabase<CQFMapLayoutDef>.GetNamed("CQF_Layout_Ecology"), seed = 517, relation = "Anywhere", spacing = 5,
            outputKey = "CQF_Runtime_Ecology", outputTags = new List<string> { "EcologyPoint" },
            ecology = new CQFEcologySettings { biome = selectedBiome, waterTerrain = DefDatabase<TerrainDef>.GetNamed("WaterShallow"),
                waterCoverage = 0.08f, rockCoverage = 0.08f, plantDensity = 0.025f,
                terrains = new List<CQFEcologyTerrain> { new CQFEcologyTerrain { terrain = TerrainDefOf.Soil } } }
        };
        foreach (string habitat in new[] { "Shore", "RockEdge", "Open" })
        {
            CQFLayoutPartRule rule = new CQFLayoutPartRule { role = habitat, count = new IntRange(1, 1),
                pointOfInterest = new CQFPointOfInterestSettings { habitat = habitat, featureDistance = new IntRange(1, 8) } };
            rule.pool.tags.Add(new CustomMapDataTagWithWeight { tag = "CQF_Runtime_EcologyPart", weight = 1f });
            Settings.parts.Add(rule);
        }
        check(Settings.layout.LabelCap != "CQF_Layout_Ecology" && "CQF_Ecology_Settings".Translate().ToString() != "CQF_Ecology_Settings", "ecology layout and settings use translated natural text");
        check(new[] { "CQFEcologySettings", "CQF_Ecology_Regenerate", "CQF_Region_Title", "CQFLayoutPartRule", "CQFPointOfInterestSettings" }
            .All(key => LanguageDatabase.activeLanguage.HaveTextForKey(key)), "active language contains region and ecology text without English fallback");
    }

    public void Preview()
    {
        previewSnapshot = Snapshot();
        plan = Settings.Preview(map);
        check(plan.Parts.Count == 3 && plan.Parts.All(p => p.Source == house), "ecology POIs select existing parts by tag for three habitat rules");
        check(plan.Ecology!.Mutable.Count > 10000 && plan.Ecology.Mutable.Contains(new IntVec3(0, 0, 0)), "ecology plans the entire map including unprotected edges");
        check(!plan.Ecology.Mutable.Any(plan.Protected.Contains) && bed!.OccupiedRect().Cells.All(plan.Protected.Contains), "ecology excludes entrance city and whole partially protected building");
        check(plan.Matches(Settings.Preview(map)), "actual map ecology preview is deterministic");
        check(originalTerrain.All(p => p.Key.GetTerrain(map) == p.Value) && map.listerThings.AllThings.Count == originalThings.Count, "ecology preview leaves actual terrain and things unchanged");
        string tag = Settings.parts[0].pool.tags[0].tag; Settings.parts[0].pool.tags[0].tag = "CQF_Runtime_MissingPart";
        bool rejected = false;
        try { Settings.Generate(map, new CustomMapGenerationContext(null!)); } catch (CQFLayoutException) { rejected = true; }
        Settings.parts[0].pool.tags[0].tag = tag;
        check(rejected && originalTerrain.All(p => p.Key.GetTerrain(map) == p.Value) && map.listerThings.AllThings.Count == originalThings.Count, "missing required POI rejects before ecological writes");
        stock!.stackCount++;
        rejected = false;
        try { Settings.Generate(map, new CustomMapGenerationContext(null!), plan); } catch (CQFLayoutException) { rejected = true; }
        stock.stackCount--;
        check(rejected && originalTerrain.All(p => p.Key.GetTerrain(map) == p.Value), "map changes invalidate ecology preview before applying terrain");
        Find.DesignatorManager.Select(new Designator_CQFLayoutPreview(map, Settings, plan));
        Find.CameraDriver.SetRootPosAndSize(map.Center.ToVector3Shifted(), 66f);
    }

    public void Generate()
    {
        CQFLayoutPlan current = Settings.Preview(map);
        if (!plan!.Matches(current)) throw new InvalidOperationException("Ecology preview changed: source=" + (plan.Ecology!.SourceSignature == current.Ecology!.SourceSignature)
            + " terrain=" + plan.Ecology.Terrains.All(p => current.Ecology.Terrains[p.Key] == p.Value)
            + " objects=" + plan.Ecology.Objects.All(p => current.Ecology.Objects.TryGetValue(p.Key, out ThingDef? value) && value == p.Value)
            + " things=" + string.Join(",", map.listerThings.AllThings.Where(t => !originalThings.TryGetValue(t.ThingID, out var old) || old.Position != t.Position || old.Health != t.HitPoints || old.Count != t.stackCount).Select(t => t.ThingID))
            + " missing=" + string.Join(",", originalThings.Values.Where(t => !t.Thing.Spawned).Select(t => t.Thing.ThingID))
            + " snapshot=" + string.Join(" | ", previewSnapshot.Except(Snapshot()).Take(5)) + " -> " + string.Join(" | ", Snapshot().Except(previewSnapshot).Take(5)));
        Settings.Generate(map, new CustomMapGenerationContext(null!), plan);
        check(originalThings.Values.All(v => v.Thing.Spawned && v.Thing.Position == v.Position && v.Thing.HitPoints == v.Health && v.Thing.stackCount == v.Count), "ecology preserves all existing artificial objects and stack counts");
        check(plan!.Protected.All(c => c.GetTerrain(map) == originalTerrain[c]) && map.roofGrid.RoofAt(new IntVec3(94, 0, 90)) == RoofDefOf.RoofConstructed, "ecology preserves all protected terrain and existing roof");
        HashSet<IntVec3> partCells = new HashSet<IntVec3>(plan.Parts.SelectMany(p => p.Bounds.Cells));
        check(plan.Ecology!.Mutable.Where(c => !partCells.Contains(c)).All(c => c.GetTerrain(map) == plan.Ecology.Terrains[c]), "generated ecological terrain matches preview outside POIs");
        check(plan.Ecology.Objects.Where(p => !partCells.Contains(p.Key)).All(p => p.Key.GetThingList(map).Any(t => t.def == p.Value)), "actual plants and rocks match ecological preview outside POIs");
        check(plan.Ecology.Objects.Values.Any(t => t.plant != null) && plan.Ecology.Objects.Values.Any(t => t.building?.isNaturalRock == true), "native ecology creates both vegetation and rock formations");
        check(map.Biome == selectedBiome && map.Biomes.Count() == 1 && map.Biomes.Single() == selectedBiome
            && map.GetComponent<MixedBiomeMapComponent>().GetBiomeAt(map.Center) == selectedBiome, "map and per-cell biome reads use selected map ecology");
        check(map.TileInfo.PrimaryBiome == worldBiome && otherMap!.Biome == otherBiome, "ecology override leaves world tile and other maps unchanged");
        CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(map).Regions;
        check(registry.All.Count(r => r.tags.Contains("EcologyPoint")) == 3 && new[] { "Shore", "RockEdge", "Open" }.All(tag => registry.All.Any(r => r.tags.Contains(tag))), "generated POIs expose output and role tags to later steps");
        check(!GameTools.isGeneratingMap && CQFLayoutExecution.Current == null && GenStep_CustomMap.generatedCount.Count == 0, "ecology generation restores transient state");
        Settings.ecology.regenerate = false; Settings.parts.Clear();
        CQFLayoutPartRule reuseRule = new CQFLayoutPartRule { count = new IntRange(1, 1) };
        reuseRule.pool.tags.Add(new CustomMapDataTagWithWeight { tag = "CQF_Runtime_EcologyPart", weight = 1f }); Settings.parts.Add(reuseRule);
        CQFLayoutPlan reuse = Settings.Preview(map);
        check(reuse.Ecology!.Mutable.Count == 0 && reuse.Ecology.Objects.Count == 0 && reuse.Ecology.Biome == null && reuse.Parts.Count == 1, "reuse mode can plan new POI without ecological or biome writes");
        map.fogGrid.ClearAllFog(); map.mapDrawer.RegenerateEverythingNow();
    }

    public void CheckLoaded(Map loaded)
    {
        check(loaded.GetComponent<MapComponent_CQFEcology>().Biome == selectedBiome && loaded.Biome == selectedBiome
            && loaded.GetComponent<MixedBiomeMapComponent>().GetBiomeAt(loaded.Center) == selectedBiome, "native save and reload preserve map-scoped ecology identity");
        CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(loaded).Regions;
        check(registry.All.Count(r => r.tags.Contains("EcologyPoint")) == 3 && registry.All.Any(r => r.key == "CQF_Runtime_Entrance" && r.protect), "native ecology save retains POI tags and protected entrance");
        check(plan!.Ecology!.Mutable.Where(c => !plan.Parts.Any(p => p.Bounds.Contains(c))).All(c => c.GetTerrain(loaded) == plan.Ecology.Terrains[c]), "native reload preserves ecological terrain");
    }

    private List<string> Snapshot() => map.AllCells.Select(c => c + ":" + c.GetTerrain(map).defName + ":" + map.roofGrid.RoofAt(c)?.defName)
        .Concat(map.listerThings.AllThings.OrderBy(t => t.thingIDNumber).Select(t => t.ThingID + ":" + t.Position + ":" + t.Rotation + ":" + t.HitPoints + ":" + t.stackCount)).ToList();

    private List<string> previewSnapshot = new List<string>();
    private readonly Map map;
    private readonly CustomMapDataDef house;
    private readonly Action<bool, string> check;
    private Dictionary<IntVec3, TerrainDef> originalTerrain = null!;
    private Dictionary<string, (Thing Thing, IntVec3 Position, int Health, int Count)> originalThings = null!;
    private CQFLayoutPlan? plan;
    private Thing? bed;
    private Thing? stock;
    private Map? otherMap;
    private BiomeDef? otherBiome;
    private BiomeDef? worldBiome;
    private BiomeDef? selectedBiome;
}
