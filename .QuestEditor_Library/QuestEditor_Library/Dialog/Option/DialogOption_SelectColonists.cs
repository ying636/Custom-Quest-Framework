using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class DialogOption_SelectColonists : DialogOption
    { 
         public override List<DialogElement_Option> GetDEOptions(Thing interviewer, Thing interviewee, DialogTreeDef def, Quest quest)
        { 
            var result = new List<DialogElement_Option>();
            foreach (var c in PawnsFinder.AllMaps_FreeColonistsSpawned.ListFullCopy())
            {
                Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
                targets.Add("Interviewee", interviewee);
                targets.Add("Interviewer", interviewer);
                targets.Add("Target", c);
                DialogElement_Option r = new DialogElement_Option(GameTools.GetDialogText(c.Label.ResolveTags(), interviewer,
                    interviewee, def, quest), () => { });

                string reason = null;
                if (this.conditions.Exists(condition => !condition.Satisfied(targets, out reason, quest)))
                {
                    r.disabled = true;
                    r.disableReason = (reason);
                }

                if (!this.requiredThings.NullOrEmpty())
                {
                    var things = new List<Thing>();
                    if (interviewee is Pawn pawn && pawn.inventory != null)
                    {
                        things.AddRange(pawn.inventory.innerContainer);
                    }

                    if (interviewee.Map.IsPlayerHome)
                    {
                        things.AddRange(GameTools.AllConsumableThing(interviewee.Map));
                    }

                    if (!GameTools.CheckRequiredThings(this.requiredThings, things, out var thingDef,
                            out var requiredThingDefs
                            , out var limit))
                    {
                        r.disabled = true;
                        r.disableReason = ("NoRequiredThing".Translate(thingDef, requiredThingDefs, limit));
                    }

                }

                var dR = this.ProduceResult(interviewee, interviewer, quest);
                r.action = () =>
                {
                    dR.actions.ForEach(a => a.Work(targets, quest));
                    if (this.removeDialogAfterSelect)
                    {
                        Log.Message(interviewer.Label);
                        GameComponent_Editor.Instance.RemoveDialog(interviewer);
                    }

                    GameTools.ConsumeRequiredThings(interviewer as Pawn, interviewee as Pawn, this.requiredThings);
                };
                r.nextIndex = dR.nextIndex;
                result.Add(r);
            }

            return result;
        }
    }
}

