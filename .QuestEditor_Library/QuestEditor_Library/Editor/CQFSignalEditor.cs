using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFSignalEditor
    {
        public static void DrawSignalLinks(ref float y, Rect inRect, float x, object owner)
        {
            if (Widgets.ButtonText(new Rect(x, y, Mathf.Max(180f, Mathf.Min(320f, inRect.width - x - 12f)), 28f), "CQF_MapSignals_Title".Translate()))
            {
                Map? map = CQFEditorContext.Map ?? Find.CurrentMap;
                CustomMapDataDef? definition = Find.WindowStack.WindowOfType<QuestEditor_SaveMapToFile>() == null ? null : QuestEditor_SaveMapToFile.def;
                Find.WindowStack.Add(new Dialog_CQFSignalSelector(owner, (signal, part, quest) =>
                {
                    if (owner is CQFAction_SentSignal action)
                    {
                        action.signal = signal;
                        action.signalIsOnlyValidInPart = part;
                        action.addQuestPrefix = quest;
                    }
                    else if (owner is TrapComp trap)
                    {
                        trap.inSignal = signal;
                        trap.signalIsOnlyValidInPart = part;
                    }
                    else if (owner is ActionComp comp)
                    {
                        comp.signal = signal;
                        comp.signalIsOnlyValidInPart = part;
                    }
                }, map, definition));
            }
            y += 34f;
        }

        public static void DrawInteractionSummary(ref float y, Rect inRect, float x, InteractionOperation operation)
        {
            Map? map = CQFEditorContext.Map ?? Find.CurrentMap;
            CQFInteractionSignalSummary summary = summaries.GetValue(operation, _ => new CQFInteractionSignalSummary());
            Thing? sourceThing = CQFEditorContext.SourceThing;
            if (summary.Dirty || summary.Map != map || summary.Name != operation.interactionText || summary.SourceThing != sourceThing)
            {
                if (sourceThing != null)
                {
                    summary.Signals = sourceThing.Map?.Parent is EditorMapObject
                        ? new List<string> { "Quest{id}." + operation.interactionText, "{part:index}." + operation.interactionText }
                        : (sourceThing.questTags ?? new List<string>()).Select(tag => tag + "." + operation.interactionText).ToList();
                }
                else
                {
                    CustomMapDataDef? definition = Find.WindowStack.WindowOfType<QuestEditor_SaveMapToFile>() == null ? null : QuestEditor_SaveMapToFile.def;
                    CQFSignalCatalog catalog = CQFSignalCatalog.Build(map, definition);
                    summary.Signals = catalog.Entries.Where(entry => ReferenceEquals(entry.Owner, operation) && entry.IsAutomatic)
                        .Select(entry => entry.DisplaySignal).Distinct().ToList();
                }
                summary.Map = map;
                summary.SourceThing = sourceThing;
                summary.Name = operation.interactionText;
                summary.Dirty = false;
            }
            string signals = summary.Signals.Count > 0 ? string.Join("\n", summary.Signals) : "CQF_MapSignals_Unknown".Translate().ToString();
            string text = "CQF_MapSignals_Automatic".Translate() + "\n" + signals;
            float width = Mathf.Max(100f, inRect.width - x - 20f);
            float height = Text.CalcHeight(text, width) + 8f;
            Widgets.Label(new Rect(x + 8f, y, width, height), text.Colorize(Color.gray));
            y += height;
            float buttonWidth = Mathf.Max(100f, Mathf.Min(320f, width - 112f));
            if (Widgets.ButtonText(new Rect(x + 8f, y, buttonWidth, 28f), "CQF_MapSignals_Title".Translate()))
            {
                CustomMapDataDef? definition = Find.WindowStack.WindowOfType<QuestEditor_SaveMapToFile>() == null ? null : QuestEditor_SaveMapToFile.def;
                Find.WindowStack.Add(new Dialog_CQFSignalSelector(null, null, map, definition));
            }
            if (Widgets.ButtonText(new Rect(x + 16f + buttonWidth, y, 96f, 28f), "CQF_MapSignals_Refresh".Translate()))
            {
                summary.Dirty = true;
            }
            y += 36f;
        }

        public static void InvalidateSummary(InteractionOperation operation)
        {
            summaries.Remove(operation);
        }

        public static void RenameInteraction(InteractionOperation operation, string newName)
        {
            if (newName == operation.interactionText)
            {
                return;
            }
            Map? map = summaries.TryGetValue(operation, out CQFInteractionSignalSummary summary) ? summary.Map : CQFEditorContext.Map ?? Find.CurrentMap;
            CustomMapDataDef? definition = Find.WindowStack.WindowOfType<QuestEditor_SaveMapToFile>() == null ? null : QuestEditor_SaveMapToFile.def;
            CQFSignalCatalog catalog = CQFSignalCatalog.Build(map, definition);
            List<CQFSignalEndpoint> references = catalog.ReferencesTo(operation);
            if (references.Count == 0)
            {
                operation.interactionText = newName;
                return;
            }
            List<CQFSignalReferenceChange> changes = catalog.BuildRenameChanges(operation, newName);
            Find.WindowStack.Add(new Dialog_CQFSignalRename(operation, newName, references, changes));
        }

        private static readonly ConditionalWeakTable<InteractionOperation, CQFInteractionSignalSummary> summaries = new ConditionalWeakTable<InteractionOperation, CQFInteractionSignalSummary>();
    }
}
