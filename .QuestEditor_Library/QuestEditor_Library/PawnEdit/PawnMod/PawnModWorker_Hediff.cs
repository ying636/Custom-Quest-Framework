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
            object[] arguments = new object[]
            {
                pawnDef,
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.PawnModWorker_Hediff.Draw(None:QuestEditor_Library.ComplexPawnDef,Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[1];
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
        internal void OpenHediffSelector(Action<HediffData> action)
        {
            Find.WindowStack.Add(new Dialog_Select<HediffDef>(new TextSelectDrawer<HediffDef>(DefDatabase<HediffDef>.AllDefsListForReading, def => def.label, def =>
            {
                action(new HediffData { def = def, severity = Mathf.Max(0f, def.initialSeverity) });
            }, null, null, null, null, null, null), "CQF_PawnEditor_Select".Translate()));
        }
        internal string HediffLabel(HediffData data)
        {
            return data?.def?.label ?? "CQF_PawnEditor_None".Translate();
        }
        internal void OpenPartSelector(ComplexPawnDef pawnDef, Action<BodyPartRecord> action)
        {
            List<BodyPartRecord> parts = new List<BodyPartRecord> { null };
            parts.AddRange(this.AvailableParts(pawnDef));
            Find.WindowStack.Add(new Dialog_Select<BodyPartRecord>(new TextSelectDrawer<BodyPartRecord>(parts, this.PartLabel, action, null, null, null, null, null, null), "CQF_PawnEditor_Select".Translate()));
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
        internal string PartLabel(ComplexPawnDef pawnDef, HediffData data)
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
        internal string PartLabel(BodyPartRecord part)
        {
            return part?.Label ?? "CQF_PawnEditor_WholeBody".Translate();
        }
    }
}
