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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            x += 8f;
            float width = Mathf.Max(80f, inRect.width - x - 12f);
            cqfReceiver.DrawBasicSettings(ref y, x, width);
            cqfReceiver.DrawRequiredThings(ref y, x, width, inRect);
            cqfReceiver.DrawConditions(ref y, x, width, inRect);
            cqfReceiver.DrawResults(ref y, x, width, inRect);
        }

        public static void DrawBasicSettings_1(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width)
        {
            cqfReceiver.DrawHeader(ref y, x, width, "InteractionOperations".Translate());
            string source = cqfReceiver.interactionText.CanTranslate() ? cqfReceiver.interactionText.Translate().ToString() : cqfReceiver.interactionText;
            string edited = Widgets.TextArea(new Rect(x + 8f, y, width - 16f, 58f), source);
            if (edited != source) CQFSignalEditor.RenameInteraction(cqfReceiver, edited);
            y += 66f;
            CQFEditorTools.DrawLabelAndText_Line(y, "TickToOperate".Translate(), ref cqfReceiver.tickToOperate, ref cqfReceiver.buffer, x + 8f, 90f);
            y += 36f;
            CQFSignalEditor.DrawInteractionSignal(ref y, new Rect(0f, 0f, x + width + 12f, CQFUIScope.ContentHeight), x, cqfReceiver);
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
                Widgets.Label(new Rect(x + 12f, y + 4f, width - 24f, 25f), "CQF_NoRequiredThings".Translate().Colorize(CQFUIStyle.Muted));
                y += 34f;
            }

            y += 12f;
        }

        public static void DrawConditions_3(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width, Rect inRect)
        {
            CQFConditionListEditor.Draw(ref y, x + 8f, width - 16f, inRect, "InteractionConditions".Translate(), cqfReceiver.conditions);
        }

        public static void DrawResults_4(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width, Rect inRect)
        {
            CQFInteractionResultEditor.Draw(ref y, x + 8f, width - 16f, inRect, cqfReceiver);
        }

        public static void DrawHeader_5(QuestEditor_Library.InteractionOperation cqfReceiver, ref float y, float x, float width, string label, Action addAction = null, string addTip = null, Action removeAction = null, string removeTip = null, Texture2D addIcon = null)
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect header = new Rect(x, y, width, 32f);
            Widgets.DrawBoxSolid(header, CQFUIStyle.Header);
            CQFUIStyle.DrawBox(header);
            Widgets.Label(new Rect(x + 8f, y + 3f, Mathf.Max(20f, width - 88f), 26f), label.Colorize(CQFUIStyle.TextColor));
            Rect button = new Rect(x + width - 60f, y + 2f, 25f, 25f);
            if (addAction != null && CQFUIStyle.ButtonImage(button, addIcon ?? TexButton.Plus))
            {
                addAction();
            }

            if (addAction != null && addTip != null)
            {
                TooltipHandler.TipRegion(button, addTip);
            }

            button.x += 30f;
            if (removeAction != null && CQFUIStyle.ButtonImage(button, TexButton.Delete))
            {
                removeAction();
            }

            if (removeAction != null && removeTip != null)
            {
                TooltipHandler.TipRegion(button, removeTip);
            }

            y += 42f;
        }
    }
}
