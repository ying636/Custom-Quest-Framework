using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_Hediff : PawnModWorker
    {
        public override PawnModData CreateData()
        {
            return new PawnModData_Hediff();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_Hediff modData = pawnDef.DataFor<PawnModData_Hediff>();
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (this.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                this.OpenHediffSelector(data => modData.hediffs.Add(data));
            }
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (this.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && modData.hediffs.Any())
            {
                CQFEditorTools.DrawFloatMenu(modData.hediffs, data => modData.hediffs.Remove(data), this.HediffLabel);
            }
            y += 42f;
            foreach (HediffData data in modData.hediffs)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 76f);
                Widgets.DrawLightHighlight(row);
                Rect hediffRect = new Rect(row.x + 8f, row.y + 6f, row.width - 16f, 30f);
                if (this.DrawTextButton(hediffRect, this.HediffLabel(data)))
                {
                    this.OpenHediffSelector(newData =>
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
                if (this.DrawTextButton(partRect, "CQF_PawnEditor_HediffPart".Translate(this.PartLabel(pawnDef, data))))
                {
                    this.OpenPartSelector(pawnDef, part => data.SetPart(pawnDef, part));
                }
                Widgets.Label(severityLabelRect, severityLabel);
                Widgets.TextFieldNumeric(severityFieldRect, ref data.severity, ref data.buffer, 0f);
                y += 82f;
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.health?.hediffSet == null)
            {
                return;
            }
            foreach (HediffData data in pawnDef.DataFor<PawnModData_Hediff>().hediffs)
            {
                if (data?.def == null)
                {
                    continue;
                }
                BodyPartRecord part = this.PartRecord(pawn, data);
                Hediff oldHediff = part == null
                    ? pawn.health.hediffSet.GetFirstHediffOfDef(data.def)
                    : pawn.health.hediffSet.hediffs.FirstOrDefault(hediff => hediff.def == data.def && hediff.Part == part);
                if (oldHediff != null)
                {
                    pawn.health.RemoveHediff(oldHediff);
                }
                Hediff hediff = HediffMaker.MakeHediff(data.def, pawn, part);
                hediff.Severity = data.severity;
                pawn.health.AddHediff(hediff);
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            if (node["hediffs"] != null)
            {
                pawnDef.DataFor<PawnModData_Hediff>().hediffs = this.LoadSaveableList<HediffData>(node["hediffs"]);
            }
        }

        private void OpenHediffSelector(Action<HediffData> action)
        {
            Find.WindowStack.Add(new Dialog_Select<HediffDef>(DefDatabase<HediffDef>.AllDefsListForReading, null, def => def.label, "CQF_PawnEditor_Select".Translate(), def =>
            {
                action(new HediffData { def = def, severity = Mathf.Max(0f, def.initialSeverity) });
            }));
        }

        private string HediffLabel(HediffData data)
        {
            return data?.def?.label ?? "CQF_PawnEditor_None".Translate();
        }

        private void OpenPartSelector(ComplexPawnDef pawnDef, Action<BodyPartRecord> action)
        {
            List<BodyPartRecord> parts = new List<BodyPartRecord> { null };
            parts.AddRange(this.AvailableParts(pawnDef));
            Find.WindowStack.Add(new Dialog_Select<BodyPartRecord>(parts, null, this.PartLabel, "CQF_PawnEditor_Select".Translate(), action));
        }

        private List<BodyPartRecord> AvailableParts(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef?.race?.race?.body?.AllParts
                .OrderBy(part => part.depth)
                .ThenBy(part => part.coverageAbs)
                .ToList() ?? new List<BodyPartRecord>();
        }

        private BodyPartRecord PartRecord(Pawn pawn, HediffData data)
        {
            if (data?.part == null)
            {
                return null;
            }
            List<BodyPartRecord> parts = pawn.RaceProps.body.GetPartsWithDef(data.part);
            if (data.partIndex >= 0 && data.partIndex < pawn.RaceProps.body.AllParts.Count)
            {
                BodyPartRecord indexedPart = pawn.RaceProps.body.AllParts[data.partIndex];
                if (indexedPart.def == data.part)
                {
                    return indexedPart;
                }
            }
            if (!data.partLabel.NullOrEmpty())
            {
                BodyPartRecord labeledPart = parts.FirstOrDefault(part => part.untranslatedCustomLabel == data.partLabel || part.customLabel == data.partLabel);
                if (labeledPart != null)
                {
                    return labeledPart;
                }
            }
            return parts.FirstOrDefault();
        }

        private string PartLabel(ComplexPawnDef pawnDef, HediffData data)
        {
            BodyPartRecord part = this.PartRecord(pawnDef, data);
            return this.PartLabel(part);
        }

        private BodyPartRecord PartRecord(ComplexPawnDef pawnDef, HediffData data)
        {
            if (data?.part == null)
            {
                return null;
            }
            List<BodyPartRecord> parts = this.AvailableParts(pawnDef);
            if (data.partIndex >= 0 && data.partIndex < parts.Count && parts[data.partIndex].def == data.part)
            {
                return parts[data.partIndex];
            }
            if (!data.partLabel.NullOrEmpty())
            {
                BodyPartRecord labeledPart = parts.FirstOrDefault(part => part.def == data.part && (part.untranslatedCustomLabel == data.partLabel || part.customLabel == data.partLabel));
                if (labeledPart != null)
                {
                    return labeledPart;
                }
            }
            return parts.FirstOrDefault(part => part.def == data.part);
        }

        private string PartLabel(BodyPartRecord part)
        {
            return part?.Label ?? "CQF_PawnEditor_WholeBody".Translate();
        }
    }
}
