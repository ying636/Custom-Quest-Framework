using Verse.AI;

public sealed class CinematicBindingTestDriver : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    public void CompleteImmediately(JobCondition condition)
    {
        Cleanup(condition);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield break;
    }
}
