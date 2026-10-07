using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class TrapCompEditor
    {
        public static void Draw_0(QuestEditor_Library.TrapComp cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            if (CQFUIStyle.ButtonText(new Rect(x, y, 325f, 25f), "CustomTrapMode".Translate(("ActionTriggerMode_" + cqfReceiver.mode.ToString()).Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu(new List<ActionTriggerMode>() { ActionTriggerMode.Signal, ActionTriggerMode.StepOn, ActionTriggerMode.Tick }, m => cqfReceiver.mode = m, m => ("ActionTriggerMode_" + m.ToString()).Translate());
            }

            y += 30f;
            if (cqfReceiver.mode == ActionTriggerMode.Signal)
            {
                CQFSignalEditor.DrawBookField(y, "TrapInSignal".Translate(), cqfReceiver.inSignal, value => cqfReceiver.inSignal = value, x, 200f, inRect.width - x - 12f);
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
            }

            Widgets.CheckboxLabeled(new Rect(x, y, 250f, 25f), "TriggerWhenDamaged".Translate(), ref cqfReceiver.triggerWhenDamaged);
            TooltipHandler.TipRegion(new Rect(x, y, 125f, 30f), "CustomTrapTip".Translate());
            y += 30f;
            CQFEditorTools.DrawActionList(ref y, x, cqfReceiver.actions, inRect, "TrapActions".Translate());
        }
    }
}
