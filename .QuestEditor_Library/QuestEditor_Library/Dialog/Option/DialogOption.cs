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
                    GameComponent_Editor.Instance.RemoveDialog(interviewer);
                }
                GameTools.ConsumeRequiredThings(interviewer as Pawn, interviewee as Pawn, this.requiredThings); 
            };
            return [result];
        }
        internal void DrawSectionHeader(ref float y, float x, float width, string label, Action addAction,
            Action removeAction, Func<bool> canRemove)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                label,
                addAction,
                removeAction,
                canRemove
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogOption.DrawSectionHeader(Ref:float,None:float,None:float,None:string,None:System.Action,None:System.Action,None:System.Func<bool>)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Rect rect = new Rect(x + 6f, startY - 2f, width - 12f, Mathf.Max(34f, endY - startY + 4f));
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawLine(new Vector2(rect.x + 6f, rect.yMax), new Vector2(rect.xMax - 6f, rect.yMax), ColorLibrary.SkyBlue, 1f);
        }
        internal void DrawEmptyState(ref float y, float x, float width, string label)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                label
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogOption.DrawEmptyState(Ref:float,None:float,None:float,None:string)", this, arguments);
            y = (float)arguments[0];
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
            if (this.editorPositionSet)
            {
                result.Add(new XElement("editorX", this.editorX));
                result.Add(new XElement("editorY", this.editorY));
                result.Add(new XElement("editorPositionSet", true));
            }
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
                result.Add(CQFSerialization.SaveList_Saveable(this.results, "results"));
            }
            if (this.requiredThings.Any())
            {
                result.Add(CQFSerialization.SaveList_Saveable(this.requiredThings, "requiredThings"));
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
        public float editorX;
        public float editorY;
        public bool editorPositionSet;
        //public bool requiredThingsWillBeGivenToInterviewer = false;
    }
}
