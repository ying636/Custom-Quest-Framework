using Verse.AI;

namespace CQF.CinematicsRuntimeChecks;

public sealed class JobDriver_CinematicInstantSuccess : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield break;
    }
}
