using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_BasicEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Basic cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Basic data = pawnDef.DataFor<PawnModData_Basic>();
            Rect row = cqfReceiver.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_DefName".Translate(), 170f);
            pawnDef.defName = Widgets.TextField(new Rect(row.x, row.y, Mathf.Min(360f, row.width), 30f), pawnDef.defName);
            cqfReceiver.EndRow(ref y);
            row = cqfReceiver.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_Label".Translate(), 170f);
            pawnDef.label = Widgets.TextField(new Rect(row.x, row.y, Mathf.Min(360f, row.width), 30f), pawnDef.label);
            cqfReceiver.EndRow(ref y);
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_PawnKind".Translate(cqfReceiver.ValueOrNone(data.kindDef?.label))))
            {
                cqfReceiver.OpenPawnKindSelector(kind => data.kindDef = kind);
            }

            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Faction".Translate() + cqfReceiver.ValueOrNone(data.faction?.label)))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefsListForReading, faction => data.faction = faction, faction => faction.label);
            }
        }
    }
}
