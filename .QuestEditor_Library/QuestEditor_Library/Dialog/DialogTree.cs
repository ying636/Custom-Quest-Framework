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
            object[] arguments = new object[]
            {
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogManagerDef.Draw(Ref:float)", this, arguments);
            y = (float)arguments[0];
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
            result.Add(CQFSerialization.SaveList(this.tags, "tags"));
            if (this.genrationConditions != null && this.genrationConditions.Any()) 
            {
                result.Add(CQFSerialization.SaveList_Saveable(this.genrationConditions, "genrationConditions"));
            }
            if (this.forcedTraits != null && this.forcedTraits.Any())
            {
                result.Add(CQFSerialization.SaveList_Saveable(this.forcedTraits, "forcedTraits"));
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
    public partial class DialogTreeDef : Def, ISaveable
    {
        public DialogTreeDef()
        {
            this.nodeMoulds[0].text = "CQF_Dialog_Node_0";
        }

        public void Update()
        {
            this.ResolveOptions();
            this.idleNodes.Clear();
            HashSet<int> visited = new HashSet<int>();
            Queue<int> pending = new Queue<int>();
            foreach (DialogNode node in this.nodeMoulds.Values)
            {
                node.subNodeIndexs.Clear();
                node.parentIndex = null;
            }
            if (this.nodeMoulds.ContainsKey(0))
            {
                visited.Add(0);
                pending.Enqueue(0);
            }
            while (pending.Count > 0)
            {
                DialogNode node = this.nodeMoulds[pending.Dequeue()];
                foreach (DialogResult result in node.options.SelectMany(option => option.results))
                {
                    if (result.nextIndex.HasValue && this.nodeMoulds.TryGetValue(result.nextIndex.Value, out DialogNode next)
                        && visited.Add(result.nextIndex.Value))
                    {
                        node.subNodeIndexs.Add(result.nextIndex.Value);
                        next.parentIndex = node.index;
                        pending.Enqueue(result.nextIndex.Value);
                    }
                }
            }
            this.idleNodes.AddRange(this.nodeMoulds.Where(pair => !visited.Contains(pair.Key)).Select(pair => pair.Value));
        }

        public void AddIdleNode(DialogNode node)
        {
            this.Update();
        }
        public bool IsIdleNode(DialogNode node)
        {
            return node.index != 0 && !this.nodeMoulds.Values.ToList().Exists(x => 
                x.options.Exists(o => o.results.Exists(r => r.nextIndex == node.index)));
        }
        public void ChangeNextNodeToOtherNode(DialogNode? parent, DialogNode? newNode,DialogResult result, bool isNewNode = false)
        {
            result.nextIndex = newNode?.index;
            this.Update();
        }
        public DialogNode CreateNewNode(DialogNode? parent)
        {
            while (this.nodeMoulds.ContainsKey(this.curIndex))
            {
                this.curIndex++;
            }
            DialogNode result = new DialogNode(this.curIndex);
            result.text = "CQF_Dialog_Node_" + this.curIndex;
            this.nodeMoulds.Add(this.curIndex, result);
            this.curIndex++;
            if (parent != null)
            {
                result.parentIndex = parent.index;
            }
            return result;
        }
        // public Dialog_NodeTree CreateDialog(Thing interviewer, Thing interviewee,Quest quest = null)
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

        public CQFDialogTreeWindow CreateCQFDialog(Thing interviewer, Thing interviewee, Quest quest = null)
        {
            quest = quest ?? GameTools.GetQuestFromThing(interviewer) ?? GameTools.GetQuestFromThing(interviewee);
            string title = GameTools.GetDialogText(this.title, interviewer, interviewee, this, quest);
            CQFDialogTreeWindow result = new CQFDialogTreeWindow(
                title,interviewer,interviewee, quest,this);
            return result;
        }

        public XElement SaveToXElement(string nodeName)
        {
            this.ResolveOptions();
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", this.defName));
            result.Add(new XElement("title", this.title));
            result.Add(new XElement("requireNonHostile", this.requireNonHostile));
            result.Add(new XElement("dialogReportKey", this.dialogReportKey));
            result.Add(new XElement("curIndex", this.curIndex));
            result.Add(new XElement("curOptionIndex", this.curOptionIndex));
            XElement optionPool = new XElement("optionMoulds");
            foreach (var option in this.optionMoulds)
                optionPool.Add(new XElement("li", new XElement("key", option.Key), option.Value.SaveToXElement("value")));
            result.Add(optionPool);
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
                result.Add(CQFSerialization.SaveList(this.extraThingRefers, "extraThingRefers"));
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
        public List<IDialogElement> Get(Thing interviewer, Thing interviewee,DialogTreeDef dialog,Quest quest)
        {
            List<IDialogElement> result = new List<IDialogElement>();
            this.images.ForEach(x =>
            {
                if (!x.imagePath.NullOrEmpty())
                {
                    result.Add(new DialogElement_Image(ContentFinder<Texture2D>.Get(x.imagePath, false), x.scale));
                }
            });
            List<string> texts =
            [
                this.text
            ];
            texts.AddRange(this.extraText);
            result.Add(new DialogElement_Text(GameTools.GetDialogText(texts.RandomElement(), interviewer, interviewee, dialog, quest)));
            return result;
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
            if (this.editorPositionSet)
            {
                result.Add(new XElement("editorX", this.editorX));
                result.Add(new XElement("editorY", this.editorY));
                result.Add(new XElement("editorPositionSet", true));
            }
            if (this.parentIndex != null)
            {
                result.Add(new XElement("parentIndex", this.parentIndex));
            }
            XElement options = this.optionsResolved
                ? new XElement("optionIds", this.optionIds.Select(id => new XElement("li", id)))
                : new XElement("options", this.options.Select(option => option.SaveToXElement("li")));
            XElement subNodeIndexs = new XElement("subNodeIndexs");
            this.subNodeIndexs.ForEach(x =>
            {
                subNodeIndexs.Add(new XElement("li", x));
            });
            result.Add(subNodeIndexs);
            result.Add(options);
            if (this.images.Any())
            {
                XElement images = new XElement("images");
                this.images.ForEach(x => images.Add(x.SaveToXElement("li")));
                result.Add(images);
            }
            if (!this.extraText.NullOrEmpty()) 
            {
                result.Add(CQFSerialization.SaveList(this.extraText, "extraText"));
            }
            return result;
        }

        public string text = "Default";
        public List<string> extraText = new List<string>();
        public int? index = null;
        public int? parentIndex = null;
        [Unsaved]
        public List<DialogOption> options = new List<DialogOption>();
        public List<int> optionIds = new List<int>();
        [Unsaved]
        public bool optionsResolved;
        public List<int> subNodeIndexs = new List<int>();
        public List<DialogImage> images = new List<DialogImage>();
        public float editorX;
        public float editorY;
        public bool editorPositionSet;
    }
    public class DialogImage : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("imagePath", this.imagePath));
            result.Add(new XElement("scale", this.scale));
            return result;
        }

        public string imagePath = string.Empty;
        public float scale = 1f;
        public string buffer_scale = "1";
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
