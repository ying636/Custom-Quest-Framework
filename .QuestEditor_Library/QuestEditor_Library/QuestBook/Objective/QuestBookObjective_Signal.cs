using System;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjective_Signal : QuestBookObjective_TargetCount
    {
        [NoTranslate]
        public string signal;

        public override bool UsesSignal => true;

        public override string Signal
        {
            get => signal;
            set => signal = value;
        }

        public override bool Process(QuestBookObjectiveProgress progress, Signal incomingSignal)
        {
            if (progress == null || incomingSignal.tag.NullOrEmpty() || signal.NullOrEmpty())
            {
                return false;
            }
            if (incomingSignal.tag != signal && !incomingSignal.tag.EndsWith("." + signal))
            {
                return false;
            }
            progress.currentCount++;
            progress.completed = progress.currentCount >= TargetCount;
            return true;
        }

        public override void DrawSpecial(ref float y, Rect inRect, float x)
        {
            DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) =>
            {
                DrawTargetCountField(card, ref rowY);
                CQFSignalEditor.DrawBookField(new Rect(card.x + 14f, rowY, 164f, 28f),
                    new Rect(card.x + 184f, rowY, card.width - 198f, 28f), "CQF_QuestBook_TriggerSignal".Translate(), signal, value => signal = value);
                rowY += 36f;
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref signal, "signal");
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!signal.NullOrEmpty())
            {
                result.Add(new XElement("signal", signal));
            }
            return result;
        }
    }
}
