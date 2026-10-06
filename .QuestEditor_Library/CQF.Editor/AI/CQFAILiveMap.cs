using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAILiveMap : ICQFAILiveMap
    {
        public CQFAILiveMap(Map map) { Map = map; }
        public Map Map { get; }
        public object Identity => Map;
        public bool IsValid => Current.Game != null && Find.Maps.Contains(Map) && ReferenceEquals(Find.CurrentMap, Map);
        public CQFAILiveMapInfo Info => new CQFAILiveMapInfo { mapId = Map.uniqueID, size = Map.Size, center = Map.Center,
            selectedThingId = Find.Selector.SingleSelectedThing?.ThingID ?? string.Empty, backend = this };
        public XElement ReadRegion(CellRect region, int offset, int limit)
        {
            if (!region.InBounds(Map) || region.Width < 1 || region.Height < 1 || (long)region.Width * region.Height > 4096) throw new InvalidDataException("CQF_AI_MapBounds");
            if (offset < 0 || offset > 4096 || limit < 1 || limit > 400) throw new InvalidDataException("CQF_AI_InvalidTool: pagination");
            return new XElement("region", new XAttribute("map", Map.uniqueID), new XAttribute("total", region.Area), new XAttribute("offset", offset),
                region.Skip(offset).Take(limit).Select(cell => new XElement("cell", new XAttribute("x", cell.x), new XAttribute("z", cell.z),
                    new XAttribute("terrain", Map.terrainGrid.TerrainAt(cell).defName), new XAttribute("roof", Map.roofGrid.RoofAt(cell)?.defName ?? ""),
                    new XAttribute("thingCount", cell.GetThingList(Map).Count), cell.GetThingList(Map).Take(20).Select(Describe))));
        }
        public XElement ReadThing(string id)
        {
            Thing thing = FindThing(id);
            XElement result = Describe(thing);
            if (thing is not Pawn) result.Add((model ??= new CQFAIModel()).Write(Configuration(thing), "configuration", true));
            return result;
        }
        public IReadOnlyList<CQFAILiveMapEdit> Prepare(XElement changes, CQFAIModel model, string command, bool generateText)
            => new CQFAILiveMapPlan(this, model, command, generateText).Build(changes);
        public XElement Validate()
        {
            List<XElement> warnings = new List<XElement>();
            foreach (Thing thing in Map.listerThings.AllThings.Where(thing => thing is CustomMapEntrance || thing is CustomMapExit))
                foreach (XElement warning in CQFAIContentValidation.PortalWarnings(CQFAIThingContext.Read(thing)))
                {
                    XElement copy = new XElement(warning); copy.Add(new XAttribute("thingId", thing.ThingID)); warnings.Add(copy);
                }
            foreach (var group in Map.listerThings.AllThings.OfType<CustomMapExit>().Where(exit => !string.IsNullOrWhiteSpace(exit.exitName)).GroupBy(exit => exit.exitName).Where(group => group.Count() > 1))
                warnings.Add(new XElement("warning", new XAttribute("code", "duplicate_exit_name"), group.Key));
            return new XElement("validation", new XAttribute("passed", IsValid), new XAttribute("scope", "live_map"), new XAttribute("warnings", warnings.Count), warnings.Take(40));
        }
        public Thing FindThing(string id)
        {
            if (id.Length > 120) throw new InvalidDataException("CQF_AI_InvalidTool: thing_id");
            return Map.listerThings.AllThings.FirstOrDefault(thing => thing.ThingID == id && thing.Spawned && thing.Map == Map)
                ?? throw new InvalidDataException("CQF_AI_LiveThingMissing: " + id);
        }
        public static CQFAILiveThingConfiguration Configuration(Thing thing)
        {
            return new CQFAILiveThingConfiguration { hitPoints = thing.def.useHitPoints ? thing.HitPoints : 0, stackCount = thing.stackCount,
                interaction = thing is InteractableThing ? (CQFAIInteractableConfiguration)CQFAIThingContext.Read(thing) : null,
                entrance = thing is CustomMapEntrance ? (CQFAIEntranceConfiguration)CQFAIThingContext.Read(thing) : null,
                exit = thing is CustomMapExit ? (CQFAIExitConfiguration)CQFAIThingContext.Read(thing) : null };
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
        }
        public static void ApplyConfiguration(Thing thing, CQFAILiveThingConfiguration configuration)
        {
            ValidateConfiguration(thing, configuration);
            if (thing.def.useHitPoints) thing.HitPoints = configuration.hitPoints;
            thing.stackCount = configuration.stackCount;
            if (configuration.interaction != null) CQFAIThingContext.Apply(thing, configuration.interaction);
            if (configuration.entrance != null) CQFAIThingContext.Apply(thing, configuration.entrance);
            if (configuration.exit != null) CQFAIThingContext.Apply(thing, configuration.exit);
        }
        private static XElement Describe(Thing thing) => new XElement("thing", new XAttribute("id", thing.ThingID), new XAttribute("def", thing.def.defName),
            new XAttribute("x", thing.Position.x), new XAttribute("z", thing.Position.z), new XAttribute("rotation", thing.Rotation.AsInt),
            new XAttribute("category", thing.def.category), new XAttribute("stuff", thing.Stuff?.defName ?? ""), new XAttribute("count", thing.stackCount),
            new XAttribute("hitPoints", thing.def.useHitPoints ? thing.HitPoints : 0), new XAttribute("maxHitPoints", thing.def.useHitPoints ? thing.MaxHitPoints : 0),
            new XAttribute("stackLimit", thing.def.stackLimit), new XAttribute("faction", thing.Faction?.Name ?? ""),
            new XAttribute("interaction", thing is InteractableThing), new XAttribute("entrance", thing is CustomMapEntrance), new XAttribute("exit", thing is CustomMapExit));
        private CQFAIModel? model;
    }
}
