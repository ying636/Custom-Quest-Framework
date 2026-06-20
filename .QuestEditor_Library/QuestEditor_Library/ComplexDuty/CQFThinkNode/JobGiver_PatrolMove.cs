using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobGiver_PatrolMove : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!this.TryGetPatrolCell(pawn, out IntVec3 cell))
            {
                return null;
            }
            if (pawn.Position == cell)
            {
                return null;
            }
            LocalTargetInfo target = new LocalTargetInfo(cell);
            if (!pawn.CanReach(target, this.pathEndMode, this.maxDanger))
            {
                this.AdvanceRouteIndex(pawn);
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.Goto, target);
            job.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, this.locomotion, LocomotionUrgency.Walk);
            return job;
        }

        private bool TryGetPatrolCell(Pawn pawn, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (!this.TryGetRoute(pawn, out CustomDutyMap runtime, out Route route))
            {
                return false;
            }
            int routeIndex = GenMath.PositiveMod(runtime.GetValue(this.routeIndexKey), route.route.Count);
            cell = route.route[routeIndex];
            return cell.IsValid;
        }

        private bool TryGetRoute(Pawn pawn, out CustomDutyMap runtime, out Route route)
        {
            route = default;
            runtime = GameComponent_ComplexDuty.Instance?.GetRuntime(pawn);
            if (runtime == null || pawn?.Map == null || this.routeKey.NullOrEmpty())
            {
                return false;
            }
            return pawn.Map.GetComponent<MapComponent_CustomMapData>().route.TryGetValue(this.routeKey, out route) && !route.route.NullOrEmpty();
        }

        private void AdvanceRouteIndex(Pawn pawn)
        {
            if (!this.TryGetRoute(pawn, out CustomDutyMap runtime, out Route route))
            {
                return;
            }
            runtime.SetValue(this.routeIndexKey, GenMath.PositiveMod(runtime.GetValue(this.routeIndexKey) + 1, route.route.Count));
        }

        [NoTranslate]
        public string routeKey;
        [NoTranslate]
        public string routeIndexKey = "PatrolRouteIndex";
        public LocomotionUrgency locomotion = LocomotionUrgency.Walk;
        public Danger maxDanger = Danger.Deadly;
        public PathEndMode pathEndMode = PathEndMode.OnCell;
    }
}
