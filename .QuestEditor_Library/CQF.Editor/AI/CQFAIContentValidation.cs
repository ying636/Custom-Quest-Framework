using System.Xml.Linq;

namespace QuestEditor_Library
{
    public static class CQFAIContentValidation
    {
        public static void Validate(object value)
        {
            if (value is CQFAILiveThingConfiguration live)
            {
                if (live.interaction != null) Validate(live.interaction);
                if (live.entrance != null) Validate(live.entrance);
                if (live.exit != null) Validate(live.exit);
            }
            if (value is CustomMapDataDef map)
                foreach (CustomThingData thing in (map.customThings ?? new List<CustomThingData>()).Concat(map.zoneCores ?? new List<CustomThingData>())) Validate(thing);
            if (value is CQFAIInteractableConfiguration configuration)
            {
                if (configuration.operationDefs == null || configuration.operationDefs.Any(def => def == null)) throw new InvalidDataException("CQF_AI_InvalidValue: interaction definitions");
                ValidateOperations(configuration.operations);
            }
            if (value is CustomThingData_InteractableThing interactive)
            {
                if (interactive.operationDefs == null || interactive.operationDefs.Any(def => def == null)) throw new InvalidDataException("CQF_AI_InvalidValue: interaction definitions");
                ValidateOperations(interactive.operations);
            }
            if (value is InteractionOperation operation) ValidateOperations(new[] { operation });
            if (value is CQFAIEntranceConfiguration entrance && (entrance.enterActions == null || entrance.tagWithChance == null || entrance.mapDefWithChance == null
                || entrance.enterActions.Any(action => action == null) || entrance.tagWithChance.Any(tag => tag == null || string.IsNullOrWhiteSpace(tag.tag) || float.IsNaN(tag.chance) || float.IsInfinity(tag.chance) || tag.chance <= 0f)
                || entrance.mapDefWithChance.Any(map => map == null || map.def == null || float.IsNaN(map.chance) || float.IsInfinity(map.chance) || map.chance <= 0f)
                || entrance.mapDefWithChance.Select(map => map.def).Distinct().Count() != entrance.mapDefWithChance.Count)) throw new InvalidDataException("CQF_AI_InvalidValue: entrance configuration");
            if (value is CQFAIExitConfiguration exit && (exit.enterActions == null || exit.enterActions.Any(action => action == null))) throw new InvalidDataException("CQF_AI_InvalidValue: exit actions");
            if (value is CustomThingData_CustomMapEntrance entry) Validate(new CQFAIEntranceConfiguration { data = entry.data, exitName = entry.exitName, enterActions = entry.enterActions, tagWithChance = entry.tagWithChance, mapDefWithChance = entry.mapDefWithChance });
            if (value is CustomThingData_CustomMapExit departure) Validate(new CQFAIExitConfiguration { exitName = departure.exitName, enterActions = departure.enterActions });
        }
        public static IEnumerable<XElement> PortalWarnings(object value)
        {
            if (value is CustomMapDataDef map)
            {
                foreach (var thing in (map.customThings ?? new List<CustomThingData>()).Concat(map.zoneCores ?? new List<CustomThingData>()))
                    foreach (XElement warning in PortalWarnings(thing)) yield return warning;
                foreach (var group in (map.customThings ?? new List<CustomThingData>()).OfType<CustomThingData_CustomMapExit>().GroupBy(exit => exit.exitName).Where(group => !string.IsNullOrEmpty(group.Key) && group.Count() > 1))
                    yield return new XElement("warning", new XAttribute("code", "duplicate_exit_name"), group.Key);
            }
            CustomMapDataDef? destination = value is CQFAIEntranceConfiguration config ? config.data : (value as CustomThingData_CustomMapEntrance)?.data;
            string? exitName = value is CQFAIEntranceConfiguration entry ? entry.exitName : (value as CustomThingData_CustomMapEntrance)?.exitName;
            if (value is CQFAIEntranceConfiguration || value is CustomThingData_CustomMapEntrance)
            {
                bool hasPool = value is CQFAIEntranceConfiguration random ? random.tagWithChance.Count > 0 || random.mapDefWithChance.Count > 0
                    : value is CustomThingData_CustomMapEntrance chance && (chance.tagWithChance.Count > 0 || chance.mapDefWithChance.Count > 0);
                if (destination == null && !hasPool) yield return new XElement("warning", new XAttribute("code", "destination_not_selected"));
                else if (destination == null) yield return new XElement("warning", new XAttribute("code", "random_destination_requires_runtime_check"));
                else if (string.IsNullOrWhiteSpace(exitName) || !(destination.customThings ?? new List<CustomThingData>()).Concat(destination.zoneCores ?? new List<CustomThingData>()).OfType<CustomThingData_CustomMapExit>().Any(exit => exit.exitName == exitName))
                    yield return new XElement("warning", new XAttribute("code", "destination_exit_missing"), new XAttribute("map", destination.defName), exitName ?? "");
            }
        }
        private static void ValidateOperations(IEnumerable<InteractionOperation> operations)
        {
            if (operations == null || operations.Any(operation => operation == null || operation.tickToOperate < 0 || operation.conditions == null || operation.results == null || operation.requiredThings == null
                || operation.conditions.Any(condition => condition == null) || operation.requiredThings.Any(thing => thing == null)
                || operation.results.Any(result => result == null || result.actions == null || result.conditions == null || result.conditions.Any(condition => condition == null) || result.actions.Any(action => action == null))))
                throw new InvalidDataException("CQF_AI_InvalidValue: interaction configuration");
        }
    }
}
