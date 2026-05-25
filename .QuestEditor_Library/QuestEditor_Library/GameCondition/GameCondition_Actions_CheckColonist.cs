using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library;

public class GameCondition_Actions_CheckColonist : GameCondition_Actions
{
    public override void GameConditionTick()
    {
        base.GameConditionTick();
        if (this.SingleMap.IsHashIntervalTick(250) && 
            !this.SingleMap.mapPawns.AllPawns.Exists(p => p.IsPlayerControlled && p.RaceProps.Humanlike))
        { 
            Find.SignalManager.SendSignal(
                new Signal($"Quest{GameTools.GetQuestFromMap(this.SingleMap).id}.NullColonist"));
            if (this.SingleMap.Parent is MapParent_Custom custom)
            { 
                custom.entrance.Destroy();
            }
        }
    }
}