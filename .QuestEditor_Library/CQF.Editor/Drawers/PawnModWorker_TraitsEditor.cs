using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_TraitsEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Traits cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Traits modData = pawnDef.DataFor<PawnModData_Traits>();
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (cqfReceiver.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                cqfReceiver.OpenTraitSelector(data => modData.traits.Add(data));
            }

            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (cqfReceiver.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && modData.traits.Any())
            {
                CQFEditorTools.DrawFloatMenu(modData.traits, data => modData.traits.Remove(data), data => data.def?.DataAtDegree(data.degree)?.label ?? "CQF_PawnEditor_None".Translate());
            }

            y += 42f;
            foreach (TraitData data in modData.traits)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                Rect traitRect = new Rect(row.x + 8f, row.y + 3f, Mathf.Max(220f, row.width - 190f), 30f);
                if (cqfReceiver.DrawTextButton(traitRect, data.def?.DataAtDegree(data.degree)?.label ?? "CQF_PawnEditor_None".Translate()))
                {
                    cqfReceiver.OpenTraitSelector(newData =>
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
    }
}
