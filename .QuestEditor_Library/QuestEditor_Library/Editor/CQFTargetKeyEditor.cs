using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFTargetKeyEditor
    {
        public static void OpenBook()
        {
            if (Find.WindowStack.WindowOfType<Window_CQFTargetKeyBook>() == null)
            {
                Find.WindowStack.Add(new Window_CQFTargetKeyBook());
            }
        }

        public static void DrawBookField(float y, string label, string? value, Action<string> selected, float x, float fieldWidth, float width)
        {
            float labelWidth = Mathf.Min(Text.CalcSize(label).x + 2f, Mathf.Max(100f, width * 0.45f));
            DrawBookField(new Rect(x, y, labelWidth, 25f),
                new Rect(x + labelWidth + 5f, y, Mathf.Max(60f, Mathf.Min(fieldWidth, width - labelWidth - 5f)), 25f),
                label, value, selected);
        }

        public static void DrawBookField(Rect labelRect, Rect fieldRect, string label, string? value, Action<string> selected, bool allowContextKeys = true)
        {
            if (Widgets.ButtonText(labelRect, label, drawBackground: false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                ShowMenu(selected, allowContextKeys);
            }
            TooltipHandler.TipRegion(labelRect, "CQF_TargetKeyBook_SelectKey".Translate());
            string previous = value ?? string.Empty;
            string edited = Widgets.TextField(fieldRect, previous);
            if (edited != previous)
            {
                selected(edited);
            }
        }

        public static void ShowMenu(Action<string> selected, bool allowContextKeys = true)
        {
            List<FloatMenuOption> options = CQFTargetKeyBook.Keys
                .Where(key => allowContextKeys || !CQFEditorTools.TargetTexts.Contains(key))
                .Select(key => new FloatMenuOption(key, () => selected(key))).ToList();
            if (allowContextKeys)
            {
                foreach (string key in CQFEditorTools.TargetTexts.Where(key => !CQFTargetKeyBook.Keys.Contains(key)))
                {
                    options.Add(new FloatMenuOption(key.Translate() + " [" + key + "]", () => selected(key)));
                }
            }
            options.Add(new FloatMenuOption("Remove".Translate(), () => selected(string.Empty)));
            options.Add(new FloatMenuOption("CQF_TargetKeyBook_Title".Translate(), OpenBook));
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
