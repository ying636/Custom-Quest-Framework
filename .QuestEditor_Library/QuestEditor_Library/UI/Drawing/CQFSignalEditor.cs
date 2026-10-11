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
            float labelWidth = Mathf.Min(160f, rect.width * 0.45f);
            DrawBookField(new Rect(rect.x, rect.y, labelWidth, rect.height), new Rect(rect.x + labelWidth + 8f, rect.y, rect.width - labelWidth - 8f, rect.height), "Signal".Translate(), value, selected);
        }

        public static void DrawBookField(float y, string label, string? value, Action<string> selected, float x, float width)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(width);
            float labelWidth = Mathf.Min(160f, Mathf.Max(20f, width * 0.45f));
            DrawBookField(new Rect(x, y, labelWidth, 25f), new Rect(x + labelWidth + 8f, y, Mathf.Max(20f, width - labelWidth - 8f), 25f), label, value, selected);
        }

        public static void DrawBookField(Rect labelRect, Rect fieldRect, string label, string? value, Action<string> selected, Action<string>? picked = null, bool allowEmpty = true)
        {
            using CQFUIScope scope = new CQFUIScope();
            GUI.color = CQFUIStyle.TextColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            if (CQFUIStyle.ButtonText(labelRect, label, false, true, CQFUIStyle.Accent, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                OpenSelector(value, picked ?? selected, allowEmpty);
            }

            TooltipHandler.TipRegion(labelRect, label + "\n" + "CQF_SignalBook_SelectSignal".Translate());
            string previous = value ?? string.Empty;
            string edited = Widgets.TextField(fieldRect, previous);
            if (edited != previous)
            {
                selected(edited);
            }
        }

        public static void DrawBookList(ref float y, string label, IEnumerable<string>? values, Action<List<string>> selected, float x, float width)
        {
            using CQFUIScope scope = new CQFUIScope(width);
            GUI.color = CQFUIStyle.TextColor;
            List<string> signals = values?.ToList() ?? new List<string>();
            Rect header = new Rect(x, y, width, 30f);
            Widgets.DrawBoxSolid(header, CQFUIStyle.Header);
            CQFUIStyle.DrawBox(header);
            if (CQFUIStyle.ButtonText(new Rect(x + 4f, y, width - 40f, 30f), label, false, overrideTextAnchor: TextAnchor.MiddleLeft))
                OpenSelector(string.Empty, value => selected(signals.Concat(new[] { value }).ToList()), false);
            TooltipHandler.TipRegion(new Rect(x + 4f, y, width - 40f, 30f), label + "\n" + "CQF_SignalBook_SelectSignal".Translate());
            if (CQFUIStyle.ButtonImage(new Rect(header.xMax - 30f, y + 2f, 26f, 26f), TexButton.Plus, tooltip: "Add".Translate()))
            {
                signals.Add(string.Empty);
                selected(signals.ToList());
            }
            y += 38f;
            int remove = -1;
            for (int index = 0; index < signals.Count; index++)
            {
                int captured = index;
                float labelWidth = Mathf.Min(160f, width * 0.45f);
                DrawBookField(new Rect(x, y, labelWidth, 25f), new Rect(x + labelWidth + 8f, y, width - labelWidth - 42f, 25f), "Signal".Translate(), signals[index], value =>
                {
                    signals[captured] = value;
                    selected(signals.ToList());
                });
                if (CQFUIStyle.ButtonImage(new Rect(x + width - 26f, y, 25f, 25f), TexButton.Delete, tooltip: "Remove".Translate()))
                    remove = index;
                y += 32f;
            }
            if (remove >= 0)
            {
                signals.RemoveAt(remove);
                selected(signals);
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
            DrawBookField(label, field, "OutSignal".Translate(), operation.outputSignal, value => operation.outputSignal = value, picked: value => RenameInteractionSignal(operation, value, map), allowEmpty: false);
            y += 40f;
        }

        public static void RenameInteraction(InteractionOperation operation, string newName, Map? map = null)
        {
            operation.outputSignal = operation.OutputSignal;
            operation.interactionText = newName;
        }

        public static void RenameInteractionSignal(InteractionOperation operation, string newName, Map? map = null)
        {
            if (newName == operation.OutputSignal)
            {
                operation.outputSignal = newName;
                return;
            }

            map ??= CQFEditorContext.Map;
            CustomMapDataDef? definition = CQFEditorBridge.Module?.CurrentEditingMap;
            CQFSignalCatalog catalog = CQFSignalCatalog.Build(map, definition);
            List<CQFSignalEndpoint> references = catalog.ReferencesTo(operation);
            if (references.Count == 0)
            {
                operation.outputSignal = newName;
                return;
            }

            List<CQFSignalReferenceChange> changes = catalog.BuildRenameChanges(operation, newName);
            Find.WindowStack.Add(new Dialog_CQFSignalRename(operation, newName, references, changes));
        }
    }
}
