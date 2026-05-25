using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class DialogManagerDef : Def, ISaveable
    {
        public void Draw(ref float y)
        {
            float last = 80f;
            Dictionary<int, DialogTreeAndConditions> replaces = new Dictionary<int, DialogTreeAndConditions>();
            foreach (DialogTreeAndConditions tree in this.trees)
            {
                int index = this.trees.IndexOf(tree);
                Rect rect = new Rect(10f, y, 350f, 40f);
                Widgets.DrawBox(new Rect(5f, y - 5f, 350f, this.heights.ContainsKey(index) ? this.heights[index] - 40f : 0f), 1, QuestEditor_Dialog.blueTex);
                y += 10f;
                rect.height = 30f;
                if (Widgets.ButtonText(rect, "DialogTree".Translate(tree.tree?.defName), false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<DialogTreeDef>.AllDefsListForReading, (x) =>
                    {
                        replaces.Add(index, new DialogTreeAndConditions(x, tree.conditions));
                    }, (x) => x.defName);
                }
                y += 30;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(10f, y, 100f, 30f), "Conditions".Translate().Colorize(ColorLibrary.SkyBlue));
                Text.Font = GameFont.Small;
                y += 40f;
                foreach (DialogCondition condition in tree.conditions)
                {
                    condition.Draw(ref y,rect,10f);
                }
                y += 5f;
                Rect button = new Rect(10f, y, 100f, 30f);
                if (Widgets.ButtonText(button, "Add".Translate()))
                {
                    CQFEditorTools.DrawFloatMenu<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), (x) =>
                    {
                        DialogCondition c = (DialogCondition)Activator.CreateInstance(x);
                        tree.conditions.Add(c);
                    }, x => x.Name.Translate());
                }
                button.x += 110f;
                if (Widgets.ButtonText(button, "Delete".Translate()) && tree.conditions.Any())
                {
                    CQFEditorTools.DrawFloatMenu<DialogCondition>(tree.conditions, (x) =>
                    {
                        tree.conditions.Remove(x);
                    }, x => x.GetType().Name.Translate());
                }
                y += 90f;
                this.heights.SetOrAdd(index, y - last);
                last = y;
            }
            foreach (KeyValuePair<int, DialogTreeAndConditions> replace in replaces)
            {
                this.trees[replace.Key] = replace.Value;
            }
        }
        public DialogTreeDef GetTree(Thing interviewer, Thing interviewee)
        {
            foreach (DialogTreeAndConditions tree in this.trees)
            {
                Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
                targets.Add("Interviewee", interviewee);
                targets.Add("Interviewer", interviewer);
                if (tree.conditions == null || !tree.conditions.Any() || !tree.conditions.Exists(c => !c.Satisfied(targets,out string reason, GameTools.GetQuestFromThing(interviewee) ?? GameTools.GetQuestFromThing(interviewer))))
                {
                    if (tree.tree != null)
                    {
                        return tree.tree;
                    }
                }
            }
            return null;
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", this.defName));
            XElement nodes = new XElement("trees");
            this.trees.ForEach(t => nodes.Add(t.SaveToXElement("li")));
            result.Add(nodes);
            if (!this.removeWhenThingDespawned)
            {
                result.Add(new XElement("removeWhenThingDespawned", this.removeWhenThingDespawned));
            }
            if (!this.removeWhenPawnDied)
            {
                result.Add(new XElement("removeWhenPawnDied", this.removeWhenPawnDied));
            }
            if (this.iconColor != ColorLibrary.BrightBlue) 
            {
                result.Add(new XElement("iconColor", this.iconColor));
            }
            result.Add(CQFEditorTools.SaveList(this.tags, "tags"));
            if (this.genrationConditions != null && this.genrationConditions.Any()) 
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.genrationConditions, "genrationConditions"));
            }
            if (this.forcedTraits != null && this.forcedTraits.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.forcedTraits, "forcedTraits"));
            }
            return result;
        }

        public bool removeWhenThingDespawned = true;
        public bool removeWhenPawnDied = true;
        public Color iconColor = ColorLibrary.BrightBlue;
        public List<string> tags = new List<string>();
        public Dictionary<int, float> heights = new Dictionary<int, float>();
        public List<DialogTreeAndConditions> trees = new List<DialogTreeAndConditions>();
        public List<DialogCondition> genrationConditions = new List<DialogCondition>();
        public List<TraitData> forcedTraits = new List<TraitData>();
    }
    public class DialogTreeAndConditions : ISaveable, IExposable
    {
        public DialogTreeAndConditions()
        {
        }
        public DialogTreeAndConditions(DialogTreeDef tree, List<DialogCondition> conditions)
        {
            this.tree = tree;
            this.conditions = conditions;
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            XElement nodes = new XElement("tree", this.tree.defName);
            XElement conditions = new XElement("conditions");
            this.conditions.ForEach(c => conditions.Add(c.SaveToXElement("li")));
            result.Add(nodes);
            result.Add(conditions);
            return result;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.tree, "DialogTreeAndConditions_tree");
            Scribe_Collections.Look(ref this.conditions, "DialogManager_conditions", LookMode.Deep);
        }

        public DialogTreeDef tree;
        public List<DialogCondition> conditions;
    }
    public class DialogTreeDef : Def, ISaveable
    {
        public void Update()
        {
            this.idleNodes.Clear();
            foreach (var keyValuePair in this.nodeMoulds)
            {
                if (this.IsIdleNode(keyValuePair.Value))
                {
                    this.AddIdleNode(keyValuePair.Value);
                }
            }
        }

        public void AddIdleNode(DialogNode node)
        {
            this.idleNodes.Add(node);
            node.subNodeIndexs.ForEach(i =>
            {
                DialogNode subNode = this.nodeMoulds[i];
                if (this.IsIdleNode(subNode))
                {
                    this.AddIdleNode(subNode);
                }
            });
        }
        public bool IsIdleNode(DialogNode node)
        {
            return node.index != 0 && !this.nodeMoulds.Values.ToList().Exists(x => 
                x.options.Exists(o => o.results.Exists(r => r.nextIndex == node.index)));
        }
        public void ChangeNextNodeToOtherNode(DialogNode parent, DialogNode newNode,DialogResult result, bool isNewNode = false)
        {
            DialogNode oldNode = result.nextIndex == null ? null : this.nodeMoulds[result.nextIndex.Value];      
            if (oldNode != null &&
                parent.subNodeIndexs.Contains(oldNode.index.Value)
                && !parent.options.Exists(o => o.results.Exists(r => r!=result
                                                                     && r.nextIndex == result.nextIndex)))
            {
                parent.subNodeIndexs.Remove(oldNode.index.Value);
                this.AddIdleNode(oldNode);
            }
            if (newNode != null)
            {
                if (newNode.index != 0 && newNode != parent && (this.idleNodes.Contains(newNode) || isNewNode))
                {
                    parent.subNodeIndexs.Add(newNode.index.Value);
                }
                if (this.idleNodes.Contains(newNode))
                {
                    this.idleNodes.Remove(newNode);
                }
                result.nextIndex = newNode.index.Value;
            }
            else
            {
                result.nextIndex = null;
            }
        }
        public DialogNode CreateNewNode(DialogNode parent)
        {
            DialogNode result = new DialogNode(this.curIndex);
            this.nodeMoulds.Add(this.curIndex, result);
            this.curIndex++;
            if (parent != null)
            {
                result.parentIndex = parent.index;
            }
            return result;
        }
        // public Dialog_NodeTree CreateDialog(Thing interviewee, Thing interviewer,Quest quest = null)
        // {   
        //     quest = quest ?? GameTools.GetQuestFromThing(interviewer) ?? GameTools.GetQuestFromThing(interviewee);
        //     Dictionary<int, DiaNode> nodes = new Dictionary<int, DiaNode>();
        //     Dictionary<DiaOption,DialogResult> resultDictionary = new Dictionary<DiaOption,DialogResult>();
        //     foreach (KeyValuePair<int, DialogNode> nodeMould in this.nodeMoulds)
        //     {
        //         List<string> texts = new List<string>();
        //         texts.Add(nodeMould.Value.text);
        //         texts.AddRange(nodeMould.Value.extraText);
        //         string text = GameTools.GetDialogText(texts.RandomElement(), interviewer,interviewee,this, quest);
        //         DiaNode node = new DiaNode(text);
        //    
        //         nodeMould.Value.options.ForEach(o =>
        //         {
        //             foreach (var or in o.GetOptions(interviewer, interviewee,this, quest))
        //             {
        //                 var option = or.option;
        //                 var diaResult = or.result;
        //                 resultDictionary.Add(option, diaResult);
        //
        //                 if (o.requiredThings.Any())
        //                 {
        //                     if (interviewee.Map != null && interviewee.Map.IsPlayerHome)
        //                     {
        //                         List<Thing> things = GameTools.AllConsumableThing(interviewee.Map).ToList();
        //                         if (interviewee is Pawn p && p.inventory != null)
        //                         {
        //                             things.AddRange(p.inventory.innerContainer.InnerListForReading);
        //                         }
        //                         if (!GameTools.CheckRequiredThings(o.requiredThings, things, out ThingDef def, out int count, out int limit))
        //                         {
        //                             option.Disable("NoRequiredThing".Translate(def, count, limit));
        //                         }
        //                     }
        //                     else if (interviewee.ParentHolder is Caravan c)
        //                     {
        //                         ThingDef def = null;
        //                         int count = 0;
        //                         int limit = 0;
        //                         if (!GameTools.CheckRequiredThings(o.requiredThings, c.Goods.ToList(), out def, out count, out limit))
        //                         {
        //                             option.Disable("NoRequiredThing".Translate(def, count, limit));
        //                         }
        //                     }
        //                     else
        //                     {
        //                         ThingDef def = null;
        //                         int count = 0;
        //                         int limit = 0;
        //                         if (!(interviewee is Pawn p) || p.inventory == null || !GameTools.CheckRequiredThings(o.requiredThings, ((Pawn)interviewee).inventory.innerContainer.InnerListForReading, out def, out count, out limit))
        //                         {
        //                             option.Disable("NoRequiredThing".Translate(def, count, limit));
        //                         }
        //                     }
        //                 }
        //                 if (o.hideFailReason)
        //                 {
        //                     option.disabledReason = null;
        //                 }
        //                 if (!o.hideWhenDisabled || !option.disabled)
        //                 {
        //                     node.options.Add(option);
        //                 }
        //             }  
        //         });
        //         nodes.Add(nodeMould.Key, node);
        //     }
        //     foreach (KeyValuePair<int, DiaNode> node in nodes)
        //     {
        //         node.Value.options.ForEach(o =>
        //         { 
        //             int? nextIndex = resultDictionary[o].nextIndex;
        //             if (nextIndex == null)
        //             {
        //                 o.resolveTree = true;
        //                 return;
        //             }
        //             o.link = nodes[nextIndex.Value];
        //         });
        //     }
        //     if (!nodes.Any())
        //     {
        //         Log.Error("Create dialog error:Null node");
        //         return null;
        //     }
        //     string title = GameTools.GetDialogText(this.title, interviewer, interviewee, this, quest);
        //     Dialog_NodeTree result = new Dialog_NodeTree(nodes.First().Value, false, false, title);
        //     return result;
        // }

        public CQFDialogTreeWindow CreateCQFDialog(Thing interviewee, Thing interviewer, Quest quest = null)
        {
            quest = quest ?? GameTools.GetQuestFromThing(interviewer) ?? GameTools.GetQuestFromThing(interviewee);
            string title = GameTools.GetDialogText(this.title, interviewer, interviewee, this, quest);
            CQFDialogTreeWindow result = new CQFDialogTreeWindow(
                title,interviewee,interviewer, quest,this);
            return result;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", this.defName));
            result.Add(new XElement("title", this.title));
            result.Add(new XElement("requireNonHostile", this.requireNonHostile));
            result.Add(new XElement("dialogReportKey", this.dialogReportKey));
            result.Add(new XElement("curIndex", this.curIndex));
            if (this.idleNodes.Any())
            {
                XElement idleNodes = new XElement("idleNodes");
                this.idleNodes.ForEach(x => idleNodes.Add(x.SaveToXElement("li")));
                result.Add(idleNodes);
            }
            XElement nodes = new XElement("nodeMoulds");
            foreach (KeyValuePair<int, DialogNode> nodeMould in this.nodeMoulds)
            {
                XElement li = new XElement("li");
                li.Add(new XElement("key", nodeMould.Key));
                li.Add(nodeMould.Value.SaveToXElement("value"));
                nodes.Add(li);
            }
            if (this.extraThingRefers.Any())
            {
                result.Add(CQFEditorTools.SaveList(this.extraThingRefers, "extraThingRefers"));
            }
            result.Add(nodes);
            return result;
        }

        public string title = "DefaultDialogKey";
        public string dialogReportKey = "DefaultDialogKey";
        public bool requireNonHostile = true;
        public int curIndex = 1;
        public List<string> extraThingRefers = new List<string>();
        public List<DialogNode> idleNodes = new List<DialogNode>();
        public Dictionary<int, DialogNode> nodeMoulds = new Dictionary<int, DialogNode>() { [0] = new DialogNode(0) };
    }
    public class DialogNode : ISaveable
    {
        public DialogNode()
        {
        }
        public DialogNode(int index)
        {
            this.index = index;
        }
        public DialogNode(int index, DialogNode parent)
        {
            this.index = index;
            parent.subNodeIndexs.Add(this.index.Value);
        }
        public IDialogElement Get(Thing interviewer, Thing interviewee,DialogTreeDef dialog,Quest quest)
        {  
            List<string> texts =
            [
                this.text
            ];
            texts.AddRange(this.extraText);
            return new DialogElement_Text(GameTools.GetDialogText(texts.RandomElement(), interviewer, interviewee, dialog, quest));
        }
        public string DebugInformation(DialogTreeDef tree)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("父节点索引：" + this.parentIndex);
            result.AppendLine("索引：" + this.index);
            result.AppendLine("子节点：");
            this.subNodeIndexs.ForEach(x => result.AppendLine(x.ToString()));
            result.AppendLine("所需空间：" + this.GetRequiredSpace(tree));
            return result.ToString().Trim();
        }
        public float GetRequiredSpace(DialogTreeDef tree)
        {
            float result = 0f;
            this.options.ForEach(o => result += o.GetRequiredSpace(tree));
            return Math.Max(result,40f);
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("text", this.text));
            result.Add(new XElement("index", this.index));
            if (this.parentIndex != null)
            {
                result.Add(new XElement("parentIndex", this.parentIndex));
            }
            XElement options = new XElement("options");
            this.options.ForEach(x =>
            {
                options.Add(x.SaveToXElement("li"));
            });
            XElement subNodeIndexs = new XElement("subNodeIndexs");
            this.subNodeIndexs.ForEach(x =>
            {
                subNodeIndexs.Add(new XElement("li", x));
            });
            result.Add(subNodeIndexs);
            result.Add(options); 
            if (!this.extraText.NullOrEmpty()) 
            {
                result.Add(CQFEditorTools.SaveList(this.extraText, "extraText"));
            }
            return result;
        }

        public string text = "Default";
        public List<string> extraText = new List<string>();
        public int? index = null;
        public int? parentIndex = null;
        public List<DialogOption> options = new List<DialogOption>();
        public List<int> subNodeIndexs = new List<int>();
    }
    public class DialogResult : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("resultName",this.resultName));
            if (this.actions.Any())
            {
                XElement actions = new XElement("actions");
                this.actions.ForEach(x =>
                {
                    actions.Add(x.SaveToXElement("li"));
                });
                result.Add(actions);
            }
            if (this.nextIndex != null)
            {
                result.Add(new XElement("nextIndex", this.nextIndex));
            }
            if (this.conditions.Any())
            {
                XElement conditions = new XElement("conditions");
                this.conditions.ForEach(c =>
                {
                    conditions.Add(c.SaveToXElement("li"));
                });    
                result.Add(conditions);
            }
            return result;
        }

        public string resultName = "Undefined";
        public List<DialogCondition> conditions = new List<DialogCondition>();
        public List<CQFAction> actions = new List<CQFAction>();
        public int? nextIndex = null;
    }
}