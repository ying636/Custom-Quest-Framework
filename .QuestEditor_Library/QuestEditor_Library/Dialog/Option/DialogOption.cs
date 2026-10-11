using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
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
        public float Draw(Rect inRect, Action<DialogResult> editResult, Action<DialogResult> showMenu, Action changed)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope();
            float x = 8f;
            float y = 8f;
            float width = inRect.width - 18f;
            Widgets.DrawHighlight(new Rect(x + 4f, y - 2f, width - 8f, 32f));
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 16f, 25f), GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            y += 40f;
            CQFEditorTools.DrawLabelAndText_Line(y, "OptionText".Translate(), ref text, x + 8f, 180f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "HideWhenDisable".Translate(), ref hideWhenDisabled);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "removeDialogAfterSelect".Translate(), ref removeDialogAfterSelect);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, width - 16f, 25f), "hideFailReason".Translate(), ref hideFailReason);
            y += 40f;
            List<Type> thingDatas = typeof(CQFThingData).AllSubclassesNonAbstract().ListFullCopy();
            thingDatas.Remove(typeof(CQFThingCategoryCount));
            DrawSectionHeader(ref y, x, width, "InteractionOption_RequiredThing".Translate(), () => CQFEditorTools.DrawFloatMenu(thingDatas, type => CQFThingData.OpenSelectWindow(type, data => requiredThings.Add(data)), type => type.Name.Translate()), () => CQFEditorTools.DrawFloatMenu(requiredThings, data => requiredThings.Remove(data), data => data.ToString()), () => requiredThings.Any());
            foreach (CQFThingData data in requiredThings)
            {
                float itemY = y;
                data.DrawWithSingleCount(ref y, inRect, x + 16f);
                DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }

            if (!requiredThings.Any())
                DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoRequiredThings".Translate());
            y += 10f;
            DrawSectionHeader(ref y, x, width, "DialogConditions".Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(), type => conditions.Add((DialogCondition)Activator.CreateInstance(type)), type => type.Name.Translate()), () => CQFEditorTools.DrawFloatMenu(conditions, condition => conditions.Remove(condition), condition => condition.GetType().Name.Translate()), () => conditions.Any());
            foreach (DialogCondition condition in conditions)
            {
                float itemY = y;
                condition.Draw(ref y, inRect, x + 16f);
                DrawListItemFrame(itemY, y, x, width);
                y += 8f;
            }

            if (!conditions.Any())
                DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogConditions".Translate());
            y += 10f;
            DrawSectionHeader(ref y, x, width, "DialogResults".Translate(), () => results.Add(new DialogResult()), () => CQFEditorTools.DrawFloatMenu(results, result => results.Remove(result), result => result.resultName), () => results.Any());
            float resultsHeight = results.Count * CQFDialogResultList.RowHeight;
            resultLists.GetValue(this, _ => new CQFDialogResultList()).Draw(this, new Rect(x + 8f, y, width - 16f, resultsHeight), result => result.resultName.CanTranslate() ? result.resultName.Translate().ToString() : result.resultName, editResult, showMenu, changed);
            y += resultsHeight;
            if (!results.Any())
                DrawEmptyState(ref y, x + 8f, width - 16f, "CQF_NoDialogResults".Translate());
            y += 10f;
            return y;
        }

        public virtual DialogResult ProduceResult(Thing target, Thing interviwer, Quest quest)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            targets.Add("Interviewee", target);
            targets.Add("Interviewer", interviwer);
            return this.results.Find(r => !r.conditions.Exists(c => !c.Satisfied(targets, out string reason, quest))) ?? new DialogResult()
            {
            };
        }

        public virtual bool Disabled(Dictionary<string, TargetInfo> targets, Quest quest, out string reason)
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

        public virtual List<DialogElement_Option> GetDEOptions(Thing interviewer, Thing interviewee, DialogTreeDef def, Quest quest)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            targets.Add("Interviewer", interviewer);
            targets.Add("Interviewee", interviewee);
            DialogElement_Option result = new DialogElement_Option(GameTools.GetDialogText(this.text.ResolveTags(), interviewer, interviewee, def, quest), () =>
            {
            });
            if (this.Disabled(targets, quest, out var reason))
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

                if (!GameTools.CheckRequiredThings(this.requiredThings, things, out var thingDef, out var requiredThingDefs, out var limit))
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
            return[result];
        }

        internal void DrawSectionHeader(ref float y, float x, float width, string label, Action addAction, Action removeAction, Func<bool> canRemove)
        {
            Rect headerRect = new Rect(x + 4f, y - 2f, width - 8f, 32f);
            Widgets.DrawHighlight(headerRect);
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 84f, 25f), label.Colorize(CQFUIStyle.Accent));
            Rect buttonRect = new Rect(x + width - 66f, y + 2f, 25f, 25f);
            if (CQFUIStyle.ButtonImage(buttonRect, TexButton.Plus))
            {
                addAction();
            }

            TooltipHandler.TipRegion(buttonRect, "Add".Translate());
            buttonRect.x += 30f;
            if (CQFUIStyle.ButtonImage(buttonRect, TexButton.Delete) && canRemove())
            {
                removeAction();
            }

            TooltipHandler.TipRegion(buttonRect, "Remove".Translate());
            y += 40f;
        }

        internal void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Rect rect = new Rect(x + 6f, startY - 2f, width - 12f, Mathf.Max(34f, endY - startY + 4f));
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawLine(new Vector2(rect.x + 6f, rect.yMax), new Vector2(rect.xMax - 6f, rect.yMax), CQFUIStyle.Accent, 1f);
        }

        internal void DrawEmptyState(ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(CQFUIStyle.Muted));
            y += 32f;
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
        public List<DialogResult> results = new List<DialogResult>()
        {
            new DialogResult()
        };
        public List<CQFThingData> requiredThings = new List<CQFThingData>();
        public float editorX;
        public float editorY;
        public bool editorPositionSet;
        private static readonly ConditionalWeakTable<DialogOption, CQFDialogResultList> resultLists = new ConditionalWeakTable<DialogOption, CQFDialogResultList>();
    //public bool requiredThingsWillBeGivenToInterviewer = false;
    }
}
