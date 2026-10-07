using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void OpenSelector(string? value, Action<string> selected, bool allowEmpty = true)
        {
            Find.WindowStack.Add(new Dialog_CQFSignalBookSelect(value, selected, allowEmpty));
        }

        public static void DrawBookField(Rect rect, string? value, Action<string> selected)
        {
            DrawBookField(rect.y, "Signal".Translate(), value, selected, rect.x, rect.width, rect.width);
        }

        public static void DrawBookField(float y, string label, string? value, Action<string> selected, float x, float fieldWidth, float width)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(width);
            float labelWidth = Mathf.Min(160f, Mathf.Max(20f, width * 0.45f));
            DrawBookField(new Rect(x, y, labelWidth, 25f),
                new Rect(x + labelWidth + 5f, y, Mathf.Max(20f, Mathf.Min(fieldWidth, width - labelWidth - 5f)), 25f),
                label, value, selected);
        }

        public static void DrawBookField(Rect labelRect, Rect fieldRect, string label, string? value, Action<string> selected)
        {
            if (CQFUIStyle.ButtonText(labelRect, label, drawBackground: false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                OpenSelector(value, selected);
            }
            TooltipHandler.TipRegion(labelRect, "CQF_SignalBook_SelectSignal".Translate());
            string previous = value ?? string.Empty;
            string edited = Widgets.TextField(fieldRect, previous);
            if (edited != previous)
            {
                selected(edited);
            }
        }

        public static void DrawInteractionSignal(ref float y, Rect inRect, float x, InteractionOperation operation)
        {
            Map? map = CQFEditorContext.Map;
            float left = x + 8f;
            float width = Mathf.Max(80f, inRect.width - left - 12f);
            float labelWidth = Mathf.Min(160f, width * 0.45f);
            Rect label = new Rect(left, y, labelWidth, 32f);
            Rect field = new Rect(label.xMax + 8f, y, width - labelWidth - 8f, 32f);
            bool open = CQFUIStyle.ButtonText(label, "OutSignal".Translate(), false, overrideTextAnchor: TextAnchor.MiddleLeft);
            open |= CQFUIStyle.ButtonText(field, operation.interactionText ?? "Select".Translate().ToString(), overrideTextAnchor: TextAnchor.MiddleLeft);
            if (open) OpenSelector(operation.interactionText, value => RenameInteraction(operation, value, map), false);
            TooltipHandler.TipRegion(label, "CQF_SignalBook_SelectSignal".Translate());
            TooltipHandler.TipRegion(field, "CQF_MapSignals_InteractionInputHint".Translate());
            y += 40f;
        }

        public static void RenameInteraction(InteractionOperation operation, string newName, Map? map = null)
        {
            if (newName == operation.interactionText)
            {
                return;
            }
            map ??= CQFEditorContext.Map;
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

    }
}
