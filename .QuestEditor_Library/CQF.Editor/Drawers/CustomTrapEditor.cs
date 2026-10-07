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
    public static class CustomTrapEditor
    {
        public static void Draw_0(QuestEditor_Library.CustomTrap cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Text.Font = GameFont.Small;
            CQFEditorTools.DrawLabelAndText_Line(y, "TrapName".Translate(), ref cqfReceiver.trapName, x, 350f);
            Rect rect = new Rect(inRect.xMax - 70f, y + 30f, 30f, 30f);
            if (CQFUIStyle.ButtonImage(rect, TexButton.Copy))
            {
                CQFEditorTools.copyTrapComps.Clear();
                foreach (var trapComp in cqfReceiver.trapComps)
                {
                    CQFEditorTools.copyTrapComps.Add(trapComp.Copy());
                }
            }

            rect.x += 35f;
            if (CQFUIStyle.ButtonImage(rect, TexButton.Paste))
            {
                foreach (var trapComp in CQFEditorTools.copyTrapComps)
                {
                    cqfReceiver.trapComps.Add(trapComp.Copy());
                }
            }

            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, cqfReceiver.TrapComps, inRect, "TrapComps".Translate().Colorize(CQFUIStyle.Accent), () =>
            {
                CQFEditorTools.DrawFloatMenu(new List<ActionTriggerMode>() { ActionTriggerMode.Signal, ActionTriggerMode.StepOn, ActionTriggerMode.Tick }, m => cqfReceiver.TrapComps.Add(new TrapComp() { mode = m }), m => ("ActionTriggerMode_" + m.ToString()).Translate());
            }, c => ("ActionTriggerMode_" + c.mode.ToString()).Translate());
        }

        public static void PasteData_1(QuestEditor_Library.CustomTrap cqfReceiver)
        {
            cqfReceiver.trapComps.Clear();
            foreach (var actionComp in CQFEditorTools.copyTrapComps)
            {
                cqfReceiver.trapComps.Add(actionComp.Copy());
            }
        }
    }
}
