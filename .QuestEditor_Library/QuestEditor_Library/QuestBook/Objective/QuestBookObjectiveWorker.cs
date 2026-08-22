using RimWorld.QuestGen;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public abstract class QuestBookObjectiveWorker
    {
        public abstract bool Process(QuestBookObjective objective, QuestBookObjectiveProgress progress, Signal signal);

        public virtual bool Check(QuestBookObjective objective, QuestBookObjectiveProgress progress)
        {
            return false;
        }
    }
}
