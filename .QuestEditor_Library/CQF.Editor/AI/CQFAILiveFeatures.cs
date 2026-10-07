using System.Reflection;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAILiveFeatures
    {
        public static void Read(Thing thing, CQFAILiveThingConfiguration value)
        {
            if (thing is LootBox box) value.loot = new CQFAILootConfiguration { lootBoxName = box.lootBoxName, tickToOpen = box.tickToOpen,
                openReport = box.openReport, destroyAfterOpening = box.destroyAfterOpening, useLootDef = box.useLootDef,
                openWhenDestroyed = box.openWhenDestroyed, loots = box.loots, lootDef = box.lootDef };
            if (thing is CustomTrap trap) value.trap = new CQFAITrapConfiguration { trapName = trap.trapName, trapComps = trap.trapComps,
                capture = trap is CustomTrap_Capture capture ? new CQFAICaptureTrapConfiguration { disarmReport = capture.disarmReport,
                    tickToDisarm = capture.tickToDisarm, disarmActions = capture.disarmActions } : null };
            if (thing is CustomDoor door) value.door = new CQFAIDoorConfiguration { openingActions = door.openingActions, openingConditions = door.openingConditions };
            if (thing is CustomContainer container) value.container = new CQFAIContainerConfiguration { tickToOpen = container.tickToOpen,
                innerThings = container.innerThings, openingActions = container.openingActions, openingConditions = container.openingConditions };
            if (thing is Spawner spawner) value.spawner = new CQFAISpawnerConfiguration { pawns = spawner.pawns };
            if (thing is ZoneCore zone) value.zone = new CQFAIZoneConfiguration { generationKey = zone.generationKey, coreRotation = zone.coreRotation,
                size = zone.size, prohibitRotatingDocking = zone.prohibitRotatingDocking, prohibitFlippingDocking = zone.prohibitFlippingDocking,
                isCenter = zone.isCenter, destroyThings = zone.destroyThings, reserveThing = zone.reserveThing, conditions = zone.conditions, coreTags = zone.coreTags };
            if (thing is GenerationActionWorker generation) value.generation = new CQFAIGenerationActionsConfiguration { actions = generation.actions };
            MapComponent_CustomMapData? component = thing.Map?.GetComponent<MapComponent_CustomMapData>();
            if (thing is not Pawn && component?.ExtraOperations.TryGetValue(thing, out List<InteractionOperation> operations) == true)
                value.extraInteractions = operations;
            if (thing is ThingWithComps textThing && thing is not Pawn)
                value.customTexts = textThing.AllComps.Select((comp, index) => new { comp, index }).Where(pair => pair.comp is CompCustomText)
                    .Select(pair =>
                    {
                        CompCustomText text = (CompCustomText)pair.comp;
                        return new CQFAICustomTextConfiguration { componentIndex = pair.index, useCustomName = text.useCustomName,
                            useCustomDescription = text.useCustomDescription, useCustomInspectText = text.useCustomInspectText,
                            customName = text.customName, customDescription = text.customDescription, customInspectText = text.customInspectText };
                    }).ToList();
            if (thing is ThingWithComps withComps && thing is not Pawn)
                value.actionWorkers = withComps.AllComps.Select((comp, index) => new { comp, index }).Where(pair => pair.comp is CompActionWorker)
                    .Select(pair => new CQFAIActionWorkerConfiguration { componentIndex = pair.index, comps = ((CompActionWorker)pair.comp).comps }).ToList();
        }
        public static void Validate(Thing thing, CQFAILiveThingConfiguration value)
        {
            if ((thing is LootBox) != (value.loot != null) || (thing is CustomTrap) != (value.trap != null)
                || (thing is CustomTrap_Capture) != (value.trap?.capture != null) || (thing is CustomDoor) != (value.door != null)
                || (thing is CustomContainer) != (value.container != null) || (thing is Spawner) != (value.spawner != null)
                || (thing is ZoneCore) != (value.zone != null) || (thing is GenerationActionWorker) != (value.generation != null))
                throw new InvalidDataException("CQF_AI_InvalidValue: live feature type");
            int[] indices = thing is ThingWithComps withComps ? withComps.AllComps.Select((comp, index) => new { comp, index })
                .Where(pair => pair.comp is CompActionWorker).Select(pair => pair.index).ToArray() : Array.Empty<int>();
            if (value.actionWorkers == null || value.actionWorkers.Any(worker => worker == null)
                || !indices.SequenceEqual(value.actionWorkers.Select(worker => worker.componentIndex)))
                throw new InvalidDataException("CQF_AI_InvalidValue: action worker component indices are fixed");
            CQFAILiveFeatureValidation.Validate(value);
            int[] textIndices = thing is ThingWithComps textThing ? textThing.AllComps.Select((comp, index) => new { comp, index })
                .Where(pair => pair.comp is CompCustomText).Select(pair => pair.index).ToArray() : Array.Empty<int>();
            if (!textIndices.SequenceEqual(value.customTexts.Select(text => text.componentIndex)))
                throw new InvalidDataException("CQF_AI_InvalidValue: custom text component indices are fixed");
            if (value.extraInteractions.Count > 0 && thing.Map?.GetComponent<MapComponent_CustomMapData>() == null)
                throw new InvalidDataException("CQF_AI_MissingResource: extra interactions require a spawned map object");
            foreach (CQFAIActionWorkerConfiguration worker in value.actionWorkers)
                foreach (ActionComp comp in worker.comps)
                    if (comp.mode is not (ActionTriggerMode.None or ActionTriggerMode.Signal or ActionTriggerMode.Damaged or ActionTriggerMode.Tick
                        or ActionTriggerMode.Destroy or ActionTriggerMode.Spawn or ActionTriggerMode.MapGeneration)
                        && !(thing is LootBox && comp.mode == ActionTriggerMode.Open))
                        throw new InvalidDataException("CQF_AI_InvalidValue: unsupported action worker trigger for this Thing");
        }
        public static void Apply(Thing thing, CQFAILiveThingConfiguration value)
        {
            Validate(thing, value);
            if (thing is LootBox box && value.loot is CQFAILootConfiguration loot)
            {
                CQFAIModel model = new CQFAIModel();
                if (box.lootDef != loot.lootDef || !XNode.DeepEquals(model.Write(box.loots), model.Write(loot.loots))) LootCache.SetValue(box, null);
                box.lootBoxName = loot.lootBoxName; box.tickToOpen = loot.tickToOpen; box.openReport = loot.openReport;
                box.destroyAfterOpening = loot.destroyAfterOpening; box.useLootDef = loot.useLootDef; box.openWhenDestroyed = loot.openWhenDestroyed;
                box.loots = loot.loots; box.lootDef = loot.lootDef!;
            }
            if (thing is CustomTrap trap && value.trap is CQFAITrapConfiguration trapValue)
            {
                trap.trapName = trapValue.trapName; trap.trapComps = trapValue.trapComps;
                if (trap is CustomTrap_Capture capture && trapValue.capture is CQFAICaptureTrapConfiguration captureValue)
                {
                    capture.disarmReport = captureValue.disarmReport; capture.tickToDisarm = captureValue.tickToDisarm; capture.disarmActions = captureValue.disarmActions;
                }
            }
            if (thing is CustomDoor door && value.door is CQFAIDoorConfiguration doorValue)
            { door.openingActions = doorValue.openingActions; door.openingConditions = doorValue.openingConditions; }
            if (thing is CustomContainer container && value.container is CQFAIContainerConfiguration containerValue)
            {
                container.tickToOpen = containerValue.tickToOpen; container.innerThings = containerValue.innerThings;
                container.openingActions = containerValue.openingActions; container.openingConditions = containerValue.openingConditions;
            }
            if (thing is Spawner spawner && value.spawner is CQFAISpawnerConfiguration spawnerValue) spawner.pawns = spawnerValue.pawns;
            if (thing is ZoneCore zone && value.zone is CQFAIZoneConfiguration zoneValue)
            {
                zone.generationKey = zoneValue.generationKey!; zone.coreRotation = zoneValue.coreRotation; zone.size = zoneValue.size;
                zone.prohibitRotatingDocking = zoneValue.prohibitRotatingDocking; zone.prohibitFlippingDocking = zoneValue.prohibitFlippingDocking;
                zone.isCenter = zoneValue.isCenter; zone.destroyThings = zoneValue.destroyThings; zone.reserveThing = zoneValue.reserveThing!;
                zone.conditions = zoneValue.conditions; zone.coreTags = zoneValue.coreTags;
            }
            if (thing is GenerationActionWorker generation && value.generation != null) generation.actions = value.generation.actions;
            MapComponent_CustomMapData? component = thing.Map?.GetComponent<MapComponent_CustomMapData>();
            if (component != null)
            {
                if (value.extraInteractions.Count == 0) component.ExtraOperations.Remove(thing);
                else component.ExtraOperations[thing] = value.extraInteractions;
            }
            if (thing is ThingWithComps textThing)
                foreach (CQFAICustomTextConfiguration data in value.customTexts)
                {
                    CompCustomText text = (CompCustomText)textThing.AllComps[data.componentIndex];
                    text.useCustomName = data.useCustomName; text.useCustomDescription = data.useCustomDescription; text.useCustomInspectText = data.useCustomInspectText;
                    text.customName = data.customName!; text.customDescription = data.customDescription!; text.customInspectText = data.customInspectText!;
                }
            if (thing is ThingWithComps withComps)
                foreach (CQFAIActionWorkerConfiguration worker in value.actionWorkers) ((CompActionWorker)withComps.AllComps[worker.componentIndex]).comps = worker.comps;
        }
        public static LootData? CaptureLoot(Thing thing) => thing is LootBox ? (LootData?)LootCache.GetValue(thing) : null;
        public static void RestoreLoot(Thing thing, LootData? value) { if (thing is LootBox) LootCache.SetValue(thing, value); }
        private static readonly FieldInfo LootCache = typeof(LootBox).GetField("innerLoot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(LootBox).FullName, "innerLoot");
    }
}
