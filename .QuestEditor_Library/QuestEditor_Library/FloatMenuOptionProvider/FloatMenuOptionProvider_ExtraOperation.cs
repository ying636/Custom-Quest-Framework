using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;

namespace QuestEditor_Library
{
    internal class FloatMenuOptionProvider_ExtraOperation : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;
        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {
            return base.SelectedPawnValid(pawn, context) && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking);
        }
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (context.FirstSelectedPawn == null)
            {
                return null;
            }
            return this.GetOperation(context.FirstSelectedPawn, clickedPawn);
        }
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            if (context == null || context.FirstSelectedPawn == null || clickedThing == null)
            {
                return null;
            }
            return this.GetOperation(context.FirstSelectedPawn, clickedThing);
        }

        private IEnumerable<FloatMenuOption> GetOperation(Pawn pawn, Thing thing)
        {
            if (pawn != null && pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly))
            {
                var result = new List<FloatMenuOption>();
                Dictionary<Thing, List<InteractionOperation>> extraOperations 
                    = pawn.Map.GetComponent<MapComponent_CustomMapData>().ExtraOperations;

                if (extraOperations != null &&
                    extraOperations.TryGetValue(thing,
                    out List<InteractionOperation> operations) && !operations.NullOrEmpty()
                    && pawn.CanReserve(thing))
                {
                    operations.ForEach(o =>
                    {
                        if (o.Satisfied(pawn, thing, out string r, GameTools.GetQuestFromThing(thing)))
                        {
                            string text = o.interactionText.Translate();
                            Job job = JobMaker.MakeJob(QEDefOf.QE_InteractingWithTarget, thing);
                            job.reportStringOverride = text;
                            result.Add(new FloatMenuOption(text, () =>
                            {
                                pawn.jobs.StopAll();
                                pawn.jobs.StartJob(job);
                            }));
                        }
                    });

                }
                return result;
            }
            return new List<FloatMenuOption>();
        }
    }
}