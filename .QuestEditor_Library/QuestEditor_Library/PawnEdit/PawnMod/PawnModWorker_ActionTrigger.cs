using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_ActionTrigger : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_ActionTrigger();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_ActionTrigger.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
        }
        public override void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)
        {
            if (pawn?.Map == null)
            {
                return;
            }
            MapComponent_CustomMapData comp = MapComponent_CustomMapData.GetComp(pawn.Map);
            foreach (PawnActionTriggerData data in pawnDef.DataFor<PawnModData_ActionTrigger>().actionTriggers)
            {
                if (data == null || data.key.NullOrEmpty())
                {
                    continue;
                }
                ThingActionTrigger trigger = comp.Triggers.Find(t => t.key == data.key);
                if (trigger == null)
                {
                    trigger = new ThingActionTrigger { key = data.key };
                    comp.Triggers.Add(trigger);
                }
                trigger.mode = data.mode;
                trigger.actions = data.actions.ListFullCopy();
                if (!trigger.things.Contains(pawn))
                {
                    trigger.things.Add(pawn);
                }
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            if (node["actionTriggers"] != null)
            {
                pawnDef.DataFor<PawnModData_ActionTrigger>().actionTriggers = this.LoadSaveableList<PawnActionTriggerData>(node["actionTriggers"]);
            }
        }
        internal string TriggerLabel(PawnActionTriggerData data)
        {
            return data?.key.NullOrEmpty() ?? true ? "CQF_PawnEditor_None".Translate() : data.key;
        }
        internal string ModeLabel(ActionTriggerMode mode)
        {
            return ("ActionTriggerMode_" + mode).Translate();
        }
        internal void DrawActions(PawnActionTriggerData data, float y, Rect panelRect)

        {
            object[] arguments = new object[]
            {
                data,
                y,
                panelRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_ActionTrigger.DrawActions(None:QuestEditor_Library.PawnActionTriggerData,None:float,None:UnityEngine.Rect)", this, arguments);
        }
        internal float TriggerPanelHeight(PawnActionTriggerData data)
        {
            return 122f + (data.actions?.Count ?? 0) * 30f;
        }
        internal string ActionLabel(CQFAction action)
        {
            return action == null ? "CQF_PawnEditor_None".Translate() : action.GetType().Name.Translate();
        }
        internal List<ActionTriggerMode> AllowedModes => new List<ActionTriggerMode> { ActionTriggerMode.Damaged };
    }
}
