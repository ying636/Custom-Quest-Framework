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
            DrawBookField(y, label, value, selected, x, fieldWidth, width, false);
        }

        public static void DrawBookField(float y, string label, string? value, Action<string> selected, float x, float fieldWidth, float width, bool allowEntityReferences)
        {
            float labelWidth = Mathf.Min(Text.CalcSize(label).x + 20f, Mathf.Max(100f, width * 0.45f));
            DrawBookField(new Rect(x, y, labelWidth, 25f), new Rect(x + labelWidth + 5f, y, Mathf.Max(60f, Mathf.Min(fieldWidth, width - labelWidth - 5f)), 25f), label, value, selected, true, allowEntityReferences);
        }

        public static void DrawBookField(Rect labelRect, Rect fieldRect, string label, string? value, Action<string> selected, bool allowContextKeys = true)
        {
            DrawBookField(labelRect, fieldRect, label, value, selected, allowContextKeys, false);
        }

        public static void DrawBookField(Rect labelRect, Rect fieldRect, string label, string? value, Action<string> selected, bool allowContextKeys, bool allowEntityReferences)
        {
            if (CQFUIStyle.ButtonText(labelRect, label, drawBackground: false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                ShowMenu(selected, allowContextKeys, allowEntityReferences);
            }

            TooltipHandler.TipRegion(labelRect, "CQF_TargetKeyBook_SelectKey".Translate());
            string previous = value ?? string.Empty;
            if (allowEntityReferences && CQFEntityReference.IsReference(previous))
            {
                if (CQFUIStyle.ButtonText(fieldRect, CQFEntityBookBridge.DisplayReference(previous), overrideTextAnchor: TextAnchor.MiddleLeft))
                {
                    Map entityMap = CQFEditorContext.Map;
                    CQFEntityBookBridge.OpenReferences(selected, quest: entityMap == null ? null : GameTools.GetQuestFromMap(entityMap));
                }
                return;
            }
            string edited = Widgets.TextField(fieldRect, previous);
            if (edited != previous)
            {
                selected(edited);
            }
        }

        public static void ShowMenu(Action<string> selected, bool allowContextKeys = true)
        {
            ShowMenu(selected, allowContextKeys, false);
        }

        public static void ShowMenu(Action<string> selected, bool allowContextKeys, bool allowEntityReferences)
        {
            List<FloatMenuOption> options = CQFTargetKeyBook.Keys.Where(key => allowContextKeys || !CQFTargetNames.Reserved.Contains(key)).Select(key => new FloatMenuOption(key, () => selected(key))).ToList();
            if (allowEntityReferences)
            {
                Map entityMap = CQFEditorContext.Map;
                var entityQuest = entityMap == null ? null : GameTools.GetQuestFromMap(entityMap);
                options.Insert(0, new FloatMenuOption("CQF_EntityBook_Title".Translate(), () => CQFEntityBookBridge.OpenReferences(selected, quest: entityQuest)));
            }
            if (allowContextKeys)
            {
                foreach (string key in CQFTargetNames.Reserved.Where(key => !CQFTargetKeyBook.Keys.Contains(key)))
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
