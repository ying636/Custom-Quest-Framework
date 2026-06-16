using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Abilities : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_Abilities();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Abilities modData = pawnDef.DataFor<PawnModData_Abilities>();
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (this.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                this.OpenAbilitySelector(ability => modData.abilities.Add(new AbilityData { def = ability }));
            }
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (this.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && modData.abilities.Any())
            {
                CQFEditorTools.DrawFloatMenu(modData.abilities, data => modData.abilities.Remove(data), this.AbilityLabel);
            }
            y += 42f;
            this.RemoveDuplicates(modData.abilities);
            foreach (AbilityData data in modData.abilities)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                Rect buttonRect = new Rect(row.x + 8f, row.y + 3f, row.width - 16f, 30f);
                if (this.DrawTextButton(buttonRect, this.AbilityLabel(data)))
                {
                    this.OpenAbilitySelector(ability => data.def = ability);
                }
                y += 42f;
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.abilities == null)
            {
                return;
            }
            PawnModData_Abilities modData = pawnDef.DataFor<PawnModData_Abilities>();
            this.RemoveDuplicates(modData.abilities);
            HashSet<AbilityDef> desired = modData.abilities.Where(data => data?.def != null).Select(data => data.def).ToHashSet();
            foreach (Ability ability in pawn.abilities.abilities.ToList())
            {
                if (!desired.Contains(ability.def))
                {
                    pawn.abilities.RemoveAbility(ability.def);
                }
            }
            foreach (AbilityDef ability in desired)
            {
                pawn.abilities.GainAbility(ability);
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            if (node["abilities"] != null)
            {
                pawnDef.DataFor<PawnModData_Abilities>().abilities = this.LoadSaveableList<AbilityData>(node["abilities"]);
            }
        }

        private void OpenAbilitySelector(Action<AbilityDef> action)
        {
            Find.WindowStack.Add(new Dialog_Select<AbilityDef>(DefDatabase<AbilityDef>.AllDefsListForReading, null, ability => ability.label, "CQF_PawnEditor_Select".Translate(), action));
        }

        private void RemoveDuplicates(List<AbilityData> abilities)
        {
            HashSet<AbilityDef> defs = new HashSet<AbilityDef>();
            for (int i = abilities.Count - 1; i >= 0; i--)
            {
                AbilityDef def = abilities[i]?.def;
                if (def == null || !defs.Add(def))
                {
                    abilities.RemoveAt(i);
                }
            }
        }

        private string AbilityLabel(AbilityData data)
        {
            return data?.def?.label ?? "CQF_PawnEditor_None".Translate();
        }
    }
}
