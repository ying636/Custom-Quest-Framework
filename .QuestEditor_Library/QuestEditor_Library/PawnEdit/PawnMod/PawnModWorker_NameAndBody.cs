using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class PawnModWorker_NameAndBody : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.KindDef == null || pawnDef.KindDef.race.race.Humanlike;
        }

        public override PawnModData CreateData()
        {
            return new PawnModData_NameAndBody();
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            PawnModData_NameAndBody data = pawnDef.DataFor<PawnModData_NameAndBody>();
            Rect row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_Name".Translate(), 140f);
            float nameWidth = Mathf.Min(150f, (row.width - 20f) / 3f);
            data.firstName = Widgets.TextField(new Rect(row.x, row.y, nameWidth, 30f), data.firstName);
            data.nickName = Widgets.TextField(new Rect(row.x + nameWidth + 10f, row.y, nameWidth, 30f), data.nickName);
            data.lastName = Widgets.TextField(new Rect(row.x + (nameWidth + 10f) * 2f, row.y, nameWidth, 30f), data.lastName);
            this.EndRow(ref y);
            Rect buttonRect = this.DrawRowLabel(ref y, inRect, x, "", 140f);
            if (this.DrawCommandText(new Rect(buttonRect.x, buttonRect.y, 180f, 30f), "CQF_PawnEditor_RandomizeName".Translate()))
            {
                this.RandomizeDefName(pawnDef, data);
            }
            this.EndRow(ref y);
            Widgets.CheckboxLabeled(new Rect(x, y, 260f, 30f), "CQF_PawnEditor_RandomNameOnGeneration".Translate(), ref data.randomName);
            this.EndRow(ref y);
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_NameMaker".Translate(this.ValueOrNone(this.NameMakerLabel(data.nameMaker)))))
            {
                this.OpenNameMakerSelector(pawnDef, data);
            }
            row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_BioAge".Translate(), 140f);
            Widgets.TextFieldNumeric(new Rect(row.x, row.y, 120f, 30f), ref data.bioAge, ref this.bioAgeBuffer);
            this.EndRow(ref y);
            row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_ChronologicalAge".Translate(), 140f);
            Widgets.TextFieldNumeric(new Rect(row.x, row.y, 120f, 30f), ref data.chrAge, ref this.chrAgeBuffer);
            this.EndRow(ref y);
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Gender".Translate(data.gender.ToString().Translate())))
            {
                CQFEditorTools.DrawFloatMenu(new List<Gender> { Gender.None, Gender.Male, Gender.Female }, gender => data.gender = gender, gender => gender.ToString().Translate());
            }
        }

        public override void ModifyGenerationRequest(ComplexPawnDef pawnDef, ref PawnGenerationRequest request)
        {
            PawnModData_NameAndBody data = pawnDef.DataFor<PawnModData_NameAndBody>();
            if (pawnDef.KindDef?.race?.race?.Humanlike != true)
            {
                return;
            }
            request.FixedBiologicalAge = data.bioAge;
            request.FixedChronologicalAge = data.chrAge;
            if (data.gender != Gender.None)
            {
                request.FixedGender = data.gender;
            }
            if (!data.lastName.NullOrEmpty())
            {
                request.SetFixedLastName(data.lastName);
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            PawnModData_NameAndBody data = pawnDef.DataFor<PawnModData_NameAndBody>();
            if (pawn.story == null)
            {
                return;
            }
            if (!data.randomName)
            {
                pawn.Name = new NameTriple(data.firstName, data.nickName, data.lastName);
            }
            else if (!preview && data.nameMaker != null)
            {
                pawn.Name = NameTriple.FromString(NameGenerator.GenerateName(data.nameMaker, (IEnumerable<string>)null, false));
            }
        }

        public override void LoadData(ComplexPawnDef pawnDef, System.Xml.XmlNode node)
        {
            PawnModData_NameAndBody data = pawnDef.DataFor<PawnModData_NameAndBody>();
            data.randomName = ParseHelper.FromString<bool>(node["randomName"]?.InnerText ?? "false");
            data.firstName = node["firstName"]?.InnerText ?? "";
            data.nickName = node["nickName"]?.InnerText ?? "";
            data.lastName = node["lastName"]?.InnerText ?? "";
            data.nameMaker = DefDatabase<RulePackDef>.GetNamedSilentFail(node["nameMaker"]?.InnerText);
            data.gender = node["gender"] == null ? Gender.Male : ParseHelper.FromString<Gender>(node["gender"].InnerText);
            data.bioAge = node["bioAge"] == null ? 14 : ParseHelper.FromString<int>(node["bioAge"].InnerText);
            data.chrAge = node["chrAge"] == null ? 14 : ParseHelper.FromString<int>(node["chrAge"].InnerText);
        }

        private void RandomizeDefName(ComplexPawnDef pawnDef, PawnModData_NameAndBody data)
        {
            RulePackDef maker = data.nameMaker ?? pawnDef.KindDef?.GetNameMaker(data.gender);
            if (maker == null)
            {
                Messages.Message("CQF_PawnEditor_NoNameMaker".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            NameTriple name = NameTriple.FromString(NameGenerator.GenerateName(maker, (IEnumerable<string>)null, false));
            data.firstName = name.First;
            data.nickName = name.NickSet ? name.Nick : "";
            data.lastName = name.Last;
        }

        private string NameMakerLabel(RulePackDef maker)
        {
            return maker == null ? "CQF_PawnEditor_None".Translate() : maker.label ?? maker.defName;
        }

        private void OpenNameMakerSelector(ComplexPawnDef pawnDef, PawnModData_NameAndBody data)
        {
            List<ExtraOption> extraOptions = new List<ExtraOption>
            {
                new ExtraOption("CQF_PawnEditor_None".Translate(), null, () => data.nameMaker = null)
            };
            Find.WindowStack.Add(new Dialog_Select<RulePackDef>(this.PawnNameMakers(pawnDef), null, this.NameMakerLabel, "CQF_PawnEditor_NameMaker".Translate(""), maker =>
            {
                data.nameMaker = maker;
            }, null, null, null, this.NameMakerPriority(pawnDef, data), extraOptions, maker => maker.defName));
        }

        private List<RulePackDef> PawnNameMakers(ComplexPawnDef pawnDef)
        {
            HashSet<RulePackDef> result = new HashSet<RulePackDef>();
            PawnModData_NameAndBody data = pawnDef.DataFor<PawnModData_NameAndBody>();
            this.AddPawnNameMaker(result, pawnDef.KindDef?.GetNameMaker(data.gender));
            foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                this.AddPawnNameMaker(result, kind.nameMaker);
                this.AddPawnNameMaker(result, kind.nameMakerFemale);
            }
            foreach (ThingDef race in DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.race != null))
            {
                this.AddPawnNameMaker(result, race.race.GetNameGenerator(Gender.Male));
                this.AddPawnNameMaker(result, race.race.GetNameGenerator(Gender.Female));
                this.AddPawnNameMaker(result, race.race.GetNameGenerator(Gender.None));
            }
            foreach (CultureDef culture in DefDatabase<CultureDef>.AllDefsListForReading)
            {
                this.AddPawnNameMaker(result, culture.GetPawnNameMaker(Gender.Male));
                this.AddPawnNameMaker(result, culture.GetPawnNameMaker(Gender.Female));
                this.AddPawnNameMaker(result, culture.GetPawnNameMaker(Gender.None));
            }
            foreach (BackstoryDef backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
            {
                this.AddPawnNameMaker(result, backstory.nameMaker);
            }
            if (ModsConfig.BiotechActive)
            {
                foreach (XenotypeDef xenotype in DefDatabase<XenotypeDef>.AllDefsListForReading)
                {
                    this.AddPawnNameMaker(result, xenotype.GetNameMaker(Gender.Male));
                    this.AddPawnNameMaker(result, xenotype.GetNameMaker(Gender.Female));
                    this.AddPawnNameMaker(result, xenotype.GetNameMaker(Gender.None));
                }
            }
            return result.OrderBy(this.NameMakerLabel).ToList();
        }

        private void AddPawnNameMaker(HashSet<RulePackDef> makers, RulePackDef maker)
        {
            if (maker != null)
            {
                makers.Add(maker);
            }
        }

        private Func<RulePackDef, int> NameMakerPriority(ComplexPawnDef pawnDef, PawnModData_NameAndBody data)
        {
            RulePackDef current = data.nameMaker;
            RulePackDef defaultMaker = pawnDef.KindDef?.GetNameMaker(data.gender);
            return maker =>
            {
                if (maker == current)
                {
                    return 0;
                }
                return maker == defaultMaker ? 1 : 10;
            };
        }

        private string bioAgeBuffer;
        private string chrAgeBuffer;
    }
}
