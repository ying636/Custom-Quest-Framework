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
            string result = base.GetInspectString();
            if (Prefs.DevMode)
            {
                result += base.GetInspectString() + "LootDatas".Translate();
                this.loots.ForEach(x => result += " " + x.dataName);
            }
            result += "CQF_OpenLootbox".Translate(this.openReport.Translate());
            return result.Trim();
        }
        public void DrawTab() 
        {
            Widgets.BeginScrollView(new Rect(7f, 25f, 475f, 590f), ref this.scrollPos, new Rect(7f, 10f, 475f, this.height));
            float y = 10f;
            CQFEditorTools.DrawLabelAndText_Line(y,"LootBoxName".Translate(),ref this.lootBoxName,16f,250f);
            Rect rectCP = new Rect(380f,y,25f,25f);
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
            y += 30f;
            Rect rect = new Rect(10f, y, 25f, 25f);
            if (this.useLootDef)
            {
                if (Widgets.ButtonText(new Rect(16f,y,400f,25f), "LootDef".Translate(this.lootDef?.defName),false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<LootDataDef>.AllDefsListForReading,d => this.lootDef = d,d => d.defName);
                }
                y += 30f;
            }
            else
            {
                if (Widgets.ButtonImage(rect,CQFEditorTools.icon_Save))
                {
                    LongEventHandler.QueueLongEvent(() =>
                    {
                        LootDataDef def = new LootDataDef();
                        def.defName = this.lootBoxName;
                        def.loots = this.loots;
                        DefDatabase<LootDataDef>.Add(def);
                        string path = Page_QuestEditor.Path + @"\Data\" + this.lootBoxName + ".xml";
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
                TooltipHandler.TipRegion(rect, "SaveAsDef".Translate());
                y += 30f;
                float initY = y;
                Rect rectData = new Rect(17f,y + 3f,600f,25f);
                foreach (LootData data in this.loots)
                {
                    if (Widgets.ButtonText(rectData,data.dataName + "  " + data.chance * 100f + "%",false)) 
                    {
                        Find.WindowStack.Add(new Dialog_EditIDrawable(data));
                    }
                    y += 30f;
                    rectData.y += 30f;
                }
                Widgets.DrawBox(new Rect(10f,initY,400f,y - initY),1,QuestEditor_Dialog.blueTex);
                y += 10f;
                if (Widgets.ButtonText(new Rect(10f, y, 100f, 38f), "AddNewLootData".Translate()))
                {
                    this.loots.Add(new LootData());
                }
                if (Widgets.ButtonText(new Rect(150f, y, 100f, 38f), "Paste".Translate()) && CQFEditorTools.lootData != null)
                {
                    this.loots.Add(CQFEditorTools.lootData.Copy());
                }
                if (Widgets.ButtonText(new Rect(300f, y, 100f, 38f), "DeleteLootData".Translate()) && this.loots.Any())
                {
                    CQFEditorTools.DrawFloatMenu(this.loots, (x) => this.loots.Remove(x), (x) => x.dataName);
                }
                y += 45f;
            }
            CQFEditorTools.DrawLabelAndText_Line(y, "JobReport".Translate(), ref this.openReport, 16f,180f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "TickToOpenLoot".Translate(), ref this.tickToOpen, ref this.buffer, 16f,180f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f,y,350f,25f),"DestroyAfterOpening".Translate(), ref this.destroyAfterOpening);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f,y,350f,25f),"OpenWhenDestroyed".Translate(), ref this.openWhenDestroyed);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(16f, y, 350f, 25f), "UseLootDef".Translate(), ref this.useLootDef);
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
                GameTools.temporaryTargets.Clear();
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
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(10f, y + 7f, 1020f, 45f), this.dataName.Colorize(ColorLibrary.SkyBlue));
            Text.Font = GameFont.Small;
            y += 50f;      
            Rect rect = new Rect(340f + x, y, 30f, 30f);
            if (Widgets.ButtonImage(rect, TexButton.Copy))
            {
                CQFEditorTools.lootData = this.Copy();
            }
            TooltipHandler.TipRegion(rect, "Copy".Translate());
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), "Rename".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE((name) => this.dataName = name));
            }
            y += 30f;
            CQFEditorTools.DrawFieldAndText(ref y,"MessageAfterOpening".Translate(),ref this.message,x,400f);
            y += 35f;
            Widgets.Label(new Rect(x,y, 150f, 25f), "LootThings".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon<CQFThingDefCount>(y,this.things,t => t.thing.label + "x" + t.count,() => Find.WindowStack.Add(new Dialog_Select<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((t2) => t2.category == ThingCategory.Item && !t2.IsCorpse),
                d => d.uiIcon, d => d.label, "SelectLootThing".Translate(), d => this.things.Add(new CQFThingDefCount { thing = d }))),340f,25f,40f);
            y += 30f;
            float initY = y;
            foreach (CQFThingDefCount thing in this.things)
            {
                y += 5f;
                thing.Draw(ref y,inRect,x - 3f); 
                y += 5f;
            }
            Widgets.DrawBox(new Rect(x, initY,inRect.width - 40f - (2*x),y - initY),1,QuestEditor_Dialog.blueTex);
            y += 25f;
            Widgets.Label(new Rect(x, y, 150f, 25f), "LootCategorys".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon<CQFThingCategoryCount>(y, this.categorys, t2 => t2.category.label + "x" + t2.count,
  () => CQFEditorTools.DrawFloatMenu<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll((t2) => t2.defName != "Corpses" &&
  !t2.Parents.Contains(ThingCategoryDefOf.Corpses) && t2 != ThingCategoryDefOf.Animals), t2 => this.categorys.Add(new CQFThingCategoryCount() { category = t2 }), t2 => t2.label),340f, 25f, 40f);
            y += 30f;
            initY = y;
            foreach (CQFThingCategoryCount cetegory in this.categorys)
            {
                y += 5f;
                cetegory.Draw(ref y, inRect, x - 3f);
                y += 5f;
            }
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), y - initY), 1, QuestEditor_Dialog.blueTex); 
            y += 25f;
            Widgets.Label(new Rect(x, y, 150f, 25f), "SpecialThingData".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, this.specialThingDatas, t2 => t2.ToString(),
  () => CQFEditorTools.DrawFloatMenu(typeof(CQFThingData).AllSubclassesNonAbstract().FindAll(t => t != typeof(CQFThingDefCount) && t != typeof(CQFThingCategoryCount)), t2 => this.specialThingDatas.Add((CQFThingData)Activator.CreateInstance(t2)), t2 => t2.Name.Translate()), 340f, 25f, 40f);
            y += 30f;
            initY = y;
            foreach (CQFThingData data in this.specialThingDatas)
            {
                y += 5f;
                data.Draw(ref y, inRect, x - 3f);
                y += 5f;
            }
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 25f;
            Widgets.Label(new Rect(x, y, 150f, 25f), "LootPawn".Translate());
            CQFEditorTools.DrawButtonForPawnData_UseIcon(y, this.pawnDatas, 25f, 40f, 340f);
            y += 30f;
            initY = y;
            foreach (PawnSpawnData pawnData in this.pawnDatas)
            {
                Rect rectData = new Rect(17f, y + 3f, 600f, 25f);
                if (Widgets.ButtonText(rectData, pawnData.dataName, false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(pawnData));
                }
                y += 30f;
            }
            Widgets.DrawBox(new Rect(x, initY, inRect.width - 40f - (2 * x), y - initY), 1, QuestEditor_Dialog.blueTex);
            y += 20f;
            CQFEditorTools.DrawLabelAndText_Line(y, "LootChance".Translate(), ref this.chance, ref this.buffer, 16f + x);
            y += 30f;
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
        public static void OpenSelectWindow(Type type, Action<CQFThingData> action)
        {
            if (type == typeof(CQFThingDefCount)) 
            {
                Find.WindowStack.Add(new Dialog_Select<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((t2) => t2.category == ThingCategory.Item && !t2.IsCorpse),
                d => d.uiIcon, d => d.label, "SelectLootThing".Translate(), d => action(new CQFThingDefCount { thing = d }), null, (t, r) => Widgets.DefIcon(r, t, null)));
            }
            if (type == typeof(CQFThingCategoryCount))
            {
                Find.WindowStack.Add(new Dialog_Select<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll((t2) => t2.defName != "Corpses" && !t2.Parents.Contains(ThingCategoryDefOf.Corpses) && t2 != ThingCategoryDefOf.Animals),
null,d => d.label, "Select".Translate(), d => action(new CQFThingCategoryCount { category = d }), null, (t, r) => Widgets.DefIcon(r, t, null)));
            }
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


