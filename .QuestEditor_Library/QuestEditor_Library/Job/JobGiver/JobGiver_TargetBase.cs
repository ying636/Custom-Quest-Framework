using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public abstract class JobGiver_TargetBase : ThinkNode_JobGiver
    {
        protected bool TryGetTarget(Pawn pawn, out TargetInfo target)
        {
            target = TargetInfo.Invalid;
            if (pawn == null || this.targetKey.NullOrEmpty())
            {
                return false;
            }
            if (this.useRuntimeDatabase)
            {
                CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.GetRuntime(pawn);
                target = runtime?.GetTarget(this.targetKey) ?? TargetInfo.Invalid;
                if (target.IsValid)
                {
                    return true;
                }
            }
            Quest quest = LordJob_ComplexCustom.GetForPawn(pawn)?.Quest;
            if (this.useQuestDatabase)
            {
                target = GameTools.GetTargetFromQuestDatabase(quest, this.targetKey);
                if (target.IsValid)
                {
                    return true;
                }
            }
            if (this.useTemporaryDatabase)
            {
                target = GameTools.GetTargetFromTemporaryDatabase(this.targetKey);
                if (target.IsValid)
                {
                    return true;
                }
            }
            if (this.useGlobalDatabase)
            {
                target = GameTools.GetTargetFromGlobalDatabase(quest, this.targetKey);
                if (target.IsValid)
                {
                    return true;
                }
            }
            return false;
        }

        [NoTranslate]
        public string targetKey = "PatrolTarget";
        public bool useRuntimeDatabase = true;
        public bool useQuestDatabase = true;
        public bool useTemporaryDatabase = true;
        public bool useGlobalDatabase = true;
    }
}
