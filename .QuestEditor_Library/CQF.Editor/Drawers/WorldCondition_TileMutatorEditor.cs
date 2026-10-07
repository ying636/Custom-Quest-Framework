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
    public static class WorldCondition_TileMutatorEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldCondition_TileMutator cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            WorldConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawSelectButton(x, ref y, "TileMutatorDef".Translate(cqfReceiver.mutator?.label ?? cqfReceiver.mutator?.defName), DefDatabase<TileMutatorDef>.AllDefsListForReading, d => cqfReceiver.mutator = d, d => d.label ?? d.defName);
        }
    }
}
