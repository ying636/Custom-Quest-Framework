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
    public static class CQFAction_SetCustomHediffEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_SetCustomHediff cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (CQFUIStyle.ButtonText(new Rect(x, y, 150f, 25f), "GivenHediff".Translate() + cqfReceiver.hediff?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HediffDef>.AllDefsListForReading.FindAll(c => typeof(CustomHediff).IsAssignableFrom(c.hediffClass)), f => cqfReceiver.hediff = f, f => f.label);
            }

            y += 30f;
            string source_label = cqfReceiver.label.CanTranslate() ? cqfReceiver.label.Translate().ToString() : cqfReceiver.label;
            string edited_label = source_label;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_CustomName".Translate(), ref edited_label, x, 100f);
            if (edited_label != source_label) cqfReceiver.label = edited_label;
            y += 30f;
            string source_desc = cqfReceiver.desc.CanTranslate() ? cqfReceiver.desc.Translate().ToString() : cqfReceiver.desc;
            string edited_desc = source_desc;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_CustomDescription".Translate(), ref edited_desc, x, 100f);
            if (edited_desc != source_desc) cqfReceiver.desc = edited_desc;
            y += 30f;
            CQFEditorTools.DrawSelectColorButtons(ref y, "HediffColor".Translate(), cqfReceiver.color, c => cqfReceiver.color = c, x);
            y += 5f;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, cqfReceiver.comps, inRect, "ActionComps".Translate(), c => c.compName, t =>
            {
                t.allowedActions = cqfReceiver.Allows;
            });
            y += 30f;
        }
    }
}
