using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CustomDoorEditor
    {
        public static void DrawTab_0(QuestEditor_Library.CustomDoor cqfReceiver)
        {
            Rect inRect = new Rect(0f, 0f, 540f, 590f);
            float width = inRect.width - 20f;
            Widgets.BeginScrollView(new Rect(0f, 0f, inRect.width, inRect.height), ref cqfReceiver.pos, new Rect(0f, 0f, width, Mathf.Max(inRect.height, cqfReceiver.height + 10f)));
            float x = 10f;
            float y = 10f;
            cqfReceiver.DrawSectionHeader(ref y, x, width, "OpeningActions".Translate(), "CustomDoorOpeningActionsTip".Translate(), () => CQFEditorTools.OpenCQFActionSelect(t => cqfReceiver.openingActions.Add((CQFAction)Activator.CreateInstance(t))), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.openingActions, a => cqfReceiver.openingActions.Remove(a), a => a.GetType().Name.Translate()), () => cqfReceiver.openingActions.Any());
            cqfReceiver.DrawActionList(ref y, x, width, inRect);
            cqfReceiver.DrawSectionHeader(ref y, x, width, "OpeningConditions".Translate(), "CustomDoorOpeningConditionsTip".Translate(), () => Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), c => c.Name.Translate(), c => cqfReceiver.openingConditions.Add((DialogCondition)Activator.CreateInstance(c)), null, null, null, null, null, null), "Select".Translate())), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.openingConditions, c => cqfReceiver.openingConditions.Remove(c), c => c.GetType().Name.Translate()), () => cqfReceiver.openingConditions.Any());
            foreach (DialogCondition c in cqfReceiver.openingConditions)
            {
                float itemY = y;
                c.Draw(ref y, inRect, x + 8f);
                cqfReceiver.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }

            if (!cqfReceiver.openingConditions.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 8f, width - 16f, "CustomDoorNoConditions".Translate());
            }

            cqfReceiver.height = y + 10f;
            Widgets.EndScrollView();
        }

        public static void DrawActionList_1(QuestEditor_Library.CustomDoor cqfReceiver, ref float y, float x, float width, Rect inRect)
        {
            foreach (CQFAction action in cqfReceiver.openingActions)
            {
                float itemY = y;
                action.Draw(ref y, inRect, x + 8f);
                cqfReceiver.DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }

            if (!cqfReceiver.openingActions.Any())
            {
                cqfReceiver.DrawEmptyState(ref y, x + 8f, width - 16f, "CustomDoorNoActions".Translate());
            }

            y += 10f;
        }

        public static void DrawSectionHeader_2(QuestEditor_Library.CustomDoor cqfReceiver, ref float y, float x, float width, string label, string tip, Action addAction, Action removeAction, Func<bool> canRemove)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Rect labelRect = new Rect(x + 8f, y + 4f, width - 84f, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.SkyBlue));
            TooltipHandler.TipRegion(labelRect, tip);
            Rect button = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(button, TexButton.Plus))
            {
                addAction();
            }

            TooltipHandler.TipRegion(button, "Add".Translate());
            button.x += 30f;
            if (Widgets.ButtonImage(button, TexButton.Delete) && canRemove())
            {
                removeAction();
            }

            TooltipHandler.TipRegion(button, "Remove".Translate());
            y += 38f;
        }

        public static void DrawEmptyState_3(QuestEditor_Library.CustomDoor cqfReceiver, ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(Color.gray));
            y += 32f;
        }
    }
}
