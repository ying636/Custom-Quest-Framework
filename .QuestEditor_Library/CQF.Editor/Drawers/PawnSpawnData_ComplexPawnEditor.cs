using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public static class PawnSpawnData_ComplexPawnEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnSpawnData_ComplexPawn cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Rect rect = new Rect(16f + x, y + 10f, 500f, 45f);
            cqfReceiver.DrawName(ref y, x, rect);
            Rect pawnRect = new Rect(20f + x, y, 360f, 25f);
            if (CQFUIStyle.ButtonText(pawnRect, "CQF_PawnEditor_ComplexPawnDef".Translate(cqfReceiver.PawnDisplayName(cqfReceiver.pawnDef)), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<ComplexPawnDef>.AllDefsListForReading, def => cqfReceiver.pawnDef = def, cqfReceiver.PawnDisplayName);
            }

            y += 30f;
            Rect lordRect = new Rect(20f + x, y, 150f, 25f);
            CQFEditorTools.DrawSelectableText(y, "LordNameWithTarget".Translate(), ref cqfReceiver.lordDataName, cqfReceiver.OpenLordSelector, x + 20f, 150f);
            TooltipHandler.TipRegion(lordRect, "CustomLordNameTip".Translate());
            y += 30f;
            cqfReceiver.DrawCanSaveWarning(ref y, x, inRect);
        }

        public static void OpenLordSelector_1(QuestEditor_Library.PawnSpawnData_ComplexPawn cqfReceiver)
        {
            if (Find.CurrentMap == null)
            {
                return;
            }

            MapComponent_CustomMapData comp = Find.CurrentMap.GetComponent<MapComponent_CustomMapData>();
            CQFEditorTools.DrawFloatMenu(comp.Lords, lord => cqfReceiver.lordDataName = lord.name, lord => lord.name);
        }
    }
}
