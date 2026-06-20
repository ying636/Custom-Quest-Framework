using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobGiver_RepairTarget : JobGiver_TargetBase
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!this.TryGetTargetThing(pawn, out Thing thing) || !this.CanRepair(pawn, thing))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.Repair, thing);
            job.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, this.locomotion, LocomotionUrgency.Walk);
            return job;
        }

        private bool TryGetTargetThing(Pawn pawn, out Thing thing)
        {
            thing = null;
            if (!this.TryGetTarget(pawn, out TargetInfo target) || !target.HasThing)
            {
                return false;
            }
            thing = target.Thing;
            return thing != null && !thing.Destroyed;
        }

        private bool CanRepair(Pawn pawn, Thing thing)
        {
            if (pawn?.Map == null || thing?.Map != pawn.Map || thing is not Building building)
            {
                return false;
            }
            if (!RepairUtility.PawnCanRepairNow(pawn, thing))
            {
                return false;
            }
            if (!pawn.CanReserve(building, 1, -1, null, this.forced))
            {
                return false;
            }
            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (building.def.mineable && building.Map.designationManager.DesignationAt(building.Position, DesignationDefOf.Mine) != null)
            {
                return false;
            }
            if (building.def.mineable && building.Map.designationManager.DesignationAt(building.Position, DesignationDefOf.MineVein) != null)
            {
                return false;
            }
            return !building.IsBurning();
        }

        public LocomotionUrgency locomotion = LocomotionUrgency.Walk;
        public bool forced = true;
    }
}
