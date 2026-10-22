using Verse;
using Verse.AI;

namespace CQF.CinematicsRuntimeChecks;

public sealed class JobDriver_CinematicDelayed : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Toil toil = ToilMaker.MakeToil("CQF_Runtime_DelayedJob");
        toil.defaultCompleteMode = ToilCompleteMode.Delay;
        toil.defaultDuration = 300;
        yield return toil;
    }
}
