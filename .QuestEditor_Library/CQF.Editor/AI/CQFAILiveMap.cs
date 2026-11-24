using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed partial class CQFAILiveMap : ICQFAILiveMap
    {
        public CQFAILiveMap(Map map) { Map = map; }
        public Map Map { get; }
        public object Identity => Map;
        public bool IsValid => Current.Game != null && Find.Maps.Contains(Map);
        public CQFAILiveMapInfo Info => new CQFAILiveMapInfo { mapId = Map.uniqueID, size = Map.Size, center = Map.Center,
            selectedThingId = Find.Selector.SingleSelectedThing is Thing selected && selected.Map == Map ? selected.ThingID : string.Empty, backend = this };
        public XElement ReadOverview(CellRect region, int divisions) => ReadOverview(region, divisions, false);

        public XElement ReadOverview(CellRect region, int divisions, bool compact)
        {
            if (!region.InBounds(Map)) throw new InvalidDataException("CQF_AI_MapBounds");
            CQFAIMapOverview overview = new CQFAIMapOverview(cell =>
                (Map.terrainGrid.TerrainAt(cell).defName, Map.roofGrid.RoofAt(cell) != null, cell.Walkable(Map)));
            XElement result = overview.Describe(region, divisions, compact);
            result.SetAttributeValue("map", Map.uniqueID);
            return result;
        }
        public XElement ReadRegion(CellRect region, int offset, int limit) => ReadRegion(region, offset, limit, false);

        public XElement ReadRegion(CellRect region, int offset, int limit, bool compact)
        {
            if (!region.InBounds(Map) || region.Width < 1 || region.Height < 1 || (long)region.Width * region.Height > 4096) throw new InvalidDataException("CQF_AI_MapBounds");
            if (offset < 0 || offset > 4096 || limit < 1 || limit > 400) throw new InvalidDataException("CQF_AI_InvalidTool: pagination");
            IntVec3[] page = region.Skip(offset).Take(limit).ToArray();
            if (compact) return ReadRegionCompact(region, offset, page);
            XElement result = new XElement("region", new XAttribute("map", Map.uniqueID), new XAttribute("total", region.Area),
                new XAttribute("offset", offset), new XAttribute("encoding", "thing_refs"));
            XElement things = new XElement("things");
            HashSet<Thing> seen = new HashSet<Thing>();
            foreach (IntVec3 cell in page)
            {
                List<Thing> contents = cell.GetThingList(Map);
                XElement entry = new XElement("cell", new XAttribute("x", cell.x), new XAttribute("z", cell.z),
                    new XAttribute("terrain", Map.terrainGrid.TerrainAt(cell).defName), new XAttribute("roof", Map.roofGrid.RoofAt(cell)?.defName ?? ""),
                    new XAttribute("thingCount", contents.Count));
                foreach (Thing thing in contents.Take(20))
                {
                    entry.Add(new XElement("thingRef", new XAttribute("id", thing.ThingID)));
                    if (seen.Add(thing)) things.Add(DescribeRegionThing(thing));
                }
                result.Add(entry);
            }
            result.Add(things);
            return result;
        }
        public XElement ReadThing(string id)
        {
            Thing thing = FindThing(id);
            XElement result = Describe(thing);
            result.Add(CQFAIRuntimeCatalog.ThingState(thing));
            if (thing is not Pawn) result.Add((model ??= new CQFAIModel()).Write(Configuration(thing), "configuration", true));
            return result;
        }
        public IReadOnlyList<CQFAILiveMapEdit> Prepare(XElement changes, CQFAIModel model, string command, bool generateText)
            => new CQFAILiveMapPlan(this, model, command, generateText).Build(changes);
        public XElement Validate()
        {
            List<XElement> warnings = new List<XElement>();
            List<XElement> errors = new List<XElement>();
            int checkedThings = 0;
            foreach (Thing thing in Map.listerThings.AllThings.Where(thing => thing is not Pawn && (thing is InteractableThing or CustomMapEntrance or CustomMapExit
                or LootBox or CustomTrap or CustomDoor or CustomContainer or Spawner or ZoneCore or GenerationActionWorker
                || Map.GetComponent<MapComponent_CustomMapData>()?.ExtraOperations.ContainsKey(thing) == true
                || thing is ThingWithComps withComps && withComps.AllComps.Any(comp => comp is CompActionWorker or CompCustomText))))
            {
                checkedThings++;
                try
                {
                    CQFAILiveThingConfiguration value = Configuration(thing);
                    CQFAIContentValidation.Validate(value);
                    CQFAILiveFeatures.Validate(thing, value);
                    if (thing is CustomMapEntrance || thing is CustomMapExit)
                        foreach (XElement warning in CQFAIContentValidation.PortalWarnings(CQFAIThingContext.Read(thing)))
                        {
                            XElement copy = new XElement(warning); copy.Add(new XAttribute("thingId", thing.ThingID)); warnings.Add(copy);
                        }
                }
                catch (InvalidDataException error)
                {
                    errors.Add(new XElement("error", new XAttribute("thingId", thing.ThingID), new XAttribute("code", error.Message.Split(':')[0]), error.Message));
                }
            }
            foreach (var group in Map.listerThings.AllThings.OfType<CustomMapExit>().Where(exit => !string.IsNullOrWhiteSpace(exit.exitName)).GroupBy(exit => exit.exitName).Where(group => group.Count() > 1))
                warnings.Add(new XElement("warning", new XAttribute("code", "duplicate_exit_name"), group.Key));
            if (Map.GetComponent<MapComponent_CustomMapData>() != null)
                try { CQFAIMapFeatures.Validate(Map, CQFAIMapFeatures.Read(Map)); }
                catch (InvalidDataException error) { errors.Add(new XElement("error", new XAttribute("code", error.Message.Split(':')[0]), error.Message)); }
            return new XElement("validation", new XAttribute("passed", IsValid && errors.Count == 0), new XAttribute("scope", "live_map_configuration"),
                new XAttribute("checkedThings", checkedThings), new XAttribute("errors", errors.Count), new XAttribute("warnings", warnings.Count), errors.Take(20), warnings.Take(20));
        }
        public Thing FindThing(string id)
        {
            if (id.Length > 120) throw new InvalidDataException("CQF_AI_InvalidTool: thing_id");
            return Map.listerThings.AllThings.FirstOrDefault(thing => thing.ThingID == id && thing.Spawned && thing.Map == Map)
                ?? throw new InvalidDataException("CQF_AI_LiveThingMissing: " + id);
        }
        public static CQFAILiveThingConfiguration Configuration(Thing thing)
        {
            CQFAILiveThingConfiguration value = new CQFAILiveThingConfiguration { hitPoints = thing.def.useHitPoints ? thing.HitPoints : 0, stackCount = thing.stackCount,
                interaction = thing is InteractableThing ? (CQFAIInteractableConfiguration)CQFAIThingContext.Read(thing) : null,
                entrance = thing is CustomMapEntrance ? (CQFAIEntranceConfiguration)CQFAIThingContext.Read(thing) : null,
                exit = thing is CustomMapExit ? (CQFAIExitConfiguration)CQFAIThingContext.Read(thing) : null };
            CQFAILiveFeatures.Read(thing, value);
            return value;
        }
        public static void ValidateConfiguration(Thing thing, CQFAILiveThingConfiguration configuration)
        {
            if (thing is Pawn || configuration.stackCount < 1 || configuration.stackCount > thing.def.stackLimit
                || thing.def.useHitPoints && (configuration.hitPoints < 1 || configuration.hitPoints > thing.MaxHitPoints)
                || !thing.def.useHitPoints && configuration.hitPoints != 0
                || (thing is InteractableThing) != (configuration.interaction != null)
                || (thing is CustomMapEntrance) != (configuration.entrance != null) || (thing is CustomMapExit) != (configuration.exit != null))
                throw new InvalidDataException("CQF_AI_InvalidValue: live thing configuration");
            if (configuration.interaction != null) CQFAIThingContext.Create(thing)!.Validate!(configuration.interaction);
            if (configuration.entrance != null) CQFAIThingContext.Create(thing)!.Validate!(configuration.entrance);
            if (configuration.exit != null) CQFAIThingContext.Create(thing)!.Validate!(configuration.exit);
            CQFAILiveFeatures.Validate(thing, configuration);
        }
        public static void ApplyConfiguration(Thing thing, CQFAILiveThingConfiguration configuration)
        {
            ValidateConfiguration(thing, configuration);
            if (thing.def.useHitPoints) thing.HitPoints = configuration.hitPoints;
            thing.stackCount = configuration.stackCount;
            if (configuration.interaction != null) CQFAIThingContext.Apply(thing, configuration.interaction);
            if (configuration.entrance != null) CQFAIThingContext.Apply(thing, configuration.entrance);
            if (configuration.exit != null) CQFAIThingContext.Apply(thing, configuration.exit);
            CQFAILiveFeatures.Apply(thing, configuration);
        }
        private static XElement DescribeRegionThing(Thing thing) => Describe(thing, true);

        private static XElement Describe(Thing thing, bool compact = false)
        {
            CellRect footprint = thing.OccupiedRect();
            XAttribute[]? details = compact ? null : new[] {
                new XAttribute("hitPoints", thing.def.useHitPoints ? thing.HitPoints : 0), new XAttribute("maxHitPoints", thing.def.useHitPoints ? thing.MaxHitPoints : 0),
                new XAttribute("stackLimit", thing.def.stackLimit), new XAttribute("faction", thing.Faction?.Name ?? ""),
                new XAttribute("interaction", thing is InteractableThing), new XAttribute("entrance", thing is CustomMapEntrance), new XAttribute("exit", thing is CustomMapExit) };
            return new XElement("thing", new XAttribute("id", thing.ThingID), new XAttribute("def", thing.def.defName),
            new XAttribute("x", thing.Position.x), new XAttribute("z", thing.Position.z), new XAttribute("rotation", thing.Rotation.AsInt),
            new XAttribute("category", thing.def.category), new XAttribute("stuff", thing.Stuff?.defName ?? ""), new XAttribute("count", thing.stackCount),
            details, CQFAIThingCatalog.Describe(thing.def, compact),
            thing.def.rotatable || thing.def.size.x > 1 || thing.def.size.z > 1
                ? new XElement("footprint", new XAttribute("x", footprint.minX), new XAttribute("z", footprint.minZ), new XAttribute("width", footprint.Width), new XAttribute("height", footprint.Height)) : null,
            ThingUtility.InteractionCellsWhenAt(thing.def, thing.Position, thing.Rotation, thing.Map)
                .Select(cell => new XElement("interactionCell", new XAttribute("x", cell.x), new XAttribute("z", cell.z))));
        }
        private CQFAIModel? model;
    }
}
