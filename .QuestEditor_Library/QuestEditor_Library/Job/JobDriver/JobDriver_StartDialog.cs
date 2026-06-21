using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_StartDialog : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.TargetThingA, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A,PathEndMode.Touch);
            yield return new Toil()
            {
                initAction = () => 
                {
                    if(GameComponent_Editor.Instance.Dialogs.TryGetValue(this.TargetThingA,out DialogManagerDef manager)
                       && manager.GetTree(this.TargetThingA,this.pawn) is DialogTreeDef dialog)
                    {
                        Find.WindowStack.Add(dialog.CreateCQFDialog(this.TargetThingA,
                            this.pawn,GameTools.GetQuestFromThing(this.TargetThingA)));
                    }
                }
                ,
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            yield break;
        }
    }
}

