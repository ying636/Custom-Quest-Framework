using RimWorld.QuestGen;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjectiveWorker_Research : QuestBookObjectiveWorker
    {
        public override bool Process(QuestBookObjective objective, QuestBookObjectiveProgress progress, Signal signal)
        {
            return false;
        }

        public override bool Check(QuestBookObjective objective, QuestBookObjectiveProgress progress)
        {
            if (objective?.targetResearch == null || progress == null)
            {
                return false;
            }
            progress.currentCount = objective.targetResearch.IsFinished ? 1 : 0;
            progress.completed = objective.targetResearch.IsFinished;
            return true;
        }
    }
}
