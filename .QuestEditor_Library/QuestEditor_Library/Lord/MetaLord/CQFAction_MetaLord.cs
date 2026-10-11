using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class CQFAction_MetaLord : CQFAction_Target
    {
        public CQFAction_MetaLord()
        {
            this.targetsText = new List<string>
            {
                "Pawn"
            };
        }

        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Dictionary<string, TargetInfo> selected = GameTools.GetTargets(targets, quest, this.targetsText);
            TargetInfo mapTarget = GameTools.GetTarget(targets, quest, this.mapKey);
            if (!this.mapKey.NullOrEmpty() && mapTarget.Map == null)
                throw new InvalidOperationException("[CQF] MetaLord action mapKey has no map target: " + this.mapKey);
            if (mapTarget.Map != null)
                selected["CQF_MetaLordMap"] = mapTarget;
            this.RealWork(selected, quest);
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Map map = targets.TryGetValue("CQF_MetaLordMap", out TargetInfo mapTarget) ? mapTarget.Map : targets.Values.Select(t => t.Map).FirstOrDefault(m => m != null);
            if (map == null)
                throw new InvalidOperationException("[CQF] MetaLord action has no map target: " + this.mapKey);
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

            if (owner == null)
                throw new InvalidOperationException("[CQF] MetaLord action cannot find organization: " + this.organizationName);
            switch (this.operation)
            {
                case MetaLordOperation.Join:
                    foreach (Pawn pawn in pawns)
                        owner.AddMember(pawn, this.groupName);
                    break;
                case MetaLordOperation.Leave:
                    foreach (Pawn pawn in pawns)
                        owner.RemoveMember(pawn, "Action");
                    break;
                case MetaLordOperation.Move:
                    foreach (Pawn pawn in pawns)
                        owner.RequestMove(pawn, map, this.groupName, this.timeoutTicks);
                    break;
                case MetaLordOperation.CancelMove:
                    foreach (Pawn pawn in pawns)
                        owner.CancelMove(pawn);
                    break;
                case MetaLordOperation.RemoveGroup:
                    Lord group = owner.Levels.FirstOrDefault(l => l.level == map)?.Lords.FirstOrDefault(l => (l.LordJob as MultLevelLordJobBase)?.groupName == this.groupName);
                    if (group == null)
                        throw new InvalidOperationException("[CQF] MetaLord group not found: " + this.groupName);
                    owner.RemoveGroup(group);
                    break;
                case MetaLordOperation.Signal:
                    if (this.signalTag.NullOrEmpty())
                        throw new InvalidOperationException("[CQF] MetaLord signal action requires signalTag.");
                    owner.Trigger(new LordEventSignal(LordEventSignalType.External) { tag = this.signalTag, pawn = pawns.FirstOrDefault(), targetMap = map });
                    break;
                case MetaLordOperation.Disband:
                    manager.Remove(owner);
                    break;
                default:
                    throw new InvalidOperationException("[CQF] Unsupported MetaLord action: " + this.operation);
            }

            if ((this.operation == MetaLordOperation.Join || this.operation == MetaLordOperation.Leave || this.operation == MetaLordOperation.Move || this.operation == MetaLordOperation.CancelMove) && pawns.Length == 0)
                throw new InvalidOperationException("[CQF] MetaLord member action has no pawn targets.");
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            if (CQFUIStyle.ButtonText(new Rect(x, y, inRect.width - x - 12f, 30f), ("CQF_MetaLordOperation_" + this.operation).Translate()))
                CQFEditorTools.DrawFloatMenu(Enum.GetValues(typeof(MetaLordOperation)).Cast<MetaLordOperation>().ToList(), value => this.operation = value, value => ("CQF_MetaLordOperation_" + value).Translate());
            y += 36f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordName".Translate(), ref this.organizationName, x, 180f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordMapKey".Translate(), ref this.mapKey, x, 180f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordGroup".Translate(), ref this.groupName, x, 180f);
            y += 30f;
            if (this.operation == MetaLordOperation.Move)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordTimeout".Translate(), ref this.timeoutTicks, ref this.timeoutBuffer, x, 100f);
                y += 30f;
            }

            if (this.operation == MetaLordOperation.Signal)
            {
                CQFSignalEditor.DrawBookField(y, "CQF_MetaLordSignal".Translate(), this.signalTag, value => this.signalTag = value, x, inRect.width - x - 12f);
                y += 30f;
            }

            if (this.operation != MetaLordOperation.CreateGroup)
                return;
            CQFEditorTools.DrawFactionSelectableText(y, "PawnDataFaction".Translate(), ref this.faction, value => this.faction = value, x, 180f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordCapacity".Translate(), ref this.capacity, ref this.capacityBuffer, x, 100f);
            y += 30f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, inRect.width - x - 12f, 30f), "CQF_MetaLordEvents".Translate() + ": " + (this.eventDef?.defName ?? "Null")))
                CQFEditorTools.DrawFloatMenu(DefDatabase<MetaLordEventDef>.AllDefsListForReading, value => this.eventDef = value, value => value.defName);
            y += 36f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, inRect.width - x - 12f, 30f), "CQF_LordData_DutyMap".Translate(this.dutyMap?.defName ?? "Null")))
                CQFEditorTools.DrawFloatMenu(DefDatabase<DutyMapDef>.AllDefsListForReading, value => this.dutyMap = value, value => value.defName);
            y += 36f;
            CQFEditorTools.DrawEditableStringList(this.groupTags, ref y, "CQF_MetaLordTags".Translate(), x: x, width: 180f);
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("operation", this.operation), new XElement("organizationName", this.organizationName ?? ""), new XElement("mapKey", this.mapKey ?? ""), new XElement("groupName", this.groupName ?? ""), new XElement("signalTag", this.signalTag ?? ""), new XElement("faction", this.faction ?? ""), new XElement("capacity", this.capacity), new XElement("timeoutTicks", this.timeoutTicks));
            if (this.eventDef != null)
                result.Add(new XElement("eventDef", this.eventDef.defName));
            if (this.dutyMap != null)
                result.Add(new XElement("dutyMap", this.dutyMap.defName));
            if (this.groupTags.Count > 0)
                result.Add(CQFSerialization.SaveList(this.groupTags, "groupTags"));
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
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                this.groupTags ??= new List<string>();
        }

        public MetaLordOperation operation;
        [NoTranslate]
        public string organizationName;
        [NoTranslate]
        public string mapKey = "TargetMap";
        [NoTranslate]
        public string groupName;
        [NoTranslate]
        public string signalTag;
        [NoTranslate]
        public string faction = "MapFaction";
        public int capacity;
        public int timeoutTicks = 60000;
        public MetaLordEventDef eventDef;
        public DutyMapDef dutyMap;
        public List<string> groupTags = new List<string>();
        public string capacityBuffer;
        public string timeoutBuffer;
    }
}
