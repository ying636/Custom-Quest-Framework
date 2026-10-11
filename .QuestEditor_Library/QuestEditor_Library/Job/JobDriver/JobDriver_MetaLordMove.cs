using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class JobDriver_MetaLordMove : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !(this.TargetThingA is CQFMapPortal portal) || !portal.IsAvailable(this.pawn) ||
                !portal.IsEnterable(out _) ||
                (this.pawn.GetLord()?.LordJob as MultLevelLordJobBase)?.metaLord?.GetMove(this.pawn) == null);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(90).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            Toil enter = ToilMaker.MakeToil("CQF_MetaLordMove");
            enter.initAction = () =>
            {
                CQFMapPortal portal = (CQFMapPortal)this.TargetThingA;
                if (portal is CustomMapEntrance entrance) entrance.TryEnter(this.pawn);
                else if (portal is CustomMapExit exit) exit.Exit(this.pawn);
                else
                {
                    Map target = portal.GetOtherMap();
                    IntVec3 cell = portal.GetDestinationLocation();
                    if (!cell.Standable(target)) cell = CellFinder.StandableCellNear(cell, target, 5);
                    if (!cell.IsValid) throw new System.InvalidOperationException("[CQF] MetaLord portal has no destination cell.");
                    this.pawn.DeSpawn();
                    GenSpawn.Spawn(this.pawn, cell, target);
                    portal.OnEntered(this.pawn);
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }
}
