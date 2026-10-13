using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class InteractableThing : Building, IDrawTabable, IPastableData, ICustomThing
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
                    if (this.def.GetModExtension<ModExtension_CustomThing>()is ModExtension_CustomThing me && me.openedGraphicdata != null)
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

                result.Append(interaction.interactionText.CanTranslate() ? interaction.interactionText.Translate().ToString() : interaction.interactionText);
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
            if (this.AllInteraction.Find(x => (x.interactionText.CanTranslate() ? x.interactionText.Translate().ToString() : x.interactionText) == operationText)is InteractionOperation operation)
            {
                return operation;
            }

            return null;
        }

        public void DrawTab()
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect outRect = new Rect(8f, 36f, Mathf.Min(536f, CQFUIScope.ContentWidth - 16f), Mathf.Max(40f, Mathf.Min(566f, CQFUIScope.ContentHeight - 44f)));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(outRect.height, this.height));
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            using CQFUIScope contentScope = new CQFUIScope(viewRect.width);
            float y = 12f;
            Rect copyAllRect = this.DrawSectionHeader(ref y, viewRect.width, "InteractionOperations".Translate(), true);
            if (CQFUIStyle.ButtonImage(copyAllRect, TexButton.Copy))
            {
                CQFEditorTools.operations.Clear();
                CQFEditorTools.operationDefs.Clear();
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

        internal void DrawOperationList(ref float y, float width)
        {
            float initY = y;
            Rect rect = new Rect(12f, y + 4f, Mathf.Max(40f, width - 100f), 32f);
            for (int i = 0; i < this.operations.Count; i++)
            {
                InteractionOperation o = this.operations[i];
                rect.y = y + 4f;
                if (CQFUIStyle.ButtonText(rect, o.interactionText.CanTranslate() ? o.interactionText.Translate().ToString() : o.interactionText, false))
                {
                    Find.WindowStack.Add(CQFEditorBridge.Editor.CreateInteractionWindow(o, this));
                }

                TooltipHandler.TipRegion(rect, "CQF_ClickToEdit".Translate());
                if (CQFUIStyle.ButtonImage(new Rect(width - 74f, y + 8f, 25f, 25f), TexButton.Copy))
                {
                    CQFEditorTools.operation = o.Copy();
                }

                TooltipHandler.TipRegion(new Rect(width - 74f, y + 8f, 25f, 25f), "Copy".Translate());
                Rect save = new Rect(width - 42f, y + 8f, 25f, 25f);
                if (CQFUIStyle.ButtonImage(save, ContentFinder<Texture2D>.Get("UI/Icon_MoveOut", true)))
                {
                    Find.WindowStack.Add(new Dialog_RenameForQE(name =>
                    {
                        LongEventHandler.QueueLongEvent(() =>
                        {
                            InteractionDataDef def = new InteractionDataDef();
                            def.defName = name;
                            def.label = o.interactionText;
                            def.interactions = new List<InteractionOperation>()
                            {
                                o
                            };
                            DefDatabase<InteractionDataDef>.Add(def);
                            string path = Path.Combine(CQFContentPaths.Quests, "Data", name + ".xml");
                            XElement defXml = new XElement("QuestEditor_Library.InteractionDataDef");
                            XElement interactionDataDefXml = new XElement("interactions");
                            interactionDataDefXml.Add(o.SaveToXElement("li"));
                            defXml.Add(new XElement("defName", name));
                            defXml.Add(new XElement("label", o.interactionText));
                            defXml.Add(interactionDataDefXml);
                            new CQFContentTextExport(name).Save(defXml, typeof(InteractionDataDef), path);
                            Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                        }, "SavingAsDef".Translate(), true, e =>
                        {
                            Log.Error("Export interaction XML failed: " + e);
                            Messages.Message("CQF_DialogGraph_Error".Translate(e.Message), MessageTypeDefOf.RejectInput);
                        });
                    }) { optionalTitle = "SetDefname".Translate() });
                }

                TooltipHandler.TipRegion(save, "SaveAsDef".Translate());
                y += 40f;
            };
            if (!this.operations.Any())
            {
                Widgets.Label(new Rect(18f, y + 4f, width - 36f, 28f), "CQF_NoInteractionOperations".Translate().Colorize(CQFUIStyle.Muted));
                y += 34f;
            }

            CQFUIStyle.DrawBox(new Rect(8f, initY, width - 16f, Mathf.Max(36f, y - initY)));
            y += 10f;
            float buttonWidth = Mathf.Min(120f, (width - 68f) / 2f);
            if (CQFUIStyle.ButtonText(new Rect(12f, y, buttonWidth, 32f), "Add".Translate()))
            {
                this.operations.Add(new InteractionOperation());
            }

            if (CQFUIStyle.ButtonText(new Rect(20f + buttonWidth, y, buttonWidth, 32f), "Remove".Translate()) && this.operations.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.operations, o => this.operations.Remove(o), o => o.interactionText.CanTranslate() ? o.interactionText.Translate().ToString() : o.interactionText);
            }

            if (CQFUIStyle.ButtonImage(new Rect(width - 42f, y + 3f, 25f, 25f), TexButton.Paste))
            {
                this.PasteInteractions(CQFEditorTools.operation == null ? null : new[] { CQFEditorTools.operation });
            }

            TooltipHandler.TipRegion(new Rect(width - 42f, y + 3f, 25f, 25f), "Paste".Translate());
            y += 42f;
        }

        internal void DrawOperationDefList(ref float y, float width)
        {
            this.DrawSimpleSectionTitle(ref y, width, "InteractionDataDefs".Translate());
            float initY = y;
            Rect rect = new Rect(18f, y + 4f, width - 36f, 32f);
            foreach (InteractionDataDef def in this.operationDefs)
            {
                rect.y = y + 4f;
                string label = def.label ?? def.defName;
                Widgets.LabelEllipses(rect, label);
                TooltipHandler.TipRegion(rect, label);
                y += 36f;
            }

            if (!this.operationDefs.Any())
            {
                Widgets.Label(new Rect(18f, y + 4f, width - 36f, 28f), "CQF_NoInteractionDefs".Translate().Colorize(CQFUIStyle.Muted));
                y += 34f;
            }

            CQFUIStyle.DrawBox(new Rect(8f, initY, width - 16f, Mathf.Max(36f, y - initY)));
            y += 10f;
            float buttonWidth = Mathf.Min(120f, (width - 68f) / 2f);
            if (CQFUIStyle.ButtonText(new Rect(12f, y, buttonWidth, 32f), "Add".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<InteractionDataDef>.AllDefsListForReading, d => this.operationDefs.Add(d), d => d.label);
            }

            if (CQFUIStyle.ButtonText(new Rect(20f + buttonWidth, y, buttonWidth, 32f), "Remove".Translate()) && this.operationDefs.Any())
            {
                CQFEditorTools.DrawFloatMenu(this.operationDefs, d => this.operationDefs.Remove(d), d => d.label);
            }

            y += 42f;
        }

        public void ProduceResult(Pawn operatorPawn, string operationText)
        {
            if (this.GetCurOperation(operationText)is InteractionOperation op && op != null)
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
                        string text = (operation.interactionText.CanTranslate() ? operation.interactionText.Translate().ToString() : operation.interactionText);
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
            Scribe_Collections.Look(ref this.operations, "InteractableThing_operations", LookMode.Deep);
            Scribe_Collections.Look(ref this.operationDefs, "operationDefs", LookMode.Def);
        }

        public void PasteData()
        {
            this.PasteInteractions(CQFEditorTools.operations, CQFEditorTools.operationDefs);
        }

        public void PasteInteractions(IEnumerable<InteractionOperation> sourceOperations, IEnumerable<InteractionDataDef> sourceDefinitions = null)
        {
            try
            {
                if (this.Destroyed)
                {
                    Messages.Message("CQF_EditorTargetUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                List<InteractionOperation> copies = (sourceOperations ?? Enumerable.Empty<InteractionOperation>()).Select(operation => operation.Copy()).ToList();
                List<InteractionDataDef> definitions = (sourceDefinitions ?? Enumerable.Empty<InteractionDataDef>()).ToList();
                if (copies.Count == 0 && definitions.Count == 0)
                {
                    Messages.Message("CQF_EditorEmptyClipboard".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                this.operations ??= new List<InteractionOperation>();
                this.operationDefs ??= new List<InteractionDataDef>();
                this.operations.AddRange(copies);
                this.operationDefs.AddRange(definitions);
            }
            catch (Exception exception)
            {
                Log.Error("[CQF] Interaction paste failed: " + exception);
                Messages.Message("CQF_EditorPasteFailed".Translate() + ": " + exception.Message, MessageTypeDefOf.RejectInput, false);
            }
        }

        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_InteractableThing(this, pos);
        }

        internal Rect DrawSectionHeader(ref float y, float width, string label, bool drawCopyButton = false)
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect header = new Rect(8f, y, width - 16f, 32f);
            Widgets.DrawBoxSolid(header, CQFUIStyle.Header);
            CQFUIStyle.DrawBox(header);
            Widgets.LabelEllipses(new Rect(16f, y + 3f, width - (drawCopyButton ? 70f : 32f), 26f), label.Colorize(CQFUIStyle.TextColor));
            Rect copyRect = Rect.zero;
            if (drawCopyButton)
            {
                copyRect = new Rect(width - 42f, y + 3f, 25f, 25f);
            }

            Text.Font = GameFont.Small;
            y += 32f;
            return copyRect;
        }

        internal void DrawSimpleSectionTitle(ref float y, float width, string label)
        {
            this.DrawSectionHeader(ref y, width, label);
        }

        public float height = 0f;
        public Vector2 scrollPos;
        public bool disable = false;
        public Graphic disabledGraphic = null;
        public List<InteractionOperation> operations = new List<InteractionOperation>();
        public List<InteractionDataDef> operationDefs = new List<InteractionDataDef>();
        private CompCustomText textComp = null;
    }

    public class InteractionOperation : ISaveable, IExposable, IDrawable
    {
        public void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            x += 8f;
            float width = Mathf.Max(80f, inRect.width - x - 12f);
            this.DrawBasicSettings(ref y, x, width);
            this.DrawRequiredThings(ref y, x, width, inRect);
            this.DrawConditions(ref y, x, width, inRect);
            this.DrawResults(ref y, x, width, inRect);
        }

        internal void DrawBasicSettings(ref float y, float x, float width)
        {
            this.DrawHeader(ref y, x, width, "InteractionOperations".Translate());
            string source = this.interactionText.CanTranslate() ? this.interactionText.Translate().ToString() : this.interactionText;
            string edited = Widgets.TextArea(new Rect(x + 8f, y, width - 16f, 58f), source);
            if (edited != source)
                this.interactionText = edited;
            y += 66f;
            bool stacked = width < 360f;
            Rect timeLabel = new Rect(x + 8f, y, stacked ? width - 16f : Mathf.Min(230f, width * 0.45f), 28f);
            using (new CQFUIScope())
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.LabelEllipses(timeLabel, "TickToOperate".Translate());
            }
            Rect timeField = new Rect(stacked ? x + 8f : timeLabel.xMax + 8f, stacked ? y + 30f : y,
                stacked ? width - 16f : x + width - timeLabel.xMax - 16f, 28f);
            Widgets.TextFieldNumeric(timeField, ref this.tickToOperate, ref this.buffer, -9999);
            y += stacked ? 66f : 36f;
            y += 12f;
        }

        internal void DrawRequiredThings(ref float y, float x, float width, Rect inRect)
        {
            this.DrawHeader(ref y, x, width, "InteractionOption_RequiredThing".Translate(), () => CQFEditorTools.DrawFloatMenu(new List<Type>() { typeof(CQFThingDefCount) }, t =>
            {
                CQFThingData.OpenSelectWindow(t, d => this.requiredThings.Add(d));
            }, t => t.Name.Translate()), "Add".Translate(), () => CQFEditorTools.DrawFloatMenu(this.requiredThings, d => this.requiredThings.Remove(d), d => d.ToString()), "Remove".Translate());
            foreach (CQFThingData thing in this.requiredThings)
            {
                CQFEditorInlineLayout.Draw(thing, ref y, x + 8f, width - 16f, rect =>
                {
                    float contentY = 0f;
                    thing.DrawWithSingleCount(ref contentY, rect, 0f);
                    return contentY + 6f;
                });
                y += 6f;
            }

            if (!this.requiredThings.Any())
            {
                Widgets.Label(new Rect(x + 12f, y + 4f, width - 24f, 25f), "CQF_NoRequiredThings".Translate().Colorize(CQFUIStyle.Muted));
                y += 34f;
            }

            y += 12f;
        }

        internal void DrawConditions(ref float y, float x, float width, Rect inRect)
        {
            CQFConditionListEditor.Draw(ref y, x, width, inRect, "InteractionConditions".Translate(), this.conditions);
        }

        internal void DrawResults(ref float y, float x, float width, Rect inRect)
        {
            CQFInteractionResultEditor.Draw(ref y, x, width, inRect, this);
        }

        internal void DrawHeader(ref float y, float x, float width, string label, Action addAction = null, string addTip = null, Action removeAction = null, string removeTip = null, Texture2D addIcon = null)
        {
            using CQFUIScope scope = new CQFUIScope();
            Rect header = new Rect(x, y, width, 32f);
            Widgets.DrawBoxSolid(header, CQFUIStyle.Header);
            CQFUIStyle.DrawBox(header);
            float toolbarWidth = (addAction == null ? 0f : 30f) + (removeAction == null ? 0f : 30f);
            Widgets.LabelEllipses(new Rect(x + 8f, y + 3f, Mathf.Max(20f, width - toolbarWidth - 16f), 26f), label.Colorize(CQFUIStyle.TextColor));
            Rect button = new Rect(x + width - 33f - (removeAction == null ? 0f : 30f), y + 3f, 25f, 25f);
            if (addAction != null && CQFUIStyle.ButtonImage(button, addIcon ?? TexButton.Plus))
            {
                addAction();
            }

            if (addAction != null && addTip != null)
            {
                TooltipHandler.TipRegion(button, addTip);
            }

            button.x += 30f;
            if (removeAction != null && CQFUIStyle.ButtonImage(button, TexButton.Delete))
            {
                removeAction();
            }

            if (removeAction != null && removeTip != null)
            {
                TooltipHandler.TipRegion(button, removeTip);
            }

            y += 42f;
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

        public bool Satisfied(Pawn target, Thing thing, out string reason, Quest quest)
        {
            foreach (DialogCondition condition in this.conditions)
            {
                Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
                targets.Add("Trigger", target);
                targets.Add("CustomThing", thing);
                if (!condition.Satisfied(targets, out reason, quest))
                {
                    return false;
                }
            }

            foreach (CQFThingData data in this.requiredThings)
            {
                if (data is CQFThingDefCount tData && target.inventory.Count(tData.thing) < tData.count.min)
                {
                    reason = "NoRequiredThing".Translate(tData.thing.label, target.inventory.Count(tData.thing), tData.count.min.ToString());
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
        public List<DialogCondition> conditions = new List<DialogCondition>();
        public List<InteractionResult> results = new List<InteractionResult>();
        public List<CQFThingData> requiredThings = new List<CQFThingData>();
    }

    public class InteractionResult : ISaveable, IExposable
    {
        public void Draw(ref float y, Rect inRect, float x = 0f)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFInteractionResultEditor.DrawResult(ref y, x, inRect.width - x - 35f, inRect, this);
        }

        public void DoResult(Pawn target, Thing thing, Quest quest)
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
                if (!condition.Satisfied(targets, out string reason, quest))
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
