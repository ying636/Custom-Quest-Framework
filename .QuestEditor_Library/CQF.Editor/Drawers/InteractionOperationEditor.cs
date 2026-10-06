using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public static class InteractionOperationEditor
    {
        public static void Draw_0(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, Rect inRect, float x)
        {
            float width = inRect.width - 35f - x;
            cqfReceiver.DrawBasicSettings(ref y, x, width);
            cqfReceiver.DrawRequiredThings(ref y, x, width, inRect);
            cqfReceiver.DrawConditions(ref y, x, width, inRect);
            cqfReceiver.DrawResults(ref y, x, width, inRect);
        }

        public static void DrawBasicSettings_1(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width)
        {
            cqfReceiver.DrawHeader(ref y, x, width, cqfReceiver.interactionText, () => Find.WindowStack.Add(new Dialog_RenameForQE(name => CQFSignalEditor.RenameInteraction(cqfReceiver, name))), "Rename".Translate(), null, null, TexButton.Rename);
            float labelWidth = 190f;
            Widgets.Label(new Rect(x + 8f, y + 4f, labelWidth, 25f), "TickToOperate".Translate());
            Widgets.TextFieldNumeric(new Rect(x + labelWidth, y, 90f, 28f), ref cqfReceiver.tickToOperate, ref cqfReceiver.buffer);
            y += 36f;
            CQFSignalEditor.DrawInteractionSummary(ref y, new Rect(0f, 0f, x + width, UI.screenHeight), x, cqfReceiver);
            y += 12f;
        }

        public static void DrawRequiredThings_2(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width, Rect inRect)
        {
            cqfReceiver.DrawHeader(ref y, x, width, "InteractionOption_RequiredThing".Translate(), () => CQFEditorTools.DrawFloatMenu(new List<Type>() { typeof(CQFThingDefCount) }, t =>
            {
                CQFThingData.OpenSelectWindow(t, d => cqfReceiver.requiredThings.Add(d));
            }, t => t.Name.Translate()), "Add".Translate(), () => CQFEditorTools.DrawFloatMenu(cqfReceiver.requiredThings, d => cqfReceiver.requiredThings.Remove(d), d => d.ToString()), "Remove".Translate());
            float initY = y;
            foreach (CQFThingData thing in cqfReceiver.requiredThings)
            {
                thing.DrawWithSingleCount(ref y, inRect, x + 10f);
                y += 6f;
            }

            if (!cqfReceiver.requiredThings.Any())
            {
                Widgets.Label(new Rect(x + 12f, y + 4f, width - 24f, 25f), "CQF_NoRequiredThings".Translate().Colorize(Color.gray));
                y += 34f;
            }

            y += 12f;
        }

        public static void DrawConditions_3(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width, Rect inRect)
        {
            CQFConditionListEditor.Draw(ref y, x, width, inRect, "InteractionConditions".Translate(), cqfReceiver.conditions);
        }

        public static void DrawResults_4(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width, Rect inRect)
        {
            CQFInteractionResultEditor.Draw(ref y, x, width, inRect, cqfReceiver);
        }

        public static void DrawHeader_5(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width, string label, Action addAction = null, string addTip = null, Action removeAction = null, string removeTip = null, Texture2D addIcon = null)
        {
            Widgets.DrawHighlight(new Rect(x - 4f, y - 2f, width + 8f, 32f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width - 90f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            Rect button = new Rect(x + width - 60f, y + 2f, 25f, 25f);
            if (addAction != null && Widgets.ButtonImage(button, addIcon ?? TexButton.Plus))
            {
                addAction();
            }

            if (addAction != null && addTip != null)
            {
                TooltipHandler.TipRegion(button, addTip);
            }

            button.x += 30f;
            if (removeAction != null && Widgets.ButtonImage(button, TexButton.Delete))
            {
                removeAction();
            }

            if (removeAction != null && removeTip != null)
            {
                TooltipHandler.TipRegion(button, removeTip);
            }

            y += 32f;
            y += 10f;
        }
    }
}
