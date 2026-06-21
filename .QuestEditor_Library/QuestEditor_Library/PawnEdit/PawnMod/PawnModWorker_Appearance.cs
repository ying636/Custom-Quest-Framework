using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Appearance : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef == null || pawnDef.KindDef.race.race.Humanlike;
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_Appearance();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Appearance data = pawnDef.DataFor<PawnModData_Appearance>();
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Hair".Translate(this.ValueOrNone(data.hair?.label))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HairDef>.AllDefsListForReading, hair => data.hair = hair, hair => hair.label);
            }
            this.DrawColorRow(ref y, inRect, x, "CQF_PawnEditor_SelectHairColor".Translate(), data.hairColor ?? Color.white, color => data.hairColor = this.Opaque(color));
            this.DrawColorRow(ref y, inRect, x, this.SkinColorLabel(data), data.skinColor, color => data.skinColor = this.Opaque(color), () => data.skinColor = null);
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_HeadType".Translate(this.ValueOrNone(data.head?.defName))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HeadTypeDef>.AllDefsListForReading, head => data.head = head, head => head.defName);
            }
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_BodyType".Translate(this.ValueOrNone(this.BodyTypeLabel(data.bodyType)))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<BodyTypeDef>.AllDefsListForReading, body => data.bodyType = body, this.BodyTypeLabel);
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            PawnModData_Appearance data = pawnDef.DataFor<PawnModData_Appearance>();
            if (pawn.story == null)
            {
                return;
            }
            pawn.story.hairDef = data.hair ?? pawn.story.hairDef;
            pawn.story.headType = data.head ?? pawn.story.headType;
            pawn.story.bodyType = data.bodyType ?? pawn.story.bodyType;
            if (data.hairColor != null)
            {
                pawn.story.HairColor = data.hairColor.Value;
            }
            if (data.skinColor != null)
            {
                pawn.story.skinColorOverride = data.skinColor.Value;
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            PawnModData_Appearance data = pawnDef.DataFor<PawnModData_Appearance>();
            data.hair = DefDatabase<HairDef>.GetNamedSilentFail(node["hair"]?.InnerText) ?? HairDefOf.Bald;
            data.head = DefDatabase<HeadTypeDef>.GetNamedSilentFail(node["head"]?.InnerText);
            data.bodyType = DefDatabase<BodyTypeDef>.GetNamedSilentFail(node["bodyType"]?.InnerText);
            data.hairColor = node["hairColor"] == null ? Color.white : ParseHelper.FromString<Color>(node["hairColor"].InnerText);
            data.skinColor = node["skinColor"] == null ? null : ParseHelper.FromString<Color>(node["skinColor"].InnerText);
        }

        private string BodyTypeLabel(BodyTypeDef bodyType)
        {
            if (bodyType == null)
            {
                return null;
            }
            return bodyType.defName.CanTranslate() ? bodyType.defName.Translate().ToString() : bodyType.defName;
        }

        private string SkinColorLabel(PawnModData_Appearance data)
        {
            return "CQF_PawnEditor_SelectSkinColor".Translate(data.skinColor == null ? "CQF_PawnEditor_DefaultSkinColor".Translate() : "CQF_PawnEditor_CustomSkinColor".Translate());
        }
    }
}
