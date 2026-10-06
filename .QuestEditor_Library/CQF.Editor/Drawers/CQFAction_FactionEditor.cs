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
    public static class CQFAction_FactionEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Faction cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFAction_TargetEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, "Faction".Translate() + ":" + cqfReceiver.faction?.label);
            rect.x = 160f;
            if (Widgets.ButtonText(rect, "Select".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefsListForReading, f => cqfReceiver.faction = f, f => f.label);
            }

            y += 30f;
        }
    }
}
