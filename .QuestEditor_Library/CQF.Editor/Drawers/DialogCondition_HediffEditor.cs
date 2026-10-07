using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public static class DialogCondition_HediffEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_Hediff cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            DialogCondition_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, "RequiredHediff".Translate() + cqfReceiver.hediff?.label);
            rect.x = 160f;
            if (CQFUIStyle.ButtonText(rect, "Select".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HediffDef>.AllDefsListForReading, h => cqfReceiver.hediff = h, h => h.label);
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "RequiredSeverity".Translate(), ref cqfReceiver.severity, ref cqfReceiver.buffer, x);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 20f), "NeedToBeGreater".Translate(), ref cqfReceiver.needToBeGreater);
            y += 25f;
        }
    }
}
