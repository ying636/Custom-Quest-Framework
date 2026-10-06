using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.Tilemaps;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class WorldCondition_LandmarkEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldCondition_Landmark cqfReceiver, ref float y, Rect inRect, float x)
        {
            WorldConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawSelectButton(x, ref y, "LandmarkDef".Translate(cqfReceiver.landmark?.label ?? cqfReceiver.landmark?.defName), DefDatabase<LandmarkDef>.AllDefsListForReading, d => cqfReceiver.landmark = d, d => d.label ?? d.defName);
        }
    }
}
