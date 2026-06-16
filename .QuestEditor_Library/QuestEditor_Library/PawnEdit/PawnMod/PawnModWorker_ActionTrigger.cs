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
            PawnModData_ActionTrigger modData = pawnDef.DataFor<PawnModData_ActionTrigger>();
            Rect addRect = new Rect(x, y, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                modData.actionTriggers.Add(new PawnActionTriggerData { key = pawnDef.defName + "_Damaged" });
            }
            TooltipHandler.TipRegion(addRect, "CQF_PawnEditor_Add".Translate());
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 28f, 28f);
            if (Widgets.ButtonImage(deleteRect, TexButton.Delete) && modData.actionTriggers.Any())
            {
                CQFEditorTools.DrawFloatMenu(modData.actionTriggers, data => modData.actionTriggers.Remove(data), this.TriggerLabel);
            }
            TooltipHandler.TipRegion(deleteRect, "CQF_PawnEditor_Delete".Translate());
            y += 42f;
            foreach (PawnActionTriggerData data in modData.actionTriggers)
            {
                float panelHeight = this.TriggerPanelHeight(data);
                Rect panelRect = new Rect(x, y, inRect.width - x - 20f, panelHeight);
                Widgets.DrawLightHighlight(panelRect);
                Widgets.DrawBox(panelRect, 1, QuestEditor_Dialog.blueTex);
                Rect keyRect = new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 30f);
                Widgets.Label(new Rect(keyRect.x, keyRect.y + 3f, 110f, 24f), "CQF_PawnEditor_TriggerKey".Translate().Colorize(ColorLibrary.PaleBlue));
                data.key = Widgets.TextField(new Rect(keyRect.x + 118f, keyRect.y, keyRect.width - 118f, 30f), data.key);
                Rect modeRect = new Rect(panelRect.x + 10f, keyRect.yMax + 6f, panelRect.width - 20f, 30f);
                if (this.DrawTextButton(modeRect, "CQF_PawnEditor_TriggerMode".Translate(this.ModeLabel(data.mode))))
                {
                    CQFEditorTools.DrawFloatMenu(this.AllowedModes, mode => data.mode = mode, this.ModeLabel);
                }
                this.DrawActions(data, modeRect.yMax + 8f, panelRect);
                y += panelHeight + 10f;
            }
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

        private string TriggerLabel(PawnActionTriggerData data)
        {
            return data?.key.NullOrEmpty() ?? true ? "CQF_PawnEditor_None".Translate() : data.key;
        }

        private string ModeLabel(ActionTriggerMode mode)
        {
            return ("ActionTriggerMode_" + mode).Translate();
        }

        private void DrawActions(PawnActionTriggerData data, float y, Rect panelRect)
        {
            Rect labelRect = new Rect(panelRect.x + 10f, y + 3f, 255f, 24f);
            string label = "CQF_PawnEditor_TriggerActions".Translate();
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.PaleBlue));
            float buttonX = labelRect.x + Text.CalcSize(label).x + 14f;
            Rect addRect = new Rect(buttonX, y, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                CQFEditorTools.OpenCQFActionSelect(type => data.actions.Add((CQFAction)Activator.CreateInstance(type)));
            }
            TooltipHandler.TipRegion(addRect, "CQF_PawnEditor_Add".Translate());
            Rect deleteRect = new Rect(addRect.xMax + 8f, y, 28f, 28f);
            if (Widgets.ButtonImage(deleteRect, TexButton.Delete) && data.actions.Any())
            {
                CQFEditorTools.DrawFloatMenu(data.actions, action => data.actions.Remove(action), this.ActionLabel);
            }
            TooltipHandler.TipRegion(deleteRect, "CQF_PawnEditor_Delete".Translate());

            float actionY = y + 34f;
            foreach (CQFAction action in data.actions)
            {
                Rect actionRect = new Rect(panelRect.x + 14f, actionY, panelRect.width - 28f, 26f);
                if (Widgets.ButtonText(actionRect, this.ActionLabel(action), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                }
                actionY += 30f;
            }
        }

        private float TriggerPanelHeight(PawnActionTriggerData data)
        {
            return 122f + (data.actions?.Count ?? 0) * 30f;
        }

        private string ActionLabel(CQFAction action)
        {
            return action == null ? "CQF_PawnEditor_None".Translate() : action.GetType().Name.Translate();
        }

        private List<ActionTriggerMode> AllowedModes => new List<ActionTriggerMode> { ActionTriggerMode.Damaged };
    }
}
