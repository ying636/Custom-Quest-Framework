using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public abstract class CustomDutyTrigger : ISaveable, IDrawable, IExposable
    {
        public abstract bool Triggered(Pawn pawn, CustomDutyMap runtime, Quest quest, Dictionary<string, TargetInfo> targets);

        public virtual void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDutyTrigger.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            return result;
        }

        public virtual void ExposeData()
        {
        }
    }

    public class CustomDutyTrigger_TickInterval : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, CustomDutyMap runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return this.intervalTicks > 0 && Find.TickManager.TicksGame - runtime.lastTransitionTick >= this.intervalTicks;
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDutyTrigger_TickInterval.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("intervalTicks", this.intervalTicks));
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.intervalTicks, "intervalTicks", 250);
        }

        public int intervalTicks = 250;
        internal string buffer;
    }

    public class CustomDutyTrigger_Damaged : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, CustomDutyMap runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return runtime?.lastDamageTick == Find.TickManager.TicksGame;
        }
    }

    public class CustomDutyTrigger_Signal : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, CustomDutyMap runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return runtime?.lastSignalTick == Find.TickManager.TicksGame &&
                (this.signal.NullOrEmpty() || runtime.lastSignal == this.ResolveSignal(quest));
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDutyTrigger_Signal.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.signal.NullOrEmpty())
            {
                result.Add(new XElement("signal", this.signal));
            }
            if (this.addQuestPrefix)
            {
                result.Add(new XElement("addQuestPrefix", this.addQuestPrefix));
            }
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.signal, "signal");
            Scribe_Values.Look(ref this.addQuestPrefix, "addQuestPrefix");
        }

        private string ResolveSignal(Quest quest)
        {
            if (this.signal.NullOrEmpty())
            {
                return this.signal;
            }
            if (!this.addQuestPrefix)
            {
                return this.signal;
            }
            return "Quest" + quest?.id + "." + this.signal;
        }

        [NoTranslate]
        public string signal;
        public bool addQuestPrefix;
    }

    public class CustomDutyTrigger_LordPawnCountBelow : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, CustomDutyMap runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return pawn?.GetLord()?.ownedPawns.Count < this.count;
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomDutyTrigger_LordPawnCountBelow.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("count", this.count));
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.count, "count", 1);
        }

        public int count = 1;
        internal string buffer;
    }
}
