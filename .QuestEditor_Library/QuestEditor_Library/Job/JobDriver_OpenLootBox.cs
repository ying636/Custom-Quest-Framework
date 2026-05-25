using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_OpenLootBox : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.TargetThingA,this.job,1,-1,null,errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            LootBox loot = (LootBox)this.TargetThingA;
            yield return Toils_Goto.Goto(TargetIndex.A,PathEndMode.Touch);
            Toil toil = Toils_General.WaitWith(TargetIndex.A, loot.tickToOpen, true, false,
                false,TargetIndex.A); 
            yield return toil;
            yield return new Toil() {initAction = () => loot.Open(this.pawn),defaultCompleteMode = ToilCompleteMode.Delay};
            yield break;
        }
    }
}
