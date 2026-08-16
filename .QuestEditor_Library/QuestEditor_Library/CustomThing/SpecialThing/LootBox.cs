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
    public class LootBox : Building , IDrawTabable, IPastableData, ICopiableData, ICustomThing
    {
        public override string Label => this.TextComp == null || !this.textComp.useCustomName ? base.Label : this.textComp.customName;
        public override string DescriptionFlavor => this.TextComp == null || !this.textComp.useCustomDescription ? base.DescriptionFlavor : this.textComp.customDescription;
        public LootData InnerData 
        {
            get 
            {
                if (this.innerLoot == null) 
                {
                    List<LootData> datas = new List<LootData>();
                    datas.AddRange(this.loots);
                    if (this.lootDef != null)
                    {
                        datas.AddRange(this.lootDef.loots);
                    }
                    if (datas.Any()) 
                    {
                        this.innerLoot = GenCollection.RandomElementByWeight(datas, (x) => x.chance);
                    }
                }
                return this.innerLoot;
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
        public override Graphic Graphic 
        {
            get 
            {
                if (!this.opened) 
                {
                    return base.Graphic;
                }
                if (this.openedGraphic == null) 
                {
                    if (this.def.GetModExtension<ModExtension_CustomThing>() is ModExtension_CustomThing me && me.openedGraphicdata !=null) 
                    {
                        this.openedGraphic = me.openedGraphicdata.GraphicColoredFor(this); 
                        return this.openedGraphic;
                    }
                    
                    this.openedGraphic = base.Graphic;
                }
                return this.openedGraphic;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder result = new StringBuilder(base.GetInspectString());
            if (Prefs.DevMode)
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }
                result.Append("LootDatas".Translate());
                foreach (LootData loot in this.loots)
                {
                    result.Append(' ');
                    result.Append(loot.dataName);
                }
            }
            if (result.Length > 0)
            {
                result.AppendLine();
            }
            result.Append("CQF_OpenLootbox".Translate(this.openReport.Translate()).ToString().Trim());
            return result.ToString().Trim();
        }
        public void DrawTab() 
        {
            Rect outRect = new Rect(8f, 18f, 536f, 584f);
            Rect viewRect = new Rect(0f, 0f, 516f, this.height);
            Widgets.BeginScrollView(outRect, ref this.scrollPos, viewRect);
            float y = 8f;
            this.DrawSectionHeader(ref y, viewRect.width, "LootBox".Translate());
            CQFEditorTools.DrawLabelAndText_Line(y, "LootBoxName".Translate(), ref this.lootBoxName, 16f, 300f);
            Rect rectCP = new Rect(448f,y,25f,25f);
            if (Widgets.ButtonImage(rectCP, TexButton.Copy))
            {
                this.CopyData();
            }
            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            rectCP.x += 30f;
            if (Widgets.ButtonImage(rectCP, TexButton.Paste))
            {
                PasteData();
            }
            TooltipHandler.TipRegion(rectCP, "Paste".Translate());
            y += 38f;
            Rect saveRect = this.DrawSectionHeader(ref y, viewRect.width, this.useLootDef ? "CQF_UseLootDataDef".Translate() : "CQF_CustomLootData".Translate(), !this.useLootDef, !this.useLootDef);
            if (this.useLootDef)
            {
                if (Widgets.ButtonText(new Rect(16f, y, 450f, 28f), "LootDef".Translate(this.lootDef?.defName), false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<LootDataDef>.AllDefsListForReading,d => this.lootDef = d,d => d.defName);
                }
                y += 38f;
            }
            else
            {
                if (Widgets.ButtonImage(saveRect, CQFEditorTools.icon_Save))
                {
                    LongEventHandler.QueueLongEvent(() =>
                    {
                        LootDataDef def = new LootDataDef();
                        def.defName = this.lootBoxName;
                        def.loots = this.loots;
                        DefDatabase<LootDataDef>.Add(def);
                        string path = Path.Combine(Page_QuestEditor.Path, "Data", this.lootBoxName + ".xml");
                        XElement defs = new XElement("Defs");
                        XElement defXml = new XElement("QuestEditor_Library.LootDataDef");
                        XElement lootsXml = new XElement("loots");
                        this.loots.ForEach(l => lootsXml.Add(l.SaveToXElement("li")));
                        defXml.Add(new XElement("defName",this.lootBoxName)); 
                        defXml.Add(lootsXml);
                        defs.Add(defXml);
                        defs.Save(path);
                        Messages.Message("SaveSucceed".Translate(path), MessageTypeDefOf.PositiveEvent);
                    },"SavingAsDef".Translate(),true,e => Log.Message(e.Message));
                }
                TooltipHandler.TipRegion(saveRect, "SaveAsDef".Translate());
                float initY = y;
                Rect rectData = new Rect(20f, y + 3f, 454f, 28f);
                foreach (LootData data in this.loots)
                {
                    if (Widgets.ButtonText(rectData,data.dataName + "  " + data.chance * 100f + "%",false)) 
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(data));
                    }
                    TooltipHandler.TipRegion(rectData, "CQF_ClickToEdit".Translate());
                    y += 32f;
                    rectData.y += 32f;
                }
                if (!this.loots.Any())
                {
                    Widgets.Label(new Rect(20f, y + 4f, 454f, 25f), "CQF_NoLootData".Translate().Colorize(Color.gray));
                    y += 32f;
                }
                Widgets.DrawBox(new Rect(10f, initY, 474f, y - initY), 1, QuestEditor_Dialog.blueTex);
                y += 10f;
                if (Widgets.ButtonText(new Rect(10f, y, 132f, 32f), "AddNewLootData".Translate()))
                {
                    this.loots.Add(new LootData());
                }
                if (Widgets.ButtonText(new Rect(156f, y, 132f, 32f), "Paste".Translate()) && CQFEditorTools.lootData != null)
                {
                    this.loots.Add(CQFEditorTools.lootData.Copy());
                }
                if (Widgets.ButtonText(new Rect(302f, y, 132f, 32f), "DeleteLootData".Translate()) && this.loots.Any())
                {
                    CQFEditorTools.DrawFloatMenu(this.loots, (x) => this.loots.Remove(x), (x) => x.dataName);
                }
                y += 44f;
            }
            this.DrawSectionHeader(ref y, viewRect.width, "CQF_LootSettings".Translate());
            CQFEditorTools.DrawLabelAndText_Line(y, "JobReport".Translate(), ref this.openReport, 16f,220f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "TickToOpenLoot".Translate(), ref this.tickToOpen, ref this.buffer, 16f,220f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f,y,300f,25f),"DestroyAfterOpening".Translate(), ref this.destroyAfterOpening);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f,y,300f,25f),"OpenWhenDestroyed".Translate(), ref this.openWhenDestroyed);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f, y, 420f, 25f), "UseLootDef".Translate(), ref this.useLootDef);
            y += 30f;
            this.height = y + 15f;
            Widgets.EndScrollView();
        }
        public void PasteData()
        {
            this.lootBoxName = CQFEditorTools.lootBoxName;
            this.tickToOpen = CQFEditorTools.tickToOpen;
            this.destroyAfterOpening = CQFEditorTools.destroyAfterOpening;
            this.openReport = CQFEditorTools.openReport;
            this.loots = new List<LootData>();
            CQFEditorTools.loots.ListFullCopy().ForEach(l => this.loots.Add(l.Copy()));
            this.buffer = CQFEditorTools.buffer;
            this.useLootDef = CQFEditorTools.useLootDef;
            this.lootDef = CQFEditorTools.lootDef;
            this.openWhenDestroyed = CQFEditorTools.openWhenDestroyed;
        }
        public void CopyData()
        {
            CQFEditorTools.lootBoxName = this.lootBoxName;
            CQFEditorTools.tickToOpen = this.tickToOpen;
            CQFEditorTools.destroyAfterOpening = this.destroyAfterOpening;
            CQFEditorTools.openReport = this.openReport;
            CQFEditorTools.buffer = this.buffer;
            CQFEditorTools.loots = this.loots.ListFullCopy();
            CQFEditorTools.useLootDef = this.useLootDef;
            CQFEditorTools.lootDef = this.lootDef;
            CQFEditorTools.openWhenDestroyed = this.openWhenDestroyed;
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.Map != null && !this.opened && this.openWhenDestroyed) 
            {
                this.InnerData?.SpawnLoots(Map, this.Position, this.GetLord(), this);
            }
            this.OpenPost();
            base.Destroy(mode);
        }
        public virtual void Open(Pawn pawn = null) 
        {
            if (!this.opened) 
            {
                QuestUtility.SendQuestTargetSignals(this.questTags, "Opened", this.Named("SUBJECT"));
                this.InnerData?.SpawnLoots(this.Map, this.Position, this.GetLord(),this,pawn);
                this.opened = true;
                this.Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlagDefOf.Things);
                if (this.destroyAfterOpening) 
                {
                    this.Destroy();
                }
                this.OpenPost();
            }
        }
        public void OpenPost() 
        {
            if (!GameTools.isGeneratingMap)
            {
                GameTools.ClearTemporaryTargets();
            }
            if (this.TryGetComp<CompActionWorker>() is CompActionWorker comp) 
            {
                foreach (var actionComp in comp.comps)
                {
                    if (actionComp.mode == ActionTriggerMode.Open) 
                    {
                        actionComp.actions.ForEach(a => a.Work(comp.GetTargetThis(), comp.Quest));
                    }
                }
            }
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }
            if (!this.opened)
            {
                if (selPawn.CanReserveAndReach(this, PathEndMode.Touch, Danger.Deadly))
                {
                    Job job = JobMaker.MakeJob(QEDefOf.QE_Open, this);
                    job.reportStringOverride = this.openReport.Translate();
                    yield return new FloatMenuOption(this.openReport.Translate(), () =>
                    {
                        if (Input.GetKeyDown(KeyCode.LeftShift))
                        {
                            selPawn.jobs.TryTakeOrderedJob(job);
                        }
                        else 
                        {
                            selPawn.jobs.StartJob(job);
                        }
                    });
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
            Scribe_Deep.Look(ref this.innerLoot, "innerLoot");
            Scribe_Collections.Look(ref this.loots, "QE_LootBox_loots",LookMode.Deep);
            Scribe_Defs.Look(ref this.lootDef, "QE_LootBox_lootDef");
            Scribe_Values.Look(ref this.lootBoxName, "QE_LootBox_lootBoxName");
            Scribe_Values.Look(ref this.openReport, "QE_LootBox_openReport");
            Scribe_Values.Look(ref this.opened, "QE_LootBox_opened");
            Scribe_Values.Look(ref this.destroyAfterOpening, "QE_LootBox_destroyAfterOpening");
            Scribe_Values.Look(ref this.tickToOpen, "QE_LootBox_tickToOpen");
            Scribe_Values.Look(ref this.buffer, "QE_LootBox_buffer");
            Scribe_Values.Look(ref this.useLootDef, "QE_LootBox_useLootDef");
            Scribe_Values.Look(ref this.openWhenDestroyed, "openWhenDestroyed");
        }

        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_LootBox(this,pos);
        }

        private Rect DrawSectionHeader(ref float y, float width, string label, bool drawSaveButton = false, bool skipLine = false)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(10f, y, width - 20f, 30f), label.Colorize(ColorLibrary.SkyBlue));
            Rect saveRect = Rect.zero;
            if (drawSaveButton)
            {
                float labelWidth = Text.CalcSize(label).x;
                saveRect = new Rect(Mathf.Min(10f + labelWidth + 12f, width - 52f), y + 2f, 25f, 25f);
            }
            Text.Font = GameFont.Small;
            y += 32f;
            if (!skipLine)
            {
                Widgets.DrawLine(new Vector2(10f, y), new Vector2(width - 20f, y), ColorLibrary.SkyBlue, 1f);
                y += 10f;
            }
            else
            {
                y += 3f;
            }
            return saveRect;
        }

        [NoTranslate]
        public string lootBoxName = "Undefined";
        public float height = 0f; 
        public int tickToOpen = 100;
        public bool opened = false;
        public bool destroyAfterOpening = false;
        public bool useLootDef = false;
        public bool openWhenDestroyed = true;
        
        public string openReport = "CQF_Open";
        public string buffer;
        public Vector2 scrollPos;
        public Graphic openedGraphic = null;
        private LootData innerLoot;
        public List<LootData> loots = new List<LootData>();
        public LootDataDef lootDef;
        private CompCustomText textComp = null; 
    }
    public class LootData : IExposable ,ISaveable,IDrawable
    {
        public LootData() 
        {
            this.dataName = "Unnamed";
        }
        public LootData Copy() 
        {
            LootData result = new LootData();
            result.dataName = this.dataName;
            result.chance = this.chance;
            result.message = this.message;
            this.pawnDatas.ForEach(d => result.pawnDatas.Add(d.Copy()));
            this.things.ForEach(d => result.things.Add((CQFThingDefCount)d.Copy()));
            this.categorys.ForEach(d => result.categorys.Add((CQFThingCategoryCount)d.Copy()));
            this.specialThingDatas.ForEach(d => result.specialThingDatas.Add(d.Copy())); 
            return result;
        }
        public void Draw(ref float y, Rect inRect, float x)
        {
            float width = inRect.width - 35f - x;
            this.DrawHeader(ref y, x + 10f, width - 10f);
            this.DrawBasicSettings(ref y, x, width);
            this.DrawThingList(ref y, inRect, x, width);
            this.DrawCategoryList(ref y, inRect, x, width);
            this.DrawSpecialThingList(ref y, inRect, x, width);
            this.DrawPawnList(ref y, x, width);
            CQFEditorTools.DrawLabelAndText_Line(y, "LootChance".Translate(), ref this.chance, ref this.buffer, 16f + x);
            y += 30f;
        }

        private void DrawHeader(ref float y, float x, float width)
        {
            Widgets.DrawHighlight(new Rect(x - 4f, y + 4f, width + 8f, 32f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y + 7f, width - 75f, 30f), this.dataName.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            Rect button = new Rect(x + width - 60f, y + 7f, 25f, 25f);
            if (Widgets.ButtonImage(button, TexButton.Rename))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE(name => this.dataName = name));
            }
            TooltipHandler.TipRegion(button, "Rename".Translate());
            button.x += 30f;
            if (Widgets.ButtonImage(button, TexButton.Copy))
            {
                CQFEditorTools.lootData = this.Copy();
            }
            TooltipHandler.TipRegion(button, "Copy".Translate());
            y += 48f;
        }

        private void DrawBasicSettings(ref float y, float x, float width)
        {
            CQFEditorTools.DrawFieldAndText(ref y, "MessageAfterOpening".Translate(), ref this.message, x + 8f, 400f);
            y += 40f;
        }

        private void DrawThingList(ref float y, Rect inRect, float x, float width)
        {
            this.DrawListHeader(ref y, x, width, "LootThings".Translate(), () => CQFThingData.OpenLootThingSelectWindow(d => this.things.Add(new CQFThingDefCount { thing = d })),
                () => CQFEditorTools.DrawFloatMenu(this.things, t => this.things.Remove(t), t => t.thing.label + "x" + t.count));
            float initY = y;
            foreach (CQFThingDefCount thing in this.things)
            {
                float itemY = y;
                thing.Draw(ref y, inRect, x + 10f);
                y += 4f;
                this.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }
            if (!this.things.Any())
            {
                this.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoLootThings".Translate());
            }
            y += 10f;
        }

        private void DrawCategoryList(ref float y, Rect inRect, float x, float width)
        {
            this.DrawListHeader(ref y, x, width, "LootCategorys".Translate(), () => CQFEditorTools.DrawFloatMenu<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll(t2 => t2.defName != "Corpses" &&
                    !t2.Parents.Contains(ThingCategoryDefOf.Corpses) && t2 != ThingCategoryDefOf.Animals), t2 => this.categorys.Add(new CQFThingCategoryCount() { category = t2 }), t2 => t2.label),
                () => CQFEditorTools.DrawFloatMenu(this.categorys, t => this.categorys.Remove(t), t => t.category.label + "x" + t.count));
            foreach (CQFThingCategoryCount cetegory in this.categorys)
            {
                float itemY = y;
                cetegory.Draw(ref y, inRect, x + 10f);
                y += 4f;
                this.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }
            if (!this.categorys.Any())
            {
                this.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoLootCategories".Translate());
            }
            y += 10f;
        }

        private void DrawSpecialThingList(ref float y, Rect inRect, float x, float width)
        {
            this.DrawListHeader(ref y, x, width, "SpecialThingData".Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(CQFThingData).AllSubclassesNonAbstract().FindAll(t => t != typeof(CQFThingDefCount) && t != typeof(CQFThingCategoryCount)),
                    t2 => this.specialThingDatas.Add((CQFThingData)Activator.CreateInstance(t2)), t2 => t2.Name.Translate()),
                () => CQFEditorTools.DrawFloatMenu(this.specialThingDatas, t => this.specialThingDatas.Remove(t), t => t.ToString()));
            foreach (CQFThingData data in this.specialThingDatas)
            {
                float itemY = y;
                data.Draw(ref y, inRect, x + 10f);
                y += 4f;
                this.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }
            if (!this.specialThingDatas.Any())
            {
                this.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoSpecialThingData".Translate());
            }
            y += 10f;
        }

        private void DrawPawnList(ref float y, float x, float width)
        {
            this.DrawListHeader(ref y, x, width, "LootPawn".Translate(), () => CQFEditorTools.DrawFloatMenu(typeof(PawnSpawnData).AllSubclassesNonAbstract(), t => this.pawnDatas.Add((PawnSpawnData)Activator.CreateInstance(t)), t => t.Name.Translate()),
                () => CQFEditorTools.DrawFloatMenu(this.pawnDatas, t => this.pawnDatas.Remove(t), t => t.dataName));
            foreach (PawnSpawnData pawnData in this.pawnDatas)
            {
                float itemY = y;
                Rect rectData = new Rect(x + 16f, y + 3f, width - 32f, 25f);
                if (Widgets.ButtonText(rectData, pawnData.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(pawnData));
                }
                TooltipHandler.TipRegion(rectData, "CQF_ClickToEdit".Translate());
                y += 30f;
                this.DrawListItemFrame(itemY, y, x + 6f, width - 12f);
                y += 8f;
            }
            if (!this.pawnDatas.Any())
            {
                this.DrawEmptyState(ref y, x + 12f, width - 24f, "CQF_NoLootPawns".Translate());
            }
            y += 10f;
        }

        private void DrawListHeader(ref float y, float x, float width, string label, Action addAction, Action removeAction)
        {
            Widgets.DrawHighlight(new Rect(x + 4f, y - 2f, width - 8f, 32f));
            Widgets.Label(new Rect(x + 8f, y + 4f, width - 84f, 25f), label.Colorize(ColorLibrary.SkyBlue));
            Rect button = new Rect(x + width - 66f, y + 2f, 25f, 25f);
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
            y += 38f;
        }

        private void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Rect rect = new Rect(x, startY - 2f, width, Mathf.Max(34f, endY - startY + 4f));
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawLine(new Vector2(x + 6f, rect.yMax), new Vector2(x + width - 6f, rect.yMax), ColorLibrary.SkyBlue, 1f);
        }

        private void DrawEmptyState(ref float y, float x, float width, string label)
        {
            Widgets.Label(new Rect(x, y + 4f, width, 25f), label.Colorize(Color.gray));
            y += 32f;
        }
        public List<Thing> SpawnLoots(Map map, IntVec3 pos, Lord lord, Thing box,Pawn opener = null)
        {
            List<Thing> result = new List<Thing>();
            string text = null;
            if (this.pawnDatas != null)
            {
                foreach (PawnSpawnData data in this.pawnDatas)
                {
                    data.Spawn(pos, map, box != null && box.questTags != null && box.questTags.Any() ? box.questTags.First() : "Null", GameTools.GetQuestFromThing(box), lord).ToList().ForEach(p =>
                      result.Add(p.Value.Thing));
                }
            }
            List<CQFThingData> datas = new List<CQFThingData>();
            datas.AddRange(this.things);
            datas.AddRange(this.categorys);
            datas.AddRange(this.specialThingDatas);
            foreach (CQFThingData thingCount in datas)
            {
                thingCount.Spawn()?.ForEach(thing =>
                {
                    GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near
                    , (t, i) =>
                    {
                        if (text == null)
                        {
                            text = t.Label;
                        }
                        else
                        {
                            text += "," + t.Label;
                        }
                        result.Add(t);
                    }); 
                });
            }
            if (box != null)
            {
                QuestUtility.SendQuestTargetSignals(box.questTags, this.dataName, box.Named("SUBJECT"));
            }
            Messages.Message(this.message.Translate(text),new LookTargets(pos,map),MessageTypeDefOf.NeutralEvent);
            result.ForEach(t => 
            {
                if (t.TryGetComp<CompQuality>() is CompQuality comp) 
                {
                    comp.SetQuality(QualityUtility.AllQualityCategories.RandomElement(),null);
                }
            });
            return result;
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("dataName", this.dataName));
            result.Add(new XElement("chance", this.chance));
            if (this.message != null && this.message != "") 
            {
                result.Add(new XElement("message", this.message));
            }
            if (this.pawnDatas.Any())
            {
                XElement pawnData = new XElement("pawnDatas");
                this.pawnDatas.ForEach((x) => pawnData.Add(x.SaveToXElement("li")));
                result.Add(pawnData);
            }
            if (this.things.Any())
            {
                XElement thingData = new XElement("things");
                this.things.ForEach((x) => thingData.Add(x.SaveToXElement("li")));
                result.Add(thingData);
            }
            if (this.categorys.Any())
            {
                XElement categoryData = new XElement("categorys");
                this.categorys.ForEach((x) => categoryData.Add(x.SaveToXElement("li")));
                result.Add(categoryData);
            }
            if (this.specialThingDatas.Any())
            {
                XElement specialThingDatas = new XElement("specialThingDatas");
                this.specialThingDatas.ForEach((x) => specialThingDatas.Add(x.SaveToXElement("li")));
                result.Add(specialThingDatas);
            }
            return result;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.dataName, "QE_LootData_dataName");
            Scribe_Values.Look(ref this.chance, "QE_LootData_chance");
            Scribe_Values.Look(ref this.buffer, "QE_LootData_buffer");
            Scribe_Values.Look(ref this.message, "QE_LootData_message");
            Scribe_Collections.Look(ref this.things, "QE_LootData_things",LookMode.Deep);
            Scribe_Collections.Look(ref this.categorys, "QE_LootData_categorys", LookMode.Deep);
            Scribe_Collections.Look(ref this.specialThingDatas, "specialThingDatas", LookMode.Deep);
            Scribe_Collections.Look(ref this.pawnDatas, "QE_LootData_pawnDatas", LookMode.Deep);
        }
        [NoTranslate]
        public string dataName;
        public float chance = 1f;
        public string buffer;
        public string message = null;
        public List<PawnSpawnData> pawnDatas = new List<PawnSpawnData>();
        public List<CQFThingDefCount> things = new List<CQFThingDefCount>();
        public List<CQFThingCategoryCount> categorys = new List<CQFThingCategoryCount>();
        public List<CQFThingData> specialThingDatas = new List<CQFThingData>();
    }
    public abstract class CQFThingData : IExposable , ISaveable,IDrawable
    {
        public static void OpenLootThingSelectWindow(Action<ThingDef> action)
        {
            List<ThingDef> defs = SelectableLootThings();
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(defs, d => d.uiIcon, d => d.label, action, null, (t, r) => Widgets.DefIcon(r, t, null), null, null, null, null, LootThingTypeFilters(defs), LootThingTypeTips(defs)), "SelectLootThing".Translate()));
        }

        public static void OpenSelectWindow(Type type, Action<CQFThingData> action)
        {
            if (type == typeof(CQFThingDefCount)) 
            {
                OpenLootThingSelectWindow(d => action(new CQFThingDefCount { thing = d }));
            }
            if (type == typeof(CQFThingCategoryCount))
            {
                Find.WindowStack.Add(new Dialog_Select<ThingCategoryDef>(new TextSelectDrawer<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll((t2) => t2.defName != "Corpses" && !t2.Parents.Contains(ThingCategoryDefOf.Corpses) && t2 != ThingCategoryDefOf.Animals), d => d.label, d => action(new CQFThingCategoryCount { category = d }), null, null, null, null, null, null), "Select".Translate()));
            }
        }

        private static Dictionary<string, Func<ThingDef, bool>> LootThingTypeFilters(List<ThingDef> defs)
        {
            Dictionary<string, Func<ThingDef, bool>> result = new Dictionary<string, Func<ThingDef, bool>>();
            foreach (ThingCategoryDef category in LootThingCategories(defs))
            {
                string label = category.label ?? category.defName;
                if (!result.ContainsKey(label))
                {
                    result.Add(label, thing => thing.thingCategories != null && thing.thingCategories.Any(c => c == category || c.Parents.Contains(category)));
                }
            }
            return result;
        }

        private static Dictionary<string, string> LootThingTypeTips(List<ThingDef> defs)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (ThingCategoryDef category in LootThingCategories(defs))
            {
                string label = category.label ?? category.defName;
                if (!result.ContainsKey(label))
                {
                    result.Add(label, category.description);
                }
            }
            return result;
        }

        private static List<ThingCategoryDef> LootThingCategories(List<ThingDef> defs)
        {
            return defs
                .Where(def => def.thingCategories != null)
                .SelectMany(def => def.thingCategories)
                .Select(category => category.Parents.FirstOrDefault(parent => parent.parent == ThingCategoryDefOf.Root) ?? category)
                .Distinct()
                .OrderBy(category => category.label ?? category.defName)
                .ToList();
        }

        private static List<ThingDef> SelectableLootThings()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading.FindAll(t => t.category == ThingCategory.Item && !t.IsCorpse);
        }

        public abstract ThingRequest GetRequest();
        public abstract List<Thing> Spawn();
        public CQFThingData Copy() 
        {
            XElement x = this.SaveToXElement("PawnSpawnData");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            CQFThingData result = DirectXmlToObject.ObjectFromXml<CQFThingData>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            this.DrawIcon(ref y);
            Widgets.Label(new Rect(60f + x, y + 5f, 35f, 35f), "x");
            int min = this.count.min;
            int max = this.count.max;
            Widgets.TextFieldNumeric<int>(new Rect(75f + x, y, 35f, 35f), ref min, ref this.bufferMin);
            Widgets.Label(new Rect(113f + x, y + 5f, 35f, 35f), "~");
            Widgets.TextFieldNumeric<int>(new Rect(125f + x, y, 35f, 35f), ref max, ref this.bufferMax);
            this.count = new IntRange(min, max);
            Rect rect = new Rect(180f + x, y + 3f, 150f, 25f);
            if (Widgets.ButtonText(rect, "SelectStuff".Translate(this.stuff?.label), false))
            {
                CQFEditorTools.DrawFloatMenu<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((t) => t.IsStuff), (t) => this.stuff = t, (t) => t.label, new List<FloatMenuOption>()
                {new FloatMenuOption("Null".Translate(),() => this.stuff = null)});
            }
            TooltipHandler.TipRegion(rect, "CQFStuffTip".Translate());
            y += 35f;
        }
        public void DrawWithSingleCount(ref float y, Rect inRect, float x)
        {
            this.DrawIcon(ref y);
            Widgets.Label(new Rect(60f + x, y + 5f, 35f, 35f), "x");
            int min = this.count.min;
            Widgets.TextFieldNumeric<int>(new Rect(75f + x, y, 35f, 35f), ref min, ref this.bufferMin);
            this.count = new IntRange(min, min);
            Rect rect = new Rect(180f + x, y + 3f, 150f, 25f);
            if (Widgets.ButtonText(rect, "SelectStuff".Translate(this.stuff?.label), false))
            {
                CQFEditorTools.DrawFloatMenu<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((t) => t.IsStuff), (t) => this.stuff = t, (t) => t.label, new List<FloatMenuOption>()
                {new FloatMenuOption("Null".Translate(),() => this.stuff = null)});
            }
            TooltipHandler.TipRegion(rect, "CQFStuffTip".Translate());
            y += 30f;
        }
        public abstract void DrawIcon(ref float y);
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            if (this.stuff != null)
            {
                result.Add(new XElement("stuff", this.stuff.defName));
            }
            if (this.count != new IntRange(1, 1)) 
            {
                result.Add(new XElement("count", this.count.ToString()));
            }
            return result;
        }
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref this.count, "QE_ThingDefCountRangeWithBuffer_count");   
            Scribe_Defs.Look(ref this.stuff, "QE_ThingDefCountRangeWithBuffer_stuff");
            Scribe_Values.Look(ref this.bufferMin, "QE_ThingDefCountRangeWithBuffer_bufferMin");
            Scribe_Values.Look(ref this.bufferMax, "QE_ThingDefCountRangeWithBuffer_bufferMax");
        }

        public string bufferMin;
        public string bufferMax;       
        public ThingDef stuff = null;   
        public IntRange count = new IntRange(1,1);
    }
    public class CQFThingDefCount : CQFThingData
    {
        public override ThingRequest GetRequest()
        {
            return ThingRequest.ForDef(this.thing);
        }
        public override void DrawIcon(ref float y)
        {
            Rect rect = new Rect(20f, y, 35f, 35f);
            Widgets.DefIcon(rect, this.thing, this.stuff);
            if (Mouse.IsOver(rect))
            {
                Vector3 mouse = Input.mousePosition;
                Widgets.DrawBox(new Rect(mouse.x, mouse.y, 70f, 40f));
                Widgets.Label(rect, this.thing.label);
            }
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("thing", this.thing.defName));
            return result;
        }
        public override string ToString()
        {
            return this.stuff?.label + " " + this.thing?.label + this.count.ToString();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.thing, "QE_ThingDefCountRangeWithBuffer_thing");
        }

        public override List<Thing> Spawn()
        {
            Thing thing = ThingMaker.MakeThing(this.thing, this.thing.MadeFromStuff 
                ? (this.stuff ?? GenStuff.RandomStuffFor(this.thing)) : null);
            thing.stackCount = this.count.RandomInRange;
            return new List<Thing>() {thing};
        }

        public ThingDef thing;
    }
    public class CQFThingCategoryCount : CQFThingData
    {
        public override ThingRequest GetRequest()
        {
            return new ThingRequest();
        }
        public override void DrawIcon(ref float y)
        {
            Rect rect = new Rect(20f, y, 35f, 35f);
            Widgets.DrawTextureFitted(new Rect(20f, y, 35f, 35f), this.category.icon, 1f);      
            if (Mouse.IsOver(rect))
            {
                Vector3 mouse = Input.mousePosition;
                Widgets.DrawBox(new Rect(mouse.x, mouse.y, 70f, 40f));
                Widgets.Label(rect, this.category.label);
            }
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("category", this.category.defName));
            return result;
        }
        public override string ToString()
        {
            return this.stuff?.label + " " + this.category?.label + this.count.ToString();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.category, "QE_ThingCategoryCount_category");
        }

        public override List<Thing> Spawn()
        {
            if (this.category.DescendantThingDefs.RandomElement() is ThingDef def)
            {
                Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? (this.stuff ?? GenStuff.RandomStuffFor(def)) : null);
                thing.stackCount = this.count.RandomInRange;
                return new List<Thing>() {thing};
            }
            return null;
        }

        public ThingCategoryDef category;
    }
    public class CQFThingSetMaker : CQFThingData
    {
        public override ThingRequest GetRequest()
        {
            return ThingRequest.ForUndefined();
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawButtonToSelectWithoutBackground(ref y,x + 7f,"ThingSetMaker".Translate(this.set?.label ?? this.set?.defName),DefDatabase<ThingSetMakerDef>.AllDefsListForReading,d => this.set = d,d => d.label ?? d.defName);
            CQFEditorTools.DrawFloatRange(ref y, "TotalMarketValueRange".Translate(),ref this.totalMarketValueRange,ref this.buffer,ref this.buffer2,x + 7f, 40f);
            y += 30f;
        }
        public override void DrawIcon(ref float y)
        {

        }
   
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("set", this.set.defName));
            result.Add(new XElement("totalMarketValueRange", this.totalMarketValueRange));
            return result;
        }
        public override string ToString()
        {
            return this.set?.label ?? this.set?.defName;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.set, "Set");
            Scribe_Values.Look(ref this.totalMarketValueRange, "totalMarketValueRange");
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.buffer2, "buffer2");
        }

        public override List<Thing> Spawn()
        {
            ThingSetMakerParams result = new ThingSetMakerParams();
            result.totalMarketValueRange = this.totalMarketValueRange;
            return this.set.root.Generate(result);
        }

        public string buffer;
        public string buffer2;
        public ThingSetMakerDef set;
        public FloatRange totalMarketValueRange = new FloatRange(100,1000);
    }
    public class CQFThingData_Corpse : CQFThingData
    {
        public static readonly List<RotStage> Stages = [RotStage.Rotting,RotStage.Dessicated,RotStage.Fresh];
        public override ThingRequest GetRequest()
        {
            return ThingRequest.ForUndefined();
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawButtonToSelectWithoutBackground(ref y, x + 7f, "QE_PawnKind".Translate(this.pawn?.label), DefDatabase<PawnKindDef>.AllDefsListForReading, d => this.pawn = d, d => d.label);
            if (Widgets.ButtonText(new Rect(x + 7f,y,200f,30f),
                    "CurRotMode".Translate(this.rotMode == null ? "Random".Translate() 
                        : this.rotMode.ToString().Translate()),false))
            {
                CQFEditorTools.DrawFloatMenu(Stages
                    ,r => this.rotMode = r,r => r.ToString().Translate()
                    ,[new FloatMenuOption("Random".Translate(),() => this.rotMode = null)]);
            }
            y += 30f;
        }
        public override void DrawIcon(ref float y)
        {

        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("pawn", this.pawn.defName));
            if (this.rotMode != null)
            {
                result.Add(new XElement("rotMode", this.rotMode));   
            }
            return result;
        }
        public override string ToString()
        {
            return this.pawn?.label;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.pawn, "pawn");
            Scribe_Values.Look(ref this.rotMode, "rotMode");
        }

        public override List<Thing> Spawn()
        {
            Pawn p = PawnGenerator.GeneratePawn(this.pawn);
            HealthUtility.SimulateKilled(p,DamageDefOf.Cut);
            if(p.Corpse.TryGetComp<CompRottable>() is {} comp)
            {
                RotStage stage = this.rotMode == null ? Stages.RandomElement() : this.rotMode.Value;
                comp.RotImmediately(stage);
            }
            return new List<Thing>() { p.Corpse };
        }

        public PawnKindDef pawn;
        public RotStage? rotMode;
    }
    public class CQFThingData_Genepack : CQFThingData
    {
        public override ThingRequest GetRequest()
        {
            return ThingRequest.ForUndefined();
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawDefList(this.genes,"Genes".Translate(),ref y, x + 5f);
        }
        public override void DrawIcon(ref float y)
        {

        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList(this.genes,"genes"));
            return result;
        }
        public override string ToString()
        {
            return this.genes.Any() ? "CQFThingData_Genepack".Translate().ToString() : this.genes.First().label;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.genes, "genes");
        }

        public override List<Thing> Spawn()
        {
            Genepack x = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
            x.Initialize(this.genes);
            return new List<Thing>() {x };
        }

        public List<GeneDef> genes = new List<GeneDef>();
    }
    public class CQFThingData_Value : CQFThingData
    {
        public override ThingRequest GetRequest()
        {
            return ThingRequest.ForUndefined();
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawButtonToSelectWithoutBackground(ref y, x + 7f,
                "CQFThingData_Value_Category".Translate(this.category?.label),
                DefDatabase<ThingCategoryDef>.AllDefsListForReading, d => this.category = d, d => d.label ?? d.defName);
            CQFEditorTools.DrawFloatRange(ref y, "TotalMarketValueRange".Translate(), ref this.totalMarketValueRange
                , ref this.buffer, ref this.buffer2, x + 7f, 40f);
            y += 30f;
        }
        public override void DrawIcon(ref float y)
        {

        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("category", this.category.defName));
            result.Add(new XElement("totalMarketValueRange", this.totalMarketValueRange));
            return result;
        }
        public override string ToString()
        {
            return this.category?.label;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.category, "category");
            Scribe_Values.Look(ref this.totalMarketValueRange, "totalMarketValueRange");
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.buffer2, "buffer2");
        }

        public override List<Thing> Spawn()
        {
            var result = new List<Thing>();

            ThingCategoryDef category = this.category;
            float remainingValue = this.totalMarketValueRange.RandomInRange;
            var cs = category.ThisAndChildCategoryDefs;
            // 找到所有属于该分类、可生成且有市场价的 ThingDef
            List<ThingDef> candidates = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def =>
                    def.thingCategories != null &&
                    def.thingCategories.Exists(c => cs.Contains(c)) &&
                    def.BaseMarketValue > 0)
                .ToList();

            if (candidates.Count == 0)
            {
                Log.Warning($"[Spawn] 分类 {category.label} 中没有可生成的物品");
                return result;
            }

            // 主循环：不断生成物品直到价值耗尽
            int safety = 500; // 安全阈防无限循环
            while (remainingValue > 0 && safety-- > 0)
            {
                // 随机挑选一个候选物品
                ThingDef def = candidates.RandomElement();

                float unitValue = def.BaseMarketValue;
                if (unitValue <= 0)
                {
                    continue;
                }

                // 计算最多可买多少个（堆叠上限限制）
                int maxCountByValue = Mathf.FloorToInt(remainingValue / unitValue);
                if (maxCountByValue <= 0)
                {
                    continue;
                }

                int stackCount = 1;

                if (def.stackLimit > 1)
                {
                    // 取较小的：不要超出价值，不要超出堆叠上限
                    stackCount = Rand.RangeInclusive(1, Mathf.Min(def.stackLimit, maxCountByValue));
                }

                Thing t = ThingMaker.MakeThing(def);
                t.stackCount = stackCount;

                result.Add(t);

                // 扣除价值
                remainingValue -= unitValue * stackCount;
            }

            return result;
        }

        public string buffer;
        public string buffer2;
        public ThingCategoryDef category; 
        public FloatRange totalMarketValueRange = new FloatRange(100, 1000);
    }
}


