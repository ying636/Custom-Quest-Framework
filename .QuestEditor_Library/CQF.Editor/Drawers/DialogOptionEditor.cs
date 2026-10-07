using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class DialogOptionEditor
    {
        public static float Draw_0(QuestEditor_Library.DialogOption cqfReceiver, Rect inRect, QuestEditor_Dialog parent, DialogNode node)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope();
            float x = 8f;
            float y = 8f;
            float width = inRect.width - 18f;
            Widgets.DrawHighlight(new Rect(x + 4f, y - 2f, width - 8f, 32f));
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 16f, 25f), cqfReceiver.GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            y += 40f;
            CQFEditorTools.DrawLabelAndText_Line(y, "OptionText".Translate(), ref cqfReceiver.text, x + 8f, 180f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "HideWhenDisable".Translate(), ref cqfReceiver.hideWhenDisabled);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "removeDialogAfterSelect".Translate(), ref cqfReceiver.removeDialogAfterSelect);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "hideFailReason".Translate(), ref cqfReceiver.hideFailReason);
            y += 40f;
            List<Type> thingDatas = typeof(CQFThingData).AllSubclassesNonAbstract().ListFullCopy();
            thingDatas.Remove(typeof(CQFThingCategoryCount));
            cqfReceiver.DrawSectionHeader(ref y, x, width, "InteractionOption_RequiredThing".Translate(), () => CQFEditorTools.DrawFloatMenu(thingDatas, type => CQFThingData.OpenSelectWindow(type, data => cqfReceiver.requiredThings.Add(data)), type => type.Name.Translate()), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.requiredThings, data => cqfReceiver.requiredThings.Remove(data), data => data.ToString()), () => cqfReceiver.requiredThings.Any());
            foreach (CQFThingData data in cqfReceiver.requiredThings)
            {
                float itemY = y;
                data.DrawWithSingleCount(ref y, inRect, x + 16f);
                cqfReceiver.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }

            if (!cqfReceiver.requiredThings.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoRequiredThings".Translate());
            }

            y += 10f;
            cqfReceiver.DrawSectionHeader(ref y, x, width, "DialogConditions".Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(), type => cqfReceiver.conditions.Add((DialogCondition)Activator.CreateInstance(type)), type => type.Name.Translate()), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.conditions, condition => cqfReceiver.conditions.Remove(condition), condition => condition.GetType().Name.Translate()), () => cqfReceiver.conditions.Any());
            foreach (DialogCondition condition in cqfReceiver.conditions)
            {
                float itemY = y;
                condition.Draw(ref y, inRect, x + 16f);
                cqfReceiver.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }

            if (!cqfReceiver.conditions.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogConditions".Translate());
            }

            y += 10f;
            cqfReceiver.DrawSectionHeader(ref y, x, width, "DialogResults".Translate(), () => cqfReceiver.results.Add(new DialogResult()), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.results, result => cqfReceiver.results.Remove(result), result => result.resultName), () => cqfReceiver.results.Any());
            float resultsHeight = cqfReceiver.results.Count * CQFDialogResultList.RowHeight;
            resultLists.GetValue(cqfReceiver, _ => new CQFDialogResultList()).Draw(cqfReceiver, new Rect(x + 8f, y, width - 16f, resultsHeight),
                result => result.resultName.CanTranslate() ? result.resultName.Translate().ToString() : result.resultName, result =>
                {
                    if (Find.WindowStack.Windows.ToList().Find(x => x.GetType() == typeof(Dialog_EditDialogResult))is Window window)
                    {
                        window.Close();
                    }

                    Find.WindowStack.Add(new Dialog_EditDialogResult(parent, result, cqfReceiver, node));
                }, result => parent.ShowOptionMenu(cqfReceiver, node, result), parent.InitCurTree);
            y += resultsHeight;

            if (!cqfReceiver.results.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogResults".Translate());
            }

            y += 10f;
            return y;
        }

        public static float Draw(this QuestEditor_Library.DialogOption cqfReceiver, Rect inRect, QuestEditor_Dialog parent, DialogNode node)
        {
            return Draw_0(cqfReceiver, inRect, parent, node);
        }

        public static void DrawSectionHeader_1(QuestEditor_Library.DialogOption cqfReceiver, ref float y, float x, float width, string label, Action addAction, Action removeAction, Func<bool> canRemove)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 84f, 25f), label.Colorize(CQFUIStyle.Accent));
            Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (CQFUIStyle.ButtonImage(buttonRect, TexButton.Plus))
            {
                addAction();
            }

            TooltipHandler.TipRegion(buttonRect, "Add".Translate());
            buttonRect.x += 30f;
            if (CQFUIStyle.ButtonImage(buttonRect, TexButton.Delete) && canRemove())
            {
                removeAction();
            }

            TooltipHandler.TipRegion(buttonRect, "Remove".Translate());
            y += 40f;
        }

        public static void DrawEmptyState_2(QuestEditor_Library.DialogOption cqfReceiver, ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(CQFUIStyle.Muted));
            y += 32f;
        }

        private static readonly ConditionalWeakTable<DialogOption, CQFDialogResultList> resultLists = new ConditionalWeakTable<DialogOption, CQFDialogResultList>();
    }
}
