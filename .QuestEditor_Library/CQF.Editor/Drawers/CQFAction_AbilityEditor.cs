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
    public static class CQFAction_AbilityEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Ability cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, inRect.width, 25f), "CQFAbilityDef".Translate(cqfReceiver.ability?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<AbilityDef>(new TextSelectDrawer<AbilityDef>(DefDatabase<AbilityDef>.AllDefsListForReading, t => t.label, t =>
                {
                    cqfReceiver.ability = t;
                }, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
        }
    }
}
