using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CustomHediff : HediffWithComps
{
    public Quest Quest
    {
        get { return GameTools.GetQuestFromThing(this.pawn); }
    }

    public void PasteSingleComp()
    {
        if (CQFEditorTools.actionComp != null)
        {
            this.comps.Add(CQFEditorTools.actionComp.Copy());
        }
    }

    public override string Label  => this.overridedLabel ?? base.Label;
    public override string Description  => this.overridedDescription ?? base.Description;
    public override Color LabelColor => this.overridedColor ?? base.LabelColor;

    public Dictionary<string, TargetInfo> GetTargetThis()
    {
        Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
        result.Add("Target", new TargetInfo(this.pawn));
        return result;
    }

    public override void Tick()
    {
        this.comps?.ForEach(s =>
        {
            if (s.mode == ActionTriggerMode.Tick && this.pawn.IsHashIntervalTick(s.tick))
            {
                s.actions.ForEach(a => a.Work(this.GetTargetThis(), this.Quest));
            }
        });
    }

    public override void Notify_Downed()
    {
        base.Notify_Downed();
        this.comps?.ForEach(s =>
        {
            if (s.mode == ActionTriggerMode.Down)
            {
                s.actions.ForEach(a => a.Work(this.GetTargetThis(), this.Quest));
            }
        });
    }

    public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt); 
        this.comps?.ForEach(s =>
        {
            if (s.mode == ActionTriggerMode.Damaged)
            {
                s.actions.ForEach(a => a.Work(this.GetTargetThis(), this.Quest));
            }
        });
    }

    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        base.Notify_KilledPawn(victim, dinfo);
        this.comps?.ForEach(s =>
        {
            if (s.mode == ActionTriggerMode.Kill)
            {
                s.actions.ForEach(a => a.Work(this.GetTargetThis(), this.Quest));
            }
        });
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref overridedLabel, "overridedLabel");
        Scribe_Values.Look(ref overridedDescription, "overridedDescription");
        Scribe_Values.Look(ref overridedColor, "overridedColor");
        Scribe_Collections.Look(ref comps, "comps", LookMode.Deep);
    }

    public string overridedLabel = null;
    public string overridedDescription = null;
    public Color? overridedColor;
    public List<ActionComp> comps = new();
}