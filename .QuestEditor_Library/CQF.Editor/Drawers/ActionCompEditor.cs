using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class ActionCompEditor
    {
        public static void Draw_0(QuestEditor_Library.ActionComp cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawLabelAndText_Line(y, "CompName".Translate(), ref cqfReceiver.compName, x, 100f);
            Rect rectCP = new Rect(380f, y, 25f, 25f);
            if (CQFUIStyle.ButtonImage(rectCP, TexButton.Copy))
            {
                CQFEditorTools.actionComp = cqfReceiver.Copy();
            }

            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            y += 30f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(40f, CQFUIScope.ContentWidth - x - 12f), 25f), "CQFActionTriggerMode".Translate(("ActionTriggerMode_" + cqfReceiver.mode.ToString()).Translate().ToString()), false))
            {
                var actions = new List<ActionTriggerMode>()
                {
                    ActionTriggerMode.Signal,
                    ActionTriggerMode.Tick,
                    ActionTriggerMode.Damaged,
                    ActionTriggerMode.Destroy,
                    ActionTriggerMode.MapGeneration,
                    ActionTriggerMode.Open
                };
                if (cqfReceiver.allowedActions != null)
                {
                    actions = cqfReceiver.allowedActions;
                }

                CQFEditorTools.DrawFloatMenu(actions, m => cqfReceiver.mode = m, m => ("ActionTriggerMode_" + m.ToString()).Translate().ToString());
            }

            y += 30f;
            if (cqfReceiver.mode == ActionTriggerMode.Signal)
            {
                CQFSignalEditor.DrawBookField(y, "InSignal".Translate(), cqfReceiver.signal, value => cqfReceiver.signal = value, x, 150f, inRect.width - x - 12f);
                y += 30f;
                Rect rect = new Rect(x, y, 350f, 25f);
                Widgets.CheckboxLabeled(rect, "SignalOnlyIsValidInPart".Translate(), ref cqfReceiver.signalIsOnlyValidInPart);
                TooltipHandler.TipRegion(rect, "SignalOnlyIsValidInPartTip".Translate());
                y += 30f;
            }

            if (cqfReceiver.mode == ActionTriggerMode.Tick)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "TickToTrigger".Translate(), ref cqfReceiver.tick, ref cqfReceiver.buffer, x);
                TooltipHandler.TipRegion(new Rect(x, y, 150f, 25f), "TickToTriggerTip".Translate());
                y += 30f;
                if (!cqfReceiver.HasValidTickInterval)
                {
                    Widgets.Label(new Rect(x, y, inRect.width - x, 25f), "CQF_ActionComp_InvalidTickInterval".Translate().Colorize(Color.red));
                    y += 30f;
                }
            }

            CQFEditorTools.DrawActionList(ref y, x, cqfReceiver.actions, inRect, "InteractionActions".Translate());
        }
    }
}
