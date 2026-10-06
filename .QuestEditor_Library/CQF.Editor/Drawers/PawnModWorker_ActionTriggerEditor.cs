using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_ActionTriggerEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_ActionTrigger cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
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
                CQFEditorTools.DrawFloatMenu(modData.actionTriggers, data => modData.actionTriggers.Remove(data), cqfReceiver.TriggerLabel);
            }

            TooltipHandler.TipRegion(deleteRect, "CQF_PawnEditor_Delete".Translate());
            y += 42f;
            foreach (PawnActionTriggerData data in modData.actionTriggers)
            {
                float panelHeight = cqfReceiver.TriggerPanelHeight(data);
                Rect panelRect = new Rect(x, y, inRect.width - x - 20f, panelHeight);
                Widgets.DrawLightHighlight(panelRect);
                Widgets.DrawBox(panelRect, 1, QuestEditor_Dialog.blueTex);
                Rect keyRect = new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 30f);
                Widgets.Label(new Rect(keyRect.x, keyRect.y + 3f, 110f, 24f), "CQF_PawnEditor_TriggerKey".Translate().Colorize(ColorLibrary.PaleBlue));
                data.key = Widgets.TextField(new Rect(keyRect.x + 118f, keyRect.y, keyRect.width - 118f, 30f), data.key);
                Rect modeRect = new Rect(panelRect.x + 10f, keyRect.yMax + 6f, panelRect.width - 20f, 30f);
                if (cqfReceiver.DrawTextButton(modeRect, "CQF_PawnEditor_TriggerMode".Translate(cqfReceiver.ModeLabel(data.mode))))
                {
                    CQFEditorTools.DrawFloatMenu(cqfReceiver.AllowedModes, mode => data.mode = mode, cqfReceiver.ModeLabel);
                }

                cqfReceiver.DrawActions(data, modeRect.yMax + 8f, panelRect);
                y += panelHeight + 10f;
            }
        }

        public static void DrawActions_1(QuestEditor_Library.PawnModWorker_ActionTrigger cqfReceiver, PawnActionTriggerData data, float y, Rect panelRect)
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
                CQFEditorTools.DrawFloatMenu(data.actions, action => data.actions.Remove(action), cqfReceiver.ActionLabel);
            }

            TooltipHandler.TipRegion(deleteRect, "CQF_PawnEditor_Delete".Translate());
            float actionY = y + 34f;
            foreach (CQFAction action in data.actions)
            {
                Rect actionRect = new Rect(panelRect.x + 14f, actionY, panelRect.width - 28f, 26f);
                if (Widgets.ButtonText(actionRect, cqfReceiver.ActionLabel(action), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                }

                actionY += 30f;
            }
        }
    }
}
