using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Noise;

namespace QuestEditor_Library
{
    public static class ReplaceData_DefEditor
    {
        public static void Draw_0(QuestEditor_Library.ReplaceData_Def cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            if (CQFUIStyle.ButtonText(new Rect(x, y, 250f, 25f), "ReplacementDef".Translate(cqfReceiver.def?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu<ReplacementDataDef>(DefDatabase<ReplacementDataDef>.AllDefsListForReading, d => cqfReceiver.def = d, d => d.defName);
            }

            y += 30f;
        }
    }
}
