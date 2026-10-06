using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library
{
    public static class CustomTrap_CaptureEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomTrap_Capture cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawLabelAndText_Line(y, "TrapName".Translate(), ref cqfReceiver.trapName, x, 250f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "DisarmReport".Translate(), ref cqfReceiver.disarmReport, x, 100f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "TickToDisarm".Translate(), ref cqfReceiver.tickToDisarm, ref cqfReceiver.buffer, x, 100f);
            y += 30f;
            CQFEditorTools.DrawActionList(ref y, x, cqfReceiver.disarmActions, inRect, "DisarmActions".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, cqfReceiver.trapComps, inRect, "TrapComps".Translate().Colorize(ColorLibrary.LightBlue), () =>
            {
                CQFEditorTools.DrawFloatMenu(new List<ActionTriggerMode>() { ActionTriggerMode.Signal, ActionTriggerMode.StepOn, ActionTriggerMode.Tick }, m => cqfReceiver.trapComps.Add(new TrapComp() { mode = m }), m => ("ActionTriggerMode_" + m.ToString()).Translate());
            }, c => ("ActionTriggerMode_" + c.mode.ToString()).Translate());
        }
    }
}
