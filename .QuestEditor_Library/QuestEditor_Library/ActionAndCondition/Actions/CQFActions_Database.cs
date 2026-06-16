using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_GetThingToRecord : CQFAction_RecordToDatabase
    {
        public override Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map != null && t.Value.Cell.GetThingList(t.Value.Map).Find(t2 => t.Value.Thing == null || t2 != t.Value.Thing) is Thing t3)
                {
                    result.Add(t.Key, t3);
                }
            });
            return result;
        }
    }

    public class CQFAction_GetCellToRecord : CQFAction_RecordToDatabase
    {
        public override Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets)
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Map != null)
                {
                    result.Add(t.Key, new TargetInfo(t.Value.Cell, t.Value.Map));
                }
            });
            return result;
        }
    }

    public class CQFAction_RecordToGroup : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DataWrite;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f;
        }

        public virtual Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets)
        {
            return targets;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "CQFAction_Record_recordKey");
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            List<Thing> result = new List<Thing>();
            this.GetTargetFromGaveTarget(targets).ToList().ForEach(t =>
            {
                if (t.Value.Thing is { } thing)
                {
                    result.Add(thing);
                }
            });
            GameComponent_Editor.Instance.GetQuestData(quest).AddGroup(this.recordKey, result);
        }

        public string recordKey;
    }

    public class CQFAction_RecordToDatabase : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DataWrite;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            if (this.recordToTemporaryBase)
            {
                result.Add(new XElement("recordToTemporaryBase", this.recordToTemporaryBase));
            }
            if (this.recordToQuestBase)
            {
                result.Add(new XElement("recordToQuestBase", recordToQuestBase));
            }
            if (this.recordToGlobalBase)
            {
                result.Add(new XElement("recordToGlobalBase", this.recordToGlobalBase));
            }
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f;
            Rect rect = new Rect(x, y, 350f, 25f);
            Widgets.CheckboxLabeled(rect, "RecordToTemporaryBase".Translate(), ref this.recordToTemporaryBase);
            TooltipHandler.TipRegion(rect, "RecordToTemporaryBase_Tip".Translate());
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "RecordToQuestBase".Translate(), ref this.recordToQuestBase);
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "RecordToGlobalBase".Translate(), ref this.recordToGlobalBase);
            y += 30f;
        }

        public virtual Dictionary<string, TargetInfo> GetTargetFromGaveTarget(Dictionary<string, TargetInfo> targets)
        {
            return targets;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "CQFAction_Record_recordKey");
            Scribe_Values.Look(ref this.recordToQuestBase, "recordToQuestBase");
            Scribe_Values.Look(ref this.recordToTemporaryBase, "CQFAction_Record_recordToTemporaryBase");
            Scribe_Values.Look(ref this.recordToGlobalBase, "recordToGlobalBase");
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.GetTargetFromGaveTarget(targets).ToList().ForEach(t =>
            {
                if (this.recordToTemporaryBase)
                {
                    GameTools.AddTemporaryTagret(this.recordKey, t.Value);
                }
                if (this.recordToQuestBase)
                {
                    GameComponent_Editor.Instance.GetQuestData(quest)?.RecordTarget(recordKey, t.Value);
                }
                if (this.recordToGlobalBase)
                {
                    GameComponent_Editor.Instance.GlobalDatabase.RecordTarget(recordKey, t.Value);
                }
            });
        }

        public string recordKey;
        public bool recordToQuestBase = false;
        public bool recordToTemporaryBase = false;
        public bool recordToGlobalBase = false;
    }

    public class CQFAction_RecordStartCell : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DataWrite;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            return result;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in targets)
            {
                if (target.Value.IsValid && target.Value.Map is Map map)
                {
                    MapComponent_CustomMapData.GetComp(map).StartCells.SetOrAdd(this.recordKey, target.Value.Cell);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "recordKey");
        }

        public string recordKey;
    }

    public class CQFAction_FinishRect : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DataWrite;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in targets)
            {
                if (target.Value.IsValid && target.Value.Map is Map map)
                {
                    if (MapComponent_CustomMapData.GetComp(map) is { } comp && comp.StartCells.ContainsKey(this.recordKey))
                    {
                        IntVec3 start = comp.StartCells[this.recordKey];
                        CellRect rect = CellRect.FromLimits(start, target.Value.Cell);
                        foreach (var cell in rect)
                        {
                            foreach (var action in this.actions)
                            {
                                action.Work(new Dictionary<string, TargetInfo>()
                                {
                                    ["Position"] = new TargetInfo(cell, target.Value.Map)
                                }, quest);
                            }
                        }
                        comp.StartCells.Remove(this.recordKey);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.recordKey, "recordKey");
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public string recordKey;
        public List<CQFAction> actions = new List<CQFAction>();
    }

    public class CQFAction_DoActionForGroup : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DataWrite;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("recordKey", this.recordKey));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in GameComponent_Editor.Instance.GetQuestData(quest).GetGroup(this.recordKey))
            {
                foreach (var action in this.actions)
                {
                    action.Work(new Dictionary<string, TargetInfo>()
                    {
                        ["Target"] = new TargetInfo(target)
                    }, quest);
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.recordKey, "recordKey");
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public string recordKey;
        public List<CQFAction> actions = new List<CQFAction>();
    }
}

