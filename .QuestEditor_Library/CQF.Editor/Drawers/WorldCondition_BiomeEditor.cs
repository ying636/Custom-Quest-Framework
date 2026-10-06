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
    public static class WorldCondition_BiomeEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldCondition_Biome cqfReceiver, ref float y, Rect inRect, float x)
        {
            WorldConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawSelectButton(x, ref y, "BiomeDef".Translate(cqfReceiver.biome?.label ?? cqfReceiver.biome?.defName), DefDatabase<BiomeDef>.AllDefsListForReading, d => cqfReceiver.biome = d, d => d.label ?? d.defName);
        }
    }
}
