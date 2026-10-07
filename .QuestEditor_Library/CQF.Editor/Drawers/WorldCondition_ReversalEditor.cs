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
    public static class WorldCondition_ReversalEditor
    {
        public static void Draw_0(QuestEditor_Library.WorldCondition_Reversal cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            WorldConditionEditor.Draw_0(cqfReceiver, ref y, inRect, x);
            CQFEditorTools.DrawSelectButton(x, ref y, typeof(WorldCondition).AllSubclassesNonAbstract(), t => cqfReceiver.condition = (WorldCondition)Activator.CreateInstance(t), t => t.Name.Translate());
            cqfReceiver.condition?.Draw(ref y, inRect, x);
            y += 30f;
        }
    }
}
