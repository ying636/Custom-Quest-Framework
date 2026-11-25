#if RIMWORLD_1_7
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class JobDriver_CQFCrossMapHaul : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.Goto(TargetIndex.B, job.targetB.HasThing ? PathEndMode.Touch : PathEndMode.OnCell);
        yield return Toils_General.Do(() =>
        {
            if (GameComponent_MapManager.Instance is not { } manager ||
                !manager.CrossMap.LoadCargo(manager, pawn, job.targetC.Thing))
                EndJobWith(JobCondition.Incompletable);
        });
    }
}
#endif
