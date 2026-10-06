using Verse;

namespace QuestEditor_Library
{
    public static class CQFAIThingContext
    {
        public static CQFAIEditorContext? Create(Thing thing, object? owner = null)
        {
            if (thing is not InteractableThing && thing is not CustomMapEntrance && thing is not CustomMapExit) return null;
            return new CQFAIEditorContext(thing.def?.defName ?? thing.GetType().Name, () => Read(thing), value => Apply(thing, value),
                value => Validate(thing, value), () => !thing.Destroyed && (owner is not Window window || Find.WindowStack.Windows.Contains(window)), owner ?? thing, thing);
        }
        public static object Read(Thing thing)
        {
            if (thing is InteractableThing interactable)
                return new CQFAIInteractableConfiguration { operations = interactable.operations, operationDefs = interactable.operationDefs };
            if (thing is CustomMapEntrance entrance)
                return new CQFAIEntranceConfiguration
                {
                    data = entrance.MapDef, exitName = entrance.exitName, opended = entrance.opended, enterActions = entrance.enterActions,
                    tagWithChance = entrance is CustomMapEntrance_Chance chance ? chance.tagWithChance : new List<TagWithChance>(),
                    mapDefWithChance = entrance is CustomMapEntrance_Chance weighted ? weighted.mapDefWithChance : new List<MapDefWithChance>()
                };
            if (thing is CustomMapExit exit) return new CQFAIExitConfiguration { exitName = exit.exitName, enterActions = exit.enterActions };
            throw new InvalidDataException("CQF_AI_UnknownType: " + thing.GetType().FullName);
        }
        public static void Apply(Thing thing, object value)
        {
            Validate(thing, value);
            if (thing is InteractableThing interactable && value is CQFAIInteractableConfiguration interaction)
            {
                interactable.operations = interaction.operations;
                interactable.operationDefs = interaction.operationDefs;
                foreach (InteractionOperation operation in interactable.operations) CQFSignalEditor.InvalidateSummary(operation);
            }
            else if (thing is CustomMapEntrance entrance && value is CQFAIEntranceConfiguration entry)
            {
                entrance.mapDef = entry.data!;
                entrance.exitName = entry.exitName!;
                entrance.enterActions = entry.enterActions;
                if (entrance.opended != entry.opended)
                {
                    if (entrance.Spawned) entrance.Swtich(entry.opended);
                    else entrance.opended = entry.opended;
                }
                if (entrance is CustomMapEntrance_Chance chance)
                {
                    chance.tagWithChance = entry.tagWithChance;
                    chance.mapDefWithChance = entry.mapDefWithChance;
                }
            }
            else if (thing is CustomMapExit exit && value is CQFAIExitConfiguration departure)
            {
                exit.exitName = departure.exitName!;
                exit.enterActions = departure.enterActions;
            }
            else throw new InvalidDataException("CQF_AI_InvalidValue: configuration type");
        }
        private static void Validate(Thing thing, object value)
        {
            CQFAIChanges.Validate(value);
            if (thing.Destroyed) throw new InvalidOperationException("CQF_AI_StaleTarget");
            if (thing is CustomMapEntrance entrance && value is CQFAIEntranceConfiguration entry)
            {
                if (entrance is not CustomMapEntrance_Chance && (entry.tagWithChance.Count > 0 || entry.mapDefWithChance.Count > 0))
                    throw new InvalidDataException("CQF_AI_InvalidValue: chance entrance required");
                if (entrance.exit != null && (entry.exitName != entrance.exitName || entry.data != entrance.MapDef))
                    throw new InvalidDataException("CQF_AI_PortalLinked");
            }
            if (thing is CustomMapExit exit && value is CQFAIExitConfiguration departure && exit.entrance != null && departure.exitName != exit.exitName)
                throw new InvalidDataException("CQF_AI_PortalLinked");
        }
    }
}
