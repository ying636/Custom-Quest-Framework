using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_AppearanceEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Appearance cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Appearance data = pawnDef.DataFor<PawnModData_Appearance>();
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Hair".Translate(cqfReceiver.ValueOrNone(data.hair?.label))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HairDef>.AllDefsListForReading, hair => data.hair = hair, hair => hair.label);
            }

            cqfReceiver.DrawColorRow(ref y, inRect, x, "CQF_PawnEditor_SelectHairColor".Translate(), data.hairColor ?? Color.white, color => data.hairColor = cqfReceiver.Opaque(color));
            cqfReceiver.DrawColorRow(ref y, inRect, x, cqfReceiver.SkinColorLabel(data), data.skinColor, color => data.skinColor = cqfReceiver.Opaque(color), () => data.skinColor = null);
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_HeadType".Translate(cqfReceiver.ValueOrNone(data.head?.defName))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HeadTypeDef>.AllDefsListForReading, head => data.head = head, head => head.defName);
            }

            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_BodyType".Translate(cqfReceiver.ValueOrNone(cqfReceiver.BodyTypeLabel(data.bodyType)))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<BodyTypeDef>.AllDefsListForReading, body => data.bodyType = body, cqfReceiver.BodyTypeLabel);
            }
        }
    }
}
