using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_AbilitiesEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Abilities cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Abilities modData = pawnDef.DataFor<PawnModData_Abilities>();
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (cqfReceiver.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                cqfReceiver.OpenAbilitySelector(ability => modData.abilities.Add(new AbilityData { def = ability }));
            }

            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (cqfReceiver.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && modData.abilities.Any())
            {
                CQFEditorTools.DrawFloatMenu(modData.abilities, data => modData.abilities.Remove(data), cqfReceiver.AbilityLabel);
            }

            y += 42f;
            cqfReceiver.RemoveDuplicates(modData.abilities);
            foreach (AbilityData data in modData.abilities)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                Rect buttonRect = new Rect(row.x + 8f, row.y + 3f, row.width - 16f, 30f);
                if (cqfReceiver.DrawTextButton(buttonRect, cqfReceiver.AbilityLabel(data)))
                {
                    cqfReceiver.OpenAbilitySelector(ability => data.def = ability);
                }

                y += 42f;
            }
        }
    }
}
