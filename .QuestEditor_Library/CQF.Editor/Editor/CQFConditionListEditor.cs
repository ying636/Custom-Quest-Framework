using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFConditionListEditor
    {
        public static void Draw(ref float y, float x, float width, Rect inRect, string title, List<DialogCondition> conditions, Action addAction = null)
        {
            width = Mathf.Max(180f, width);
            Widgets.LabelEllipses(new Rect(x, y, width - 70f, 28f), title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(x + width - 58f, y, 25f, 25f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                if (addAction != null)
                {
                    addAction();
                }
                else
                {
                    Select(condition =>
                    {
                        conditions.Add(condition);
                        expanded.Add(condition, condition);
                    });
                }
            }
            TooltipHandler.TipRegion(addRect, "Add".Translate());
            Rect pasteRect = new Rect(x + width - 28f, y, 25f, 25f);
            if (Button(pasteRect, TexButton.Paste, "Paste", copiedCondition != null))
            {
                DialogCondition condition = copiedCondition.Copy();
                conditions.Add(condition);
                expanded.Add(condition, condition);
            }
            y += 34f;
            Action mutation = null;
            for (int i = 0; i < conditions.Count; i++)
            {
                int index = i;
                DialogCondition condition = conditions[i];
                float startY = y;
                DrawHeader(ref y, x, width, condition, i + 1, () =>
                {
                    Rect button = new Rect(x + width - 116f, startY + 3f, 23f, 23f);
                    if (Button(button, TexButton.ReorderUp, "CQF_EditorMoveUp", index > 0))
                    {
                        mutation = () => Move(conditions, index, index - 1);
                    }
                    button.x += 28f;
                    if (Button(button, TexButton.ReorderDown, "CQF_EditorMoveDown", index + 1 < conditions.Count))
                    {
                        mutation = () => Move(conditions, index, index + 1);
                    }
                    button.x += 28f;
                    if (Button(button, TexButton.Copy, "Copy", true))
                    {
                        copiedCondition = condition.Copy();
                    }
                    button.x += 28f;
                    if (Button(button, TexButton.Delete, "Remove", true))
                    {
                        mutation = () => conditions.RemoveAt(index);
                    }
                });
                DrawBody(ref y, x, width, inRect, condition);
                Widgets.DrawBox(new Rect(x, startY, width, y - startY), 1, QuestEditor_Dialog.blueTex);
                y += 10f;
            }
            mutation?.Invoke();
            if (conditions.Count == 0)
            {
                Widgets.Label(new Rect(x + 8f, y, width - 16f, 25f), "CQF_NoResultConditions".Translate().Colorize(Color.gray));
                y += 30f;
            }
            y += 6f;
        }

        private static void DrawHeader(ref float y, float x, float width, DialogCondition condition, int index, Action drawButtons)
        {
            Widgets.DrawHighlight(new Rect(x, y, width, 30f));
            bool open = expanded.TryGetValue(condition, out _);
            if (Widgets.ButtonText(new Rect(x + 2f, y + 3f, 22f, 22f), open ? "−" : "+", false))
            {
                if (open)
                {
                    expanded.Remove(condition);
                }
                else
                {
                    expanded.Add(condition, condition);
                }
            }
            Rect labelRect = new Rect(x + 30f, y + 3f, width - (drawButtons == null ? 38f : 150f), 25f);
            string label = (index > 0 ? index + ". " : string.Empty) + condition.GetType().Name.Translate();
            Widgets.LabelEllipses(labelRect, label);
            TooltipHandler.TipRegion(labelRect, CQFEditorEntrySummary.Describe(condition));
            drawButtons?.Invoke();
            y += 32f;
            string details = CQFEditorEntrySummary.Details(condition);
            if (!details.NullOrEmpty())
            {
                Rect summaryRect = new Rect(x + 12f, y, width - 20f, 25f);
                Widgets.LabelEllipses(summaryRect, details.Colorize(Color.gray));
                TooltipHandler.TipRegion(summaryRect, details);
                y += 28f;
            }
        }

        private static void DrawBody(ref float y, float x, float width, Rect inRect, DialogCondition condition)
        {
            if (!expanded.TryGetValue(condition, out _))
            {
                return;
            }
            CQFEditorInlineLayout.Draw(condition, ref y, x + 12f, width - 24f, rect =>
            {
                float contentY = 0f;
                if (condition is DialogCondition_WithSubConditions composite)
                {
                    CQFEditorTools.DrawLabelAndText_Line(contentY, "CQFFailReason".Translate(), ref condition.failReason, 0f, 100f);
                    contentY += 32f;
                    if (composite.AllowMultipleChildConditions)
                    {
                        Draw(ref contentY, 0f, rect.width, rect, "CQF_SubConditions".Translate(), composite.ChildConditions);
                    }
                    else
                    {
                        DrawSingleChild(ref contentY, 0f, rect.width, rect, composite);
                    }
                }
                else
                {
                    condition.Draw(ref contentY, rect, 0f);
                }
                return contentY;
            });
            y += 5f;
        }

        private static void DrawSingleChild(ref float y, float x, float width, Rect inRect, DialogCondition_WithSubConditions parent)
        {
            if (Widgets.ButtonText(new Rect(x, y, width - 34f, 28f), "CQF_SelectDialogCondition".Translate()))
            {
                Select(condition =>
                {
                    parent.ChildCondition = condition;
                    expanded.Add(condition, condition);
                });
            }
            Rect pasteRect = new Rect(x + width - 28f, y, 25f, 25f);
            if (Button(pasteRect, TexButton.Paste, "Paste", copiedCondition != null))
            {
                parent.ChildCondition = copiedCondition.Copy();
                expanded.Add(parent.ChildCondition, parent.ChildCondition);
            }
            y += 34f;
            if (parent.ChildCondition != null)
            {
                DrawHeader(ref y, x, width, parent.ChildCondition, 0, null);
                DrawBody(ref y, x, width, inRect, parent.ChildCondition);
            }
        }

        private static void Select(Action<DialogCondition> accept)
        {
            Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(),
                type => type.Name.Translate(), type => accept((DialogCondition)Activator.CreateInstance(type)),
                null, type => (type.Name + "_Tip").CanTranslate() ? (type.Name + "_Tip").Translate().ToString() : string.Empty,
                null, type => type.Name, null, null), "CQF_SelectDialogCondition".Translate()));
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

        private static void Move(List<DialogCondition> conditions, int from, int to)
        {
            DialogCondition condition = conditions[from];
            conditions.RemoveAt(from);
            conditions.Insert(to, condition);
        }

        private static DialogCondition copiedCondition;
        private static readonly ConditionalWeakTable<DialogCondition, object> expanded = new ConditionalWeakTable<DialogCondition, object>();
    }
}
