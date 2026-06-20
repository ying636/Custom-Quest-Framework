using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobGiver_GotoTarget : JobGiver_TargetBase
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!this.TryGetTarget(pawn, out TargetInfo target))
            {
                return null;
            }
            LocalTargetInfo localTarget = target.HasThing ? new LocalTargetInfo(target.Thing) : new LocalTargetInfo(target.Cell);
            if (!localTarget.IsValid || pawn.Position == localTarget.Cell)
            {
                return null;
            }
            if (!pawn.CanReach(localTarget, this.pathEndMode, this.maxDanger))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.Goto, localTarget);
            job.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, this.locomotion, LocomotionUrgency.Walk);
            return job;
        }

        public LocomotionUrgency locomotion = LocomotionUrgency.Walk;
        public Danger maxDanger = Danger.Deadly;
        public PathEndMode pathEndMode = PathEndMode.OnCell;
    }
}
