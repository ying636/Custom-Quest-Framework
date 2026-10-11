using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class LordData : ISaveable, IDrawable, IExposable
    {
        public LordJobData Data
        {
            get
            {
                if (this.lordJobData == null)
                {
                    this.lordJobData = new LordJobData()
                    {
                        lordData = this
                    };
                }

                this.lordJobData.lordData = this;
                return lordJobData;
            }
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("name", this.name));
            result.Add(this.Data.SaveToXElement("lordJobData"));
            result.Add(new XElement("faction", this.faction));
            if (this.actions.Any())
            {
                result.Add(CQFSerialization.SaveList_Saveable(this.actions, "actions"));
            }

            if (!this.metaLordName.NullOrEmpty())
                result.Add(new XElement("metaLordName", this.metaLordName));
            if (this.metaLordEvents != null)
                result.Add(new XElement("metaLordEvents", this.metaLordEvents.defName));
            if (this.groupTags.Count > 0)
                result.Add(CQFSerialization.SaveList(this.groupTags, "groupTags"));
            if (this.capacity > 0)
                result.Add(new XElement("capacity", this.capacity));
            return result;
        }

        public void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawLabelAndText_Line(y, "LordName".Translate(), ref this.name, x, Mathf.Max(80f, inRect.width - x - 180f));
            y += 30f;
            this.Data.Draw(ref y, inRect, x);
            if (typeof(MultLevelLordJobBase).IsAssignableFrom(this.Data.LordJob))
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordName".Translate(), ref this.metaLordName, x, 180f);
                y += 30f;
                CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MetaLordCapacity".Translate(), ref this.capacity, ref this.capacityBuffer, x, 100f);
                y += 30f;
                if (CQFUIStyle.ButtonText(new Rect(x, y, inRect.width - x - 12f, 30f), "CQF_MetaLordEvents".Translate() + ": " + (this.metaLordEvents?.defName ?? "Null")))
                    CQFEditorTools.DrawFloatMenu(DefDatabase<MetaLordEventDef>.AllDefsListForReading, value => this.metaLordEvents = value, value => value.defName);
                y += 36f;
                CQFEditorTools.DrawEditableStringList(this.groupTags, ref y, "CQF_MetaLordTags".Translate(), x: x, width: 180f);
            }

            CQFEditorTools.DrawSelectableText(y, "MapDataFaction".Translate(), ref this.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => this.faction = f.defName, (f) => f.label, new List<FloatMenuOption>() { new FloatMenuOption("RandomHostile".Translate(), () => this.faction = "RandomHostile"), new FloatMenuOption("RandomAlly".Translate(), () => this.faction = "RandomAlly"), new FloatMenuOption("RandomNeutral".Translate(), () => this.faction = "RandomNeutral"), new FloatMenuOption("PawnDataMapFaction".Translate(), () => this.faction = "MapFaction") }), x, Mathf.Max(80f, inRect.width - x - 180f));
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow_UseIcon(ref y, x, this.actions, inRect, "ActionsAfterGeneration".Translate(), a => a.GetType().Name.Translate());
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.name, "name");
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Deep.Look(ref this.lordJobData, "lordJobData");
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
            Scribe_Values.Look(ref this.metaLordName, "metaLordName");
            Scribe_Defs.Look(ref this.metaLordEvents, "metaLordEvents");
            Scribe_Collections.Look(ref this.groupTags, "groupTags", LookMode.Value);
            Scribe_Values.Look(ref this.capacity, "capacity");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                this.groupTags ??= new List<string>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.lordJobData != null)
            {
                this.lordJobData.lordData = this;
            }
        }

        public string name = "default";
        public string faction;
        public LordJobData lordJobData;
        public List<CQFAction_Lord> actions = new List<CQFAction_Lord>();
        [NoTranslate]
        public string metaLordName;
        public MetaLordEventDef metaLordEvents;
        public int capacity;
        public List<string> groupTags = new List<string>();
        public string capacityBuffer;
    }

    public class LordJobData : ISaveable, IDrawable, IExposable
    {
        public virtual bool JobSelectable => true;
        public virtual Type LordJob => this.lordJob;

        public virtual LordJob CreateJob(Map map, Quest quest)
        {
            LordJob result = (LordJob)Activator.CreateInstance(this.lordJob);
            if (result is LordJob_ComplexCustom complexJob)
            {
                complexJob.defaultDutyMap = this.dutyMap;
                complexJob.defaultStartNodeId = this.dutyMapStartNodeId;
            }

            if (result is MultLevelLordJobBase multiJob)
            {
                if (this.lordData == null || this.lordData.metaLordName.NullOrEmpty())
                    throw new InvalidOperationException("[CQF] Multi-level LordData requires metaLordName.");
                multiJob.metaLord = map.GetComponent<MapComponent_CQFLord>().Manager.GetOrCreate(this.lordData.metaLordName, map, GameTools.GetFaction(this.lordData.faction, map), this.lordData.metaLordEvents, quest);
                multiJob.groupName = this.lordData.name;
                multiJob.capacity = this.lordData.capacity;
                multiJob.groupTags = new List<string>(this.lordData.groupTags);
            }

            return result;
        }

        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            this.DrawName(ref y, inRect, x);
            y += 38f;
            if (this.JobSelectable)
            {
                if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(80f, inRect.width - x - 12f), 32f), "CQF_LordJob".Translate(this.lordJob.Name.CanTranslate() ? this.lordJob.Name.Translate().ToString() : this.lordJob.Name), false, overrideTextAnchor: TextAnchor.MiddleLeft))
                {
                    Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(typeof(LordJob).AllSubclassesNonAbstract(), t => t.Name.CanTranslate() ? t.Name.Translate().ToString() : t.Name, t => this.lordJob = t, null, t => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : ""), "Select".Translate()));
                }
            }
            else
            {
                Widgets.Label(new Rect(x, y, Mathf.Max(80f, inRect.width - x - 12f), 32f), "CQF_LordJob".Translate(this.LordJob.Name.CanTranslate() ? this.LordJob.Name.Translate().ToString() : this.LordJob.Name));
            }

            y += 38f;
            if (typeof(LordJob_ComplexCustom).IsAssignableFrom(this.lordJob))
            {
                this.DrawComplexDutyMap(ref y, x);
            }
        }

        public virtual void DrawName(ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(x, y, Mathf.Max(80f, inRect.width - x - 12f), 32f);
            if (CQFUIStyle.ButtonText(rect, this.GetType().Name.Translate(), false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                List<Type> types = typeof(LordJobData).AllSubclassesNonAbstract().ListFullCopy();
                types.Add(typeof(LordJobData));
                Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(types, t => t.Name.CanTranslate() ? t.Name.Translate().ToString() : t.Name, t =>
                {
                    this.lordData.lordJobData = (LordJobData)Activator.CreateInstance(t);
                    this.lordData.lordJobData.lordData = this.lordData;
                }, null, t => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : ""), "Select".Translate()));
            }

            if ((this.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (this.GetType().Name + "_Tip").Translate());
            }
        }

        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            if (this.lordJob != typeof(LordJob_Custom))
            {
                result.Add(new XElement("lordJob", this.lordJob.FullName));
            }

            if (this.dutyMap != null)
            {
                result.Add(new XElement("dutyMap", this.dutyMap.defName));
            }

            if (!this.dutyMapStartNodeId.NullOrEmpty())
            {
                result.Add(new XElement("dutyMapStartNodeId", this.dutyMapStartNodeId));
            }

            return result;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref this.lordJob, "lordJob", typeof(LordJob_Custom), true);
            Scribe_Defs.Look(ref this.dutyMap, "dutyMap");
            Scribe_Values.Look(ref this.dutyMapStartNodeId, "dutyMapStartNodeId");
        }

        internal void DrawComplexDutyMap(ref float y, float x)
        {
            if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(80f, CQFUIScope.ContentWidth - x - 12f), 32f), "CQF_LordData_DutyMap".Translate(this.dutyMap?.defName ?? "Null"), false, overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                Find.WindowStack.Add(new Dialog_Select<DutyMapDef>(new TextSelectDrawer<DutyMapDef>(DefDatabase<DutyMapDef>.AllDefsListForReading, d => d.defName, d => this.dutyMap = d, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 38f;
            if (this.dutyMap != null && this.dutyMap.nodes.Any())
            {
                CQFEditorTools.DrawSelectableText(y, "CQF_LordData_DutyMapStartNode".Translate(), ref this.dutyMapStartNodeId, () => CQFEditorTools.DrawFloatMenu(this.dutyMap.nodes, node => this.dutyMapStartNodeId = node.nodeId, node => node.nodeId), x, 180f);
                y += 38f;
            }
        }

        public Type lordJob = typeof(LordJob_Custom);
        public LordData lordData;
        public DutyMapDef dutyMap;
        public string dutyMapStartNodeId;
    }

    public class LordJobData_DefendBase : LordJobData
    {
        public override bool JobSelectable => false;
        public override Type LordJob => typeof(LordJob_DefendBase);

        public override LordJob CreateJob(Map map, Quest quest)
        {
            return new LordJob_DefendBase(GameTools.GetFaction(this.faction, map), GameTools.GetTarget(null, quest, this.targetPositionName).Cell, 10);
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "TargetPositionName".Translate(), ref this.targetPositionName, x, 150);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "PawnDataFaction".Translate(), ref this.faction, x, 150);
            y += 30f;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.targetPositionName, "targetPositionName");
            Scribe_Values.Look(ref this.faction, "faction");
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.targetPositionName != null)
            {
                result.Add(new XElement("targetPositionName", this.targetPositionName));
            }

            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction));
            }

            return result;
        }

        public string targetPositionName;
        public string faction;
    }
}
