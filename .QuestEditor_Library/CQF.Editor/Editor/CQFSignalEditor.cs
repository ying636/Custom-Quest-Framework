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
        public static void OpenBook()
        {
            Window_CQFSignalBook? window = Find.WindowStack.WindowOfType<Window_CQFSignalBook>();
            if (window == null)
            {
                Find.WindowStack.Add(new Window_CQFSignalBook());
            }
        }

        public static void DrawBookField(Rect rect, string? value, Action<string> selected)
        {
            DrawBookField(rect.y, "Signal".Translate(), value, selected, rect.x, rect.width, rect.width);
        }

        public static void DrawBookField(float y, string label, string? value, Action<string> selected, float x, float fieldWidth, float width)
        {
            float labelWidth = Mathf.Min(Text.CalcSize(label).x + 2f, Mathf.Max(100f, width * 0.45f));
            DrawBookField(new Rect(x, y, labelWidth, 25f),
                new Rect(x + labelWidth + 5f, y, Mathf.Max(60f, Mathf.Min(fieldWidth, width - labelWidth - 5f)), 25f),
                label, value, selected);
        }

        public static void DrawBookField(Rect labelRect, Rect fieldRect, string label, string? value, Action<string> selected)
        {
            if (Widgets.ButtonText(labelRect, label, drawBackground: false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                List<FloatMenuOption> options = CQFSignalBook.Signals
                    .Select(signal => new FloatMenuOption(signal, () => selected(signal))).ToList();
                options.Add(new FloatMenuOption("Remove".Translate(), () => selected(string.Empty)));
                options.Add(new FloatMenuOption("CQF_SignalBook_Title".Translate(), OpenBook));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(labelRect, "CQF_SignalBook_SelectSignal".Translate());
            string previous = value ?? string.Empty;
            string edited = Widgets.TextField(fieldRect, previous);
            if (edited != previous)
            {
                selected(edited);
            }
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
