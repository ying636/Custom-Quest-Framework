using Verse;
using Verse.AI;

namespace CQF.CinematicsRuntimeChecks;

public sealed class JobDriver_CinematicInstantFailure : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Toil toil = ToilMaker.MakeToil("CQF_Runtime_InstantFailure");
        toil.initAction = () => EndJobWith(JobCondition.Incompletable);
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return toil;
    }
}
