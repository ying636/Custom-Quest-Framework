using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestBookObjectiveEditor
    {
        public static void Draw_0(QuestEditor_Library.QuestBookObjective cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            cqfReceiver.DrawCommonStart(ref y, inRect);
            cqfReceiver.DrawSpecial(ref y, inRect, x);
            cqfReceiver.DrawCommonRules(ref y, inRect);
        }

        public static void DrawSpecial_1(QuestEditor_Library.QuestBookObjective cqfReceiver, ref float y, Rect inRect, float x)
        {
        }

        public static void DrawCommonStart_2(QuestEditor_Library.QuestBookObjective cqfReceiver, ref float y, Rect inRect)
        {
            float width = inRect.width - 16f;
            Widgets.Label(new Rect(8f, y, width, 32f), "CQF_QuestBook_ObjectiveEditor".Translate().Colorize(CQFUIStyle.Accent));
            y += 38f;
            cqfReceiver.DrawSection(ref y, width, "CQF_QuestBook_ObjectiveBasic", 154f, card =>
            {
                float rowY = card.y + 46f;
                cqfReceiver.DrawTextField(card, ref rowY, "CQF_QuestBook_ObjectiveName", ref cqfReceiver.labelKey, false);
                cqfReceiver.DrawTextField(card, ref rowY, "CQF_QuestBook_ObjectiveDescription", ref cqfReceiver.descriptionKey, true);
            });
            cqfReceiver.DrawSection(ref y, width, "CQF_QuestBook_ObjectiveIcon", 160f, card =>
            {
                Rect previewRect = new Rect(card.x + 14f, card.y + 44f, 64f, 64f);
                CQFUIStyle.DrawBox(previewRect, 1);
                cqfReceiver.DrawObjectiveIcon(previewRect.ContractedBy(8f));
                float buttonX = previewRect.xMax + 18f;
                cqfReceiver.DrawTextButton(new Rect(buttonX, previewRect.y, 168f, 26f), "CQF_QuestBook_SelectThingIcon", cqfReceiver.SelectThingIcon);
                cqfReceiver.DrawTextButton(new Rect(buttonX, previewRect.y + 32f, 168f, 26f), "CQF_QuestBook_SelectImageIcon", cqfReceiver.SelectImageIcon);
                cqfReceiver.DrawTextButton(new Rect(buttonX, previewRect.y + 64f, 100f, 26f), "CQF_QuestBook_Clear", cqfReceiver.ClearIcon);
            });
        }

        public static void DrawCommonRules_3(QuestEditor_Library.QuestBookObjective cqfReceiver, ref float y, Rect inRect)
        {
            cqfReceiver.DrawSection(ref y, inRect.width - 16f, "CQF_QuestBook_ObjectiveRules", 82f, card =>
            {
                Rect toggleRect = new Rect(card.x + 14f, card.y + 48f, card.width - 28f, 28f);
                Widgets.DrawHighlightIfMouseover(toggleRect);
                Widgets.CheckboxLabeled(toggleRect, "CQF_QuestBook_Optional".Translate(), ref cqfReceiver.optional, placeCheckboxNearText: false);
                TooltipHandler.TipRegion(toggleRect, "CQF_QuestBook_OptionalTip".Translate());
            });
        }

        public static void DrawDetectionSection_4(QuestEditor_Library.QuestBookObjective cqfReceiver, ref float y, Rect inRect, QuestEditor_Library.QuestBookObjective.DetectionContentDrawer contentDrawer)
        {
            float startY = y;
            float width = inRect.width - 16f;
            float measuredY = startY + 84f;
            bool previousEnabled = GUI.enabled;
            Color previousColor = GUI.color;
            GUI.enabled = false;
            GUI.color = Color.clear;
            contentDrawer(new Rect(8f, startY, width, 0f), ref measuredY);
            GUI.color = previousColor;
            GUI.enabled = previousEnabled;
            float cardHeight = measuredY - startY + 10f;
            Rect card = new Rect(8f, startY, width, cardHeight);
            CQFUIStyle.DrawMenuSection(card);
            Widgets.Label(new Rect(card.x + 14f, card.y + 10f, card.width - 28f, 28f), "CQF_QuestBook_ObjectiveDetection".Translate().Colorize(CQFUIStyle.Accent));
            float rowY = card.y + 48f;
            Widgets.Label(new Rect(card.x + 14f, rowY + 2f, 164f, 24f), "CQF_QuestBook_ObjectiveType".Translate());
            Widgets.Label(new Rect(card.x + 184f, rowY + 2f, card.width - 198f, 24f), cqfReceiver.GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            rowY += 36f;
            contentDrawer(card, ref rowY);
            y = card.yMax + 12f;
        }

        public static void DrawSection_5(QuestEditor_Library.QuestBookObjective cqfReceiver, ref float y, float width, string titleKey, float height, Action<Rect> contentDrawer)
        {
            Rect card = new Rect(8f, y, width, height);
            CQFUIStyle.DrawMenuSection(card);
            Widgets.Label(new Rect(card.x + 14f, card.y + 10f, card.width - 28f, 28f), titleKey.Translate().Colorize(CQFUIStyle.Accent));
            contentDrawer(card);
            y += height + 12f;
        }

        public static void DrawTextField_6(QuestEditor_Library.QuestBookObjective cqfReceiver, Rect card, ref float y, string labelKey, ref string value, bool multiline)
        {
            float fieldX = card.x + 184f;
            float fieldWidth = card.width - 198f;
            float fieldHeight = multiline ? 54f : 28f;
            Widgets.Label(new Rect(card.x + 14f, y + 2f, 164f, 24f), labelKey.Translate());
            Rect field = new Rect(fieldX, y, fieldWidth, fieldHeight);
            value = multiline ? Widgets.TextArea(field, value ?? string.Empty) : Widgets.TextField(field, value ?? string.Empty);
            y += multiline ? 64f : 36f;
        }

        public static void DrawRowLabel_7(QuestEditor_Library.QuestBookObjective cqfReceiver, Rect card, float y, string labelKey)
        {
            Widgets.Label(new Rect(card.x + 14f, y + 2f, 164f, 24f), labelKey.Translate());
        }

        public static void DrawTextButton_8(QuestEditor_Library.QuestBookObjective cqfReceiver, Rect rect, string labelKey, Action action)
        {
            if (CQFUIStyle.ButtonText(rect, labelKey.Translate()))
            {
                action();
            }
        }

        public static void SelectThingIcon_9(QuestEditor_Library.QuestBookObjective cqfReceiver)
        {
            QuestBookTextureEntry.OpenSelect(path =>
            {
                cqfReceiver.iconPath = path;
                cqfReceiver.iconManuallySelected = true;
            }, "CQF_QuestBook_SelectThingIcon");
        }

        public static void SelectImageIcon_10(QuestEditor_Library.QuestBookObjective cqfReceiver)
        {
            Find.WindowStack.Add(new Dialog_SelectDialogImage(path =>
            {
                cqfReceiver.iconPath = path;
                cqfReceiver.iconManuallySelected = true;
            }, cqfReceiver.iconPath));
        }
    }
}
