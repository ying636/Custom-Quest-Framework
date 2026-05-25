using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_EnterOrExitSubMap : JobDriver
    {
        public string MapName 
        {
            get 
            {
                if (this.TargetThingA is CustomMapEntrance entrance)
                {
                    return entrance.MapName;
                }
                else if (this.TargetThingA is CustomMapExit exit)
                {
                    return ((MapParent_Custom)exit.Map.Parent).MapName;
                }
                return "Null";
            }
        }
        public override string GetReport()
        {
            return this.TargetThingA is CustomMapEntrance ? "JobDriver_EnterSubMap_Report".Translate(this.MapName) : "JobDriver_ExitSubMap_Report".Translate(this.MapName);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
            yield return new Toil()
            {
                tickAction = () =>
                {
                    this.pawn.ClearAllReservations();
                    if (this.TargetThingA is CustomMapEntrance entrance)
                    {
                        entrance.TryEnter(this.pawn);
                    }
                    else if(this.TargetThingA is CustomMapExit exit)
                    {
                        exit.Exit(this.pawn);
                    }
                    this.EndJobWith(JobCondition.Succeeded);
                }
                ,
                defaultCompleteMode = ToilCompleteMode.Never
            };
            yield break;
        }
    }
}