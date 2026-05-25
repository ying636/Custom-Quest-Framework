using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;

namespace QuestEditor_Library
{
    internal class FloatMenuOptionProvider_Dialog : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;
        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {

            return base.SelectedPawnValid(pawn, context) && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking);
        }
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            if (context.FirstSelectedPawn == null)
            {
                return null;
            }
            return this.GetDialogOption(context.FirstSelectedPawn,clickedThing);
        }

        private IEnumerable<FloatMenuOption> GetDialogOption(Pawn pawn, Thing thing)
        {
            if (pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly))
            {
                Dictionary<Thing, DialogManagerDef> dialogs = Current.Game.GetComponent<GameComponent_Editor>().Dialogs;
                if (dialogs != null && dialogs.TryGetValue(thing, out DialogManagerDef manager) && manager.GetTree(pawn, thing) is DialogTreeDef def)
                {
                    if (def.requireNonHostile && thing.HostileTo(pawn))
                    {
                        yield return (new FloatMenuOption("UnableToInterviewTargetIsHostile".Translate().CapitalizeFirst(), null));
                    }
                    else
                    {
                        yield return (new FloatMenuOption(GameTools.GetDialogText(def.dialogReportKey, pawn, thing, def, GameTools.GetQuestFromThing(thing)), () =>
                        {
                            pawn.jobs.StopAll();
                            Job job = JobMaker.MakeJob(QEDefOf.QE_StartDialog, thing);
                            job.reportStringOverride = GameTools.GetDialogText(def.dialogReportKey, pawn, thing, def, GameTools.GetQuestFromThing(thing));
                            pawn.jobs.StartJob(job);
                        }));
                    }
                }
            }
            yield break;
        }
    }
}