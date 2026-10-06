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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual bool CanSaveToMap()
        {
            return this.kind != null && this.count.max >= 1;
        }

        protected internal void DrawCanSaveWarning(ref float y, float x, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData.DrawCanSaveWarning(Ref:float,None:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        public void DrawName(ref float y, float x, Rect nameRect)

        {
            object[] arguments = new object[]
            {
                y,
                x,
                nameRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData.DrawName(Ref:float,None:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual void DrawKind(float x, ref float y)

        {
            object[] arguments = new object[]
            {
                x,
                y
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData.DrawKind(None:float,Ref:float)", this, arguments);
            y = (float)arguments[1];
        }
        public void DrawInventory(ref float y, float x = 0f)

        {
            object[] arguments = new object[]
            {
                y,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnSpawnData.DrawInventory(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
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
