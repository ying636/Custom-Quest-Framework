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
    public static class WorldAction_GenerateMapAndEnterEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldAction_GenerateMapAndEnter cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            WorldActionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawSelectButton(x, ref y, "WorldObjectDef".Translate(cqfReceiver.worldObject?.label ?? cqfReceiver.worldObject?.defName), DefDatabase<WorldObjectDef>.AllDefsListForReading, d => cqfReceiver.worldObject = d, d => d.label ?? d.defName);
        }
    }
}
