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
            PawnModData_Traits modData = pawnDef.DataFor<PawnModData_Traits>();
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (this.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                this.OpenTraitSelector(data => modData.traits.Add(data));
            }
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (this.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && modData.traits.Any())
            {
                CQFEditorTools.DrawFloatMenu(modData.traits, data => modData.traits.Remove(data), data => data.def?.DataAtDegree(data.degree)?.label ?? "CQF_PawnEditor_None".Translate());
            }
            y += 42f;
            foreach (TraitData data in modData.traits)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                Rect traitRect = new Rect(row.x + 8f, row.y + 3f, Mathf.Max(220f, row.width - 190f), 30f);
                if (this.DrawTextButton(traitRect, data.def?.DataAtDegree(data.degree)?.label ?? "CQF_PawnEditor_None".Translate()))
                {
                    this.OpenTraitSelector(newData =>
                    {
                        data.def = newData.def;
                        data.degree = newData.degree;
                    });
                }
                Widgets.Label(new Rect(traitRect.xMax + 10f, row.y + 6f, 70f, 24f), "CQF_PawnEditor_Chance".Translate());
                Widgets.TextFieldPercent(new Rect(traitRect.xMax + 80f, row.y + 3f, 80f, 30f), ref data.chance, ref data.buffer);
                y += 42f;
            }
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

        private void OpenTraitSelector(Action<TraitData> action)
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
