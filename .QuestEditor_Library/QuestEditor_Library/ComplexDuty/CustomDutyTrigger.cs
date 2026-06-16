using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using RimWorld.QuestGen;

namespace QuestEditor_Library
{
    public abstract class CustomDutyTrigger : ISaveable, IDrawable, IExposable
    {
        public abstract bool Triggered(Pawn pawn, DutyMapRuntime runtime, Quest quest, Dictionary<string, TargetInfo> targets);

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

    public class CustomDutyTrigger_Condition : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, DutyMapRuntime runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return true;
        }
    }

    public class CustomDutyTrigger_TickInterval : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, DutyMapRuntime runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return this.intervalTicks > 0 && Find.TickManager.TicksGame % this.intervalTicks == 0;
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

    public class CustomDutyTrigger_LordPawnCountBelow : CustomDutyTrigger
    {
        public override bool Triggered(Pawn pawn, DutyMapRuntime runtime, Quest quest, Dictionary<string, TargetInfo> targets)
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
