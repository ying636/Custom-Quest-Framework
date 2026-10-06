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

        public override void DrawSpecial(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective_TargetCount.DrawSpecial(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        protected internal void DrawTargetCountField(Rect card, ref float y)

        {
            object[] arguments = new object[]
            {
                card,
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective_TargetCount.DrawTargetCountField(None:UnityEngine.Rect,Ref:float)", this, arguments);
            y = (float)arguments[1];
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
        internal string countBuffer;
    }
}
