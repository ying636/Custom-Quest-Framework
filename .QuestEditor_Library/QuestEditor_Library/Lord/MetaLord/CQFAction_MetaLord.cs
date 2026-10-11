using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class CQFAction_MetaLord : CQFAction_Target
    {
        public CQFAction_MetaLord()
        {
            this.targetsText = new List<string> { "Pawn" };
        }

        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Dictionary<string, TargetInfo> selected = GameTools.GetTargets(targets, quest, this.targetsText);
            TargetInfo mapTarget = GameTools.GetTarget(targets, quest, this.mapKey);
            if (!this.mapKey.NullOrEmpty() && mapTarget.Map == null)
                throw new InvalidOperationException("[CQF] MetaLord action mapKey has no map target: " + this.mapKey);
            if (mapTarget.Map != null) selected["CQF_MetaLordMap"] = mapTarget;
            this.RealWork(selected, quest);
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Map map = targets.TryGetValue("CQF_MetaLordMap", out TargetInfo mapTarget) ? mapTarget.Map :
                targets.Values.Select(t => t.Map).FirstOrDefault(m => m != null);
            if (map == null) throw new InvalidOperationException("[CQF] MetaLord action has no map target: " + this.mapKey);
            MetaLordManager manager = map.GetComponent<MapComponent_CQFLord>().Manager;
            MetaLord owner = manager.Get(this.organizationName, map);
            Pawn[] pawns = targets.Values.Select(t => t.Thing).OfType<Pawn>().Distinct().ToArray();
            if (this.operation == MetaLordOperation.CreateGroup)
            {
                Faction resolved = pawns.FirstOrDefault()?.Faction ?? GameTools.GetFaction(this.faction, map);
                owner = manager.GetOrCreate(this.organizationName, map, resolved, this.eventDef, quest);
                owner.CreateGroup(map, this.groupName, this.dutyMap, this.capacity, this.groupTags);
                return;
            }
            if (owner == null) throw new InvalidOperationException("[CQF] MetaLord action cannot find organization: " + this.organizationName);
            switch (this.operation)
            {
                case MetaLordOperation.Join:
                    foreach (Pawn pawn in pawns) owner.AddMember(pawn, this.groupName);
                    break;
                case MetaLordOperation.Leave:
                    foreach (Pawn pawn in pawns) owner.RemoveMember(pawn, "Action");
                    break;
                case MetaLordOperation.Move:
                    foreach (Pawn pawn in pawns) owner.RequestMove(pawn, map, this.groupName, this.timeoutTicks);
                    break;
                case MetaLordOperation.CancelMove:
                    foreach (Pawn pawn in pawns) owner.CancelMove(pawn);
                    break;
                case MetaLordOperation.RemoveGroup:
                    Lord group = owner.Levels.FirstOrDefault(l => l.level == map)?.Lords
                        .FirstOrDefault(l => (l.LordJob as MultLevelLordJobBase)?.groupName == this.groupName);
                    if (group == null) throw new InvalidOperationException("[CQF] MetaLord group not found: " + this.groupName);
                    owner.RemoveGroup(group);
                    break;
                case MetaLordOperation.Signal:
                    if (this.signalTag.NullOrEmpty()) throw new InvalidOperationException("[CQF] MetaLord signal action requires signalTag.");
                    owner.Trigger(new LordEventSignal(LordEventSignalType.External)
                    { tag = this.signalTag, pawn = pawns.FirstOrDefault(), targetMap = map });
                    break;
                case MetaLordOperation.Disband:
                    manager.Remove(owner);
                    break;
                default:
                    throw new InvalidOperationException("[CQF] Unsupported MetaLord action: " + this.operation);
            }
            if ((this.operation == MetaLordOperation.Join || this.operation == MetaLordOperation.Leave ||
                this.operation == MetaLordOperation.Move || this.operation == MetaLordOperation.CancelMove) && pawns.Length == 0)
                throw new InvalidOperationException("[CQF] MetaLord member action has no pawn targets.");
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            object[] arguments = { y, inRect, x };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_MetaLord.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("operation", this.operation), new XElement("organizationName", this.organizationName ?? ""),
                new XElement("mapKey", this.mapKey ?? ""), new XElement("groupName", this.groupName ?? ""),
                new XElement("signalTag", this.signalTag ?? ""), new XElement("faction", this.faction ?? ""),
                new XElement("capacity", this.capacity), new XElement("timeoutTicks", this.timeoutTicks));
            if (this.eventDef != null) result.Add(new XElement("eventDef", this.eventDef.defName));
            if (this.dutyMap != null) result.Add(new XElement("dutyMap", this.dutyMap.defName));
            if (this.groupTags.Count > 0) result.Add(CQFSerialization.SaveList(this.groupTags, "groupTags"));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.operation, "operation");
            Scribe_Values.Look(ref this.organizationName, "organizationName");
            Scribe_Values.Look(ref this.mapKey, "mapKey", "TargetMap");
            Scribe_Values.Look(ref this.groupName, "groupName");
            Scribe_Values.Look(ref this.signalTag, "signalTag");
            Scribe_Values.Look(ref this.faction, "faction", "MapFaction");
            Scribe_Values.Look(ref this.capacity, "capacity");
            Scribe_Values.Look(ref this.timeoutTicks, "timeoutTicks", 60000);
            Scribe_Defs.Look(ref this.eventDef, "eventDef");
            Scribe_Defs.Look(ref this.dutyMap, "dutyMap");
            Scribe_Collections.Look(ref this.groupTags, "groupTags", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) this.groupTags ??= new List<string>();
        }

        public MetaLordOperation operation;
        [NoTranslate] public string organizationName;
        [NoTranslate] public string mapKey = "TargetMap";
        [NoTranslate] public string groupName;
        [NoTranslate] public string signalTag;
        [NoTranslate] public string faction = "MapFaction";
        public int capacity;
        public int timeoutTicks = 60000;
        public MetaLordEventDef eventDef;
        public DutyMapDef dutyMap;
        public List<string> groupTags = new List<string>();
        public string capacityBuffer;
        public string timeoutBuffer;
    }
}
