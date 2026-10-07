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
    public static class DialogCondition_FactionEditor
    {
        public static void Draw_0(QuestEditor_Library.DialogCondition_Faction cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            DialogCondition_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            if (CQFUIStyle.ButtonText(rect, "RequiredFaction".Translate(cqfReceiver.faction?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<FactionDef>(new TextSelectDrawer<FactionDef>(DefDatabase<FactionDef>.AllDefsListForReading, t => t.label, t =>
                {
                    cqfReceiver.faction = t;
                }, null, null, null, null, null, null), "Select".Translate()));
            }

            y += 30f;
        }
    }
}
