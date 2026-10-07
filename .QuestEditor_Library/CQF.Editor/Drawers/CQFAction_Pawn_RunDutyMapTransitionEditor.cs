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
    public static class CQFAction_Pawn_RunDutyMapTransitionEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Pawn_RunDutyMapTransition cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 260f, 25f);
            if (CQFUIStyle.ButtonText(rect, "CQF_DutyMapDef".Translate(cqfReceiver.dutyMap?.defName ?? "Null"), false))
            {
                Find.WindowStack.Add(new Dialog_Select<DutyMapDef>(new TextSelectDrawer<DutyMapDef>(DefDatabase<DutyMapDef>.AllDefsListForReading, d => d.defName, d => cqfReceiver.dutyMap = d, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DutyMapNodeId".Translate(), ref cqfReceiver.toNodeId, x, 150f);
            y += 30f;
        }
    }
}
