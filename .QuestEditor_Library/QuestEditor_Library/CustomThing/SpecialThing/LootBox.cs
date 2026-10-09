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
            result.Append("CQF_OpenLootbox".Translate((this.openReport.CanTranslate() ? this.openReport.Translate().ToString() : this.openReport)).ToString().Trim());
            return result.ToString().Trim();
        }
        public void DrawTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootBox.DrawTab()", this, arguments);
        }
        public void PasteData()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootBox.PasteData()", this, arguments);
        }
        public void CopyData()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootBox.CopyData()", this, arguments);
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
                    job.reportStringOverride = (this.openReport.CanTranslate() ? this.openReport.Translate().ToString() : this.openReport);
                    yield return new FloatMenuOption((this.openReport.CanTranslate() ? this.openReport.Translate().ToString() : this.openReport), () =>
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
        internal Rect DrawSectionHeader(ref float y, float width, string label, bool drawSaveButton = false, bool skipLine = false)

        {
            object[] arguments = new object[]
            {
                y,
                width,
                label,
                drawSaveButton,
                skipLine
            };
            object result = CQFEditorBridge.Invoke("QuestEditor_Library.LootBox.DrawSectionHeader(Ref:float,None:float,None:string,None:bool,None:bool)", this, arguments);
            y = (float)arguments[0];
            return (UnityEngine.Rect)result;
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawHeader(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawHeader(Ref:float,None:float,None:float)", this, arguments);
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
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawBasicSettings(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawThingList(ref float y, Rect inRect, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawThingList(Ref:float,None:UnityEngine.Rect,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawCategoryList(ref float y, Rect inRect, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawCategoryList(Ref:float,None:UnityEngine.Rect,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawSpecialThingList(ref float y, Rect inRect, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawSpecialThingList(Ref:float,None:UnityEngine.Rect,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawPawnList(ref float y, float x, float width)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawPawnList(Ref:float,None:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawListHeader(ref float y, float x, float width, string label, Action addAction, Action removeAction)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                width,
                label,
                addAction,
                removeAction
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawListHeader(Ref:float,None:float,None:float,None:string,None:System.Action,None:System.Action)", this, arguments);
            y = (float)arguments[0];
        }
        internal void DrawListItemFrame(float startY, float endY, float x, float width)
        {
            Rect rect = new Rect(x, startY - 2f, width, Mathf.Max(34f, endY - startY + 4f));
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.DrawLine(new Vector2(x + 6f, rect.yMax), new Vector2(x + width - 6f, rect.yMax), CQFUIStyle.Accent, 1f);
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
            CQFEditorBridge.Invoke("QuestEditor_Library.LootData.DrawEmptyState(Ref:float,None:float,None:float,None:string)", this, arguments);
            y = (float)arguments[0];
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
            if (!string.IsNullOrWhiteSpace(this.message))
                Messages.Message((this.message.CanTranslate() ? this.message.Translate(text) : this.message.Formatted(text).ToString()),new LookTargets(pos,map),MessageTypeDefOf.NeutralEvent);
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
        [CQFLocalizableText]
        public string message = null;
        public List<PawnSpawnData> pawnDatas = new List<PawnSpawnData>();
        public List<CQFThingDefCount> things = new List<CQFThingDefCount>();
        public List<CQFThingCategoryCount> categorys = new List<CQFThingCategoryCount>();
        public List<CQFThingData> specialThingDatas = new List<CQFThingData>();
    }
    public abstract class CQFThingData : IExposable , ISaveable,IDrawable
    {
        public virtual bool CanSelectStuff => true;

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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public void DrawWithSingleCount(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData.DrawWithSingleCount(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
        public override bool CanSelectStuff => thing?.MadeFromStuff == true;

        public override ThingRequest GetRequest()
        {
            return ThingRequest.ForDef(this.thing);
        }
        public override void DrawIcon(ref float y)

        {
            object[] arguments = new object[]
            {
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingDefCount.DrawIcon(Ref:float)", this, arguments);
            y = (float)arguments[0];
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
            object[] arguments = new object[]
            {
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingCategoryCount.DrawIcon(Ref:float)", this, arguments);
            y = (float)arguments[0];
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingSetMaker.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void DrawIcon(ref float y)

        {
            object[] arguments = new object[]
            {
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingSetMaker.DrawIcon(Ref:float)", this, arguments);
            y = (float)arguments[0];
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData_Corpse.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void DrawIcon(ref float y)

        {
            object[] arguments = new object[]
            {
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData_Corpse.DrawIcon(Ref:float)", this, arguments);
            y = (float)arguments[0];
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData_Genepack.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void DrawIcon(ref float y)

        {
            object[] arguments = new object[]
            {
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData_Genepack.DrawIcon(Ref:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFSerialization.SaveList(this.genes,"genes"));
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData_Value.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void DrawIcon(ref float y)

        {
            object[] arguments = new object[]
            {
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFThingData_Value.DrawIcon(Ref:float)", this, arguments);
            y = (float)arguments[0];
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
