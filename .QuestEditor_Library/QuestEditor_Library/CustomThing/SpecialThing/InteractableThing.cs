using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public class InteractableThing : Building, IDrawTabable,IPastableData, ICustomThing
    {
        public override string Label => this.TextComp == null || !this.textComp.useCustomName ? base.Label : this.textComp.customName;
        public override string DescriptionFlavor => this.TextComp == null || !this.textComp.useCustomDescription ? base.DescriptionFlavor : this.textComp.customDescription;
        public override Graphic Graphic
        {
            get
            {
                if (!this.disable)
                {
                    return base.Graphic;
                }
                if (this.disabledGraphic == null)
                {
                    Graphic baseGraphic = base.Graphic;
                    if (this.def.GetModExtension<ModExtension_CustomThing>() is ModExtension_CustomThing me && me.openedGraphicdata != null)
                    {
                        this.disabledGraphic = me.openedGraphicdata.Graphic;
                        return this.disabledGraphic;
                    }
                    this.disabledGraphic = GraphicDatabase.Get(this.def.graphicData.graphicClass, this.def.graphicData.texPath + "_disabled", baseGraphic.Shader, baseGraphic.drawSize, baseGraphic.color, baseGraphic.colorTwo, baseGraphic.maskPath == null ? null : baseGraphic.maskPath + "_opened");
                }
                return this.disabledGraphic;
            }
        }
        public CompCustomText TextComp
        {
            get
            {
                if (this.textComp == null)
                {
                    this.textComp = this.TryGetComp<CompCustomText>();
                }
                return this.textComp;
            }
        }
        public List<InteractionOperation> AllInteraction 
        {
            get 
            {
                List<InteractionOperation> result = new List<InteractionOperation>();
                result.AddRange(this.operations);
                return result;
            }
        }
        public override string GetInspectString()
        {
            StringBuilder result = new StringBuilder(base.GetInspectString());
            if (!Prefs.DevMode)
            {
                return result.ToString().Trim();
            }

            foreach (InteractionOperation interaction in this.AllInteraction)
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }
                result.Append(interaction.interactionText);
            }
            if (result.Length > 0)
            {
                result.AppendLine();
            }
            result.Append("CQF_InteracteThing".Translate().ToString().Trim());
            return result.ToString().Trim();
        }
        public InteractionOperation GetCurOperation(string operationText) 
        {
            if (this.AllInteraction.Find(x => x.interactionText.Translate() == operationText) is InteractionOperation operation) 
            {
                return operation;
            }
            return null;
        }
        public void DrawTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractableThing.DrawTab()", this, arguments);
        }
        internal void DrawOperationList(ref float y, float width)

        {
            object[] arguments = new object[]
            {
                y,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractableThing.DrawOperationList(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawOperationDefList(ref float y, float width)

        {
            object[] arguments = new object[]
            {
                y,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractableThing.DrawOperationDefList(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public void ProduceResult(Pawn operatorPawn, string operationText)
        {
            if (this.GetCurOperation(operationText) is InteractionOperation op && op != null) 
            {
                Quest quest = GameTools.GetQuestFromThing(this);
                if (DebugSettings.godMode) 
                {
                    Log.Message(quest?.name);
                }
                op.ProduceResult(operatorPawn, this, quest);
            } 
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }
            if (!this.disable)
            {
                if (selPawn.CanReserveAndReach(this, PathEndMode.Touch, Danger.Deadly))
                {
                    foreach (InteractionOperation operation in this.AllInteraction)
                    {
                        string failReason = "Unkown";
                        string text = operation.interactionText.Translate();
                        if (operation.Satisfied(selPawn, this, out failReason, GameTools.GetQuestFromThing(this)))
                        {
                            Job job = JobMaker.MakeJob(QEDefOf.QE_InteractingWithTarget, this);
                            job.reportStringOverride = text;
                            yield return new FloatMenuOption(text, () =>
                            {
                                selPawn.jobs.StopAll();
                                selPawn.jobs.StartJob(job);
                            });
                        }
                        else
                        {
                            yield return new FloatMenuOption($"{text}({failReason})", null);
                        }
                    }
                }
                else
                {
                    yield return new FloatMenuOption("CantReseverveOrReachLootBox".Translate(), null);
                }
            }
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.disable, "InteractableThing_disable");
            Scribe_Collections.Look(ref this.operations, "InteractableThing_operations",LookMode.Deep);
            Scribe_Collections.Look(ref this.operationDefs, "operationDefs", LookMode.Def);
        }

        public void PasteData()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractableThing.PasteData()", this, arguments);
        }
        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_InteractableThing(this,pos);
        }
        internal Rect DrawSectionHeader(ref float y, float width, string label, bool drawCopyButton = false)

        {
            object[] arguments = new object[]
            {
                y,
                width,
                label,
                drawCopyButton
            };
            object result = CQFEditorBridge.Invoke("QuestEditor_Library.InteractableThing.DrawSectionHeader(Ref:float,None:float,None:string,None:bool)", this, arguments);
            y = (float)arguments[0];
            return (UnityEngine.Rect)result;
        }
        internal void DrawSimpleSectionTitle(ref float y, float width, string label)

        {
            object[] arguments = new object[]
            {
                y,
                width,
                label
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractableThing.DrawSimpleSectionTitle(Ref:float,None:float,None:string)", this, arguments);
            y = (float)arguments[0];
        }
        public float height = 0f;
        public Vector2 scrollPos;
        public bool disable = false;
        public Graphic disabledGraphic = null;
        public List<InteractionOperation> operations = new List<InteractionOperation>();
        public List<InteractionDataDef> operationDefs = new List<InteractionDataDef>();
        private CompCustomText textComp = null;
    }
    public class InteractionOperation : ISaveable , IExposable,IDrawable
    {    
        public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractionOperation.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawBasicSettings(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractionOperation.DrawBasicSettings(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawRequiredThings(ref float y, float x, float width, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractionOperation.DrawRequiredThings(Ref:float,None:float,None:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawConditions(ref float y, float x, float width, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractionOperation.DrawConditions(Ref:float,None:float,None:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawResults(ref float y, float x, float width, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractionOperation.DrawResults(Ref:float,None:float,None:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawHeader(ref float y, float x, float width, string label, Action addAction = null, string addTip = null, Action removeAction = null, string removeTip = null, Texture2D addIcon = null)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                label,
                addAction,
                addTip,
                removeAction,
                removeTip,
                addIcon
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractionOperation.DrawHeader(Ref:float,None:float,None:float,None:string,None:System.Action,None:string,None:System.Action,None:string,None:UnityEngine.Texture2D)", this, arguments);
            y = (float)arguments[0];
        }
        public InteractionOperation Copy() 
        {
            XElement x = this.SaveToXElement("InteractionOperation");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            InteractionOperation result = DirectXmlToObject.ObjectFromXml<InteractionOperation>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public void ProduceResult(Pawn interacter, Thing thing, Quest quest)
        {
            foreach (InteractionResult r in this.results)
            {
                if (r.Satisfied(interacter, thing, quest))
                {
                    r.DoResult(interacter, thing, quest);
                    if (this.onlyGenerateSingleResult)
                    {
                        break;
                    }
                }
            }
            Dictionary<ThingCategoryDef, int> categoryAndCount = new Dictionary<ThingCategoryDef, int>();
            foreach (CQFThingData data in this.requiredThings)
            {
                if (data is CQFThingDefCount tData)
                {
                    interacter.inventory.innerContainer.Take(interacter.inventory.innerContainer.ToList().Find(i => i.def == tData.thing), tData.count.min).Destroy();
                }
                if (data is CQFThingCategoryCount cData)
                {
                    categoryAndCount.Add(cData.category, cData.count.min);
                }
            }
            //target.inventory.innerContainer.InnerListForReading.ListFullCopy().ForEach(t => 
            //{
            //    categoryAndCount.ToList().ListFullCopy().ForEach(c => 
            //    {
            //        if (t.HasThingCategory(c.Key)) 
            //        {
            //            int count = t.stackCount;
            //            t.SplitOff(Math.Max(c.Value, count)).Destroy();
            //            categoryAndCount.SetOrAdd(c.Key,Math.Max(0,c.Value - count));
            //            if (categoryAndCount[c.Key] <= 0) 
            //            {
            //                categoryAndCount.Remove(c.Key);
            //            }

            //        }
            //    });
            //});
            QuestUtility.SendQuestTargetSignals(thing.questTags, this.interactionText, thing.Named("SUBJECT"));

            if (!GameTools.isGeneratingMap)
            {
                GameTools.ClearTemporaryTargets();
            }
        }
        public bool Satisfied(Pawn target,Thing thing,out string reason, Quest quest)
        {
            foreach (DialogCondition condition in this.conditions)
            {
                Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
                targets.Add("Trigger", target);
                targets.Add("CustomThing", thing);
                if (!condition.Satisfied(targets, out reason,quest)) 
                {
                    return false;
                }
            }
            foreach (CQFThingData data in this.requiredThings) 
            {
                if (data is CQFThingDefCount tData && target.inventory.Count(tData.thing) < tData.count.min) 
                {
                    reason = "NoRequiredThing".Translate(tData.thing.label
                        , target.inventory.Count(tData.thing), tData.count.min.ToString());
                    return false;
                }
                if (data is CQFThingCategoryCount cData)
                {
                    //int count = 0;
                    //target.inventory.innerContainer.ToList().ForEach(t =>
                    //{
                    //    if (t.HasThingCategory(cData.category))
                    //    {
                    //        count += t.stackCount;
                    //    }
                    //});
                    //if (count < cData.count.min)
                    //{
                    //    reason = "NoRequiredThingCategory".Translate(cData.category.label, cData.count.ToString());
                    //    return false;
                    //} 
                }
            }
            reason = null;
            return true;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.onlyGenerateSingleResult, "InteractionOperation_onlyGenerateSingleResult");
            Scribe_Values.Look(ref this.tickToOperate, "InteractionOperation_tickToOperate");
            Scribe_Values.Look(ref this.interactionText, "InteractionOperation_interactionText");
            Scribe_Collections.Look(ref this.requiredThings, "requiredThings", LookMode.Deep);
            Scribe_Collections.Look(ref this.conditions, "InteractionOperation_conditions", LookMode.Deep);
            Scribe_Collections.Look(ref this.results, "InteractionOperation_results", LookMode.Deep); 
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("interactionText", this.interactionText));
            result.Add(new XElement("tickToOperate", this.tickToOperate));
            if (this.onlyGenerateSingleResult)
            {
                result.Add(new XElement("onlyGenerateSingleResult", this.onlyGenerateSingleResult));
            }
            if (this.results.Any())
            {
                XElement results = new XElement("results");
                this.results.ForEach(x =>
                {
                    results.Add(x.SaveToXElement("li"));
                });
                result.Add(results);
            }
            if (this.requiredThings.Any())
            {
                XElement results = CQFSerialization.SaveList_Saveable(this.requiredThings, "requiredThings");
                result.Add(results);
            }
            if (this.conditions.Any())
            {
                XElement conditions = new XElement("conditions");
                this.conditions.ForEach(x =>
                {
                    conditions.Add(x.SaveToXElement("li"));
                });
                result.Add(conditions);
            }
            return result;
        }

        public string buffer;
        public string interactionText = "DefaultInteractionText";
        public int tickToOperate = 100;
        public bool onlyGenerateSingleResult = false;
        public List<DialogCondition>  conditions = new List<DialogCondition>();
        public List<InteractionResult>  results = new List<InteractionResult>();
        public List<CQFThingData>  requiredThings = new List<CQFThingData>();
    }
    public class InteractionResult : ISaveable, IExposable 
    {
        public void Draw(ref float y, Rect inRect, float x = 0f)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.InteractionResult.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public void DoResult(Pawn target, Thing thing,Quest quest) 
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            targets.Add("Trigger", target);
            targets.Add("CustomThing", thing);
            this.actions.ForEach(x => x.Work(targets, quest));
        }
        public bool Satisfied(Pawn target, Thing thing, Quest quest) 
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            targets.Add("Trigger", target);
            targets.Add("CustomThing", thing);
            foreach (DialogCondition condition in this.conditions)
            {
                if (!condition.Satisfied(targets, out string reason,quest))
                {
                    return false;
                }
            }
            return true;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.resultName, "InteractionResult_resultName");
            Scribe_Collections.Look(ref this.conditions, "InteractionResult_conditions", LookMode.Deep);
            Scribe_Collections.Look(ref this.actions, "InteractionResult_actions", LookMode.Deep);
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            XElement conditions = new XElement("conditions");
            this.conditions.ForEach(x =>
            {
                conditions.Add(x.SaveToXElement("li"));
            });
            XElement actions = new XElement("actions");
            this.actions.ForEach(x =>
            {
                actions.Add(x.SaveToXElement("li"));
            });
            result.Add(new XElement("resultName", this.resultName));
            result.Add(actions);
            result.Add(conditions);
            return result;
        }
        [NoTranslate]
        public string resultName = "DefaultName";
        public List<DialogCondition> conditions = new List<DialogCondition>();
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
