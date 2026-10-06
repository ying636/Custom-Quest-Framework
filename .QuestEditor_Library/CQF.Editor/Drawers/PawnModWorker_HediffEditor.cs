using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_HediffEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Hediff cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Hediff modData = pawnDef.DataFor<PawnModData_Hediff>();
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (cqfReceiver.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                cqfReceiver.OpenHediffSelector(data => modData.hediffs.Add(data));
            }

            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (cqfReceiver.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && modData.hediffs.Any())
            {
                CQFEditorTools.DrawFloatMenu(modData.hediffs, data => modData.hediffs.Remove(data), cqfReceiver.HediffLabel);
            }

            y += 42f;
            foreach (HediffData data in modData.hediffs)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 76f);
                Widgets.DrawLightHighlight(row);
                Rect hediffRect = new Rect(row.x + 8f, row.y + 6f, row.width - 16f, 30f);
                if (cqfReceiver.DrawTextButton(hediffRect, cqfReceiver.HediffLabel(data)))
                {
                    cqfReceiver.OpenHediffSelector(newData =>
                    {
                        data.def = newData.def;
                        data.severity = newData.severity;
                    });
                }

                string severityLabel = "CQF_PawnEditor_Severity".Translate();
                float severityLabelWidth = Text.CalcSize(severityLabel).x;
                Rect severityFieldRect = new Rect(row.xMax - 94f, hediffRect.yMax + 6f, 86f, 30f);
                Rect severityLabelRect = new Rect(severityFieldRect.x - severityLabelWidth - 10f, severityFieldRect.y + 3f, severityLabelWidth, 24f);
                float partWidth = Mathf.Max(160f, severityLabelRect.x - row.x - 18f);
                Rect partRect = new Rect(row.x + 8f, hediffRect.yMax + 6f, partWidth, 30f);
                if (cqfReceiver.DrawTextButton(partRect, "CQF_PawnEditor_HediffPart".Translate(cqfReceiver.PartLabel(pawnDef, data))))
                {
                    cqfReceiver.OpenPartSelector(pawnDef, part => data.SetPart(pawnDef, part));
                }

                Widgets.Label(severityLabelRect, severityLabel);
                Widgets.TextFieldNumeric(severityFieldRect, ref data.severity, ref data.buffer, 0f);
                y += 82f;
            }
        }
    }
}
