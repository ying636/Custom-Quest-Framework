using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_MoveInTargetToSubMap : JobDriver
    {
        public CustomMapEntrance Entrance 
        {
            get 
            {
                if (this.entrance == null) 
                {
                    if (this.pawn.Map.GetComponent<MapComponent_CustomMapData>().designatedAndMap.TryGetValue(this.TargetThingA, out Thing targetEntrance) && targetEntrance is
        CustomMapEntrance entrance)
                    {
                        this.entrance = entrance;
                    }
                    else 
                    {
                        this.EndJobWith(JobCondition.Errored);
                        return null;
                    }
                }
                return this.entrance;
            } 
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.TargetThingA, this.job, 1, -1, null, errorOnFailed);
        }
        public override string GetReport()
        {
            return "JobDriver_MoveInTargetToSubMap_Report".Translate(this.TargetThingA.Label,this.Entrance.Label);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoCell(this.Entrance.InteractionCell, PathEndMode.Touch);
            yield return new Toil()
            {
                initAction = () =>
                {
                    this.pawn.Map.designationManager.TryRemoveDesignationOn(this.TargetThingA,QEDefOf.QE_MoveIn);
                    entrance.TryEnter(this.TargetThingA);
                }
                ,
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            yield break;
        }

        public CustomMapEntrance entrance = null;
    }
}