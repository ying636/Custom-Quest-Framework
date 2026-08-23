using System;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class QuestBookObjective_TargetCount : QuestBookObjective
    {
        public int targetCount = 1;

        public override bool UsesTargetCount => true;

        public override int TargetCount
        {
            get => Math.Max(1, targetCount);
            set => targetCount = Math.Max(1, value);
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_QuestBook_TargetCount".Translate(), ref targetCount, ref countBuffer, x, 320f);
            y += 30f;
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

        private string countBuffer;
    }
}
