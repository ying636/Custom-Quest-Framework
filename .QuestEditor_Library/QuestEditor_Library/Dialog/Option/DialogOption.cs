using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class DialogOptionAndResult
    {
        public DialogOptionAndResult(DiaOption option, DialogResult result)
        {
            this.option = option;
            this.result = result;
        }

        public DiaOption option;
        public DialogResult result;
    }

    public class DialogOption : ISaveable
    {
        public virtual DialogResult ProduceResult(Thing target,Thing interviwer,Quest quest) 
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            targets.Add("Interviewee", target);
            targets.Add("Interviewer", interviwer);
            return this.results.Find(r => !r.conditions.Exists(c => !c.Satisfied(targets,out string reason, quest))) 
                ?? new DialogResult() {};
        }

        public virtual bool Disabled(Dictionary<string, TargetInfo> targets,Quest quest
        ,out string reason)
        { 
            foreach (DialogCondition condition in this.conditions)
            {
                if (!condition.Satisfied(targets, out reason, quest))
                {
                    return true;
                } 
            }
            reason = null;
            return false;
        }
 
        public virtual List<DialogElement_Option> GetDEOptions(Thing interviewer
            ,Thing interviewee,DialogTreeDef def,Quest quest)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            targets.Add("Interviewer", interviewer);
            targets.Add("Interviewee", interviewee);

            DialogElement_Option result = new DialogElement_Option(
                GameTools.GetDialogText(this.text.ResolveTags(), interviewer, interviewee, def, quest), () => { });
            if (this.Disabled(targets,quest,out var reason))
            {
                result.disabled = true;
                result.disableReason = (reason);
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
                    result.disabled = true;
                    result.disableReason = ("NoRequiredThing".Translate(thingDef, requiredThingDefs, limit));
                }
            }

            var dR = this.ProduceResult(interviewee, interviewer, quest);
            result.nextIndex = dR.nextIndex;
            result.action = () =>
            {
                dR.actions.ForEach(a => a.Work(targets, quest));
                if (this.removeDialogAfterSelect)
                {
                    GameComponent_Editor.Component.RemoveDialog(interviewer);
                }
                GameTools.ConsumeRequiredThings(interviewer as Pawn, interviewee as Pawn, this.requiredThings); 
            };
            return [result];
        }

        public virtual float Draw(Rect inRect,QuestEditor_Dialog parent, DialogNode node)
        {
            float y = 5f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f,y,inRect.width,40f),this.GetType().Name.Translate());
            Text.Font = GameFont.Small;
            y += 40f;
            CQFEditorTools.DrawLabelAndText_Line(y, "OptionText".Translate(), ref this.text, 0f, 250f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(0f, y, 450f, 25f), "HideWhenDisable".Translate(),
                ref this.hideWhenDisabled);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(0f, y, 450f, 25f), "removeDialogAfterSelect".Translate(),
                ref this.removeDialogAfterSelect);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(0f, y, 450f, 25f), "hideFailReason".Translate(),
                ref this.hideFailReason);
            y += 30f;
            List<Type> thingDatas = typeof(CQFThingData).AllSubclassesNonAbstract().ListFullCopy();
            thingDatas.Remove(typeof(CQFThingCategoryCount));
            CQFEditorTools.DrawIDrawList(ref y, 10f, this.requiredThings, inRect,
                "InteractionOption_RequiredThing".Translate(), () =>
                    CQFEditorTools.DrawFloatMenu(thingDatas,
                        t => { CQFThingData.OpenSelectWindow(t, d => this.requiredThings.Add(d)); },
                        t => t.Name.Translate()), t => t.ToString(), (t, y2, rect, x) =>
                {
                    t.DrawWithSingleCount(ref y2, rect, x);
                    return y2;
                });
            y += 10f;
            Widgets.Label(new Rect(0f, y, 150f, 25f), "DialogConditions".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            foreach (DialogCondition condition in this.conditions)
            {
                condition.Draw(ref y, inRect, 0f);
            }

            CQFEditorTools.DrawButtonForList(ref y, this.conditions, d => d.GetType().Name.Translate(),
                () => CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(),
                    x => this.conditions.Add((DialogCondition)Activator.CreateInstance(x)),
                    x => x.Name.Translate()), 10f);
            y += 30f;
            float x2 = 0f;
            Widgets.Label(new Rect(x2, y, 255f, 25f), "DialogResults".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            Vector2 start = new Vector2(x2, y);
            Vector2 end = new Vector2(inRect.width - (x2 * 2) - 10f, y);
            Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            foreach (DialogResult d in this.results)
            {
                y += 5f;
                if (Widgets.ButtonText(new Rect(x2, y, 500f, 25f), d.resultName, false))
                {
                    if (Find.WindowStack.Windows.ToList()
                            .Find(x => x.GetType() == typeof(Dialog_EditDialogResult)) is Window window)
                    {
                        window.Close();
                    }

                    Find.WindowStack.Add(new Dialog_EditDialogResult(parent, d, this, node));
                }

                y += 30f;
                start.y = y;
                end.y = y;
                Widgets.DrawLine(start, end, ColorLibrary.SkyBlue, 1f);
            }

            y += 25f;
            CQFEditorTools.DrawButtonForList(ref y, this.results, d => d.GetType().Name.Translate(),
                () => this.results.Add(new DialogResult()), 10f);
            return y;
        }

        public virtual float GetRequiredSpace(DialogTreeDef tree)
        {
            float result = 0f;
            DialogNode parent = null;
            List<DialogNode> subNodes = new List<DialogNode>();
            foreach (KeyValuePair<int, DialogNode> node in tree.nodeMoulds)
            {
                if (node.Value.options.Contains(this))
                {
                    parent = node.Value; 
                }
                if (this.results.Exists(r => r.nextIndex == node.Key))
                {
                    subNodes.Add(node.Value);
                }
            }

            foreach (DialogNode node in subNodes)
            {
                if (parent.subNodeIndexs.Contains(node.index.Value))
                {
                    result += node.GetRequiredSpace(tree);   
                }
            }

            return Math.Max(result, 40f);
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            result.Add(new XElement("text", this.text));
            if (this.hideWhenDisabled)
            {
                result.Add(new XElement("hideWhenDisabled", this.hideWhenDisabled));
            }
            if (this.removeDialogAfterSelect)
            {
                result.Add(new XElement("removeDialogAfterSelect", this.removeDialogAfterSelect));
            }
            if (this.hideFailReason)
            {
                result.Add(new XElement("hideFailReason", this.hideFailReason));
            }
            //result.Add(new XElement("requiredThingsWillBeGivenToInterviewer", this.requiredThingsWillBeGivenToInterviewer));
            if (this.conditions.Any())
            {
                XElement conditions = new XElement("conditions");
                this.conditions.ForEach(c =>
                {
                    conditions.Add(c.SaveToXElement("li"));
                });     
                result.Add(conditions);
            }
            if (this.results.Any()) 
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.results, "results"));
            }
            if (this.requiredThings.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.requiredThings, "requiredThings"));
            }
            return result;
        }
        public string DebugInformation(DialogTreeDef tree)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("所需空间：" + this.GetRequiredSpace(tree));
            return result.ToString().Trim();
        }


        public string text = "Default";
        public bool hideWhenDisabled = false;
        public bool hideFailReason = false;
        public bool removeDialogAfterSelect = false;
        public List<DialogCondition> conditions = new List<DialogCondition>();
        public List<DialogResult> results = new List<DialogResult>() {new DialogResult()};
        public List<CQFThingData> requiredThings = new List<CQFThingData>();
        //public bool requiredThingsWillBeGivenToInterviewer = false;
    }
}