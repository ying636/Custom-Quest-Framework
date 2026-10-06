using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFSignalCatalog
    {
        private CQFSignalCatalog(Map? map, CustomMapDataDef? definition)
        {
            this.Map = map;
            this.Definition = definition;
        }

        public Map? Map { get; }
        public CustomMapDataDef? Definition { get; }
        public IReadOnlyList<CQFSignalEndpoint> Entries => this.entries;

        public static CQFSignalCatalog Build(Map? map, CustomMapDataDef? definition = null)
        {
            CQFSignalCatalog result = new CQFSignalCatalog(map, definition);
            if (map != null)
            {
                foreach (Thing thing in map.listerThings.AllThings)
                {
                    result.AddThing(thing);
                }
                MapComponent_CustomMapData component = map.GetComponent<MapComponent_CustomMapData>();
                if (component != null)
                {
                    foreach (KeyValuePair<Thing, List<InteractionOperation>> pair in component.ExtraOperations)
                    {
                        if (pair.Key?.Spawned == true)
                        {
                            result.AddOperations(pair.Value, pair.Key, pair.Key.LabelCap, map.Parent is EditorMapObject, "");
                        }
                    }
                    foreach (ThingActionTrigger trigger in component.Triggers)
                    {
                        result.AddActions(trigger.actions, trigger.things?.FirstOrDefault(), trigger.key, map.Parent is EditorMapObject, "", false, "");
                    }
                    foreach (CQFEventArea area in component.EventAreas)
                    {
                        result.AddActions(area.actions, null, area.key, map.Parent is EditorMapObject, "", false, "");
                    }
                }
            }
            if (definition != null)
            {
                foreach (CustomThingData data in definition.customThings)
                {
                    result.AddData(data, definition.defName);
                }
                foreach (GenerationAction action in definition.generationActions)
                {
                    result.AddActions(action.actions, null, definition.defName + " " + action.pos, true, "", false, definition.defName);
                }
            }
            return result;
        }

        public CQFSignalEndpoint? FindOwner(object owner)
        {
            return this.entries.FirstOrDefault(entry => ReferenceEquals(entry.Owner, owner));
        }

        public List<CQFSignalEndpoint> ReferencesTo(InteractionOperation operation)
        {
            List<CQFSignalEndpoint> signals = this.entries.Where(entry => ReferenceEquals(entry.Owner, operation) && entry.IsAutomatic).ToList();
            return this.entries.Where(entry => entry.IsReceiver && signals.Any(signal => signal.Matches(entry))).ToList();
        }

        public List<CQFSignalReferenceChange> BuildRenameChanges(InteractionOperation operation, string newName)
        {
            List<CQFSignalReferenceChange> changes = new List<CQFSignalReferenceChange>();
            foreach (CQFSignalEndpoint reference in this.ReferencesTo(operation))
            {
                if (changes.Any(change => ReferenceEquals(change.Owner, reference.Owner))
                    || this.entries.Any(entry => !entry.IsReceiver && entry.Matches(reference) && !ReferenceEquals(entry.Owner, operation)))
                {
                    continue;
                }
                string oldName = operation.interactionText;
                if (oldName.NullOrEmpty() || !reference.Signal.EndsWith(oldName, StringComparison.Ordinal))
                {
                    continue;
                }
                string replacement = reference.Signal.Substring(0, reference.Signal.Length - oldName.Length) + newName;
                changes.Add(new CQFSignalReferenceChange(reference.Owner, reference.Signal, replacement, reference.SourceLabel));
            }
            return changes;
        }

        private void AddThing(Thing thing)
        {
            bool template = thing.Map?.Parent is EditorMapObject;
            string source = thing.LabelCap + " " + thing.Position;
            CompActionWorker? worker = thing.TryGetComp<CompActionWorker>();
            if (worker != null)
            {
                this.AddComps(worker.comps, thing, source, template, "");
            }
            if (thing is CustomTrap trap)
            {
                this.AddTraps(trap.trapComps, trap.trapName, thing, source, template, "");
                if (trap is CustomTrap_Capture capture)
                {
                    this.AddActions(capture.disarmActions, thing, source, template, this.TrapPrefix(trap.trapName, template), null, "");
                }
            }
            if (thing is InteractableThing interactable)
            {
                this.AddOperations(interactable.operations, thing, source, template, "");
                foreach (InteractionDataDef operationDef in interactable.operationDefs)
                {
                    this.AddOperations(operationDef.interactions, thing, source + " / " + operationDef.defName, template, "");
                }
            }
            if (thing is GenerationActionWorker generator)
            {
                this.AddActions(generator.actions, thing, source, template, "", false, "");
            }
            if (thing is LootBox box)
            {
                this.AddAutomatic(box, "Opened", thing, source, template, template ? box.lootBoxName + "." : "");
                foreach (LootData loot in box.loots)
                {
                    this.AddAutomatic(loot, loot.dataName, thing, source, template, template ? box.lootBoxName + "." : "");
                }
                if (box.lootDef != null)
                {
                    foreach (LootData loot in box.lootDef.loots)
                    {
                        this.AddAutomatic(loot, loot.dataName, thing, source, template, template ? box.lootBoxName + "." : "");
                    }
                }
            }
        }

        private void AddData(CustomThingData data, string partName)
        {
            if (data == null)
            {
                return;
            }
            string source = partName + " / " + (data.customName ?? data.def?.label ?? data.GetType().Name) + " " + data.position;
            this.AddComps(data.comps, null, source, true, partName);
            if (data is CustomThingData_CustomTrap trap)
            {
                this.AddTraps(trap.trapComps, trap.trapName, null, source, true, partName);
                this.AddActions(trap.disarmActions, null, source, true, this.TrapPrefix(trap.trapName, true), null, partName);
            }
            if (data is CustomThingData_InteractableThing interactable)
            {
                this.AddOperations(interactable.operations, null, source, true, partName);
                foreach (InteractionDataDef operationDef in interactable.operationDefs)
                {
                    this.AddOperations(operationDef.interactions, null, source + " / " + operationDef.defName, true, partName);
                }
            }
            if (data is CustomThingData_LootBox box)
            {
                this.AddAutomatic(box, "Opened", null, source, true, box.lootBoxName + ".");
                foreach (LootData loot in box.loots)
                {
                    this.AddAutomatic(loot, loot.dataName, null, source, true, box.lootBoxName + ".");
                }
            }
        }

        private void AddComps(IEnumerable<ActionComp> comps, Thing? thing, string source, bool template, string partName)
        {
            foreach (ActionComp comp in comps ?? Enumerable.Empty<ActionComp>())
            {
                if (comp.mode == ActionTriggerMode.Signal)
                {
                    this.AddReceiver(comp, comp.signal, comp.signalIsOnlyValidInPart, thing, source + " / " + comp.compName, template, partName);
                }
                this.AddActions(comp.actions, thing, source + " / " + comp.compName, template, "", comp.signalIsOnlyValidInPart, partName);
            }
        }

        private void AddTraps(IEnumerable<TrapComp> comps, string trapName, Thing? thing, string source, bool template, string partName)
        {
            foreach (TrapComp comp in comps ?? Enumerable.Empty<TrapComp>())
            {
                if (comp.mode == ActionTriggerMode.Signal)
                {
                    this.AddReceiver(comp, comp.inSignal, comp.signalIsOnlyValidInPart, thing, source, template, partName);
                }
                this.AddActions(comp.actions, thing, source, template, this.TrapPrefix(trapName, template), null, partName);
            }
        }

        private void AddReceiver(object owner, string signal, bool partScoped, Thing? thing, string source, bool template, string partName)
        {
            this.entries.Add(new CQFSignalEndpoint(owner, signal, source, true, false, template,
                template || (signal?.StartsWith("Quest", StringComparison.Ordinal) == true), partScoped, thing, partName: partName));
        }

        private void AddOperations(IEnumerable<InteractionOperation> operations, Thing? thing, string source, bool template, string partName)
        {
            foreach (InteractionOperation operation in operations ?? Enumerable.Empty<InteractionOperation>())
            {
                this.AddAutomatic(operation, operation.interactionText, thing, source, template, "", partName);
                foreach (InteractionResult result in operation.results)
                {
                    this.AddActions(result.actions, thing, source + " / " + operation.interactionText + " / " + result.resultName, template, "", null, partName);
                }
            }
        }

        private void AddAutomatic(object owner, string signal, Thing? thing, string source, bool template, string prefix, string partName = "")
        {
            if (template)
            {
                this.entries.Add(new CQFSignalEndpoint(owner, prefix + signal, source, false, true, true, true, false, thing));
                string partSignal = "{" + (partName.NullOrEmpty() ? "part" : partName) + ":index}." + prefix + signal;
                this.entries.Add(new CQFSignalEndpoint(owner, partSignal, source, false, true, true, false, true, thing,
                    partName: partName, selectable: false, displaySignal: partSignal));
                return;
            }
            foreach (string tag in thing?.questTags ?? Enumerable.Empty<string>())
            {
                this.entries.Add(new CQFSignalEndpoint(owner, tag + "." + signal, source, false, true, false,
                    tag.StartsWith("Quest", StringComparison.Ordinal), false, thing));
            }
        }

        private void AddActions(IEnumerable<CQFAction> actions, Thing? thing, string source, bool template, string prefix, bool? partOverride, string partName)
        {
            foreach (CQFAction action in actions ?? Enumerable.Empty<CQFAction>())
            {
                this.AddAction(action, thing, source, template, prefix, partOverride, partName, new HashSet<CQFAction>());
            }
        }

        private void AddAction(CQFAction action, Thing? thing, string source, bool template, string prefix, bool? partOverride, string partName, HashSet<CQFAction> visited)
        {
            if (action == null || !visited.Add(action))
            {
                return;
            }
            if (action is CQFAction_SentSignal signal)
            {
                Quest? quest = template ? null : thing != null ? GameTools.GetQuestFromThing(thing) : this.Map != null ? GameTools.GetQuestFromMap(this.Map) : null;
                string runtimePrefix = signal.addQuestPrefix && quest != null ? "Quest" + quest.id + "." : "";
                string actualPrefix = template ? prefix : runtimePrefix;
                this.entries.Add(new CQFSignalEndpoint(signal, actualPrefix + signal.signal, source, false, false, template,
                    template ? signal.addQuestPrefix : runtimePrefix.Length > 0, template && (partOverride ?? signal.signalIsOnlyValidInPart), thing,
                    actualPrefix, partName, canChangePartScope: !partOverride.HasValue));
            }
            if (!actionFields.TryGetValue(action.GetType(), out FieldInfo[] fields))
            {
                fields = action.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(field => typeof(CQFAction).IsAssignableFrom(field.FieldType)
                        || typeof(IEnumerable<CQFAction>).IsAssignableFrom(field.FieldType)).ToArray();
                actionFields.Add(action.GetType(), fields);
            }
            foreach (FieldInfo field in fields)
            {
                object? value = field.GetValue(action);
                if (value is CQFAction child)
                {
                    this.AddAction(child, thing, source, template, "", false, partName, visited);
                }
                else if (value is IEnumerable<CQFAction> children)
                {
                    foreach (CQFAction nested in children)
                    {
                        this.AddAction(nested, thing, source, template, "", false, partName, visited);
                    }
                }
            }
        }

        private string TrapPrefix(string trapName, bool template)
        {
            return template && trapName != "undefined" ? trapName + "." : "";
        }

        private readonly List<CQFSignalEndpoint> entries = new List<CQFSignalEndpoint>();
        private static readonly Dictionary<Type, FieldInfo[]> actionFields = new Dictionary<Type, FieldInfo[]>();
    }
}
