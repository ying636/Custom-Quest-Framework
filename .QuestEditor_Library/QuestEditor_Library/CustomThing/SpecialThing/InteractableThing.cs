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
            Rect outRect = new Rect(8f, 18f, 536f, 584f);
            Rect viewRect = new Rect(0f, 0f, 516f, this.height);
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            float y = 12f;
            Rect copyAllRect = this.DrawSectionHeader(ref y, viewRect.width, "InteractionOperations".Translate(), true);
            if (Widgets.ButtonImage(copyAllRect, TexButton.Copy))
            {
                this.operations.ForEach(o => CQFEditorTools.operations.Add(o.Copy()));
                this.operationDefs.ForEach(o => CQFEditorTools.operationDefs.Add(o));
            }
            TooltipHandler.TipRegion(copyAllRect, "Copy".Translate());
            this.DrawOperationList(ref y, viewRect.width);
            y += 18f;
            this.DrawOperationDefList(ref y, viewRect.width);
            this.height = y + 10f;
            Widgets.EndScrollView();
        }

        private void DrawOperationList(ref float y, float width)
        {
            float initY = y;
            Rect rect = new Rect(18f, y + 6f, 340f, 30f);
            for(int i = 0; i<this.operations.Count; i++)
            {
                InteractionOperation o = this.operations[i];
                rect.y = y + 6f;
                if (Widgets.ButtonText(rect, o.interactionText, false))
                {
                    Find.WindowStack.Add(new Dialog_InteractionOption(o));
                }
                TooltipHandler.TipRegion(rect, "CQF_ClickToEdit".Translate());
                if (Widgets.ButtonImage(new Rect(426f, y + 8f, 25f, 25f), TexButton.Copy))
                {
                    CQFEditorTools.operation = o.Copy();
                }
                TooltipHandler.TipRegion(new Rect(426f, y + 8f, 25f, 25f), "Copy".Translate());
                Rect save = new Rect(456f,y + 8f,25f,25f);
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
                y += 40f;
            };
            if (!this.operations.Any())
            {
                Widgets.Label(new Rect(16f, y + 4f, 420f, 25f), "CQF_NoInteractionOperations".Translate().Colorize(Color.gray));
                y += 34f;
            }
            Widgets.DrawBox(new Rect(10f, initY, width - 42f, Mathf.Max(42f, y - initY)), 1, QuestEditor_Dialog.blueTex);
            y += 10f;
            if (Widgets.ButtonText(new Rect(15f, y, 120f, 32f), "Add".Translate()))
            {
                this.operations.Add(new InteractionOperation());
            }
            if (Widgets.ButtonText(new Rect(155f, y, 120f, 32f), "Remove".Translate()) && this.operations.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.operations, o => this.operations.Remove(o), o => o.interactionText);
            }
            if (Widgets.ButtonImage(new Rect(295f, y + 3f, 25f, 25f), TexButton.Paste) && CQFEditorTools.operation != null)
            {
                this.operations.Add(CQFEditorTools.operation.Copy());
            }
            TooltipHandler.TipRegion(new Rect(295f, y + 3f, 25f, 25f), "Paste".Translate());
            y += 42f;
        }

        private void DrawOperationDefList(ref float y, float width)
        {
            this.DrawSimpleSectionTitle(ref y, width, "InteractionDataDefs".Translate());
            Rect rect = new Rect(18f, y + 6f, width - 90f, 28f);
            foreach (InteractionDataDef def in this.operationDefs)
            {
                rect.y = y + 6f;
                Widgets.Label(rect, def.label ?? def.defName);
                y += 36f;
            }
            if (!this.operationDefs.Any())
            {
                Widgets.Label(new Rect(16f, y + 4f, 420f, 25f), "CQF_NoInteractionDefs".Translate().Colorize(Color.gray));
                y += 34f;
            }
            y += 6f;
            if (Widgets.ButtonText(new Rect(15f, y, 120f, 32f), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<InteractionDataDef>.AllDefsListForReading, d => this.operationDefs.Add(d), d => d.label);
            }
            if (Widgets.ButtonText(new Rect(155f, y, 120f, 32f), "Remove".Translate()) && this.operationDefs.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.operationDefs, d => this.operationDefs.Remove(d), d => d.label);
            }
            y += 42f;
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

        private Rect DrawSectionHeader(ref float y, float width, string label, bool drawCopyButton = false)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, width - 35f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Rect copyRect = Rect.zero;
            if (drawCopyButton)
            {
                float labelWidth = Text.CalcSize(label).x;
                copyRect = new Rect(Mathf.Min(15f + labelWidth + 12f, width - 58f), y + 2f, 25f, 25f);
            }
            Text.Font = GameFont.Small;
            y += 32f;
            return copyRect;
        }

        private void DrawSimpleSectionTitle(ref float y, float width, string label)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(15f, y, width - 35f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            y += 38f;
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
            float width = inRect.width - 35f - x;
            this.DrawBasicSettings(ref y, x, width);
            this.DrawRequiredThings(ref y, x, width, inRect);
            this.DrawConditions(ref y, x, width, inRect);
            this.DrawResults(ref y, x, width, inRect);
        }

        private void DrawBasicSettings(ref float y, float x, float width)
        {
            this.DrawHeader(ref y, x, width, this.interactionText, () => Find.WindowStack.Add(new Dialog_RenameForQE(name => this.interactionText = name)), "Rename".Translate(), null, null, TexButton.Rename);
            float labelWidth = 190f;
            Widgets.Label(new Rect(x + 8f, y + 4f, labelWidth, 25f), "TickToOperate".Translate());
            Widgets.TextFieldNumeric(new Rect(x + labelWidth, y, 90f, 28f), ref this.tickToOperate, ref this.buffer);
            y += 36f;
            Widgets.CheckboxLabeled(new Rect(x + 8f, y, 330f, 28f), "onlyGenerateSingleResult".Translate(), ref this.onlyGenerateSingleResult);
            y += 42f;
        }

        private void DrawRequiredThings(ref float y, float x, float width, Rect inRect)
        {
            this.DrawHeader(ref y, x, width, "InteractionOption_RequiredThing".Translate(), () =>
                CQFEditorTools.DrawFloatMenu(new List<Type>() { typeof(CQFThingDefCount) }, t =>
                {
                    CQFThingData.OpenSelectWindow(t, d => this.requiredThings.Add(d));
                }, t => t.Name.Translate()), "Add".Translate(), () =>
                CQFEditorTools.DrawFloatMenu(this.requiredThings, d => this.requiredThings.Remove(d), d => d.ToString()), "Remove".Translate());
            float initY = y;
            foreach (CQFThingData thing in this.requiredThings)
            {
                thing.DrawWithSingleCount(ref y, inRect, x + 10f);
                y += 6f;
            }
            if (!this.requiredThings.Any())
            {
                Widgets.Label(new Rect(x + 12f, y + 4f, width - 24f, 25f), "CQF_NoRequiredThings".Translate().Colorize(Color.gray));
                y += 34f;
            }
            y += 12f;
        }

        private void DrawConditions(ref float y, float x, float width, Rect inRect)
        {
            this.DrawHeader(ref y, x, width, "InteractionConditions".Translate(), () =>
                CQFEditorTools.DrawFloatMenu(typeof(DialogCondition).AllSubclassesNonAbstract(), c =>
                    this.conditions.Add((DialogCondition)Activator.CreateInstance(c)), c => c.Name.Translate()), "Add".Translate(), () =>
                CQFEditorTools.DrawFloatMenu(this.conditions, c => this.conditions.Remove(c), c => c.GetType().Name.Translate()), "Remove".Translate());
            float initY = y;
            foreach (DialogCondition condition in this.conditions)
            {
                float itemY = y;
                condition.Draw(ref y, inRect, x + 10f);
                y += 4f;
                this.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }
            if (!this.conditions.Any())
            {
                Widgets.Label(new Rect(x + 12f, y + 4f, width - 24f, 25f), "CQF_NoInteractionConditions".Translate().Colorize(Color.gray));
                y += 34f;
            }
            y += 12f;
        }

        private void DrawResults(ref float y, float x, float width, Rect inRect)
        {
            this.DrawHeader(ref y, x, width, "InteractionResults".Translate(), () => this.results.Add(new InteractionResult()), "Add".Translate(), () =>
                CQFEditorTools.DrawFloatMenu(this.results, r => this.results.Remove(r), r => r.resultName), "Remove".Translate());
            float initY = y;
            foreach (InteractionResult result in this.results)
            {
                result.Draw(ref y, inRect, x + 10f);
                y += 10f;
            }
            if (!this.results.Any())
            {
                Widgets.Label(new Rect(x + 12f, y + 4f, width - 24f, 25f), "CQF_NoInteractionResults".Translate().Colorize(Color.gray));
                y += 34f;
            }
            y += 12f;
        }

        private void DrawHeader(ref float y, float x, float width, string label, Action addAction = null, string addTip = null, Action removeAction = null, string removeTip = null, Texture2D addIcon = null)
        {
            Widgets.DrawHighlight(new Rect(x - 4f, y - 2f, width + 8f, 32f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width - 90f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            Rect button = new Rect(x + width - 60f, y + 2f, 25f, 25f);
            if (addAction != null && Widgets.ButtonImage(button, addIcon ?? TexButton.Plus))
            {
                addAction();
            }
            if (addAction != null && addTip != null)
            {
                TooltipHandler.TipRegion(button, addTip);
            }
            button.x += 30f;
            if (removeAction != null && Widgets.ButtonImage(button, TexButton.Delete))
            {
                removeAction();
            }
            if (removeAction != null && removeTip != null)
            {
                TooltipHandler.TipRegion(button, removeTip);
            }
            y += 32f;
            y += 10f;
        }

        private void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Widgets.DrawBox(new Rect(x, startY - 4f, width, Mathf.Max(34f, endY - startY + 8f)), 1, QuestEditor_Dialog.blueTex);
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
            float width = inRect.width - 70f - x;
            Widgets.DrawHighlight(new Rect(x - 4f, y - 2f, width + 8f, 32f));
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(x, y + 4f, width - 75f, 28f), this.resultName.Colorize(ColorLibrary.PaleBlue));
            Text.Font = GameFont.Small;
            Rect renameRect = new Rect(x + width - 60f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(renameRect, TexButton.Rename))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE(name => this.resultName = name));
            }
            TooltipHandler.TipRegion(renameRect, "Rename".Translate());
            Rect toggleRect = new Rect(x + width - 30f, y + 2f, 25f, 25f);
            if (this.show)
            {
                if (Widgets.ButtonImage(toggleRect, CQFEditorTools.hideIcon))
                {
                    this.show = false;
                }
                TooltipHandler.TipRegion(toggleRect, "Hide".Translate());
                y += 30f;
                this.DrawSubHeader(ref y, x + 8f, width - 16f, "If".Translate(), () => Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(DialogCondition).AllSubclassesNonAbstract(), c => c.Name.Translate(), c =>
                    this.conditions.Add((DialogCondition)Activator.CreateInstance(c)), null, null, null, null, null, null), "Select".Translate())), () => CQFEditorTools.DrawFloatMenu(this.conditions, c => this.conditions.Remove(c), c => c.GetType().Name.Translate()));
                foreach (DialogCondition c in this.conditions)
                {
                    float itemY = y;
                    c.Draw(ref y, inRect, x + 8f);
                    y += 4f;
                    this.DrawListItemFrame(itemY, y, x + 4f, width - 8f);
                    y += 8f;
                }
                if (!this.conditions.Any())
                {
                    this.DrawEmptyState(ref y, x + 16f, width - 32f, "CQF_NoResultConditions".Translate());
                }
                this.DrawSubHeader(ref y, x + 8f, width - 16f, "InteractionActions".Translate(), () => CQFEditorTools.OpenCQFActionSelect(t => this.actions.Add((CQFAction)Activator.CreateInstance(t))),
                    () => CQFEditorTools.DrawFloatMenu(this.actions, a => this.actions.Remove(a), a => a.GetType().Name.Translate()));
                foreach (CQFAction action in this.actions)
                {
                    action.Draw(ref y, inRect, x + 8f);
                    y += 6f;
                }
                if (!this.actions.Any())
                {
                    this.DrawEmptyState(ref y, x + 16f, width - 32f, "CQF_NoResultActions".Translate());
                }
            }
            else 
            {
                if (Widgets.ButtonImage(toggleRect,CQFEditorTools.showIcon))
                {
                    this.show = true;
                }
                TooltipHandler.TipRegion(toggleRect,"Show".Translate());
                y += 30f;
            }
            Widgets.DrawLine(new Vector2(x, y), new Vector2(x + width, y), Color.gray, 1f);
            y += 8f;
        }

        private void DrawSubHeader(ref float y, float x, float width, string label, Action addAction, Action removeAction)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(x, y + 4f, width - 70f, 25f), label.Colorize(ColorLibrary.SkyBlue));
            Rect button = new Rect(x + width - 60f, y + 2f, 25f, 25f);
            if (Widgets.ButtonImage(button, TexButton.Plus))
            {
                addAction();
            }
            TooltipHandler.TipRegion(button, "Add".Translate());
            button.x += 30f;
            if (Widgets.ButtonImage(button, TexButton.Delete))
            {
                removeAction();
            }
            TooltipHandler.TipRegion(button, "Remove".Translate());
            y += 32f;
        }

        private void DrawEmptyState(ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(Color.gray));
            y += 32f;
        }

        private void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Widgets.DrawBox(new Rect(x, startY - 4f, width, Mathf.Max(34f, endY - startY + 8f)), 1, QuestEditor_Dialog.blueTex);
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
