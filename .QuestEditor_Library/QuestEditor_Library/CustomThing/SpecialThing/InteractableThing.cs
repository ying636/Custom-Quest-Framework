using System;
using System.Collections.Generic;
using System.Linq;
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
            string result = base.GetInspectString();
            this.AllInteraction.ForEach(x => result += " " + x.interactionText);
            result += "CQF_InteracteThing".Translate();
            return result.Trim();
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
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref this.scrollPos, new Rect(7f, 10f, 475f, this.height));
            Widgets.DrawBox(new Rect(8f, 10f, 470f, this.height), 1, QuestEditor_Dialog.blueTex);
            float y = 20f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, 900f, 38f), "InteractionOperations".Translate().Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            if (Widgets.ButtonImage(new Rect(450f, y, 25f, 25f), TexButton.Copy))
            {
                this.operations.ForEach(o => CQFEditorTools.operations.Add(o.Copy()));
                this.operationDefs.ForEach(o => CQFEditorTools.operationDefs.Add(o));
            }
            TooltipHandler.TipRegion(new Rect(450f, y, 25f, 25f), "Copy".Translate());
            y += 40f;
            Rect rect = new Rect(15f, y, 400f, 30f);
            for(int i = 0; i<this.operations.Count; i++)
            {
                InteractionOperation o = this.operations[i];
                rect.y = y;
                if (Widgets.ButtonText(rect, o.interactionText, false))
                {
                    Find.WindowStack.Add(new Dialog_InteractionOption(o));
                }
                if (Widgets.ButtonImage(new Rect(450f, y, 25f, 25f), TexButton.Copy))
                {
                    CQFEditorTools.operation = o.Copy();
                }
                TooltipHandler.TipRegion(new Rect(450f, y, 25f, 25f), "Copy".Translate());
                Rect save = new Rect(420f,y,25f,25f);
                if (Widgets.ButtonImage(save, ContentFinder<Texture2D>.Get("UI/Icon_MoveOut", true)))
                {
                    Find.WindowStack.Add(new Dialog_RenameForQE(name =>
                    {
                        LongEventHandler.QueueLongEvent(() =>
                    {
                        InteractionDataDef def = new InteractionDataDef();
                        def.defName = name;
                        def.label = o.interactionText;
                        def.interactions = new List<InteractionOperation>() { o };
                        DefDatabase<InteractionDataDef>.Add(def);
                        string path = Page_QuestEditor.Path + @"\Data\" + o.interactionText + ".xml";
                        XElement defs = new XElement("Defs");
                        XElement defXml = new XElement("QuestEditor_Library.InteractionDataDef");
                        XElement interactionDataDefXml = new XElement("interactions");
                        interactionDataDefXml.Add(o.SaveToXElement("li"));
                        defXml.Add(new XElement("defName", name));
                        defXml.Add(new XElement("label", o.interactionText));
                        defXml.Add(interactionDataDefXml);
                        defs.Add(defXml);
                        defs.Save(path);
                        Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                    }, "SavingAsDef".Translate(), true, e => Log.Message(e.Message));
                    })
                    {optionalTitle = "SetDefname".Translate()});
                }
                TooltipHandler.TipRegion(save, "SaveAsDef".Translate());
                y += 35f;
            };
            y += 5f;       
            if (Widgets.ButtonImage(new Rect(212.5f, y, 25f, 25f), TexButton.Paste) && CQFEditorTools.operation != null)
            {
                this.operations.Add(CQFEditorTools.operation.Copy());
            }
            TooltipHandler.TipRegion(new Rect(212.5f, y, 25f, 25f), "Paste".Translate());
            CQFEditorTools.DrawButtonForList(ref y, this.operations, x => x.interactionText,10f);
            y += 5f;
            CQFEditorTools.DrawEditableList(this.operationDefs,ref y,(r,o) => Widgets.Label(r,o.label),o => o.label,() => 
            CQFEditorTools.DrawFloatMenu(DefDatabase<InteractionDataDef>.AllDefsListForReading,d => this.operationDefs.Add(d),d => d.label),"InteractionDataDefs".Translate(),null,true,15f,320f);
            this.height = y + 5f;
            Widgets.EndScrollView();
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
            this.operations.AddRange(CQFEditorTools.operations.ListFullCopy());
            this.operationDefs.AddRange(CQFEditorTools.operationDefs.ListFullCopy());
        }

        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_InteractableThing(this,pos);
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
            float curY = y;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, curY, inRect.width, 35f), this.interactionText.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            curY += 30f;
            if (Widgets.ButtonText(new Rect(x, curY, 150f, 25f), "Rename".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE((name) => this.interactionText = name));
            }
            curY += 30f;
            CQFEditorTools.DrawLabelAndText_Line(curY, "TickToOperate".Translate(), ref this.tickToOperate, ref this.buffer, x);
            curY += 30f;
            Widgets.CheckboxLabeled(new Rect(x, curY, 350f, 25f), "onlyGenerateSingleResult".Translate(), ref this.onlyGenerateSingleResult);
            curY += 30f;
            CQFEditorTools.DrawIDrawList(ref curY, x, this.requiredThings, inRect, "InteractionOption_RequiredThing".Translate(), () =>
            CQFEditorTools.DrawFloatMenu(new List<Type>() {typeof(CQFThingDefCount) }, t =>
            {
                CQFThingData.OpenSelectWindow(t, d => this.requiredThings.Add(d));
            }, t => t.Name.Translate()), t => t.ToString(), (t, y2, rect, x2) =>
            {
                t.DrawWithSingleCount(ref y2, rect, x2);
                return y2;
            });
            float initY = curY;
            curY += 15f;
            Widgets.Label(new Rect(x + 5f, curY, inRect.width, 30f), "InteractionConditions".Translate().Colorize(ColorLibrary.PaleBlue));
            curY += 25f;
            this.conditions.ForEach(c =>
            {
                c.Draw(ref curY, inRect, x + 5f);
            });
            curY += 10f;
            CQFEditorTools.DrawButtonForList(ref curY, this.conditions, c => c.GetType().Name.Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(), c =>
                this.conditions.Add((DialogCondition)Activator.CreateInstance(c)), c => c.Name.Translate()), 10, 150f);
            curY += 5f;
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), curY - initY), 1, QuestEditor_Dialog.blueTex);
            curY += 5f;
            initY = curY;
            curY += 5f;
            Widgets.Label(new Rect(x + 5f, curY, inRect.width, 30f), "InteractionResults".Translate().Colorize(ColorLibrary.PaleBlue));

            curY += 35f;
            this.results.ForEach(c =>
            {
                curY += 3f;
                c.Draw(ref curY, inRect, x + 5f);
                curY += 3f;
            });
            curY += 10f;
            CQFEditorTools.DrawButtonForList<InteractionResult>(ref curY, this.results, c => c.resultName, 10, 150f);
            curY += 5f; 
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), curY - initY), 1, QuestEditor_Dialog.blueTex);
            curY += 5f;
            y = curY;
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
                GameTools.temporaryTargets.Clear();
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
                XElement results = CQFEditorTools.SaveList_Saveable(this.requiredThings, "requiredThings");
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
        public void Draw(ref float y,Rect inRect,float x = 0f)
        {
            x += 5f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, 350f, 35f),this.resultName.Colorize(ColorLibrary.SkyBlue));   
            Text.Font = GameFont.Small;
            y += 40f;
            if (this.show)
            {
                Rect rect = new Rect(x + 360f, y - 40f, 25f, 25f);
                if (Widgets.ButtonImage(rect, CQFEditorTools.hideIcon))
                {
                    this.show = false;
                }
                TooltipHandler.TipRegion(rect, "Hide".Translate());
                if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), "Rename".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_RenameForQE((name) => this.resultName = name));
                }
                y += 30f;
                float initY = y;
                Widgets.Label(new Rect(x, y, 150f, 25f), "If".Translate().Colorize(ColorLibrary.PaleBlue));
                CQFEditorTools.DrawButtonWithIcon(y, () => Find.WindowStack.Add(new Dialog_Select<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), null, c => c.Name.Translate(), "Select".Translate(), c =>
    this.conditions.Add((DialogCondition)Activator.CreateInstance(c)))), () => CQFEditorTools.DrawFloatMenu(this.conditions, c => this.conditions.Remove(c), c => c.GetType().Name.Translate()), inRect.width - 150f, 30);
                y += 30f;
                foreach (DialogCondition c in this.conditions)
                {
                    c.Draw(ref y, inRect, x);
                }
                y += 5f;
                CQFEditorTools.DrawActionList(ref y, x, this.actions, inRect, "InteractionActions".Translate().Colorize(ColorLibrary.SkyBlue), false);
            }
            else 
            {
                Rect rect = new Rect(x + 360f, y - 40f, 25f, 25f);
                if (Widgets.ButtonImage(rect,CQFEditorTools.showIcon))
                {
                    this.show = true;
                }
                TooltipHandler.TipRegion(rect,"Show".Translate());
            }
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


        bool show = true;
    }
}