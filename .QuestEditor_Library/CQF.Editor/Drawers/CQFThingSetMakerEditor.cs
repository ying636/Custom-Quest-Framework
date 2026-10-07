using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;
using System.Xml.Linq;
using System.Xml;

namespace QuestEditor_Library
{
    public static class CQFThingSetMakerEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFThingSetMaker cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawButtonToSelectWithoutBackground(ref y, x + 7f, "ThingSetMaker".Translate(cqfReceiver.set?.label ?? cqfReceiver.set?.defName), DefDatabase<ThingSetMakerDef>.AllDefsListForReading, d => cqfReceiver.set = d, d => d.label ?? d.defName);
            CQFEditorTools.DrawFloatRange(ref y, "TotalMarketValueRange".Translate(), ref cqfReceiver.totalMarketValueRange, ref cqfReceiver.buffer, ref cqfReceiver.buffer2, x + 7f, 40f);
            y += 30f;
        }

        public static void DrawIcon_1(QuestEditor_Library.CQFThingSetMaker cqfReceiver, ref float y)
        {
        }
    }
}
