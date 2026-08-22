using System;
using RimWorld.QuestGen;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjectiveWorker_Signal : QuestBookObjectiveWorker
    {
        public override bool Process(QuestBookObjective objective, QuestBookObjectiveProgress progress, Signal signal)
        {
            if (objective == null || progress == null || signal.tag.NullOrEmpty() || objective.signal.NullOrEmpty())
            {
                return false;
            }
            if (signal.tag != objective.signal && !signal.tag.EndsWith("." + objective.signal))
            {
                return false;
            }
            progress.currentCount++;
            progress.completed = progress.currentCount >= Math.Max(1, objective.targetCount);
            return true;
        }
    }
}
