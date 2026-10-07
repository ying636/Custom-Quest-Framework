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
    public static class CQFThingData_CorpseEditor
    {
        public static void Draw_0(QuestEditor_Library.CQFThingData_Corpse cqfReceiver, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            CQFEditorTools.DrawButtonToSelectWithoutBackground(ref y, x + 7f, "QE_PawnKind".Translate(cqfReceiver.pawn?.label), DefDatabase<PawnKindDef>.AllDefsListForReading, d => cqfReceiver.pawn = d, d => d.label);
            if (CQFUIStyle.ButtonText(new Rect(x + 7f, y, 200f, 30f), "CurRotMode".Translate(cqfReceiver.rotMode == null ? "Random".Translate() : cqfReceiver.rotMode.ToString().Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu(QuestEditor_Library.CQFThingData_Corpse.Stages, r => cqfReceiver.rotMode = r, r => r.ToString().Translate(), [new FloatMenuOption("Random".Translate(), () => cqfReceiver.rotMode = null)]);
            }

            y += 30f;
        }

        public static void DrawIcon_1(QuestEditor_Library.CQFThingData_Corpse cqfReceiver, ref float y)
        {
        }
    }
}
