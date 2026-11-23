using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class JobDriver_CompositeMapTravel : JobDriver
{
    public override string GetReport() => "CQF_Composite_TravelReport".Translate();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
        yield return new Toil
        {
            tickAction = QueueTravel,
            defaultCompleteMode = ToilCompleteMode.Never
        };
    }

    private void QueueTravel()
    {
        if (queued) return;
        GameComponent_MapManager manager = GameComponent_MapManager.Instance
            ?? throw new InvalidOperationException("[CQF] Map manager is unavailable.");
        queued = true;
        manager.QueueCompositeTransfer(() =>
        {
            queued = false;
            if (!pawn.Spawned || pawn.jobs.curDriver != this) return;
            if (job.targetA.Thing is CQFCompositeMapPortal portal && portal.Map == pawn.Map)
            {
                if (!manager.TryTravelCompositeMap(pawn, portal.Destination, portal.Arrival))
                    EndJobWith(JobCondition.Incompletable);
            }
            else
            {
                EndJobWith(JobCondition.Incompletable);
            }
        });
    }

    private bool queued;
}
