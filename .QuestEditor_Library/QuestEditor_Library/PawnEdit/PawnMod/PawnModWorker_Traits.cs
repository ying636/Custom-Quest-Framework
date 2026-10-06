using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Traits : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef == null || pawnDef.KindDef.race.race.Humanlike;
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_Traits();
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
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Traits.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
        }
        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            PawnModData_Traits modData = pawnDef.DataFor<PawnModData_Traits>();
            if (pawn.story?.traits == null || modData.traits.NullOrEmpty())
            {
                return;
            }
            foreach (Trait trait in pawn.story.traits.allTraits.ToList())
            {
                pawn.story.traits.RemoveTrait(trait);
            }
            foreach (TraitData data in modData.traits)
            {
                if (data?.def != null && (preview || Rand.Chance(data.chance)))
                {
                    pawn.story.traits.GainTrait(new Trait(data.def, data.degree));
                }
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            if (node["traits"] != null)
            {
                pawnDef.DataFor<PawnModData_Traits>().traits = this.LoadSaveableList<TraitData>(node["traits"]);
            }
        }
        internal void OpenTraitSelector(Action<TraitData> action)
        {
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t => t.degreeDatas.ForEach(s => stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s))));
            Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(new TextSelectDrawer<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, t => t.Value.label, t =>
            {
                action(new TraitData() { def = t.Key, degree = t.Value.degree, chance = 1f });
            }, null, null, null, null, null, null), "CQF_PawnEditor_Select".Translate()));
        }
    }
}
