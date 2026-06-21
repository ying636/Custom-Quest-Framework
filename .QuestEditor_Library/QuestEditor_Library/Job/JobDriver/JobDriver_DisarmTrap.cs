using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_DisarmTrap : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.TargetThingA, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CustomTrap_Capture trap = (CustomTrap_Capture)this.TargetThingA;
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, trap.tickToDisarm, true);
            toil.AddPreTickAction(() => this.pawn.rotationTracker.FaceTarget(trap.Position));
            yield return toil;
            yield return new Toil() { initAction = () => trap.Disarm(this.pawn), defaultCompleteMode = ToilCompleteMode.Delay };
            yield break;
        }
    }
}
