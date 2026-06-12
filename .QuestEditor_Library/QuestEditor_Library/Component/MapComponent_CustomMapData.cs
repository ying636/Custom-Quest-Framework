using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class MapComponent_CustomMapData : MapComponent
    {
        public MapComponent_CustomMapData(Map map) : base(map)
        {

        }
        public List<MapParent_Custom> Submaps 
        {
            get 
            {
                if (this.subMaps == null) 
                {
                    this.subMaps = new List<MapParent_Custom>();
                }
                return this.subMaps;
            }
        }
        public Dictionary<Thing, List<InteractionOperation>> ExtraOperations
        {
            get
            {
                if (this.extraOperations == null)
                {
                    this.extraOperations = new Dictionary<Thing, List<InteractionOperation>>();
                }
                return this.extraOperations;
            }
        }
        public Dictionary<Building, List<PawnSpawnData>> PawnSpawnDatas_Building
        {
            get 
            {
                if (this.pawnSpawnDatas_Building == null) 
                {
                    this.pawnSpawnDatas_Building = new Dictionary<Building, List<PawnSpawnData>>();
                }
                return this.pawnSpawnDatas_Building;
            }
        }
        public List<LordWithName> Lords 
        {
            get 
            {
                if (this.customLords == null) 
                {
                    this.customLords = new List<LordWithName>();
                }
                return this.customLords;
            }
        }
        public List<ThingActionTrigger> Triggers 
        {
            get
            {
                if (this.triggers == null)
                {
                    this.triggers = new List<ThingActionTrigger>();
                }
                return this.triggers;
            }
        }
        public List<CQFEventArea> EventAreas
        {
            get
            {
                if (this.eventAreas == null)
                {
                    this.eventAreas = new List<CQFEventArea>();
                }
                return this.eventAreas;
            }
        }
        public Dictionary<string, IntVec3> StartCells
        {
            get
            {
                if (this.startCells == null)
                {
                    this.startCells = new Dictionary<string, IntVec3>();
                }
                return this.startCells;
            }
        }
        public string QuestTag => this.questTag.NullOrEmpty() ? null : this.questTag;
        public bool TryGetLord(string name, out Lord lord)
        {
            LordWithName l = null;
            bool result = !name.NullOrEmpty() && this.Lords.Find(l2 => l2.name == name) != null;
            l = this.Lords.Find(l2 => l2.name == name);
            lord = l?.lord;
            return result;
        }
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!this.init)
            {
                this.init = true;
                this.map?.spawnedThings?.ToList().ListFullCopy().ForEach(t =>
                {
                    if (t is CustomMapEntrance entrance && entrance.CustomMap?.Parent is MapParent_Custom parent)
                    {
                        this.AddSubMap(parent);
                    }
                });
                this.RebuildEventAreaPawnCache();
            }
            this.pawnSpawnDatas_Tick?.ForEach(data =>
            {
                data.time++;
                if (data.time > data.data.timeToSpawn)
                {
                    data.time = 0;
                    int count = data.data.count.RandomInRange;
                    for (int i = 0; i < count; i++)
                    {
                        if (this.TryGetLord(data.data.lordDataName, out Lord lord))
                        {
                            data.data.Spawn(data.position, this.map, QuestTag, Find.QuestManager.QuestsListForReading.Find(q => "Quest" + q.id == this.QuestTag), lord);
                        }
                        else
                        {
                            data.data.Spawn(data.position, this.map, QuestTag, Find.QuestManager.QuestsListForReading.Find(q => "Quest" + q.id == this.QuestTag),null,false);
                        }
                    }
                }
            });
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                this.CheckEventAreas();
            }
        }
        public void AddOrReplaceEventArea(CQFEventArea area, bool replaceExisting = true)
        {
            if (area == null || area.key.NullOrEmpty())
            {
                return;
            }
            if (replaceExisting)
            {
                this.EventAreas.RemoveAll(a => a.key == area.key);
            }
            CQFEventArea oldArea = this.EventAreas.Find(a => a.key == area.key);
            if (oldArea != null)
            {
                oldArea.Merge(area);
                oldArea.InitializeRuntime(this.map);
                return;
            }
            area.InitializeRuntime(this.map);
            this.EventAreas.Add(area);
        }
        public CQFEventArea GetEventArea(string key)
        {
            if (key.NullOrEmpty())
            {
                return null;
            }
            return this.EventAreas.Find(a => a.key == key);
        }
        public void RemoveEventArea(string key)
        {
            if (key.NullOrEmpty())
            {
                return;
            }
            this.EventAreas.RemoveAll(a => a.key == key);
        }
        public void Notify_PawnSpawned(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }
            foreach (CQFEventArea area in this.EventAreas)
            {
                area.Notify_PawnSpawned(pawn, this.map);
            }
        }
        public void Notify_PawnDespawned(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }
            foreach (CQFEventArea area in this.EventAreas)
            {
                area.Notify_PawnDespawned(pawn);
            }
        }
        public void Notify_ThingDamaged(Thing thing, DamageInfo dinfo) 
        {
            if (thing is Building building &&
                this.PawnSpawnDatas_Building.TryGetValue(building,
                out List<PawnSpawnData> list))
            {
                foreach (PawnSpawnData data in list)
                {
                    if (data is PawnSpawnData pawnData && pawnData.spawnType == SpawnType.BuildingDamaged)
                    {
                        if (this.TryGetLord(pawnData.lordDataName, out Lord lord))
                        {
                            data.Spawn(building.Position, building.Map, this.QuestTag, Find.QuestManager.QuestsListForReading.Find(q => "Quest" + q.id == this.QuestTag), lord);
                        }
                        else
                        {
                            data.Spawn(building.Position, building.Map, this.QuestTag, Find.QuestManager.QuestsListForReading.Find(q => "Quest" + q.id == this.QuestTag));
                        }
                    }
                }
            }
            foreach (ThingActionTrigger trigger in this.Triggers.Where(t => t.things.Contains(thing) && t.mode == ActionTriggerMode.Damaged).ToList())
            {
                trigger.Trigger(new Dictionary<string, TargetInfo>() 
                {
                    ["CustomThing"] = thing,
                    ["Trigger"] = dinfo.Instigator
                },GameTools.GetQuestFromThing(thing));
            }
        }
        public void AddSubMap(MapParent_Custom sub)
        {
            if (!this.Submaps.Contains(sub)) 
            {
                this.Submaps.Add(sub);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving) 
            {
                this.PawnSpawnDatas_Building.RemoveAll(b => b.Key == null || b.Key.Destroyed);
            } 
            Scribe_Values.Look(ref this.questTag,"questTag");
            Scribe_Collections.Look(ref this.subMaps, "QE_MapComponent_CustomMapData_subMaps",LookMode.Reference);
            Scribe_Collections.Look(ref this.customLords, "QE_MapComponent_CustomMapData_customLords", LookMode.Deep);
            Scribe_Collections.Look(ref this.designatedAndMap, "CQF_MapComponent_designatedAndMap", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref this.route, "QE_MapComponent_Editor_route", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref this.extraOperations, "QE_MapComponent_Editor_extraOperations", LookMode.Reference, LookMode.Deep,ref this.extraOperations_Thing,ref this.extraOperations_Operation);

            Scribe_Collections.Look(ref this.pawnSpawnDatas_Building, "QE_LordJob_DefendAndPatrol_pawnSpawnDatas_Building", LookMode.Reference, LookMode.Deep, ref this.tmpBuildings, ref this.tmpPawnDatas);
            Scribe_Collections.Look(ref this.pawnSpawnDatas_Tick, "QE_LordJob_DefendAndPatrol_pawnSpawnDatas_Tick", LookMode.Deep);
            Scribe_Collections.Look(ref this.triggers, "triggers",LookMode.Deep);
            Scribe_Collections.Look(ref this.eventAreas, "eventAreas", LookMode.Deep);
            Scribe_Collections.Look(ref this.startCells, "startCells", LookMode.Value,LookMode.Value);
            Scribe_Deep.Look(ref this.background, "background");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EventAreas.RemoveAll(a => a == null || a.key.NullOrEmpty());
            }
        }
        private void CheckEventAreas()
        {
            if (!this.EventAreas.Any())
            {
                return;
            }
            List<CQFEventArea> triggeredAreas = new List<CQFEventArea>();
            Quest quest = GameTools.GetQuestFromMap(this.map);
            foreach (CQFEventArea area in this.EventAreas)
            {
                if (area.TryTrigger(this.map, quest))
                {
                    triggeredAreas.Add(area);
                }
            }
            foreach (CQFEventArea area in triggeredAreas)
            {
                this.EventAreas.Remove(area);
            }
        }
        private void RebuildEventAreaPawnCache()
        {
            foreach (CQFEventArea area in this.EventAreas)
            {
                area.InitializeRuntime(this.map);
            }
        }
 
        public bool init = false;
        public List<MapParent_Custom> subMaps = new List<MapParent_Custom>();
        public Dictionary<Thing, Thing> designatedAndMap = new Dictionary<Thing, Thing>();

        List<ThingActionTrigger> triggers = new List<ThingActionTrigger>();
        List<CQFEventArea> eventAreas = new List<CQFEventArea>();

        public Dictionary<Building, List<PawnSpawnData>> pawnSpawnDatas_Building = new Dictionary<Building, List<PawnSpawnData>>();
        public List<Building> tmpBuildings = new List<Building>();
        public List<List<PawnSpawnData>> tmpPawnDatas = new List<List<PawnSpawnData>>();
        public List<PawnDataWithPosAndTime> pawnSpawnDatas_Tick = new List<PawnDataWithPosAndTime>();

        private List<LordWithName> customLords = new List<LordWithName>();

        private Dictionary<Thing, List<InteractionOperation>> extraOperations = new Dictionary<Thing, List<InteractionOperation>>();
        private List<Thing> extraOperations_Thing = new List<Thing>();
        private List<List<InteractionOperation>> extraOperations_Operation = new List<List<InteractionOperation>>();

        Dictionary<string,IntVec3> startCells = new Dictionary<string,IntVec3>();

        public Dictionary<string, Route> route = new Dictionary<string, Route>();

        public string questTag = "";

        public CustomMapBackgroundData background;



        public static MapComponent_CustomMapData GetComp(Map map)
        {
            return map.GetComponent<MapComponent_CustomMapData>();
        }
    }
    public class LordWithName : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.name, "LordWithName_name");
            Scribe_Deep.Look(ref this.data, "LordWithName_data");
            Scribe_References.Look(ref this.lord, "LordWithName_lord");
        }

        public string name = "default";
        public Lord lord;
        public LordData data = new LordData();
    }

    public class ThingActionTrigger : IExposable
    {
        public void Trigger(Dictionary<string,TargetInfo> targets,Quest quset) 
        {
            foreach (var action in actions)
            {
                action.Work(targets,quset);
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.key,"key");
            Scribe_Values.Look(ref this.mode, "mode");
            Scribe_Collections.Look(ref this.things, "things",LookMode.Reference);
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public string key;
        public List<Thing> things = new List<Thing>();
        public List<CQFAction> actions = new List<CQFAction>();
        public ActionTriggerMode mode;
    }
    public class CQFEventArea : IExposable
    {
        public void AddCell(IntVec3 cell)
        {
            if (!cell.IsValid)
            {
                return;
            }
            if (!this.Cells.Contains(cell))
            {
                this.Cells.Add(cell);
            }
            this.cellSet?.Add(cell);
        }
        public void InitializeRuntime(Map map)
        {
            this.cellSet = new HashSet<IntVec3>(this.Cells);
            this.pawns = new HashSet<Pawn>();
            if (map == null)
            {
                return;
            }
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                this.Notify_PawnSpawned(pawn, map);
            }
        }
        public void Merge(CQFEventArea area)
        {
            foreach (IntVec3 cell in area.Cells)
            {
                this.AddCell(cell);
            }
            if (!area.faction.NullOrEmpty())
            {
                this.faction = area.faction;
            }
            this.onlyHumanlike = area.onlyHumanlike;
            this.actions = area.actions ?? new List<CQFAction>();
        }
        public void Notify_PawnSpawned(Pawn pawn, Map map)
        {
            this.EnsureRuntime(map);
            if (this.PawnMatches(pawn, map))
            {
                this.pawns.Add(pawn);
            }
            else
            {
                this.pawns.Remove(pawn);
            }
        }
        public void Notify_PawnDespawned(Pawn pawn)
        {
            this.pawns?.Remove(pawn);
        }
        public bool TryTrigger(Map map, Quest quest)
        {
            this.EnsureRuntime(map);
            foreach (Pawn pawn in this.pawns.ToList())
            {
                if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map != map)
                {
                    this.pawns.Remove(pawn);
                    continue;
                }
                if (this.cellSet.Contains(pawn.Position))
                {
                    this.Trigger(pawn, map, quest);
                    return true;
                }
            }
            return false;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.key, "key");
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Values.Look(ref this.onlyHumanlike, "onlyHumanlike");
            Scribe_Collections.Look(ref this.cells, "cells", LookMode.Value);
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }
        private void EnsureRuntime(Map map)
        {
            if (this.cellSet == null || this.pawns == null)
            {
                this.InitializeRuntime(map);
            }
        }
        private bool PawnMatches(Pawn pawn, Map map)
        {
            if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map != map)
            {
                return false;
            }
            if (this.onlyHumanlike && !pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (this.faction.NullOrEmpty() || this.faction == "Any")
            {
                return true;
            }
            if (this.faction == "Player")
            {
                return pawn.Faction == Faction.OfPlayer;
            }
            if (this.faction == "Hostile" || this.faction == "RandomHostile")
            {
                return pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
            }
            if (this.faction == "Ally" || this.faction == "RandomAlly")
            {
                return pawn.Faction != null && !pawn.Faction.IsPlayer && pawn.Faction.PlayerRelationKind == FactionRelationKind.Ally;
            }
            if (this.faction == "Neutral" || this.faction == "RandomNeutral")
            {
                return pawn.Faction != null && !pawn.Faction.IsPlayer && pawn.Faction.PlayerRelationKind == FactionRelationKind.Neutral;
            }
            if (this.faction == "MapFaction")
            {
                return pawn.Faction != null && map?.Parent?.Faction != null && pawn.Faction == map.Parent.Faction;
            }
            return pawn.Faction != null && pawn.Faction.def.defName == this.faction;
        }
        private void Trigger(Pawn pawn, Map map, Quest quest)
        {
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>()
            {
                ["Trigger"] = pawn,
                ["Position"] = new TargetInfo(pawn.Position, map),
                ["Map"] = new TargetInfo(pawn.Position, map)
            };
            foreach (CQFAction action in this.actions)
            {
                action.Work(targets, quest);
            }
        }

        public List<IntVec3> Cells
        {
            get
            {
                if (this.cells == null)
                {
                    this.cells = new List<IntVec3>();
                }
                return this.cells;
            }
        }

        public string key;
        public string faction;
        public bool onlyHumanlike;
        public List<IntVec3> cells = new List<IntVec3>();
        public List<CQFAction> actions = new List<CQFAction>();

        private HashSet<IntVec3> cellSet;
        private HashSet<Pawn> pawns;
    }
}

