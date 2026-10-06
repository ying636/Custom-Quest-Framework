using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFActionListEditor
    {
        public static void Draw(ref float y, float x, float width, Rect inRect, string title, List<CQFAction> actions, string tip = null)
        {
            width = Mathf.Max(180f, width);
            Rect titleRect = new Rect(x, y, width - 70f, 28f);
            Widgets.LabelEllipses(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(titleRect, tip);
            }
            Rect addRect = new Rect(x + width - 58f, y, 25f, 25f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                CQFEditorTools.OpenCQFActionSelect(type =>
                {
                    CQFAction action = (CQFAction)Activator.CreateInstance(type);
                    actions.Add(action);
                    expanded.Add(action, action);
                });
            }
            TooltipHandler.TipRegion(addRect, "Add".Translate());
            Rect pasteRect = new Rect(x + width - 28f, y, 25f, 25f);
            if (Button(pasteRect, TexButton.Paste, "Paste", copiedAction != null))
            {
                CQFAction action = copiedAction.Copy();
                actions.Add(action);
                expanded.Add(action, action);
            }
            y += 34f;
            Action mutation = null;
            for (int i = 0; i < actions.Count; i++)
            {
                int index = i;
                CQFAction action = actions[i];
                float startY = y;
                Rect row = new Rect(x, y, width, 30f);
                Widgets.DrawHighlight(row);
                bool open = expanded.TryGetValue(action, out _);
                Rect foldRect = new Rect(x + 2f, y + 3f, 22f, 22f);
                if (Widgets.ButtonText(foldRect, open ? "−" : "+", false))
                {
                    if (open)
                    {
                        expanded.Remove(action);
                    }
                    else
                    {
                        expanded.Add(action, action);
                    }
                    open = !open;
                }
                Rect labelRect = new Rect(x + 30f, y + 3f, width - 150f, 25f);
                string label = (i + 1) + ". " + action.GetType().Name.Translate();
                Widgets.LabelEllipses(labelRect, label);
                TooltipHandler.TipRegion(labelRect, CQFEditorEntrySummary.Describe(action));
                Rect button = new Rect(x + width - 116f, y + 3f, 23f, 23f);
                if (Button(button, TexButton.ReorderUp, "CQF_EditorMoveUp", i > 0))
                {
                    mutation = () => Move(actions, index, index - 1);
                }
                button.x += 28f;
                if (Button(button, TexButton.ReorderDown, "CQF_EditorMoveDown", i + 1 < actions.Count))
                {
                    mutation = () => Move(actions, index, index + 1);
                }
                button.x += 28f;
                if (Button(button, TexButton.Copy, "Copy", true))
                {
                    copiedAction = action.Copy();
                }
                button.x += 28f;
                if (Button(button, TexButton.Delete, "Remove", true))
                {
                    mutation = () => actions.RemoveAt(index);
                }
                y += 32f;
                string details = CQFEditorEntrySummary.Details(action);
                if (!details.NullOrEmpty())
                {
                    Rect summaryRect = new Rect(x + 12f, y, width - 20f, 25f);
                    Widgets.LabelEllipses(summaryRect, details.Colorize(Color.gray));
                    TooltipHandler.TipRegion(summaryRect, details);
                    y += 28f;
                }
                if (open)
                {
                    CQFEditorInlineLayout.Draw(action, ref y, x + 12f, width - 24f, rect =>
                    {
                        float contentY = 0f;
                        action.Draw(ref contentY, rect, 0f);
                        return contentY;
                    });
                    y += 5f;
                }
                Widgets.DrawBox(new Rect(x, startY, width, y - startY), 1, QuestEditor_Dialog.blueTex);
                y += 10f;
            }
            mutation?.Invoke();
            if (actions.Count == 0)
            {
                Widgets.Label(new Rect(x + 8f, y, width - 16f, 25f), "CQF_NoResultActions".Translate().Colorize(Color.gray));
                y += 30f;
            }
            y += 6f;
        }

        private static bool Button(Rect rect, Texture2D texture, string key, bool enabled)
        {
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            bool clicked = Widgets.ButtonImage(rect, texture);
            GUI.enabled = previous;
            TooltipHandler.TipRegion(rect, key.Translate());
            return clicked;
        }

        private static void Move(List<CQFAction> actions, int from, int to)
        {
            CQFAction action = actions[from];
            actions.RemoveAt(from);
            actions.Insert(to, action);
        }

        private static CQFAction copiedAction;
        private static readonly ConditionalWeakTable<CQFAction, object> expanded = new ConditionalWeakTable<CQFAction, object>();
    }
}
