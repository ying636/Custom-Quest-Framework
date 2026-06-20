using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_LookAround : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_General.Wait(this.job.count > 0 ? this.job.count : 45, TargetIndex.B);
            yield return Toils_General.Wait(this.job.takeInventoryDelay > 0 ? this.job.takeInventoryDelay : 45, TargetIndex.C);
            yield return Toils_General.Do(this.AdvanceRouteIndex);
        }

        private void AdvanceRouteIndex()
        {
            CustomDutyMap runtime = GameComponent_ComplexDuty.Instance?.GetRuntime(this.pawn);
            if (runtime == null || this.pawn?.Map == null || this.job.dutyTag.NullOrEmpty())
            {
                return;
            }
            if (!this.pawn.Map.GetComponent<MapComponent_CustomMapData>().route.TryGetValue(this.job.dutyTag, out Route route) || route.route.NullOrEmpty())
            {
                return;
            }
            string routeIndexKey = this.job.controlGroupTag.NullOrEmpty() ? "PatrolRouteIndex" : this.job.controlGroupTag;
            runtime.SetValue(routeIndexKey, GenMath.PositiveMod(runtime.GetValue(routeIndexKey) + 1, route.route.Count));
        }
    }
}
