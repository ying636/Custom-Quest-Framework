using AllsparkCrisis;
using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQFAndRS;

public class CQFAction_InfectTarget : CQFAction_Target
{
    public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        foreach (TargetInfo target in targets.Values)
        {
            if (target.Thing is Building building && BiometalUtitly.CanBeInfected(building))
            {
                BiometalUtitly.Change(building);
            }
        }
    }
}
