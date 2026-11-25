#if RIMWORLD_1_7
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class JobDriver_CQFCrossMapWalk : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool portal = job.GetTarget(TargetIndex.B).HasThing;
        yield return Toils_Goto.Goto(portal ? TargetIndex.B : TargetIndex.A, portal ? PathEndMode.Touch : PathEndMode.OnCell);
        yield return Toils_General.Do(() => GameComponent_MapManager.Instance?.CrossMap.ReachedDeparture(pawn, job.targetA.Cell));
    }
}
#endif
