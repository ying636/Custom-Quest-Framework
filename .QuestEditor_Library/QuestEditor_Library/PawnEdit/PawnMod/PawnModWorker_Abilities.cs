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
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Abilities.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
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
        internal void OpenAbilitySelector(Action<AbilityDef> action)
        {
            Find.WindowStack.Add(new Dialog_Select<AbilityDef>(new LabeledTextureSelectDrawer<AbilityDef>(DefDatabase<AbilityDef>.AllDefsListForReading, ability => ability.uiIcon, ability => ability.label, action, null, null, null, null, ability => ability.defName, null, null), "CQF_PawnEditor_Select".Translate()));
        }
        internal void RemoveDuplicates(List<AbilityData> abilities)
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
        internal string AbilityLabel(AbilityData data)
        {
            return data?.def?.label ?? "CQF_PawnEditor_None".Translate();
        }
    }
}
