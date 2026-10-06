using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_CreateEventArea : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.EventArea;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_CreateEventArea.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("key", this.key));
            if (!this.faction.NullOrEmpty())
            {
                result.Add(new XElement("faction", this.faction));
            }
            if (this.onlyHumanlike)
            {
                result.Add(new XElement("onlyHumanlike", this.onlyHumanlike));
            }
            if (!this.replaceExisting)
            {
                result.Add(new XElement("replaceExisting", this.replaceExisting));
            }
            result.Add(CQFSerialization.SaveList_Saveable(this.actions, "actions"));
            return result;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.TryGetMap(targets, out Map map) && MapComponent_CustomMapData.GetComp(map) is MapComponent_CustomMapData comp)
            {
                comp.AddOrReplaceEventArea(new CQFEventArea()
                {
                    key = this.key,
                    faction = this.faction,
                    onlyHumanlike = this.onlyHumanlike,
                    actions = this.actions.ListFullCopy()
                }, this.replaceExisting);
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.key, "key");
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Values.Look(ref this.onlyHumanlike, "onlyHumanlike");
            Scribe_Values.Look(ref this.replaceExisting, "replaceExisting", true);
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        private bool TryGetMap(Dictionary<string, TargetInfo> targets, out Map map)
        {
            map = null;
            if (targets != null)
            {
                foreach (TargetInfo target in targets.Values)
                {
                    if (target.Map != null)
                    {
                        map = target.Map;
                        return true;
                    }
                }
            }
            if (Find.CurrentMap != null)
            {
                map = Find.CurrentMap;
            }
            return map != null;
        }

        public string key;
        public string faction = "Any";
        public bool onlyHumanlike;
        public bool replaceExisting = true;
        public List<CQFAction> actions = new List<CQFAction>();
    }

    public class CQFAction_AddCellToEventArea : CQFAction_Target
    {
        public CQFAction_AddCellToEventArea()
        {
            this.targetsText = new List<string>() { "Position" };
        }

        public override CQFActionCategory ActionCategory => CQFActionCategory.EventArea;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddCellToEventArea.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("key", this.key));
            return result;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in targets)
            {
                if (target.Value.Map is Map map && MapComponent_CustomMapData.GetComp(map) is MapComponent_CustomMapData comp
                                              && comp.GetEventArea(this.key) is CQFEventArea area)
                {
                    area.AddCell(target.Value.Cell);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.key, "key");
        }

        public string key;
    }

    public class CQFAction_DeleteEventArea : CQFAction_Target
    {
        public CQFAction_DeleteEventArea()
        {
            this.targetsText = new List<string>() { "Position" };
        }

        public override CQFActionCategory ActionCategory => CQFActionCategory.EventArea;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_DeleteEventArea.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("key", this.key));
            return result;
        }

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (var target in targets)
            {
                if (target.Value.Map is Map map && MapComponent_CustomMapData.GetComp(map) is MapComponent_CustomMapData comp)
                {
                    comp.RemoveEventArea(this.key);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.key, "key");
        }

        public string key;
    }
}
