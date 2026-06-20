using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class DutyMapDef : Def, ISaveable
    {
        public DutyMapNode StartNode => this.nodes.Find(n => n.nodeId == this.startNodeId) ?? this.nodes.FirstOrDefault();

        public DutyMapNode GetNode(string nodeId)
        {
            return this.nodes.Find(n => n.nodeId == nodeId);
        }

        public IEnumerable<DutyMapTransition> TransitionsFrom(string nodeId)
        {
            return this.transitions.Where(t => t.fromNodeId == nodeId);
        }

        public DutyMapNode CreateNode()
        {
            DutyMapNode result = new DutyMapNode
            {
                nodeId = "Node" + this.nextNodeIndex,
                editorPosition = new Vector2(80f + this.nextNodeIndex * 35f, 120f)
            };
            this.nextNodeIndex++;
            this.nodes.Add(result);
            if (this.startNodeId.NullOrEmpty())
            {
                this.startNodeId = result.nodeId;
            }
            return result;
        }

        public DutyMapNode CreateNode(Vector2 editorPosition)
        {
            DutyMapNode result = this.CreateNode();
            result.editorPosition = editorPosition;
            return result;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", this.defName));
            if (!this.label.NullOrEmpty())
            {
                result.Add(new XElement("label", this.label));
            }
            if (!this.description.NullOrEmpty())
            {
                result.Add(new XElement("description", this.description));
            }
            result.Add(new XElement("startNodeId", this.startNodeId));
            result.Add(new XElement("nextNodeIndex", this.nextNodeIndex));
            result.Add(CQFEditorTools.SaveList_Saveable(this.nodes, "nodes"));
            result.Add(CQFEditorTools.SaveList_Saveable(this.transitions, "transitions"));
            return result;
        }

        public string startNodeId;
        public int nextNodeIndex = 1;
        public List<DutyMapNode> nodes = new List<DutyMapNode>();
        public List<DutyMapTransition> transitions = new List<DutyMapTransition>();
    }

    public class DutyMapNode : ISaveable, IExposable
    {
        public DutyMapNode()
        {
        }

        public PawnDuty MakeDuty(Pawn pawn, Quest quest)
        {
            DutyDef dutyDef = this.duty ?? DutyDefOf.Defend;
            PawnDuty result = new PawnDuty(dutyDef);
            result.focus = this.ResolveTarget(this.focusTarget, pawn, quest);
            result.focusSecond = this.ResolveTarget(this.focusSecondTarget, pawn, quest);
            result.focusThird = this.ResolveTarget(this.focusThirdTarget, pawn, quest);
            result.radius = this.radius;
            result.locomotion = this.locomotion;
            result.maxDanger = this.maxDanger;
            result.wanderRadius = this.wanderRadius > 0f ? this.wanderRadius : null;
            result.overrideFacing = this.overrideFacing;
            result.tag = this.tag;
            return result;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("nodeId", this.nodeId));
            result.Add(new XElement("editorPosition", this.editorPosition));
            if (this.duty != null)
            {
                result.Add(new XElement("duty", this.duty.defName));
            }
            if (!this.focusTarget.NullOrEmpty())
            {
                result.Add(new XElement("focusTarget", this.focusTarget));
            }
            if (!this.focusSecondTarget.NullOrEmpty())
            {
                result.Add(new XElement("focusSecondTarget", this.focusSecondTarget));
            }
            if (!this.focusThirdTarget.NullOrEmpty())
            {
                result.Add(new XElement("focusThirdTarget", this.focusThirdTarget));
            }
            if (this.radius >= 0f)
            {
                result.Add(new XElement("radius", this.radius));
            }
            if (this.wanderRadius > 0f)
            {
                result.Add(new XElement("wanderRadius", this.wanderRadius));
            }
            if (this.locomotion != LocomotionUrgency.None)
            {
                result.Add(new XElement("locomotion", this.locomotion));
            }
            if (this.maxDanger != Danger.None)
            {
                result.Add(new XElement("maxDanger", this.maxDanger));
            }
            if (this.overrideFacing.IsValid)
            {
                result.Add(new XElement("overrideFacing", this.overrideFacing));
            }
            if (!this.tag.NullOrEmpty())
            {
                result.Add(new XElement("tag", this.tag));
            }
            if (this.enterActions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.enterActions, "enterActions"));
            }
            if (this.exitActions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.exitActions, "exitActions"));
            }
            return result;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.nodeId, "nodeId");
            Scribe_Values.Look(ref this.editorPosition, "editorPosition");
            Scribe_Defs.Look(ref this.duty, "duty");
            Scribe_Values.Look(ref this.focusTarget, "focusTarget");
            Scribe_Values.Look(ref this.focusSecondTarget, "focusSecondTarget");
            Scribe_Values.Look(ref this.focusThirdTarget, "focusThirdTarget");
            Scribe_Values.Look(ref this.radius, "radius", -1f);
            Scribe_Values.Look(ref this.wanderRadius, "wanderRadius");
            Scribe_Values.Look(ref this.locomotion, "locomotion");
            Scribe_Values.Look(ref this.maxDanger, "maxDanger");
            Scribe_Values.Look(ref this.overrideFacing, "overrideFacing");
            Scribe_Values.Look(ref this.tag, "tag");
            Scribe_Collections.Look(ref this.enterActions, "enterActions", LookMode.Deep);
            Scribe_Collections.Look(ref this.exitActions, "exitActions", LookMode.Deep);
        }

        private LocalTargetInfo ResolveTarget(string targetKey, Pawn pawn, Quest quest)
        {
            if (targetKey.NullOrEmpty())
            {
                return LocalTargetInfo.Invalid;
            }
            if (targetKey == "Pawn")
            {
                return pawn;
            }
            TargetInfo target = GameTools.GetTargetFromQuestDatabase(quest, targetKey);
            if (target.IsValid)
            {
                return target.HasThing ? new LocalTargetInfo(target.Thing) : new LocalTargetInfo(target.Cell);
            }
            target = GameTools.GetTargetFromGlobalDatabase(quest, targetKey);
            if (target.IsValid)
            {
                return target.HasThing ? new LocalTargetInfo(target.Thing) : new LocalTargetInfo(target.Cell);
            }
            target = GameTools.GetTargetFromTemporaryDatabase(targetKey);
            if (target.IsValid)
            {
                return target.HasThing ? new LocalTargetInfo(target.Thing) : new LocalTargetInfo(target.Cell);
            }
            return LocalTargetInfo.Invalid;
        }

        public string nodeId = "Node";
        public Vector2 editorPosition = new Vector2(80f, 120f);
        public DutyDef duty;
        [NoTranslate]
        public string focusTarget;
        [NoTranslate]
        public string focusSecondTarget;
        [NoTranslate]
        public string focusThirdTarget;
        public float radius = -1f;
        public float wanderRadius;
        public LocomotionUrgency locomotion = LocomotionUrgency.Walk;
        public Danger maxDanger = Danger.Deadly;
        public Rot4 overrideFacing = Rot4.Invalid;
        [NoTranslate]
        public string tag;
        public List<CQFAction> enterActions = new List<CQFAction>();
        public List<CQFAction> exitActions = new List<CQFAction>();
    }

    public class DutyMapTransition : ISaveable, IDrawable, IExposable
    {
        public bool CanTransition(Pawn pawn, CustomDutyMap runtime, Quest quest, Dictionary<string, TargetInfo> targets)
        {
            return this.triggers.NullOrEmpty() || this.triggers.All(trigger => trigger.Triggered(pawn, runtime, quest, targets));
        }

        public void Draw(ref float y, Rect inRect, float x)
        {
            Widgets.Label(new Rect(x, y, 260f, 25f), "CQF_DutyMapTransition".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DutyMapFromNode".Translate(), ref this.fromNodeId, x, 150f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DutyMapToNode".Translate(), ref this.toNodeId, x, 150f);
            y += 30f;
            CQFEditorTools.DrawIDrawList(ref y, x, this.triggers, inRect, "CQF_DutyTransitionTriggers".Translate(),
                this.OpenTriggerSelect, trigger => this.TriggerLabel(trigger.GetType()));
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("fromNodeId", this.fromNodeId));
            result.Add(new XElement("toNodeId", this.toNodeId));
            if (!this.triggers.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.triggers, "triggers"));
            }
            return result;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.fromNodeId, "fromNodeId");
            Scribe_Values.Look(ref this.toNodeId, "toNodeId");
            Scribe_Collections.Look(ref this.triggers, "triggers", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.triggers == null)
            {
                this.triggers = new List<CustomDutyTrigger>();
            }
        }

        private void OpenTriggerSelect()
        {
            List<Type> types = typeof(CustomDutyTrigger).AllSubclassesNonAbstract()
                .OrderBy(type => this.TriggerLabel(type))
                .ToList();
            Find.WindowStack.Add(new Dialog_Select<Type>(types, null, this.TriggerLabel, "CQF_DutySelectTrigger".Translate(), type =>
            {
                this.triggers.Add((CustomDutyTrigger)Activator.CreateInstance(type));
            }, null, null, null, null, null, type => type.Name));
        }

        private string TriggerLabel(Type type)
        {
            string key = "CQF_" + type.Name;
            return key.CanTranslate() ? key.Translate().ToString() : type.Name;
        }

        public string fromNodeId;
        public string toNodeId;
        public List<CustomDutyTrigger> triggers = new List<CustomDutyTrigger>();
    }
}
