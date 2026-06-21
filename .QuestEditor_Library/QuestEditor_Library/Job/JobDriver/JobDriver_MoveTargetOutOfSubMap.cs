using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_MoveTargetOutOfSubMap : JobDriver
    {
        public CustomMapExit Exit 
        {
            get 
            {
                if (this.exit == null) 
                {
                    if (this.pawn.Map.Parent is MapParent_Custom custom && custom.exit != null)
                    {
                        this.exit = custom.exit;
                    }
                    else 
                    {
                        this.EndJobWith(JobCondition.Errored);
                        return null;
                    }
                }
                return this.exit;
            } 
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.TargetThingA, this.job, 1, -1, null, errorOnFailed);
        }
        public override string GetReport()
        {
            return "JobDriver_MoveTargetOutOfSubMap_Report".Translate(this.TargetThingA.Label,((MapParent_Custom)this.exit.Map.Parent).MapName);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoCell(this.Exit.InteractionCell, PathEndMode.Touch);
            yield return new Toil()
            {
                initAction = () =>
                {
                    this.pawn.Map.designationManager.TryRemoveDesignationOn(this.TargetThingA,QEDefOf.QE_MoveOut);
                    this.Exit.Exit(this.TargetThingA);
                }
                ,
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            yield break;
        }

        public CustomMapExit exit = null;
    }
}