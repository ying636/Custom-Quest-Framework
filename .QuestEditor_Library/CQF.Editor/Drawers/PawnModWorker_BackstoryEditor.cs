using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_BackstoryEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Backstory cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            PawnModData_Backstory data = pawnDef.DataFor<PawnModData_Backstory>();
            cqfReceiver.DrawBackstoryButton(ref y, inRect, x, "CQF_PawnEditor_Childhood".Translate(cqfReceiver.ValueOrNone(data.childhood?.title)), backstory => data.childhood = backstory);
            cqfReceiver.DrawBackstoryButton(ref y, inRect, x, "CQF_PawnEditor_Adulthood".Translate(cqfReceiver.ValueOrNone(data.adulthood?.title)), backstory => data.adulthood = backstory);
        }

        public static void DrawBackstoryButton_1(QuestEditor_Library.PawnModWorker_Backstory cqfReceiver, ref float y, Rect inRect, float x, string label, Action<BackstoryDef> action)
        {
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, label))
            {
                Find.WindowStack.Add(new Dialog_Select<BackstoryDef>(new TextSelectDrawer<BackstoryDef>(DefDatabase<BackstoryDef>.AllDefsListForReading, backstory => backstory.title, action, null, null, null, null, null, null), "CQF_PawnEditor_Select".Translate()));
            }
        }
    }
}
