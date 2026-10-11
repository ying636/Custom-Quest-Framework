using System;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class QuestBookObjective_TargetCount : QuestBookObjective
    {
        public override bool UsesTargetCount => true;
        public override int TargetCount { get => Math.Max(1, targetCount); set => targetCount = Math.Max(1, value); }

        public override void DrawSpecial(ref float y, Rect inRect, float x)
        {
            this.DrawDetectionSection(ref y, inRect, (Rect card, ref float rowY) => this.DrawTargetCountField(card, ref rowY));
        }

        protected internal void DrawTargetCountField(Rect card, ref float y)
        {
            this.DrawRowLabel(card, y, "CQF_QuestBook_TargetCount");
            Widgets.TextFieldNumeric<int>(new Rect(card.x + 184f, y, card.width - 198f, 28f), ref this.targetCount, ref this.countBuffer, 1);
            y += 36f;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetCount, "targetCount", 1);
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("targetCount", TargetCount));
            return result;
        }

        public int targetCount = 1;
        internal string countBuffer;
    }
}
