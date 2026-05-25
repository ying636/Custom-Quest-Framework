using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class PawnSpawnData : IExposable, ISaveable, IDrawable
    {
        public virtual PawnSpawnData Copy()
        {
            XElement x = this.SaveToXElement("PawnSpawnData");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            PawnSpawnData result = DirectXmlToObject.ObjectFromXml<PawnSpawnData>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(16f + x, y + 10f, 500f, 45f);
            this.DrawName(ref y, x, rect);
            this.DrawKind(x, ref y);
            CQFEditorTools.DrawSelectableText(y, "PawnDataFaction".Translate(), ref this.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => this.faction = f.defName, (f) => f.label, new List<FloatMenuOption>()
            {
                new FloatMenuOption("RandomHostile".Translate(),() => this.faction = "RandomHostile"),
                new FloatMenuOption("RandomAlly".Translate(),() => this.faction = "RandomAlly"),
                new FloatMenuOption("RandomNeutral".Translate(),() => this.faction = "RandomNeutral"),
                new FloatMenuOption("PawnDataMapFaction".Translate(),() => this.faction = "MapFaction")
            }), 20f + x, 120f);
            TooltipHandler.TipRegion(new Rect(x + 20f, y, 340f, 25f), "PawnDataFactionTip".Translate());
            y += 30f;
            TooltipHandler.TipRegion(rect, "Copy".Translate());
            Rect spawmType = new Rect(20f + x, y, 420f, 25f);
            if (Widgets.ButtonText(spawmType, "SpawnType".Translate(this.spawnType.ToString().Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu<SpawnType>(new List<SpawnType>() { SpawnType.BuildingDamaged, SpawnType.BuildingTick, SpawnType.MapGeneration, SpawnType.BuildingDestroyed }, (t) => this.spawnType = t, (t) => t.ToString().Translate());
            }
            if (this.spawnType == SpawnType.BuildingTick)
            {
                TooltipHandler.TipRegion(spawmType, "SpawnTypeTip_BuildingTick".Translate());
                y += 30f;
                string text_Time = "TimeToSpawn".Translate();
                Widgets.Label(new Rect(20f + x, y, 150f, 25f), text_Time);
                Widgets.TextFieldNumeric<int>(new Rect(Text.CalcSize(text_Time).x + x + 25f, y, 150f, 25f), ref this.timeToSpawn, ref this.buffer_time);
            }
            y += 30f;
            string text_Spawn = "SpawnMessage".Translate();
            Widgets.Label(new Rect(20f + x, y, 150f, 25f), text_Spawn);
            this.spawnMessage = Widgets.TextField(new Rect(Text.CalcSize(text_Spawn).x + x + 25f, y, 300f, 25f), this.spawnMessage);
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "QE_Count".Translate(), ref this.count, ref this.buffer, ref this.bufferMax, x + 20f);
            Rect enable = new Rect(20f + x, y, 150f, 25f);
            Widgets.CheckboxLabeled(enable, "EnableLord".Translate(), ref this.enableLord);
            TooltipHandler.TipRegion(enable, new TipSignal("LordAndFactionTip".Translate()));
            y += 30f;
            if (this.enableLord)
            {
                Rect rectDuty = new Rect(20f + x, y, 250f, 25f);
                if (Widgets.ButtonText(rectDuty,
                    "DutyType".Translate(this.duty == null 
                    ? null :
                    (this.duty.HasModExtension<ModExtension_CustomDuty>()
                    ? this.duty.label : this.duty.defName.Translate().ToString())), false))
                {
                    Find.WindowStack.Add(new Dialog_Select<DutyDef>(DefDatabase<DutyDef>.AllDefsListForReading,
                        null, (d) => d == null ? null : d.HasModExtension<ModExtension_CustomDuty>() ? d.label : d.defName.CanTranslate() ? d.defName.Translate().ToString() : d.defName, "Select".Translate(), (d) => this.duty = d,
                        null, null, d => d.description, d => d.HasModExtension<ModExtension_CustomDuty>() || d.defName.CanTranslate() ? 1 : 5));
                }
                if (this.duty?.description != null && this.duty.description != "")
                {
                    TooltipHandler.TipRegion(rectDuty, this.duty.description);
                }
                if (this.duty == QEDefOf.QE_Duty_Guard && Widgets.ButtonText(new Rect(180f + x, y, 200f, 25f), "CurRoute".Translate(this.routeName), false) && Find.CurrentMap != null && Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Any())
                {
                    CQFEditorTools.DrawFloatMenu<string>(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().route.Keys.ToList(), (r) => this.routeName = r, (r) => r);
                }
                y += 30f;
                if (this.duty == QEDefOf.QE_Duty_Waiter)
                {
                    CQFEditorTools.DrawButtonAndText(ref y, "PawnRotation".Translate(this.rotation.ToStringHuman()), "SelectRotation".Translate(), () => CQFEditorTools.DrawFloatMenu<Rot4>(new List<Rot4>() { Rot4.East, Rot4.West, Rot4.North, Rot4.South }, (r) => this.rotation = r, (r) => r.ToStringHuman()), 20f + x);
                }
                Rect rect2 = new Rect(20f + x, y, 150f, 25f);
                CQFEditorTools.DrawSelectableText(y, "LordNameWithTarget".Translate(),ref this.lordDataName,
                    () => CQFEditorTools.DrawFloatMenu(Find.CurrentMap.GetComponent<MapComponent_CustomMapData>().Lords, l => this.lordDataName = l.data.name, l => l.data.name)
                ,x + 20f,150f);
                TooltipHandler.TipRegion(rect2, "CustomLordNameTip".Translate());
                y += 30f;
            }
            List<DialogManagerDef> managers = new List<DialogManagerDef>();
            managers.AddRange(DefDatabase<DialogManagerDef>.AllDefsListForReading);
            managers.AddRange(CQFEditorTools.GetObject<DialogManagerDef>(Page_QuestEditor.Path + @"\DialogTree\", "//QuestEditor_Library.DialogManagerDef"));
            CQFEditorTools.DrawButtonAndText(ref y, "DialogTree".Translate(this.dialogManager?.defName), "Select".Translate(),
                () => CQFEditorTools.DrawFloatMenu(managers, (t) => this.dialogManager = t, (t) => t.defName), 20f + x);
            y += 5f;
            if (Widgets.ButtonText(new Rect(20f + x, y, 300f, 30f), "Misc".Translate(), false))
            {
                Find.WindowStack.Add(new QuestEditor_PawnDataMisc(this));
            }
            y += 35f;
            this.DrawInventory(ref y, x);
            y += 5f;
        }
        public virtual void DrawKind(float x, ref float y)
        {
            if (Widgets.ButtonText(new Rect(20f + x, y, 250f, 25f), "QE_PawnKind".Translate(this.kind?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(
                    DefDatabase<PawnKindDef>.AllDefs.ToList(),null,k => k.label,"Select".Translate()
                    , (k) => this.kind = k));
            }
            y += 30f;
        }
        public void DrawName(ref float y, float x, Rect nameRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(nameRect, this.dataName.Colorize(ColorLibrary.SkyBlue));
            nameRect.width -= 300f;
            TooltipHandler.TipRegion(nameRect, "PawnDataNameTip".Translate());
            Text.Font = GameFont.Small;
            Rect rect = new Rect(370f + x, y, 30f, 30f);
            if (Widgets.ButtonImage(rect, TexButton.Copy))
            {
                CQFEditorTools.data = this.Copy();
            }
            y += 50f;
            if (Widgets.ButtonText(new Rect(16f + x, y, 150f, 25f), "Rename".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE((name) => this.dataName = name));
            }
            y += 40f;
        }
        public void DrawInventory(ref float y, float x = 0f)
        {
            Widgets.Label(new Rect(16f + x, y, 150f, 25f), "InventoryThing".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, this.inventoryThings, t2 => t2.thing.label + "x" + t2.count,
() => Find.WindowStack.Add(new Dialog_Select<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((c) => c.category == ThingCategory.Item && !c.IsCorpse),
c => c.uiIcon, c => c.label, "Select".Translate(),
(d) => this.inventoryThings.Add(new CQFThingDefCount() { thing = d }))), 340f, 25f, 40f);
            y += 30f;
            Widgets.DrawLine(new Vector2(16f + x, y), new Vector2(465f + x, y), ColorLibrary.SkyBlue, 2.5f);
            foreach (CQFThingDefCount thing in this.inventoryThings)
            {
                y += 5f;
                thing.Draw(ref y, new Rect(), x);
                y += 5f;
            }
            y += 20f;
            y += 45f;
            Widgets.Label(new Rect(16f + x, y, 150f, 25f), "InventoryThingCategorys".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, this.inventoryCategorys, t2 => t2.category.label + "x" + t2.count,
() => Find.WindowStack.Add(new Dialog_Select<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll((c) => c.defName != "Corpses" && !c.Parents.Contains(ThingCategoryDefOf.Corpses) && c != ThingCategoryDefOf.Animals), c => c.icon, c => c.label, "Select".Translate(),
          (d) => this.inventoryCategorys.Add(new CQFThingCategoryCount() { category = d }))), 340f, 25f, 40f);
            y += 30f;
            Widgets.DrawLine(new Vector2(16f + x, y), new Vector2(465f + x, y), ColorLibrary.SkyBlue, 2f);
            foreach (CQFThingCategoryCount cetegory in this.inventoryCategorys)
            {
                y += 5f;
                cetegory.Draw(ref y, new Rect(), x);
                y += 5f;
            }
            y += 45f;
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            if (this.GetType() != typeof(PawnSpawnData))
            {
                result.SetAttributeValue("Class", this.GetType().FullName);
            }
            if (this.dataName != "undefined")
            {
                result.Add(new XElement("dataName", this.dataName));
            }
            if (this.kind != null)
            {
                result.Add(new XElement("kind", this.kind.defName));
            }
            if (this.enableLord)
            {
                result.Add(new XElement("enableLord", this.enableLord));
                if (!this.lordDataName.NullOrEmpty())
                {
                    result.Add(new XElement("lordDataName", this.lordDataName));
                }
            }
            if (!this.roundTrip)
            {
                result.Add(new XElement("roundTrip", this.roundTrip));
            }
            result.Add(new XElement("count", this.count));
            if (this.spawnType == SpawnType.BuildingTick)
            {
                result.Add(new XElement("timeToSpawn", this.timeToSpawn));
            }
            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction));
            }
            if (this.routeName != null)
            {
                result.Add(new XElement("routeName", this.routeName));
            }
            if (this.generationChance != 1f)
            {
                result.Add(new XElement("generationChance", this.generationChance));
            }
            if (this.dialogManager != null)
            {
                result.Add(new XElement("dialogManager", this.dialogManager.defName));
            }
            if (this.enableLord)
            {
                result.Add(new XElement("duty", this.duty?.defName));
            }
            result.Add(new XElement("spawnType", this.spawnType));
            if (this.rotation != Rot4.South)
            {
                result.Add(new XElement("rotation", this.rotation.ToStringWord()));
            }
            if (this.spawnMessage != null && this.spawnMessage != "")
            {
                result.Add(new XElement("spawnMessage", this.spawnMessage));
            }
            if (this.extraKinds != null && this.extraKinds.Any())
            {
                XElement extra = new XElement("extraKinds");
                this.extraKinds.ForEach(x => extra.Add(new XElement("li", x.defName)));
                result.Add(extra);
            }
            if (this.actions.Any())
            {
                XElement actions = new XElement("actions");
                this.actions.ForEach(x => actions.Add(x.SaveToXElement("li")));
                result.Add(actions);
            }
            if (this.hediffs.Any())
            {
                XElement hediffs = new XElement("hediffs");
                this.hediffs.ForEach(x => hediffs.Add(x.SaveToXElement("li")));
                result.Add(hediffs);
            }
            if (this.inventoryThings.Any())
            {
                XElement thingData = new XElement("inventoryThings");
                this.inventoryThings.ForEach((x) => thingData.Add(x.SaveToXElement("li")));
                result.Add(thingData);
            }
            if (this.inventoryCategorys.Any())
            {
                XElement categoryData = new XElement("inventoryCategorys");
                this.inventoryCategorys.ForEach((x) => categoryData.Add(x.SaveToXElement("li")));
                result.Add(categoryData);
            }
            return result;
        }
        public virtual Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null,bool setLord = true)
        {
            try
            {
                if (!Rand.Chance(this.generationChance) || !position.InBounds(map))
                {
                    return null;
                }
                Faction faction = GameTools.GetFaction(this.faction, map);
                Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
                if (!position.Fogged(map) && this.spawnMessage != null && !this.spawnMessage.NullOrEmpty())
                {
                    Messages.Message(this.spawnMessage.Translate(), new LookTargets(position, map), MessageTypeDefOf.NeutralEvent);
                }
                if (setLord && faction != null && lord == null && this.enableLord)
                {
                    lord = map.lordManager.lords.Find(l => l.LordJob is LordJob_Custom && l.faction == faction);
                    if (lord == null)
                    {
                        lord = LordMaker.MakeNewLord(faction, new LordJob_Custom(), map);
                    }
                }
                List<PawnKindDef> kinds = new List<PawnKindDef>();
                kinds.AddRange(this.extraKinds);
                kinds.Add(this.kind);
                List<Pawn> pawns = new List<Pawn>();
                MakePawns(position, questTag, quest, lord, faction, result, kinds, pawns);
                this.SpawnPnaw(pawns, position, map);
                if (this.dataName != "undefined")
                {
                    List<Pawn> ps = new List<Pawn>();
                    result.Values.ToList().ForEach(t => ps.Add(t.Thing as Pawn));
                    GameComponent_Editor.Component.GetQuestData(quest)?.AddGroup(this.dataName, ps);
                }
                return result;
            }
            catch (Exception ex) 
            {
                Log.Error("CQF Error:" + ex);
                return null;
            }
        }

        public virtual void MakePawns(IntVec3 position, string questTag, Quest quest, Lord lord,
            Faction faction, Dictionary<string, TargetInfo> result, List<PawnKindDef> kinds, List<Pawn> pawns)
        {
            foreach (PawnKindDef kind in kinds)
            {
                try
                {
                    int count = this.count.RandomInRange;
                    for (int i = 0; i < count; i++)
                    {
                        Pawn pawn = (Pawn)PawnGenerator.GeneratePawn(kind, faction);
                        if (pawn == null)
                        {
                            continue;
                        }
                        pawns.Add(pawn);
                        this.ActionAfterGeneration(pawn, quest, i, questTag);
                        if (lord != null)
                        {
                            if (this.duty == DutyDefOf.Defend && this.routeName.NullOrEmpty()
                                && lord.LordJob is LordJob_Custom lordJob)
                            {
                                lordJob.defendDatas.SetOrAdd(pawn, position);
                            }
                            lord.AddPawn(pawn);
                            PawnDuty duty = new PawnDuty(this.duty);
                            duty.overrideFacing = this.rotation;
                            duty.focus = new LocalTargetInfo(position);
                            pawn.mindState.duty = duty;
                            if (lord.LordJob is LordJob_Custom job)
                            {
                                job.pawnDutyDatas.Add(pawn, this.duty);
                            }
                        }
                        if (pawn.kindDef == this.kind)
                        {
                            result.SetOrAdd(this.dataName + "." + i, pawn);
                        }
                        else
                        {
                            result.SetOrAdd(this.dataName + "_" + pawn.kindDef.defName + "." + i, pawn);
                        }
                    }
                }
                catch(Exception e) 
                {
                    Log.Error("Spawn pawn fail:" + e);
                }
            }
        }

        public virtual void SpawnPnaw(List<Pawn> pawns, IntVec3 position, Map map) 
        {
            this.way.SpawnPnaw(pawns, position, map);
        }
        public void ActionAfterGeneration(Pawn pawn, Quest quest, int i, string questTag)
        {
            this.actions.ForEach(x => x.Work(new Dictionary<string, TargetInfo>()
            {
                [this.dataName + "." + i] = pawn
            }, quest));
            foreach (HediffInformation hediff in this.hediffs)
            {
                BodyPartRecord record = null;
                if (hediff.part != null)
                {
                    List<BodyPartRecord> records = pawn.RaceProps.body.GetPartsWithDef(hediff.part);
                    record = hediff.partLabel == null || hediff.partLabel == "" ? records.First() : records.Find(x => x.customLabel == hediff.partLabel);
                }
                pawn.health.AddHediff(hediff.hediff, record).Severity = hediff.severity;
            }
            if (this.dialogManager != null)
            {
                Current.Game.GetComponent<GameComponent_Editor>().AddDialog(pawn, this.dialogManager);
            }
            pawn.questTags = new List<string>()
                {
                 string.Concat(new object[]
                  {
                       questTag,
                       ".",
                       this.dataName,
                      ".",
                     i
                   })
                 ,
                                  string.Concat(new object[]
                  {
                       questTag,
                       ".",
                       this.dataName
                   })
                 ,
                 questTag
                };
            this.inventoryThings.ForEach(x =>
            {
                Thing thing = ThingMaker.MakeThing(x.thing, x.stuff);
                thing.stackCount = x.count.RandomInRange;
                pawn.inventory.innerContainer.TryAdd(thing);
            });

            this.inventoryCategorys.ForEach(x =>
            {
                Thing thing = ThingMaker.MakeThing(x.category.DescendantThingDefs.RandomElement(), x.stuff);
                thing.stackCount = x.count.RandomInRange;
                pawn.inventory.innerContainer.TryAdd(thing);
            });
        }
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref this.dataName, "QE_PawnData_dataName");
            Scribe_Values.Look(ref this.buffer, "QE_PawnData_buffer");
            Scribe_Values.Look(ref this.bufferMax, "QE_PawnData_bufferMax");
            Scribe_Values.Look(ref this.buffer_time, "QE_PawnData_buffer_time");
            Scribe_Values.Look(ref this.enableLord, "QE_PawnData_isOneOfLord");
            Scribe_Values.Look(ref this.count, "QE_PawnData_count");
            Scribe_Values.Look(ref this.lordDataName, "QE_PawnData_lordDataName");
            Scribe_Values.Look(ref this.timeToSpawn, "QE_PawnData_timeToSpawn");
            Scribe_Values.Look(ref this.routeName, "QE_PawnData_routeName");
            Scribe_Values.Look(ref this.spawnType, "QE_PawnData_spawnType");
            Scribe_Values.Look(ref this.spawnMessage, "QE_PawnData_spawnMessage");
            Scribe_Values.Look(ref this.rotation, "QE_PawnData_rotation");
            Scribe_Values.Look(ref this.buffer_chance, "buffer_chance");
            Scribe_Values.Look(ref this.generationChance, "QE_PawnData_generationChance");
            Scribe_Values.Look(ref this.roundTrip, "QE_PawnData_roundTrip");
            Scribe_Values.Look(ref this.faction, "QE_PawnData_faction");
            Scribe_Defs.Look(ref this.duty, "QE_PawnData_duty");
            Scribe_Defs.Look(ref this.dialogManager, "QE_PawnData_dialogManager");
            Scribe_Defs.Look(ref this.kind, "QE_PawnData_kind");
            Scribe_Deep.Look(ref this.way,"way");
            Scribe_Collections.Look(ref this.extraKinds, "QE_PawnSpawnData_extraKind", LookMode.Def);
            Scribe_Collections.Look(ref this.inventoryThings, "QE_PawnSpawnData_inventoryThings", LookMode.Deep);
            Scribe_Collections.Look(ref this.inventoryCategorys, "QE_PawnSpawnData_inventoryCategorys", LookMode.Deep);
            Scribe_Collections.Look(ref this.hediffs, "QE_PawnSpawnData_hediffs", LookMode.Deep);
            Scribe_Collections.Look(ref this.actions, "QE_PawnSpawnData_actions", LookMode.Deep);
        }

        [NoTranslate]
        public string dataName = "undefined";
        public string buffer;
        public string bufferMax;
        public string buffer_time;
        public string buffer_chance;
        public float generationChance = 1f;
        public IntRange count = new IntRange(1, 1);
        public int timeToSpawn = 0;
        public bool enableLord = false;
        public string lordDataName;
        public string spawnMessage = null;

        public string routeName = null;
        public bool roundTrip = true;
        public Rot4 rotation = Rot4.South;
        public SpawnType spawnType = SpawnType.MapGeneration;
        public string faction = null;
        public ArrivingWay way = new ArrivingWay();
        public DutyDef duty = DutyDefOf.Defend;
        public PawnKindDef kind = null;
        public List<PawnKindDef> extraKinds = new List<PawnKindDef>();
        public DialogManagerDef dialogManager = null;
        public List<HediffInformation> hediffs = new List<HediffInformation>();
        public List<CQFAction> actions = new List<CQFAction>();
        public List<CQFThingDefCount> inventoryThings = new List<CQFThingDefCount>();
        public List<CQFThingCategoryCount> inventoryCategorys = new List<CQFThingCategoryCount>();
    }
    public class PawnSpawnData_Random : PawnSpawnData
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(16f + x, y + 10f, 500f, 45f);
            this.DrawName(ref y, x, rect);
            if (Widgets.ButtonText(new Rect(16f + x, y, 420f, 25f), "SpawnType".Translate(this.spawnType.ToString().Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu<SpawnType>(new List<SpawnType>() { SpawnType.BuildingDamaged, SpawnType.BuildingTick, SpawnType.MapGeneration, SpawnType.BuildingDestroyed }, (t) => this.spawnType = t, (t) => t.ToString().Translate());
            }
            if (this.spawnType == SpawnType.BuildingTick)
            {
                y += 30f;
                string text_Time = "TimeToSpawn".Translate();
                Widgets.Label(new Rect(16f + x, y, 150f, 25f), text_Time);
                Widgets.TextFieldNumeric<int>(new Rect(Text.CalcSize(text_Time).x + x + 25f, y, 150f, 25f), ref this.timeToSpawn, ref this.buffer_time);
            }
            y += 30f;
            CQFEditorTools.DrawPawnDataList_UseWindow_UseIcon(ref y, 16f + x, this.datas, inRect, "PawnSpawnDatas".Translate(), d => d.dataName);
        }
        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            if (!this.datas.Any())
            {
                Log.Error("Custom Quset Framework Error:Pawn data list of PawnSpawnData_Random is empty");
                return null;
            }
            return this.datas.RandomElement().Spawn(position, map, questTag, quest, lord,setLord);
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList_Saveable<PawnSpawnData>(this.datas, "datas"));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.datas, "PawnSpawnData_Random_datas", LookMode.Deep);
        }

        public List<PawnSpawnData> datas = new List<PawnSpawnData>();
    }
    public class PawnSpawnData_Faction : PawnSpawnData
    {
        public override void DrawKind(float x, ref float y)
        {
            Rect rect = new Rect(20f + x, y, 250f, 25f);
            if (Widgets.ButtonText(rect, "CQF_PawnGroupMaker".Translate(this.kindDef?.defName), false) && !this.faction.NullOrEmpty() && FactionDef.Named(this.faction) is FactionDef factionDef && !factionDef.pawnGroupMakers.NullOrEmpty())
            {
                CQFEditorTools.DrawFloatMenu(factionDef.pawnGroupMakers, (k) => this.kindDef = k.kindDef, (k) =>
                {
                    return k.kindDef.defName + ":" + k.commonality;
                });
            }
            TooltipHandler.TipRegion(rect, "CQF_PawnGroupMaker_Tip".Translate());
            y += 30f;
            CQFEditorTools.DrawIntRange(ref y, "SpawmPoint".Translate(), ref this.point, ref buffer1, ref buffer2, x + 20f, 80f);
        }
        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null,bool setLord = true)
        {
            if (GameTools.GetFaction(this.faction, map) is Faction f)
            {
                List<PawnGroupMaker> makers = f.def.pawnGroupMakers.FindAll(g => this.kindDef == null ? true : g.kindDef == this.kindDef);
                if (makers.Any() && makers.RandomElementByWeight(m => m.commonality) is PawnGroupMaker maker)
                {
                    if (!Rand.Chance(this.generationChance) || !position.InBounds(map))
                    {
                        return null;
                    }
                    Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
                    if (!position.Fogged(map) && this.spawnMessage != null && !this.spawnMessage.NullOrEmpty())
                    {
                        Messages.Message(this.spawnMessage.Translate(), new LookTargets(position, map), MessageTypeDefOf.NeutralEvent);
                    }
                    PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms();
                    pawnGroupMakerParms.groupKind = this.kindDef;
                    pawnGroupMakerParms.tile = map.Tile;
                    pawnGroupMakerParms.faction = f;
                    pawnGroupMakerParms.points = Mathf.Max(this.point.RandomInRange, f.def.MinPointsToGeneratePawnGroup(this.kindDef, null));
                    int i = 0;
                    List<Pawn> pawns = new List<Pawn>();
                    maker.GeneratePawns(pawnGroupMakerParms).ToList().ForEach(p =>
                    {
                        pawns.Add(p);
                        this.ActionAfterGeneration(p, quest, i, questTag);
                        if (lord != null)
                        {
                            lord.AddPawn(p);
                            PawnDuty duty = new PawnDuty(this.duty);
                            duty.overrideFacing = this.rotation;
                            duty.focus = new LocalTargetInfo(position);
                            p.mindState.duty = duty;
                            if (lord.LordJob is LordJob_Custom job)
                            {
                                job.pawnDutyDatas.Add(p, this.duty);
                            }
                        }
                        if (p.kindDef == this.kind)
                        {
                            result.SetOrAdd(this.dataName + "." + i, p);
                        }
                        else
                        {
                            result.SetOrAdd(this.dataName + "_" + p.kindDef.defName + "." + i, p);
                        }
                        i++;
                    });
                    this.SpawnPnaw(pawns, position, map);
                    if (this.dataName != "undefined")
                    {
                        List<Pawn> ps = new List<Pawn>();
                        result.Values.ToList().ForEach(t => ps.Add(t.Thing as Pawn));
                        GameComponent_Editor.Component.GetQuestData(quest)?.AddGroup(this.dataName, ps);
                    }
                    return result;
                }
            }
            Log.Error("Custom Quset Framework Error:Pawn data lack faction");
            return null;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("point", this.point.ToString()));
            if (this.kindDef != null)
            {
                result.Add(new XElement("kindDef", this.kindDef.defName));
            }
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.point, "point");
            Scribe_Defs.Look(ref this.kindDef, "kindDef");
        }

        public IntRange point = new IntRange();
        public PawnGroupKindDef kindDef;
        public string buffer1;
        public string buffer2;
    }
    public class PawnSpawnData_Group : PawnSpawnData
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(20f + x, y, 250f, 25f);
            if (Widgets.ButtonText(rect, "CQF_PawnGroupDef".Translate(this.group?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu<GroupDataDef>(DefDatabase<GroupDataDef>.AllDefsListForReading, (k) => this.group = k, (k) =>
                {
                    return k.label;
                });
            }
            y += 30f;
        }
        public override Dictionary<string, TargetInfo> Spawn(IntVec3 position, Map map, string questTag, Quest quest, Lord lord = null, bool setLord = true)
        {
            return this.group.Generate(map, position, quest);
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.group != null)
            {
                result.Add(new XElement("group", this.group.defName));
            }
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.group, "group");
        }

        public GroupDataDef group;
    }
    public class DutyData
    {


    }
    public class HediffInformation : ISaveable, IExposable
    {
        public HediffInformation() { }
        public HediffInformation(HediffDef hediff, BodyPartDef part, float severity, string partLabel)
        {
            this.partLabel = partLabel;
            this.part = part;
            this.hediff = hediff;
            this.severity = severity;
            this.buffer = "";
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("partLabel", this.partLabel));
            result.Add(new XElement("hediff", this.hediff.defName));
            result.Add(new XElement("part", this.part?.defName));
            result.Add(new XElement("severity", this.severity));
            result.Add(new XElement("partLabelForSeeing", this.partLabelForSeeing));
            return result;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.partLabelForSeeing, "HediffInformation_partLabelForSeeing");
            Scribe_Values.Look(ref this.severity, "HediffInformation_severity");
            Scribe_Values.Look(ref this.partLabel, "HediffInformation_partLabel");
            Scribe_Defs.Look(ref this.part, "HediffInformation_part");
            Scribe_Defs.Look(ref this.hediff, "HediffInformation_hediff");
        }

        public string buffer;
        public float severity;
        public string partLabelForSeeing;
        public string partLabel;
        public BodyPartDef part;
        public HediffDef hediff;
    }

    public class ArrivingWay : IExposable,IDrawable
    {     
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
             
        }
        public virtual void SpawnPnaw(List<Pawn> pawns, IntVec3 position, Map map)
        {
            pawns.ForEach(pawn => GenSpawn.Spawn(pawn, position, map));  
        }
        public virtual void ExposeData()
        {
        
        }
    }
    public class ArrivingWay_DropPod : ArrivingWay 
    {
        public override void SpawnPnaw(List<Pawn> pawns, IntVec3 position, Map map)
        {
            ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
            foreach (Thing item in pawns)
            {
                activeDropPodInfo.innerContainer.TryAdd(item, true);
            }
            DropPodUtility.MakeDropPodAt(position,map, activeDropPodInfo);
        }
    }
}