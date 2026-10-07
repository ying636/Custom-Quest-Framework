using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_DialogEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Dialog cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            PawnModData_Dialog data = pawnDef.DataFor<PawnModData_Dialog>();
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_DialogManager".Translate(cqfReceiver.ValueOrNone(data.dialogManager?.defName))))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("CQF_PawnEditor_None".Translate(), () => data.dialogManager = null)
                };
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, manager => data.dialogManager = manager, manager => manager.defName, options);
            }
        }
    }
}
