using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIMapFeatures
    {
        public static CQFAIMapConfiguration Read(Map map)
        {
            MapComponent_CustomMapData component = Component(map);
            return new CQFAIMapConfiguration
            {
                eventAreas = component.EventAreas.ToList(),
                triggers = component.Triggers.Select(trigger => new CQFAIMapTriggerConfiguration
                {
                    key = trigger.key, mode = trigger.mode, actions = trigger.actions,
                    thingIds = trigger.things?.Select(thing => thing?.ThingID ?? string.Empty).ToList() ?? new List<string>()
                }).ToList()
            };
        }
        public static void Validate(CQFAIMapConfiguration value)
        {
            if (value.eventAreas == null || value.triggers == null || value.eventAreas.Count > 512 || value.triggers.Count > 512
                || value.eventAreas.Any(area => area == null || string.IsNullOrWhiteSpace(area.key) || area.key.Length > 200
                    || area.cells == null || area.cells.Count is < 1 or > 4096 || area.cells.Distinct().Count() != area.cells.Count
                    || area.cells.Any(cell => !cell.IsValid || cell.y != 0) || area.actions == null || area.actions.Any(action => action == null))
                || value.triggers.Any(trigger => trigger == null || string.IsNullOrWhiteSpace(trigger.key) || trigger.key.Length > 200
                    || trigger.mode != ActionTriggerMode.Damaged || trigger.thingIds == null || trigger.thingIds.Count is < 1 or > 512
                    || trigger.thingIds.Any(id => string.IsNullOrWhiteSpace(id) || id.Length > 120) || trigger.thingIds.Distinct().Count() != trigger.thingIds.Count
                    || trigger.actions == null || trigger.actions.Any(action => action == null)))
                throw new InvalidDataException("CQF_AI_InvalidValue: map configuration");
            if (value.eventAreas.Select(area => area.key).Distinct().Count() != value.eventAreas.Count
                || value.triggers.Select(trigger => trigger.key).Distinct().Count() != value.triggers.Count)
                throw new InvalidDataException("CQF_AI_InvalidValue: duplicate map configuration key");
            foreach (CQFEventArea area in value.eventAreas)
                if (!string.IsNullOrEmpty(area.faction) && area.faction is not ("Any" or "Player" or "Hostile" or "RandomHostile" or "Ally"
                    or "RandomAlly" or "Neutral" or "RandomNeutral" or "MapFaction")
                    && !DefDatabase<RimWorld.FactionDef>.AllDefsListForReading.Any(def => def.defName == area.faction))
                    throw new InvalidDataException("CQF_AI_MissingResource: event area faction " + area.faction);
        }
        public static void Validate(Map map, CQFAIMapConfiguration value)
        {
            Validate(value);
            if (value.eventAreas.Any(area => area.cells.Any(cell => !cell.InBounds(map)))) throw new InvalidDataException("CQF_AI_MapBounds");
            foreach (string id in value.triggers.SelectMany(trigger => trigger.thingIds))
                if (FindThing(map, id) is not Building) throw new InvalidDataException("CQF_AI_InvalidValue: map damage triggers require a Building " + id);
        }
        public static CQFAILiveMapEdit Prepare(Map map, CQFAIModel model, XElement changes, string command, bool generateText)
        {
            MapComponent_CustomMapData component = Component(map);
            CQFAIMapConfiguration before = (CQFAIMapConfiguration)model.Copy(Read(map));
            CQFAIMapConfiguration after = (CQFAIMapConfiguration)new CQFAIChanges(model).Build(before, changes, command, generateText);
            Validate(map, after);
            CQFEventArea[] oldAreas = component.EventAreas.ToArray();
            ThingActionTrigger[] oldTriggers = component.Triggers.ToArray();
            string expected = Snapshot();
            List<CQFEventArea> areas = after.eventAreas.Select(area =>
            {
                CQFEventArea? old = oldAreas.FirstOrDefault(previous => previous.key == area.key);
                return old != null && XNode.DeepEquals(model.Write(old), model.Write(area)) ? old : area;
            }).ToList();
            List<ThingActionTrigger> triggers = after.triggers.Select(trigger => new ThingActionTrigger
            {
                key = trigger.key, mode = trigger.mode, actions = trigger.actions,
                things = trigger.thingIds.Select(id => FindThing(map, id)).ToList()
            }).ToList();
            return new CQFAILiveMapEdit("map_configuration", () =>
            {
                if (Snapshot() != expected) throw new InvalidOperationException("CQF_AI_StaleTarget");
                Validate(map, after);
                foreach (CQFEventArea area in areas.Where(area => !oldAreas.Contains(area))) area.InitializeRuntime(map);
                component.EventAreas.Clear(); component.EventAreas.AddRange(areas);
                component.Triggers.Clear(); component.Triggers.AddRange(triggers);
                if (!XNode.DeepEquals(model.Write(Read(map)), model.Write(after))) throw new InvalidDataException("CQF_AI_ApplyMismatch: map configuration");
            }, () =>
            {
                foreach (CQFEventArea area in oldAreas) area.InitializeRuntime(map);
                component.EventAreas.Clear(); component.EventAreas.AddRange(oldAreas);
                component.Triggers.Clear(); component.Triggers.AddRange(oldTriggers);
            }, Snapshot, () => new XElement("mapConfigurationEdited", new XAttribute("eventAreas", areas.Count), new XAttribute("triggers", triggers.Count)));
            string Snapshot() => model.Write(Read(map)).ToString(SaveOptions.DisableFormatting);
        }
        private static Thing FindThing(Map map, string id) => map.listerThings.AllThings.FirstOrDefault(thing => thing.ThingID == id && thing.Spawned && thing.Map == map)
            ?? throw new InvalidDataException("CQF_AI_LiveThingMissing: " + id);
        private static MapComponent_CustomMapData Component(Map map) => map.GetComponent<MapComponent_CustomMapData>()
            ?? throw new InvalidDataException("CQF_AI_MissingResource: map component");
    }
}
