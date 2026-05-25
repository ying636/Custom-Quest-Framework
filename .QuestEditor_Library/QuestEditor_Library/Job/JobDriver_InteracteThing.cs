using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public class JobDriver_InteracteThing : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.TargetThingA, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            string text = this.GetReport();
            InteractionOperation operation = null;
            if (this.TargetThingA is InteractableThing thing)
            {
                operation = thing.GetCurOperation(text);
            }
            if (this.pawn?.Map?.GetComponent<MapComponent_CustomMapData>() is MapComponent_CustomMapData component
                && component.ExtraOperations.TryGetValue(this.TargetThingA, out List<InteractionOperation> operations) && operation == null)
            {
                operation = operations.Find(o => o.interactionText.Translate() == text);
            }
            if (operation != null)
            {
                yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.InteractionCell);
                Toil toil = Toils_General.WaitWith(TargetIndex.A, operation.tickToOperate, true
                    , false, false,TargetIndex.A);
                yield return toil;
                yield return new Toil()
                {
                    initAction = () =>
{
    Quest quest = GameTools.GetQuestFromThing(this.TargetThingA);
    if (DebugSettings.godMode)
    {
        Log.Message(quest?.name);
    }
    operation.ProduceResult(this.pawn, this.TargetThingA, quest);
},
                    defaultCompleteMode = ToilCompleteMode.Delay
                };
            }
            yield break;
        }
    }
}