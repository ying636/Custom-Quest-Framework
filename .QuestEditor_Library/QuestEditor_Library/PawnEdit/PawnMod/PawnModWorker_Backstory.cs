using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Backstory : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef == null || pawnDef.KindDef.race.race.Humanlike;
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_Backstory();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Backstory data = pawnDef.DataFor<PawnModData_Backstory>();
            this.DrawBackstoryButton(ref y, inRect, x, "CQF_PawnEditor_Childhood".Translate(this.ValueOrNone(data.childhood?.title)), backstory => data.childhood = backstory);
            this.DrawBackstoryButton(ref y, inRect, x, "CQF_PawnEditor_Adulthood".Translate(this.ValueOrNone(data.adulthood?.title)), backstory => data.adulthood = backstory);
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            PawnModData_Backstory data = pawnDef.DataFor<PawnModData_Backstory>();
            if (pawn.story == null)
            {
                return;
            }
            if (data.childhood != null)
            {
                pawn.story.Childhood = data.childhood;
            }
            if (data.adulthood != null)
            {
                pawn.story.Adulthood = data.adulthood;
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            PawnModData_Backstory data = pawnDef.DataFor<PawnModData_Backstory>();
            data.childhood = DefDatabase<BackstoryDef>.GetNamedSilentFail(node["childhood"]?.InnerText);
            data.adulthood = DefDatabase<BackstoryDef>.GetNamedSilentFail(node["adulthood"]?.InnerText);
        }

        private void DrawBackstoryButton(ref float y, Rect inRect, float x, string label, Action<BackstoryDef> action)
        {
            if (this.DrawSelectRow(ref y, inRect, x, label))
            {
                Find.WindowStack.Add(new Dialog_Select<BackstoryDef>(new TextSelectDrawer<BackstoryDef>(DefDatabase<BackstoryDef>.AllDefsListForReading, backstory => backstory.title, action, null, null, null, null, null, null), "CQF_PawnEditor_Select".Translate()));
            }
        }
    }
}
