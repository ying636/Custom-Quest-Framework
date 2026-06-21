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
                    "DutyType".Translate(CQFEditorTools.DutyLabel(this.duty)), false))
                {
                    CQFEditorTools.OpenDutySelect(d => this.duty = d);
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
            CQFEditorTools.DrawButtonAndText(ref y, "DialogTree".Translate(this.dialogManager?.defName), "Select".Translate(),
                () => CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, (t) => this.dialogManager = t, (t) => t.defName), 20f + x);
            y += 5f;
            if (Widgets.ButtonText(new Rect(20f + x, y, 300f, 30f), "Misc".Translate(), false))
            {
                Find.WindowStack.Add(new QuestEditor_PawnDataMisc(this));
            }
            y += 35f;
            this.DrawInventory(ref y, x);
            y += 5f;
            this.DrawCanSaveWarning(ref y, x, inRect);
        }
        public virtual bool CanSaveToMap()
        {
            return this.kind != null && this.count.max >= 1;
        }

        protected void DrawCanSaveWarning(ref float y, float x, Rect inRect)
        {
            if (this.CanSaveToMap())
            {
                return;
            }
            Rect rect = new Rect(20f + x, y, Mathf.Max(360f, inRect.width - x - 60f), 44f);
            Widgets.Label(rect, "PawnDataCannotSaveToMapWarning".Translate().Colorize(Color.red));
            TooltipHandler.TipRegion(rect, "PawnDataCannotSaveToMapWarning".Translate());
            y += 50f;
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
            TooltipHandler.TipRegion(rect, "Copy".Translate());
            Rect tipRect = new Rect(rect.xMax + 5f, y + 2.5f, 25f, 25f);
            Widgets.ButtonImage(tipRect, CQFEditorTools.TipIcon);
            string tipKey = this.GetType().Name + "_Tip";
            if (tipKey.CanTranslate())
            {
                TooltipHandler.TipRegion(tipRect, tipKey.Translate());
            }
            y += 50f;
            if (Widgets.ButtonText(new Rect(16f + x, y, 150f, 25f), "Rename".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameForQE((name) => this.dataName = name));
            }
            y += 40f;
        }

        public virtual void DrawKind(float x, ref float y)
        {
            if (Widgets.ButtonText(new Rect(20f + x, y, 250f, 25f), "QE_PawnKind".Translate(this.kind?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(new TextSelectDrawer<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefs.ToList(), k => k.label, (k) => this.kind = k, null, null, null, null, null, null), "Select".Translate()));
            }
            y += 30f;
        }
        public void DrawInventory(ref float y, float x = 0f)
        {
            Widgets.Label(new Rect(16f + x, y, 150f, 25f), "InventoryThing".Translate());
            CQFEditorTools.DrawButtonForList_UseIcon(y, this.inventoryThings, t2 => t2.thing.label + "x" + t2.count,
() => Find.WindowStack.Add(new Dialog_Select<ThingDef>(new TextureSelectDrawer<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading.FindAll((c) => c.category == ThingCategory.Item && !c.IsCorpse), c => c.uiIcon, c => c.label, (d) => this.inventoryThings.Add(new CQFThingDefCount() { thing = d }), null, null, null, null, null, null, null), "Select".Translate())), 340f, 25f, 40f);
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
() => Find.WindowStack.Add(new Dialog_Select<ThingCategoryDef>(new TextureSelectDrawer<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading.FindAll((c) => c.defName != "Corpses" && !c.Parents.Contains(ThingCategoryDefOf.Corpses) && c != ThingCategoryDefOf.Animals), c => c.icon, c => c.label, (d) => this.inventoryCategorys.Add(new CQFThingCategoryCount() { category = d }), null, null, null, null, null, null, null), "Select".Translate())), 340f, 25f, 40f);
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
                    GameComponent_Editor.Instance.GetQuestData(quest)?.AddGroup(this.dataName, ps);
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
                            if (lord.LordJob is LordJob_ComplexCustom complexJob)
                            {
                                complexJob.ApplyDefaultDutyMap(pawn, quest);
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
                GameComponent_Editor.Instance.AddDialog(pawn, this.dialogManager);
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
}



