using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookChapterState : IExposable
    {
        public string chapterId;
        public QuestBookStepStatus status;

        public void ExposeData()
        {
            Scribe_Values.Look(ref chapterId, "chapterId");
            Scribe_Values.Look(ref status, "status");
        }
    }

    public class QuestBookStepState : IExposable
    {
        public string chapterId;
        public string stepId;
        public QuestBookStepStatus status;
        public List<QuestBookObjectiveProgress> objectives = new List<QuestBookObjectiveProgress>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref chapterId, "chapterId");
            Scribe_Values.Look(ref stepId, "stepId");
            Scribe_Values.Look(ref status, "status");
            Scribe_Collections.Look(ref objectives, "objectives", LookMode.Deep);
        }
    }

    public class QuestBookObjectiveProgress : IExposable
    {
        public int currentCount;
        public bool completed;

        public void ExposeData()
        {
            Scribe_Values.Look(ref currentCount, "currentCount");
            Scribe_Values.Look(ref completed, "completed");
        }
    }

    public enum QuestBookState
    {
        Locked,
        Active,
        Completed,
        Failed
    }

    public enum QuestBookStepStatus
    {
        Locked,
        Active,
        Completed,
        Failed,
        Skipped
    }
}
