using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobGiver_PatrolObserve : JobGiver_AIFightEnemies
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Job fightJob = base.TryGiveJob(pawn);
            if (fightJob != null)
            {
                return fightJob;
            }
            if (!this.TryGetPatrolCell(pawn, out IntVec3 cell) || pawn.Position != cell)
            {
                return null;
            }
            Job job = JobMaker.MakeJob(QEDefOf.CQF_DutyMapLookAround, cell);
            Rot4 baseRotation = pawn.Rotation;
            job.SetTarget(TargetIndex.B, cell + baseRotation.Rotated(RotationDirection.Counterclockwise).FacingCell);
            job.SetTarget(TargetIndex.C, cell + baseRotation.Rotated(RotationDirection.Clockwise).FacingCell);
            job.dutyTag = this.routeKey;
            job.controlGroupTag = this.routeIndexKey;
            job.count = this.leftTicks;
            job.takeInventoryDelay = this.rightTicks;
            return job;
        }

        private bool TryGetPatrolCell(Pawn pawn, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.GetRuntime(pawn);
            if (runtime == null || pawn?.Map == null || this.routeKey.NullOrEmpty())
            {
                return false;
            }
            if (!pawn.Map.GetComponent<MapComponent_CustomMapData>().route.TryGetValue(this.routeKey, out Route route) || route.route.NullOrEmpty())
            {
                return false;
            }
            int routeIndex = GenMath.PositiveMod(runtime.GetValue(this.routeIndexKey), route.route.Count);
            cell = route.route[routeIndex];
            return cell.IsValid;
        }

        [NoTranslate]
        public string routeKey;
        [NoTranslate]
        public string routeIndexKey = "PatrolRouteIndex";
        public int leftTicks = 45;
        public int rightTicks = 45;
    }
}
