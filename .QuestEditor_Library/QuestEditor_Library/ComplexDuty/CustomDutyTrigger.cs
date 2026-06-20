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
            string key = "CQF_" + this.GetType().Name;
            string tipKey = key + "_Tip";
            Widgets.Label(new Rect(x, y, 260f, 25f), (key.CanTranslate() ? key.Translate().ToString() : this.GetType().Name.Translate().ToString()).Colorize(ColorLibrary.SkyBlue));
            if (tipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(new Rect(x, y, 260f, 25f), tipKey.Translate());
            }
            y += 30f;
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
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_IntervalTicks".Translate(), ref this.intervalTicks, ref this.buffer, x, 150f);
            y += 30f;
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
        private string buffer;
    }

    public class CustomDutyTrigger_Conditions : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, CustomDutyMap runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            Dictionary<string, TargetInfo> contextTargets = targets ?? new Dictionary<string, TargetInfo>();
            if (!contextTargets.ContainsKey("Target"))
            {
                contextTargets = new Dictionary<string, TargetInfo>(contextTargets)
                {
                    ["Target"] = new TargetInfo(pawn)
                };
            }
            return this.conditions.NullOrEmpty() || this.conditions.All(condition => condition.Satisfied(contextTargets, out _, quest));
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x + 5f, this.conditions, inRect, "Conditions".Translate(), condition => condition.GetType().Name.Translate());
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.conditions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.conditions, "conditions"));
            }
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.conditions, "conditions", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.conditions == null)
            {
                this.conditions = new List<DialogCondition>();
            }
        }

        public List<DialogCondition> conditions = new List<DialogCondition>();
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
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "InSignal".Translate(), ref this.signal, x, 150f);
            y += 30f;
            Rect rect = new Rect(x, y, 260f, 25f);
            Widgets.CheckboxLabeled(rect, "CQF_DutySignalAddQuestPrefix".Translate(), ref this.addQuestPrefix);
            if ("CQF_DutySignalAddQuestPrefix_Tip".CanTranslate())
            {
                TooltipHandler.TipRegion(rect, "CQF_DutySignalAddQuestPrefix_Tip".Translate());
            }
            y += 30f;
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
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_PawnCount".Translate(), ref this.count, ref this.buffer, x, 150f);
            y += 30f;
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
        private string buffer;
    }
}
