using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class PawnModWorker_GenesEditor
    {
        public static void Draw_0(QuestEditor_Library.PawnModWorker_Genes cqfReceiver, ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            PawnModData_Genes data = pawnDef.DataFor<PawnModData_Genes>();
            cqfReceiver.EnsureXenotype(data);
            if (cqfReceiver.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Xenotype".Translate(data.xenotype.LabelCap)))
            {
                cqfReceiver.OpenXenotypeSelector(data);
            }

            if (data.xenotype != null && !data.xenotype.descriptionShort.NullOrEmpty())
            {
                Rect rect = new Rect(x, y, inRect.width - x - 20f, 60f);
                Widgets.Label(rect, data.xenotype.descriptionShort);
                cqfReceiver.EndRow(ref y, 66f);
            }

            cqfReceiver.DrawCustomGenes(data, ref y, inRect, x);
        }

        public static void DrawCustomGenes_1(QuestEditor_Library.PawnModWorker_Genes cqfReceiver, PawnModData_Genes data, ref float y, Rect inRect, float x)
        {
            List<GeneDef> customGenes = data.customGenes;
            Rect labelRect = new Rect(x, y + 3f, 150f, 25f);
            Widgets.Label(labelRect, "CQF_PawnEditor_CustomGenes".Translate().Colorize(CQFUIStyle.Accent));
            Rect addRect = new Rect(labelRect.xMax + 8f, y, 90f, 30f);
            if (cqfReceiver.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                cqfReceiver.OpenGeneSelector(gene => customGenes.Add(gene));
            }

            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 90f, 30f);
            if (cqfReceiver.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && customGenes.Any())
            {
                CQFEditorTools.DrawFloatMenu(customGenes, gene => customGenes.Remove(gene), cqfReceiver.GeneLabel);
            }

            y += 42f;
            cqfReceiver.RemoveDuplicateGenes(customGenes);
            foreach (GeneDef gene in customGenes)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                Rect buttonRect = new Rect(row.x + 8f, row.y + 3f, row.width - 16f, 30f);
                if (cqfReceiver.DrawTextButton(buttonRect, cqfReceiver.GeneLabel(gene)))
                {
                    cqfReceiver.OpenGeneSelector(newGene =>
                    {
                        int index = customGenes.IndexOf(gene);
                        if (index >= 0)
                        {
                            customGenes[index] = newGene;
                        }
                    });
                }

                y += 42f;
            }
        }
    }
}
