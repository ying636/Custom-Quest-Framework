using System;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class QuestBookObjective_SignalEditor
    {
        public static void DrawSpecial_0(QuestEditor_Library.QuestBookObjective_Signal cqfReceiver, ref float y, Rect inRect, float x)
        {
            cqfReceiver.DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) =>
            {
                cqfReceiver.DrawTargetCountField(card, ref rowY);
                CQFSignalEditor.DrawBookField(new Rect(card.x + 14f, rowY, 164f, 28f), new Rect(card.x + 184f, rowY, card.width - 198f, 28f), "CQF_QuestBook_TriggerSignal".Translate(), cqfReceiver.signal, value => cqfReceiver.signal = value);
                rowY += 36f;
            });
        }
    }
}
