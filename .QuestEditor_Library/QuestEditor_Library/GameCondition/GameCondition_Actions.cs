using RimWorld;
using Verse;

namespace QuestEditor_Library;

public class GameCondition_Actions : GameCondition
{
    public override string Label => base.Label.Formatted(
         (this.Permanent ?  null : this.TicksLeft.ToStringTicksToDays())
    ,(this.tick - this.curProgress).ToStringTicksToDays());
    public override void GameConditionTick()
    {
        base.GameConditionTick();
        if (this.useTick)
        {
            this.curProgress++;
            if (this.curProgress > this.tick)
            {
                Trigger();
                this.curProgress = 0;
            }
        }
    }

    public void Trigger()
    {
        foreach (var cqfAction in this.actions)
        {
            cqfAction.Work(
                new Dictionary<string, TargetInfo> { ["Map"] = new TargetInfo(IntVec3.Zero, this.SingleMap) },
                GameTools.GetQuestFromMap(this.SingleMap));
        }   
    }

    public override void End()
    {
        base.End();
        if (!this.useTick)
        {
            Trigger();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref useTick,"useTick");
        Scribe_Values.Look(ref tick,"tick"); 
        Scribe_Values.Look(ref curProgress,"curProgress");
        
        Scribe_Collections.Look(ref actions,"actions", LookMode.Deep);
    }

    public int curProgress;
    public bool useTick;
    public int tick;
    public List<CQFAction> actions = new List<CQFAction>();
}