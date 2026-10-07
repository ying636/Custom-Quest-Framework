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
    public static class CQFAction_HediffEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Hediff cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (CQFUIStyle.ButtonText(rect, "GivenHediff".Translate() + cqfReceiver.hediff?.label, false))
            {
                Find.WindowStack.Add(new Dialog_Select<HediffDef>(new TextSelectDrawer<HediffDef>(DefDatabase<HediffDef>.AllDefsListForReading, t => t.label, t =>
                {
                    cqfReceiver.hediff = t;
                }, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "SeverityOfHediff".Translate(), ref cqfReceiver.severity, ref cqfReceiver.buffer, x);
            y += 30f;
            if (CQFUIStyle.ButtonText(new Rect(x, y, 350f, 25f), "CQFBodyPartForHediff".Translate(cqfReceiver.labelBuffer ?? "FullBody".Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<BodyDef>.AllDefsListForReading, b =>
                {
                    CQFEditorTools.DrawFloatMenu(b.AllParts, h =>
                    {
                        cqfReceiver.bodyPart = h.def;
                        cqfReceiver.customLabel = h.untranslatedCustomLabel;
                        cqfReceiver.labelBuffer = h.customLabel ?? h.def.label;
                    }, h => h.customLabel ?? h.def.label);
                }, b => b.label);
            }

            y += 30f;
        }
    }
}
