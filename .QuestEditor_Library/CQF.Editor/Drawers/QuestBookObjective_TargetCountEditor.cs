using System;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestBookObjective_TargetCountEditor
    {
        public static void DrawSpecial_0(QuestEditor_Library.QuestBookObjective_TargetCount cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) => cqfReceiver.DrawTargetCountField(card, ref rowY));
        }

        public static void DrawTargetCountField_1(QuestEditor_Library.QuestBookObjective_TargetCount cqfReceiver, Rect card, ref float y)
        {
            cqfReceiver.DrawRowLabel(card, y, "CQF_QuestBook_TargetCount");
            Widgets.TextFieldNumeric<int>(new Rect(card.x + 184f, y, card.width - 198f, 28f), ref cqfReceiver.targetCount, ref cqfReceiver.countBuffer, 1);
            y += 36f;
        }
    }
}
