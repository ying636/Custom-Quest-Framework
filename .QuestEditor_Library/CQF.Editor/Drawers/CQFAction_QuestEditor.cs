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
    public static class CQFAction_QuestEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFAction_Quest cqfReceiver, ref float y, Rect inRect, float x)
        {
            CQFActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 450f, 25f), "CQFQuestDef".Translate(cqfReceiver.quest?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<QuestScriptDef>.AllDefsListForReading, (d) => cqfReceiver.quest = d, (d) => d.defName);
            }

            y += 30f;
        }
    }
}
