using System.Linq;
using RimWorld.QuestGen;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjectiveWorker_Resource : QuestBookObjectiveWorker
    {
        public override bool Process(QuestBookObjective objective, QuestBookObjectiveProgress progress, Signal signal)
        {
            return false;
        }

        public override bool Check(QuestBookObjective objective, QuestBookObjectiveProgress progress)
        {
            if (objective?.targetThingDef == null || progress == null)
            {
                return false;
            }
            int count = Find.Maps.Where(map => map.IsPlayerHome).Sum(map => map.resourceCounter.GetCount(objective.targetThingDef));
            progress.currentCount = count;
            progress.completed = count >= System.Math.Max(1, objective.targetCount);
            return true;
        }
    }
}
