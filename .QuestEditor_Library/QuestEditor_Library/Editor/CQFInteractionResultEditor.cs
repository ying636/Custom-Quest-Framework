using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFInteractionResultEditor
    {
        public static void Draw(ref float y, float x, float width, Rect inRect, InteractionOperation operation)
        {
            Widgets.LabelEllipses(new Rect(x, y, width - 70f, 28f), "InteractionResults".Translate().Colorize(ColorLibrary.PaleBlue));
            Rect button = new Rect(x + width - 58f, y, 25f, 25f);
            if (Button(button, TexButton.Plus, "Add", true))
            {
                InteractionResult result = new InteractionResult();
                operation.results.Add(result);
                expanded.Add(result, result);
            }
            button.x += 30f;
            if (Button(button, TexButton.Paste, "Paste", copiedResult != null))
            {
                InteractionResult result = Copy(copiedResult);
                operation.results.Add(result);
                expanded.Add(result, result);
            }
            y += 36f;
            string modeKey = operation.onlyGenerateSingleResult ? "CQF_EditorFirstMatchingResult" : "CQF_EditorAllMatchingResults";
            if (Widgets.ButtonText(new Rect(x, y, width, 30f), modeKey.Translate()))
            {
                List<FloatMenuOption> modes = new List<FloatMenuOption>
                {
                    new FloatMenuOption("CQF_EditorFirstMatchingResult".Translate(), () => operation.onlyGenerateSingleResult = true),
                    new FloatMenuOption("CQF_EditorAllMatchingResults".Translate(), () => operation.onlyGenerateSingleResult = false)
                };
                Find.WindowStack.Add(new FloatMenu(modes));
            }
            y += 38f;
            bool unconditionalSeen = false;
            Action mutation = null;
            for (int i = 0; i < operation.results.Count; i++)
            {
                int index = i;
                InteractionResult result = operation.results[i];
                if (operation.onlyGenerateSingleResult && unconditionalSeen)
                {
                    string warning = "CQF_EditorUnreachableResult".Translate();
                    float height = Text.CalcHeight(warning, width - 12f);
                    Widgets.Label(new Rect(x + 6f, y, width - 12f, height), warning.Colorize(ColorLibrary.Yellow));
                    y += height + 5f;
                }
                DrawResult(ref y, x, width, inRect, result, i + 1, headerY =>
                {
                    Rect control = new Rect(x + width - 145f, headerY + 3f, 23f, 23f);
                    if (Button(control, TexButton.ReorderUp, "CQF_EditorMoveUp", index > 0))
                    {
                        mutation = () => Move(operation.results, index, index - 1);
                    }
                    control.x += 28f;
                    if (Button(control, TexButton.ReorderDown, "CQF_EditorMoveDown", index + 1 < operation.results.Count))
                    {
                        mutation = () => Move(operation.results, index, index + 1);
                    }
                    control.x += 28f;
                    if (Button(control, TexButton.Copy, "Copy", true))
                    {
                        copiedResult = Copy(result);
                    }
                    control.x += 28f;
                    if (Button(control, TexButton.Delete, "Remove", true))
                    {
                        mutation = () => operation.results.RemoveAt(index);
                    }
                });
                unconditionalSeen |= result.conditions.Count == 0;
            }
            mutation?.Invoke();
            if (operation.results.Count == 0)
            {
                Widgets.Label(new Rect(x + 8f, y, width - 16f, 25f), "CQF_NoInteractionResults".Translate().Colorize(Color.gray));
                y += 30f;
            }
            y += 12f;
        }

        public static void DrawResult(ref float y, float x, float width, Rect inRect, InteractionResult result, int index = 0, Action<float> drawButtons = null)
        {
            float startY = y;
            Widgets.DrawHighlight(new Rect(x, y, width, 30f));
            bool open = expanded.TryGetValue(result, out _);
            if (Widgets.ButtonText(new Rect(x + 2f, y + 3f, 22f, 22f), open ? "−" : "+", false))
            {
                if (open)
                {
                    expanded.Remove(result);
                }
                else
                {
                    expanded.Add(result, result);
                }
                open = !open;
            }
            Rect labelRect = new Rect(x + 30f, y + 3f, width - (drawButtons == null ? 65f : 185f), 25f);
            string label = (index > 0 ? index + ". " : string.Empty) + result.resultName;
            Widgets.LabelEllipses(labelRect, label.Colorize(ColorLibrary.PaleBlue));
            TooltipHandler.TipRegion(labelRect, label);
            drawButtons?.Invoke(y);
            Rect renameRect = new Rect(x + width - 28f, y + 3f, 23f, 23f);
            if (Button(renameRect, TexButton.Rename, "Rename", true))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE(name => result.resultName = name));
            }
            y += 32f;
            string conditionSummary = result.conditions.Count == 0 ? "CQF_NoResultConditions".Translate().ToString()
                : "If".Translate() + " " + string.Join(" ∧ ", result.conditions.Select(CQFEditorEntrySummary.Describe));
            Rect summaryRect = new Rect(x + 12f, y, width - 24f, 25f);
            Widgets.LabelEllipses(summaryRect, conditionSummary.Colorize(Color.gray));
            TooltipHandler.TipRegion(summaryRect, conditionSummary);
            y += 28f;
            string actionSummary = "InteractionActions".Translate() + " " + result.actions.Count;
            if (result.actions.Count > 0)
            {
                actionSummary += " · " + string.Join(" → ", result.actions.Select(CQFEditorEntrySummary.Describe));
            }
            summaryRect.y = y;
            Widgets.LabelEllipses(summaryRect, actionSummary.Colorize(Color.gray));
            TooltipHandler.TipRegion(summaryRect, actionSummary);
            y += 30f;
            if (open)
            {
                CQFConditionListEditor.Draw(ref y, x + 12f, width - 24f, inRect, "If".Translate(), result.conditions);
                CQFActionListEditor.Draw(ref y, x + 12f, width - 24f, inRect, "InteractionActions".Translate(), result.actions);
            }
            Widgets.DrawBox(new Rect(x, startY, width, y - startY), 1, QuestEditor_Dialog.blueTex);
            y += 12f;
        }

        private static InteractionResult Copy(InteractionResult result)
        {
            return new InteractionResult
            {
                resultName = result.resultName,
                conditions = result.conditions.Select(condition => condition.Copy()).ToList(),
                actions = result.actions.Select(action => action.Copy()).ToList()
            };
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

        private static void Move(List<InteractionResult> results, int from, int to)
        {
            InteractionResult result = results[from];
            results.RemoveAt(from);
            results.Insert(to, result);
        }

        private static InteractionResult copiedResult;
        private static readonly ConditionalWeakTable<InteractionResult, object> expanded = new ConditionalWeakTable<InteractionResult, object>();
    }
}
