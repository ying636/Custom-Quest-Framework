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
    public class PawnModWorker_Genes : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return ModsConfig.BiotechActive && (pawnDef.KindDef == null || pawnDef.KindDef.race.race.Humanlike);
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_Genes();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Genes data = pawnDef.DataFor<PawnModData_Genes>();
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Xenotype".Translate(this.ValueOrNone(data.xenotype?.LabelCap))))
            {
                this.OpenXenotypeSelector(data);
            }
            if (data.xenotype != null && !data.xenotype.descriptionShort.NullOrEmpty())
            {
                Rect rect = new Rect(x, y, inRect.width - x - 20f, 60f);
                Widgets.Label(rect, data.xenotype.descriptionShort);
                this.EndRow(ref y, 66f);
            }
            this.DrawCustomGenes(data, ref y, inRect, x);
        }

        public override void ModifyGenerationRequest(ComplexPawnDef pawnDef, ref PawnGenerationRequest request)
        {
            PawnModData_Genes data = pawnDef.DataFor<PawnModData_Genes>();
            if (ModsConfig.BiotechActive && data.xenotype != null)
            {
                request.ForcedXenotype = data.xenotype;
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (!ModsConfig.BiotechActive || pawn.genes == null)
            {
                return;
            }
            PawnModData_Genes data = pawnDef.DataFor<PawnModData_Genes>();
            if (data.xenotype != null && pawn.genes.Xenotype != data.xenotype)
            {
                pawn.genes.SetXenotype(data.xenotype);
            }
            this.ApplyCustomGenes(data, pawn);
        }

        public override void LoadData(ComplexPawnDef pawnDef, XmlNode node)
        {
            PawnModData_Genes data = pawnDef.DataFor<PawnModData_Genes>();
            data.xenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail(node["xenotype"]?.InnerText);
            data.customGenes.Clear();
            XmlNode customGenesNode = node.SelectSingleNode("customGenes");
            if (customGenesNode == null)
            {
                return;
            }
            foreach (XmlNode li in customGenesNode.SelectNodes("li"))
            {
                GeneDef gene = DefDatabase<GeneDef>.GetNamedSilentFail(li.InnerText.Trim());
                if (gene != null)
                {
                    data.customGenes.Add(gene);
                }
            }
            this.RemoveDuplicateGenes(data.customGenes);
        }

        public override IEnumerable<string> GetPreviewApplyKeyParts(ComplexPawnDef pawnDef)
        {
            foreach (GeneDef gene in pawnDef.DataFor<PawnModData_Genes>().customGenes)
            {
                yield return gene?.defName;
            }
        }

        private void DrawCustomGenes(PawnModData_Genes data, ref float y, Rect inRect, float x)
        {
            List<GeneDef> customGenes = data.customGenes;
            Rect labelRect = new Rect(x, y + 3f, 150f, 25f);
            Widgets.Label(labelRect, "CQF_PawnEditor_CustomGenes".Translate().Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(labelRect.xMax + 8f, y, 90f, 30f);
            if (this.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                this.OpenGeneSelector(gene => customGenes.Add(gene));
            }
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 90f, 30f);
            if (this.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && customGenes.Any())
            {
                CQFEditorTools.DrawFloatMenu(customGenes, gene => customGenes.Remove(gene), this.GeneLabel);
            }
            y += 42f;
            this.RemoveDuplicateGenes(customGenes);
            foreach (GeneDef gene in customGenes)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                Rect buttonRect = new Rect(row.x + 8f, row.y + 3f, row.width - 16f, 30f);
                if (this.DrawTextButton(buttonRect, this.GeneLabel(gene)))
                {
                    this.OpenGeneSelector(newGene =>
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

        private void ApplyCustomGenes(PawnModData_Genes data, Pawn pawn)
        {
            this.RemoveDuplicateGenes(data.customGenes);
            HashSet<GeneDef> desired = data.customGenes.Where(gene => gene != null).ToHashSet();
            if (this.appliedCustomGenes.TryGetValue(pawn, out HashSet<GeneDef> previous))
            {
                foreach (GeneDef geneDef in previous.Where(gene => !desired.Contains(gene)).ToList())
                {
                    if (pawn.genes.HasXenogene(geneDef))
                    {
                        Gene gene = pawn.genes.GetGene(geneDef);
                        if (gene != null)
                        {
                            pawn.genes.RemoveGene(gene);
                        }
                    }
                }
            }
            HashSet<GeneDef> applied = new HashSet<GeneDef>();
            foreach (GeneDef geneDef in desired)
            {
                if (pawn.genes.HasActiveGene(geneDef))
                {
                    continue;
                }
                pawn.genes.AddGene(geneDef, true);
                applied.Add(geneDef);
            }
            this.appliedCustomGenes[pawn] = applied;
        }

        private void OpenXenotypeSelector(PawnModData_Genes data)
        {
            List<ExtraOption> extraOptions = new List<ExtraOption>
            {
                new ExtraOption("CQF_PawnEditor_None".Translate(), null, () => data.xenotype = null)
            };
            Find.WindowStack.Add(new Dialog_Select<XenotypeDef>(DefDatabase<XenotypeDef>.AllDefsListForReading, null, xenotype => xenotype.LabelCap, "CQF_PawnEditor_SelectXenotype".Translate(), xenotype =>
            {
                data.xenotype = xenotype;
            }, null, null, xenotype => xenotype.descriptionShort ?? xenotype.description, xenotype => -Mathf.RoundToInt(xenotype.displayPriority * 1000f), extraOptions, xenotype => xenotype.defName));
        }

        private void OpenGeneSelector(Action<GeneDef> action)
        {
            Find.WindowStack.Add(new Dialog_Select<GeneDef>(DefDatabase<GeneDef>.AllDefsListForReading, null, this.GeneLabel, "CQF_PawnEditor_SelectGene".Translate(), action, null, null, gene => gene.description, gene => Mathf.RoundToInt(gene.displayOrderInCategory * 1000f), null, gene => gene.defName));
        }

        private void RemoveDuplicateGenes(List<GeneDef> genes)
        {
            if (genes == null)
            {
                return;
            }
            HashSet<GeneDef> defs = new HashSet<GeneDef>();
            for (int i = genes.Count - 1; i >= 0; i--)
            {
                GeneDef gene = genes[i];
                if (gene == null || !defs.Add(gene))
                {
                    genes.RemoveAt(i);
                }
            }
        }

        private string GeneLabel(GeneDef gene)
        {
            return gene?.label ?? "CQF_PawnEditor_None".Translate();
        }

        private readonly Dictionary<Pawn, HashSet<GeneDef>> appliedCustomGenes = new Dictionary<Pawn, HashSet<GeneDef>>();
    }
}
