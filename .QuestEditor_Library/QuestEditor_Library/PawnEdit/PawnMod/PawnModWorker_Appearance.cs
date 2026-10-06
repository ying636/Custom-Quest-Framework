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
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Appearance.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
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
        internal string BodyTypeLabel(BodyTypeDef bodyType)
        {
            if (bodyType == null)
            {
                return null;
            }
            return bodyType.defName.CanTranslate() ? bodyType.defName.Translate().ToString() : bodyType.defName;
        }
        internal string SkinColorLabel(PawnModData_Appearance data)
        {
            return "CQF_PawnEditor_SelectSkinColor".Translate(data.skinColor == null ? "CQF_PawnEditor_DefaultSkinColor".Translate() : "CQF_PawnEditor_CustomSkinColor".Translate());
        }
    }
}
