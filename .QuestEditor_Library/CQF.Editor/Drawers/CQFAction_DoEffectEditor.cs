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
    public static class CQFAction_DoEffectEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_DoEffect cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 350f, 25f);
            if (Widgets.ButtonText(rect, "CQF_EffectDef".Translate(cqfReceiver.effect?.label ?? cqfReceiver.effect?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<EffecterDef>.AllDefsListForReading, d => cqfReceiver.effect = d, d => d.label ?? d.defName);
            }

            y += 30f;
        }
    }
}
