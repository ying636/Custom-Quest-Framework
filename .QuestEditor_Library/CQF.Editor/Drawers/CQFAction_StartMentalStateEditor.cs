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
    public static class CQFAction_StartMentalStateEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_StartMentalState cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "CQFMentalState".Translate(cqfReceiver.state?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<MentalStateDef>.AllDefsListForReading, f => cqfReceiver.state = f, f => f.label);
            }

            y += 30f;
            if (cqfReceiver.state == MentalStateDefOf.SocialFighting)
            {
                CQFTargetKeyEditor.DrawBookField(y, "stateTargetText".Translate(), cqfReceiver.stateTargetText, value => cqfReceiver.stateTargetText = value, x, 150f, inRect.width - x - 20f);
                y += 30f;
            }
        }
    }
}
