using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ComplexPawnDef : Def, ISaveable
    {
        public Pawn GetPawn()
        {
            if (this.unique && Current.Game?.GetComponent<GameComponent_Editor>() is GameComponent_Editor component
                && component.pawns.TryGetValue(this.defName, out Pawn result))
            {
                return result;
            }
            return this.Spawn();
        }

        public Pawn Spawn()
        {
            return this.CreatePawn(true);
        }

        public Pawn CreatePreviewPawn()
        {
            return this.CreatePawn(false);
        }

        private Pawn CreatePawn(bool cacheUnique)
        {
            if (this.kindDef == null)
            {
                Log.Error("QuestEditorError:Spawn ComplexPawnDef without PawnKindDef");
                return null;
            }
            PawnGenerationRequest request = new PawnGenerationRequest(this.kindDef, this.GetFaction());
            foreach (PawnModDef mod in this.AvailableMods())
            {
                mod.Worker.ModifyGenerationRequest(this, ref request);
            }
            Pawn result = PawnGenerator.GeneratePawn(request);
            this.ApplyModsToPawn(result, false);
            if (cacheUnique && this.unique && Current.Game?.GetComponent<GameComponent_Editor>() is GameComponent_Editor component)
            {
                component.pawns.SetOrAdd(this.defName, result);
            }
            return result;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            this.AvailableMods().ForEach(mod => mod.Worker.SaveData(this, result));
            return result;
        }

        public List<PawnModDef> AvailableMods()
        {
            return DefDatabase<PawnModDef>.AllDefsListForReading
                .Where(def => def.Worker.CanAddFor(this))
                .OrderBy(def => def.order)
                .ToList();
        }

        public void ApplyModsToPawn(Pawn pawn, bool preview)
        {
            foreach (PawnModDef mod in this.AvailableMods())
            {
                mod.Worker.ApplyToPawn(this, pawn, preview);
            }
        }

        public void NotifyPawnSpawned(Pawn pawn, Quest quest)
        {
            foreach (PawnModDef mod in this.AvailableMods())
            {
                mod.Worker.OnPawnSpawned(this, pawn, quest);
            }
        }

        private Faction GetFaction()
        {
            if (this.faction == null)
            {
                return null;
            }
            return this.faction.isPlayer ? Find.FactionManager.OfPlayer : Find.FactionManager.FirstFactionOfDef(this.faction);
        }

        public bool unique;
        public bool randomName;
        public string firstName = "";
        public string nickName = "";
        public string lastName = "";
        public RulePackDef nameMaker;
        public Color? hairColor = Color.white;
        public HairDef hair = HairDefOf.Bald;
        public Color? skinColor = Color.white;
        public HeadTypeDef head;
        public BodyTypeDef bodyType;
        public int bioAge = 14;
        public int chrAge = 14;
        public Gender gender = Gender.None;
        public PawnKindDef kindDef;
        public FactionDef faction;
        public BackstoryDef childhood;
        public BackstoryDef adulthood;
        public List<SkillData> skills = new List<SkillData>();
        public List<TraitData> traits = new List<TraitData>();
        public List<AbilityData> abilities = new List<AbilityData>();
        public List<ThingData> apparels = new List<ThingData>();
        public List<HediffData> hediffs = new List<HediffData>();
        public DialogManagerDef dialogManager;
        public List<PawnActionTriggerData> actionTriggers = new List<PawnActionTriggerData>();
        public ThingData weapon;
    }

    public class PawnModDef : Def
    {
        public PawnModWorker Worker
        {
            get
            {
                if (this.workerInt == null)
                {
                    this.workerInt = (PawnModWorker)Activator.CreateInstance(this.workerClass);
                    this.workerInt.def = this;
                }
                return this.workerInt;
            }
        }

        public string EditorLabel => this.TranslateOrFallback(this.defName + ".label", this.LabelCap);

        public string EditorDescription => this.TranslateOrFallback(this.defName + ".description", this.description);

        public Type workerClass = typeof(PawnModWorker);
        public int order;
        private PawnModWorker workerInt;

        private string TranslateOrFallback(string key, string fallback)
        {
            return key.CanTranslate() ? key.Translate().ToString() : fallback;
        }
    }

    public class PawnModWorker
    {
        public virtual bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return true;
        }

        public virtual void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
        }

        public virtual void ModifyGenerationRequest(ComplexPawnDef pawnDef, ref PawnGenerationRequest request)
        {
        }

        public virtual void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
        }

        public virtual void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
        }

        public virtual void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)
        {
        }

        protected Rect DrawRowLabel(ref float y, Rect inRect, float x, string label, float labelWidth = 150f, float height = 30f)
        {
            Rect labelRect = new Rect(x, y + 3f, labelWidth, 25f);
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.PaleBlue));
            return new Rect(x + labelWidth + 8f, y, Mathf.Max(120f, inRect.width - x - labelWidth - 24f), height);
        }

        protected void EndRow(ref float y, float height = 30f)
        {
            y += height + 8f;
        }

        protected bool DrawTextButton(Rect rect, string label, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            return Widgets.ButtonText(rect, label, false, true, true, anchor);
        }

        protected bool DrawCommandText(Rect rect, string label)
        {
            return this.DrawTextButton(rect, label.Colorize(ColorLibrary.PaleBlue), TextAnchor.MiddleCenter);
        }

        protected bool DrawSelectRow(ref float y, Rect inRect, float x, string label, float height = 30f)
        {
            Rect rect = new Rect(x, y, inRect.width - x - 20f, height);
            bool result = this.DrawTextButton(rect, label);
            this.EndRow(ref y, height);
            return result;
        }

        protected string ValueOrNone(string value)
        {
            return value.NullOrEmpty() ? "CQF_PawnEditor_None".Translate().ToString() : value;
        }

        protected void AddText(XElement root, string name, string value)
        {
            if (!value.NullOrEmpty())
            {
                root.Add(new XElement(name, value));
            }
        }

        protected void AddDef(XElement root, string name, Def value)
        {
            if (value != null)
            {
                root.Add(new XElement(name, value.defName));
            }
        }

        protected void AddColor(XElement root, string name, Color? value)
        {
            if (value != null)
            {
                Color color = value.Value;
                root.Add(new XElement(name, $"({color.r}, {color.g}, {color.b}, {color.a})"));
            }
        }

        public PawnModDef def;
    }

    public class PawnModWorker_Basic : PawnModWorker
    {
        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            root.Add(new XElement("defName", pawnDef.defName));
            this.AddText(root, "label", pawnDef.label);
            if (pawnDef.unique)
            {
                root.Add(new XElement("unique", pawnDef.unique));
            }
            this.AddDef(root, "kindDef", pawnDef.kindDef);
            this.AddDef(root, "faction", pawnDef.faction);
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            Rect row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_DefName".Translate(), 170f);
            pawnDef.defName = Widgets.TextField(new Rect(row.x, row.y, Mathf.Min(360f, row.width), 30f), pawnDef.defName);
            this.EndRow(ref y);
            row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_Label".Translate(), 170f);
            pawnDef.label = Widgets.TextField(new Rect(row.x, row.y, Mathf.Min(360f, row.width), 30f), pawnDef.label);
            this.EndRow(ref y);
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_PawnKind".Translate(this.ValueOrNone(pawnDef.kindDef?.label))))
            {
                Find.WindowStack.Add(new Dialog_Select<PawnKindDef>(DefDatabase<PawnKindDef>.AllDefsListForReading, null, kind => kind.label, "CQF_PawnEditor_SelectPawnKind".Translate(), kind => pawnDef.kindDef = kind));
            }
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Faction".Translate() + this.ValueOrNone(pawnDef.faction?.label)))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<FactionDef>.AllDefsListForReading, faction => pawnDef.faction = faction, faction => faction.label);
            }
        }
    }

    public class PawnModWorker_NameAndBody : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.kindDef == null || pawnDef.kindDef.race.race.Humanlike;
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            Rect row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_Name".Translate(), 140f);
            float nameWidth = Mathf.Min(150f, (row.width - 20f) / 3f);
            pawnDef.firstName = Widgets.TextField(new Rect(row.x, row.y, nameWidth, 30f), pawnDef.firstName);
            pawnDef.nickName = Widgets.TextField(new Rect(row.x + nameWidth + 10f, row.y, nameWidth, 30f), pawnDef.nickName);
            pawnDef.lastName = Widgets.TextField(new Rect(row.x + (nameWidth + 10f) * 2f, row.y, nameWidth, 30f), pawnDef.lastName);
            this.EndRow(ref y);
            Rect buttonRect = this.DrawRowLabel(ref y, inRect, x, "", 140f);
            if (this.DrawCommandText(new Rect(buttonRect.x, buttonRect.y, 180f, 30f), "CQF_PawnEditor_RandomizeName".Translate()))
            {
                this.RandomizeDefName(pawnDef);
            }
            this.EndRow(ref y);
            Widgets.CheckboxLabeled(new Rect(x, y, 260f, 30f), "CQF_PawnEditor_RandomNameOnGeneration".Translate(), ref pawnDef.randomName);
            this.EndRow(ref y);
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_NameMaker".Translate(this.ValueOrNone(this.NameMakerLabel(pawnDef.nameMaker)))))
            {
                this.OpenNameMakerSelector(pawnDef);
            }
            row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_BioAge".Translate(), 140f);
            Widgets.TextFieldNumeric(new Rect(row.x, row.y, 120f, 30f), ref pawnDef.bioAge, ref this.bioAgeBuffer);
            this.EndRow(ref y);
            row = this.DrawRowLabel(ref y, inRect, x, "CQF_PawnEditor_ChronologicalAge".Translate(), 140f);
            Widgets.TextFieldNumeric(new Rect(row.x, row.y, 120f, 30f), ref pawnDef.chrAge, ref this.chrAgeBuffer);
            this.EndRow(ref y);
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Gender".Translate(pawnDef.gender.ToString().Translate())))
            {
                CQFEditorTools.DrawFloatMenu(new List<Gender> { Gender.None, Gender.Male, Gender.Female }, gender => pawnDef.gender = gender, gender => gender.ToString().Translate());
            }
        }

        public override void ModifyGenerationRequest(ComplexPawnDef pawnDef, ref PawnGenerationRequest request)
        {
            if (pawnDef.kindDef?.race?.race?.Humanlike != true)
            {
                return;
            }
            request.FixedBiologicalAge = pawnDef.bioAge;
            request.FixedChronologicalAge = pawnDef.chrAge;
            if (pawnDef.gender != Gender.None)
            {
                request.FixedGender = pawnDef.gender;
            }
            if (!pawnDef.lastName.NullOrEmpty())
            {
                request.SetFixedLastName(pawnDef.lastName);
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.story == null)
            {
                return;
            }
            if (!pawnDef.randomName)
            {
                pawn.Name = new NameTriple(pawnDef.firstName, pawnDef.nickName, pawnDef.lastName);
            }
            else if (!preview && pawnDef.nameMaker != null)
            {
                pawn.Name = NameTriple.FromString(NameGenerator.GenerateName(pawnDef.nameMaker, (IEnumerable<string>)null, false));
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            if (pawnDef.randomName)
            {
                root.Add(new XElement("randomName", pawnDef.randomName));
            }
            this.AddText(root, "firstName", pawnDef.firstName);
            this.AddText(root, "nickName", pawnDef.nickName);
            this.AddText(root, "lastName", pawnDef.lastName);
            this.AddDef(root, "nameMaker", pawnDef.nameMaker);
            if (pawnDef.gender != Gender.None)
            {
                root.Add(new XElement("gender", pawnDef.gender));
            }
            if (pawnDef.bioAge != 14)
            {
                root.Add(new XElement("bioAge", pawnDef.bioAge));
            }
            if (pawnDef.chrAge != 14)
            {
                root.Add(new XElement("chrAge", pawnDef.chrAge));
            }
        }

        private void RandomizeDefName(ComplexPawnDef pawnDef)
        {
            RulePackDef maker = pawnDef.nameMaker ?? pawnDef.kindDef?.GetNameMaker(pawnDef.gender);
            if (maker == null)
            {
                Messages.Message("CQF_PawnEditor_NoNameMaker".Translate(), MessageTypeDefOf.CautionInput);
                return;
            }
            NameTriple name = NameTriple.FromString(NameGenerator.GenerateName(maker, (IEnumerable<string>)null, false));
            pawnDef.firstName = name.First;
            pawnDef.nickName = name.NickSet ? name.Nick : "";
            pawnDef.lastName = name.Last;
        }

        private string NameMakerLabel(RulePackDef maker)
        {
            return maker == null ? "CQF_PawnEditor_None".Translate() : maker.label ?? maker.defName;
        }

        private void OpenNameMakerSelector(ComplexPawnDef pawnDef)
        {
            List<ExtraOption> extraOptions = new List<ExtraOption>
            {
                new ExtraOption("CQF_PawnEditor_None".Translate(), null, () => pawnDef.nameMaker = null)
            };
            Find.WindowStack.Add(new Dialog_Select<RulePackDef>(this.PawnNameMakers(pawnDef), null, this.NameMakerLabel, "CQF_PawnEditor_NameMaker".Translate(""), maker =>
            {
                pawnDef.nameMaker = maker;
            }, null, null, null, this.NameMakerPriority(pawnDef), extraOptions, maker => maker.defName));
        }

        private List<RulePackDef> PawnNameMakers(ComplexPawnDef pawnDef)
        {
            HashSet<RulePackDef> result = new HashSet<RulePackDef>();
            this.AddPawnNameMaker(result, pawnDef.kindDef?.GetNameMaker(pawnDef.gender));
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

        private Func<RulePackDef, int> NameMakerPriority(ComplexPawnDef pawnDef)
        {
            RulePackDef current = pawnDef.nameMaker;
            RulePackDef defaultMaker = pawnDef.kindDef?.GetNameMaker(pawnDef.gender);
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

    public class PawnModWorker_Appearance : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.kindDef == null || pawnDef.kindDef.race.race.Humanlike;
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_Hair".Translate(this.ValueOrNone(pawnDef.hair?.label))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HairDef>.AllDefsListForReading, hair => pawnDef.hair = hair, hair => hair.label);
            }
            this.DrawColorRow(ref y, inRect, x, "CQF_PawnEditor_SelectHairColor".Translate(), pawnDef.hairColor ?? Color.white, color => pawnDef.hairColor = this.Opaque(color));
            this.DrawColorRow(ref y, inRect, x, "CQF_PawnEditor_SelectSkinColor".Translate(), pawnDef.skinColor ?? Color.white, color => pawnDef.skinColor = this.Opaque(color));
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_HeadType".Translate(this.ValueOrNone(pawnDef.head?.defName))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HeadTypeDef>.AllDefsListForReading, head => pawnDef.head = head, head => head.defName);
            }
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_BodyType".Translate(this.ValueOrNone(this.BodyTypeLabel(pawnDef.bodyType)))))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<BodyTypeDef>.AllDefsListForReading, body => pawnDef.bodyType = body, this.BodyTypeLabel);
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.story == null)
            {
                return;
            }
            pawn.story.hairDef = pawnDef.hair ?? pawn.story.hairDef;
            pawn.story.headType = pawnDef.head ?? pawn.story.headType;
            pawn.story.bodyType = pawnDef.bodyType ?? pawn.story.bodyType;
            if (pawnDef.hairColor != null)
            {
                pawn.story.HairColor = pawnDef.hairColor.Value;
            }
            if (pawnDef.skinColor != null)
            {
                pawn.story.skinColorOverride = pawnDef.skinColor.Value;
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            this.AddDef(root, "hair", pawnDef.hair);
            this.AddDef(root, "head", pawnDef.head);
            this.AddDef(root, "bodyType", pawnDef.bodyType);
            this.AddColor(root, "hairColor", pawnDef.hairColor);
            this.AddColor(root, "skinColor", pawnDef.skinColor);
        }

        private void DrawColorRow(ref float y, Rect inRect, float x, string label, Color color, Action<Color> apply)
        {
            Rect rect = new Rect(x, y, inRect.width - x - 20f, 30f);
            if (this.DrawTextButton(rect, label))
            {
                this.OpenColorDialog(label, color, apply);
            }
            this.DrawColorSwatch(new Rect(rect.xMax - 32f, rect.y + 3f, 24f, 24f), color);
            this.EndRow(ref y);
        }

        private void DrawColorButton(Rect rect, string label, Color color, Action<Color> apply)
        {
            if (this.DrawTextButton(rect, label))
            {
                this.OpenColorDialog(label, color, apply);
            }
        }

        private void OpenColorDialog(string label, Color color, Action<Color> apply)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("CQF_PawnEditor_ColorLibrary".Translate(), () => Find.WindowStack.Add(new Dialog_ChooseColor(label, color, DefDatabase<ColorDef>.AllDefsListForReading.Select(def => def.color).ToList(), apply))),
                new FloatMenuOption("CQF_PawnEditor_HexColor".Translate(), () => Find.WindowStack.Add(new Dialog_RGB(color, apply)))
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawColorSwatch(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(rect, color);
            Widgets.DrawBox(rect);
        }

        private Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        private string BodyTypeLabel(BodyTypeDef bodyType)
        {
            if (bodyType == null)
            {
                return null;
            }
            return bodyType.defName.CanTranslate() ? bodyType.defName.Translate().ToString() : bodyType.defName;
        }
    }

    public class PawnModWorker_Backstory : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.kindDef == null || pawnDef.kindDef.race.race.Humanlike;
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            this.DrawBackstoryButton(ref y, inRect, x, "CQF_PawnEditor_Childhood".Translate(this.ValueOrNone(pawnDef.childhood?.title)), backstory => pawnDef.childhood = backstory);
            this.DrawBackstoryButton(ref y, inRect, x, "CQF_PawnEditor_Adulthood".Translate(this.ValueOrNone(pawnDef.adulthood?.title)), backstory => pawnDef.adulthood = backstory);
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.story == null)
            {
                return;
            }
            if (pawnDef.childhood != null)
            {
                pawn.story.Childhood = pawnDef.childhood;
            }
            if (pawnDef.adulthood != null)
            {
                pawn.story.Adulthood = pawnDef.adulthood;
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            this.AddDef(root, "childhood", pawnDef.childhood);
            this.AddDef(root, "adulthood", pawnDef.adulthood);
        }

        private void DrawBackstoryButton(ref float y, Rect inRect, float x, string label, Action<BackstoryDef> action)
        {
            if (this.DrawSelectRow(ref y, inRect, x, label))
            {
                Find.WindowStack.Add(new Dialog_Select<BackstoryDef>(DefDatabase<BackstoryDef>.AllDefsListForReading, null, backstory => backstory.title, "CQF_PawnEditor_Select".Translate(), action));
            }
        }
    }

    public class PawnModWorker_Traits : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.kindDef == null || pawnDef.kindDef.race.race.Humanlike;
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (this.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                this.OpenTraitSelector(data => pawnDef.traits.Add(data));
            }
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (this.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && pawnDef.traits.Any())
            {
                CQFEditorTools.DrawFloatMenu(pawnDef.traits, data => pawnDef.traits.Remove(data), data => data.def?.DataAtDegree(data.degree)?.label ?? "CQF_PawnEditor_None".Translate());
            }
            y += 42f;
            foreach (TraitData data in pawnDef.traits)
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                Rect traitRect = new Rect(row.x + 8f, row.y + 3f, Mathf.Max(220f, row.width - 190f), 30f);
                if (this.DrawTextButton(traitRect, data.def?.DataAtDegree(data.degree)?.label ?? "CQF_PawnEditor_None".Translate()))
                {
                    this.OpenTraitSelector(newData =>
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

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.story?.traits == null || pawnDef.traits.NullOrEmpty())
            {
                return;
            }
            foreach (Trait trait in pawn.story.traits.allTraits.ToList())
            {
                pawn.story.traits.RemoveTrait(trait);
            }
            foreach (TraitData data in pawnDef.traits)
            {
                if (data?.def != null && (preview || Rand.Chance(data.chance)))
                {
                    pawn.story.traits.GainTrait(new Trait(data.def, data.degree));
                }
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            if (!pawnDef.traits.NullOrEmpty())
            {
                root.Add(CQFEditorTools.SaveList_Saveable(pawnDef.traits, "traits"));
            }
        }

        private void OpenTraitSelector(Action<TraitData> action)
        {
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t => t.degreeDatas.ForEach(s => stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s))));
            Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "CQF_PawnEditor_Select".Translate(), t =>
            {
                action(new TraitData() { def = t.Key, degree = t.Value.degree, chance = 1f });
            }));
        }
    }

    public class PawnModWorker_Skills : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.kindDef?.race?.race?.Humanlike ?? false;
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading.OrderBy(def => def.listOrder))
            {
                SkillData data = this.DataFor(pawnDef.skills, skill);
                Rect row = new Rect(x, y, Mathf.Min(360f, inRect.width - x - 20f), 26f);
                this.DrawSkillRow(skill, data, row);
                y += 30f;
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.skills == null)
            {
                return;
            }
            foreach (SkillData data in pawnDef.skills)
            {
                if (data?.def == null || pawn.skills.GetSkill(data.def) is not SkillRecord record)
                {
                    continue;
                }
                record.Level = Mathf.Clamp(data.level, 0, 20);
                record.passion = data.passion;
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            if (!pawnDef.skills.NullOrEmpty())
            {
                root.Add(CQFEditorTools.SaveList_Saveable(pawnDef.skills, "skills"));
            }
        }

        private SkillData DataFor(List<SkillData> list, SkillDef skill)
        {
            SkillData result = list.FirstOrDefault(data => data.def == skill);
            if (result == null)
            {
                result = new SkillData { def = skill };
                list.Add(result);
            }
            return result;
        }

        private void DrawSkillRow(SkillDef skill, SkillData data, Rect row)
        {
            if (Mouse.IsOver(row))
            {
                GUI.DrawTexture(row, TexUI.HighlightTex);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(row.x + 6f, row.y, 100f, row.height);
            Widgets.Label(labelRect, skill.skillLabel.CapitalizeFirst().Colorize(ColorLibrary.PaleBlue));

            Rect passionRect = new Rect(labelRect.xMax, row.y + 1f, 24f, 24f);
            Widgets.DrawLightHighlight(passionRect);
            Widgets.DrawBox(passionRect, 1);
            Widgets.DrawHighlightIfMouseover(passionRect);
            if (data.passion == Passion.Minor)
            {
                GUI.DrawTexture(passionRect, SkillUI.PassionMinorIcon);
            }
            else if (data.passion == Passion.Major)
            {
                GUI.DrawTexture(passionRect, SkillUI.PassionMajorIcon);
            }
            if (Widgets.ButtonInvisible(passionRect))
            {
                CQFEditorTools.DrawFloatMenu(this.Passions, passion => data.passion = passion, this.PassionLabel);
            }
            TooltipHandler.TipRegion(passionRect, "CQF_PawnEditor_SkillPassion".Translate(this.PassionLabel(data.passion)));

            Rect barRect = new Rect(passionRect.xMax + 4f, row.y + 1f, row.xMax - passionRect.xMax - 10f, 24f);
            if (Mouse.IsOver(barRect))
            {
                this.UpdateLevelByMouse(data, barRect);
            }
            Widgets.FillableBar(barRect, Mathf.Max(0.01f, Mathf.Clamp(data.level, 0, 20) / 20f), this.SkillBarFillTex, null, false);
            Widgets.Label(new Rect(barRect.x + 6f, barRect.y + 2f, 40f, 20f), Mathf.Clamp(data.level, 0, 20).ToStringCached());
            TooltipHandler.TipRegion(barRect, "CQF_PawnEditor_SkillLevel".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void UpdateLevelByMouse(SkillData data, Rect barRect)
        {
            UnityEngine.Event current = UnityEngine.Event.current;
            if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag)
            {
                return;
            }
            if (current.button != 0)
            {
                return;
            }
            float percent = Mathf.Clamp01((current.mousePosition.x - barRect.x) / barRect.width);
            data.level = Mathf.Clamp(Mathf.RoundToInt(percent * 20f), 0, 20);
            data.levelBuffer = data.level.ToString();
            current.Use();
        }

        private string PassionLabel(Passion passion)
        {
            return ("Passion" + passion).Translate();
        }

        private List<Passion> Passions => new List<Passion> { Passion.None, Passion.Minor, Passion.Major };

        private readonly Texture2D SkillBarFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.12f));
    }

    public class PawnModWorker_Abilities : PawnModWorker
    {
        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (this.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                this.OpenAbilitySelector(ability => pawnDef.abilities.Add(new AbilityData { def = ability }));
            }
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (this.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && pawnDef.abilities.Any())
            {
                CQFEditorTools.DrawFloatMenu(pawnDef.abilities, data => pawnDef.abilities.Remove(data), this.AbilityLabel);
            }
            y += 42f;
            this.RemoveDuplicates(pawnDef.abilities);
            foreach (AbilityData data in pawnDef.abilities)
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
            this.RemoveDuplicates(pawnDef.abilities);
            HashSet<AbilityDef> desired = pawnDef.abilities.Where(data => data?.def != null).Select(data => data.def).ToHashSet();
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

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            this.RemoveDuplicates(pawnDef.abilities);
            if (!pawnDef.abilities.NullOrEmpty())
            {
                root.Add(CQFEditorTools.SaveList_Saveable(pawnDef.abilities, "abilities"));
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

    public class PawnModWorker_Apparel : PawnModWorker
    {
        public override bool CanAddFor(ComplexPawnDef pawnDef)
        {
            return pawnDef.kindDef == null || pawnDef.kindDef.race.race.Humanlike;
        }

        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            this.RemoveDuplicateLayers(pawnDef.apparels);
            foreach (ApparelLayerDef layer in this.AvailableLayers())
            {
                Rect row = new Rect(x, y, inRect.width - x - 20f, 36f);
                Widgets.DrawLightHighlight(row);
                ThingData data = this.ApparelForLayer(pawnDef.apparels, layer);
                Rect layerRect = new Rect(row.x + 8f, row.y + 6f, 120f, 24f);
                Widgets.Label(layerRect, this.LayerLabel(layer).Colorize(ColorLibrary.PaleBlue));
                Rect iconRect = new Rect(layerRect.xMax + 8f, row.y + 4f, 28f, 28f);
                if (data?.def?.uiIcon != null)
                {
                    Widgets.DrawTextureFitted(iconRect, data.def.uiIcon, 1f);
                }
                Rect buttonRect = new Rect(iconRect.xMax + 8f, row.y + 3f, row.width - iconRect.width - layerRect.width - 32f, 30f);
                if (this.DrawTextButton(buttonRect, this.ThingLabel(data)))
                {
                    this.OpenLayerSelectDialog(pawnDef.apparels, layer);
                }
                y += 42f;
            }
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.apparel == null)
            {
                return;
            }
            pawn.apparel.DestroyAll();
            this.RemoveDuplicateLayers(pawnDef.apparels);
            foreach (ThingData data in pawnDef.apparels)
            {
                if (data?.def == null)
                {
                    continue;
                }
                Apparel apparel = ThingMaker.MakeThing(data.def, this.StuffFor(data.def, data.stuff)) as Apparel;
                if (apparel != null)
                {
                    pawn.apparel.Wear(apparel);
                }
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            this.RemoveDuplicateLayers(pawnDef.apparels);
            if (!pawnDef.apparels.NullOrEmpty())
            {
                root.Add(CQFEditorTools.SaveList_Saveable(pawnDef.apparels, "apparels"));
            }
        }

        private void OpenLayerSelectDialog(List<ThingData> apparels, ApparelLayerDef layer)
        {
            List<ThingDef> defs = new List<ThingDef> { null };
            defs.AddRange(DefDatabase<ThingDef>.AllDefsListForReading.Where(def => this.ApparelInLayer(def, layer)));
            ThingData data = this.ApparelForLayer(apparels, layer) ?? new ThingData();
            this.OpenSelectDialog(data, defs, () => this.SetLayerApparel(apparels, layer, data));
        }

        private void OpenSelectDialog(ThingData data, List<ThingDef> defs, Action onSelected = null)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(defs, def => def?.uiIcon, def => def?.label ?? "CQF_PawnEditor_None".Translate().ToString(), "CQF_PawnEditor_Select".Translate(), def =>
            {
                if (def == null)
                {
                    data.def = null;
                    data.stuff = null;
                    onSelected?.Invoke();
                    return;
                }
                if (def.MadeFromStuff)
                {
                    this.OpenStuffDialog(data, def, onSelected);
                    return;
                }
                this.SetThingData(data, def, null);
                onSelected?.Invoke();
            }, def => def?.graphic?.Color ?? Color.white));
        }

        private List<ApparelLayerDef> AvailableLayers()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsApparel && !def.apparel.layers.NullOrEmpty())
                .Select(def => def.apparel.LastLayer)
                .Distinct()
                .OrderBy(layer => layer.drawOrder)
                .ThenBy(layer => layer.defName)
                .ToList();
        }

        private bool ApparelInLayer(ThingDef def, ApparelLayerDef layer)
        {
            return def != null && def.IsApparel && def.apparel?.LastLayer == layer;
        }

        private ThingData ApparelForLayer(List<ThingData> apparels, ApparelLayerDef layer)
        {
            return apparels?.FirstOrDefault(data => this.ApparelInLayer(data?.def, layer));
        }

        private void SetLayerApparel(List<ThingData> apparels, ApparelLayerDef layer, ThingData data)
        {
            apparels.RemoveAll(item => item == data || this.ApparelInLayer(item?.def, layer));
            if (data?.def != null)
            {
                apparels.Add(data);
            }
        }

        private void ClearLayer(List<ThingData> apparels, ApparelLayerDef layer)
        {
            apparels.RemoveAll(data => this.ApparelInLayer(data?.def, layer));
        }

        private void RemoveDuplicateLayers(List<ThingData> apparels)
        {
            HashSet<ApparelLayerDef> layers = new HashSet<ApparelLayerDef>();
            for (int i = apparels.Count - 1; i >= 0; i--)
            {
                ApparelLayerDef layer = apparels[i]?.def?.apparel?.LastLayer;
                if (layer == null || !layers.Add(layer))
                {
                    apparels.RemoveAt(i);
                }
            }
        }

        private string LayerLabel(ApparelLayerDef layer)
        {
            return layer.label.NullOrEmpty() ? layer.defName : layer.label;
        }

        private void OpenStuffDialog(ThingData data, ThingDef def, Action onSelected)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(def).ToList(), stuff => stuff.uiIcon, stuff => stuff.label, "CQF_PawnEditor_SelectStuff".Translate(), stuff =>
            {
                this.SetThingData(data, def, stuff);
                onSelected?.Invoke();
            }, stuff => stuff.graphic?.Color ?? Color.white));
        }

        private void SetThingData(ThingData data, ThingDef def, ThingDef stuff)
        {
            data.def = def;
            data.hitPoint = def.BaseMaxHitPoints;
            data.stuff = def.MadeFromStuff ? stuff : null;
        }

        private string ThingLabel(ThingData data)
        {
            if (data?.def == null)
            {
                return "CQF_PawnEditor_None".Translate();
            }
            if (data.def.MadeFromStuff && data.stuff != null)
            {
                return data.def.label + " - " + data.stuff.label;
            }
            return data.def.label;
        }

        private ThingDef StuffFor(ThingDef def, ThingDef stuff)
        {
            return def.MadeFromStuff ? stuff ?? GenStuff.DefaultStuffFor(def) : null;
        }
    }

    public class PawnModWorker_Weapon : PawnModWorker
    {
        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            if (pawnDef.weapon == null)
            {
                pawnDef.weapon = new ThingData();
            }
            Rect row = new Rect(x, y, inRect.width - x - 20f, 30f);
            Rect iconRect = new Rect(row.x, row.y + 3f, 24f, 24f);
            if (pawnDef.weapon.def?.uiIcon != null)
            {
                Widgets.DrawTextureFitted(iconRect, pawnDef.weapon.def.uiIcon, 1f);
            }
            if (this.DrawTextButton(new Rect(iconRect.xMax + 8f, row.y, row.width - 120f, 30f), "CQF_PawnEditor_Weapon".Translate(this.ThingLabel(pawnDef.weapon))))
            {
                this.OpenSelectDialog(pawnDef.weapon);
            }
            if (this.DrawCommandText(new Rect(row.xMax - 100f, row.y, 100f, 30f), "CQF_PawnEditor_Delete".Translate()))
            {
                pawnDef.weapon = null;
            }
            this.EndRow(ref y);
        }

        public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview)
        {
            if (pawn.equipment == null)
            {
                return;
            }
            pawn.equipment.DestroyAllEquipment();
            if (pawnDef.weapon?.def == null)
            {
                return;
            }
            ThingWithComps equipment = ThingMaker.MakeThing(pawnDef.weapon.def, this.StuffFor(pawnDef.weapon.def, pawnDef.weapon.stuff)) as ThingWithComps;
            if (equipment != null)
            {
                pawn.equipment.AddEquipment(equipment);
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            if (pawnDef.weapon != null)
            {
                root.Add(pawnDef.weapon.SaveToXElement("weapon"));
            }
        }

        private void OpenSelectDialog(ThingData data)
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsWeapon).ToList();
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(defs, def => def.uiIcon, def => def.label, "CQF_PawnEditor_Select".Translate(), def =>
            {
                if (def.MadeFromStuff)
                {
                    this.OpenStuffDialog(data, def);
                    return;
                }
                this.SetThingData(data, def, null);
            }, def => def.graphic?.Color ?? Color.white));
        }

        private void OpenStuffDialog(ThingData data, ThingDef def)
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(GenStuff.AllowedStuffsFor(def).ToList(), stuff => stuff.uiIcon, stuff => stuff.label, "CQF_PawnEditor_SelectStuff".Translate(), stuff =>
            {
                this.SetThingData(data, def, stuff);
            }, stuff => stuff.graphic?.Color ?? Color.white));
        }

        private void SetThingData(ThingData data, ThingDef def, ThingDef stuff)
        {
            data.def = def;
            data.hitPoint = def.BaseMaxHitPoints;
            data.stuff = def.MadeFromStuff ? stuff : null;
        }

        private string ThingLabel(ThingData data)
        {
            if (data?.def == null)
            {
                return "CQF_PawnEditor_None".Translate();
            }
            if (data.def.MadeFromStuff && data.stuff != null)
            {
                return data.def.label + " - " + data.stuff.label;
            }
            return data.def.label;
        }

        private ThingDef StuffFor(ThingDef def, ThingDef stuff)
        {
            return def.MadeFromStuff ? stuff ?? GenStuff.DefaultStuffFor(def) : null;
        }
    }

    public class PawnModWorker_Hediff : PawnModWorker
    {
        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            Rect addRect = new Rect(x, y, 120f, 30f);
            if (this.DrawCommandText(addRect, "CQF_PawnEditor_Add".Translate()))
            {
                this.OpenHediffSelector(data => pawnDef.hediffs.Add(data));
            }
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 120f, 30f);
            if (this.DrawCommandText(deleteRect, "CQF_PawnEditor_Delete".Translate()) && pawnDef.hediffs.Any())
            {
                CQFEditorTools.DrawFloatMenu(pawnDef.hediffs, data => pawnDef.hediffs.Remove(data), this.HediffLabel);
            }
            y += 42f;
            foreach (HediffData data in pawnDef.hediffs)
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
            foreach (HediffData data in pawnDef.hediffs)
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

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            if (!pawnDef.hediffs.NullOrEmpty())
            {
                root.Add(CQFEditorTools.SaveList_Saveable(pawnDef.hediffs, "hediffs"));
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
            return pawnDef.kindDef?.race?.race?.body?.AllParts
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

    public class PawnModWorker_Dialog : PawnModWorker
    {
        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            if (this.DrawSelectRow(ref y, inRect, x, "CQF_PawnEditor_DialogManager".Translate(this.ValueOrNone(pawnDef.dialogManager?.defName))))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("CQF_PawnEditor_None".Translate(), () => pawnDef.dialogManager = null)
                };
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, manager => pawnDef.dialogManager = manager, manager => manager.defName, options);
            }
        }

        public override void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)
        {
            if (pawnDef.dialogManager != null && pawn != null)
            {
                Current.Game?.GetComponent<GameComponent_Editor>()?.AddDialog(pawn, pawnDef.dialogManager);
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            this.AddDef(root, "dialogManager", pawnDef.dialogManager);
        }
    }

    public class PawnModWorker_ActionTrigger : PawnModWorker
    {
        public override void Draw(ComplexPawnDef pawnDef, ref float y, Rect inRect, float x)
        {
            Rect addRect = new Rect(x, y, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                pawnDef.actionTriggers.Add(new PawnActionTriggerData { key = pawnDef.defName + "_Damaged" });
            }
            TooltipHandler.TipRegion(addRect, "CQF_PawnEditor_Add".Translate());
            Rect deleteRect = new Rect(addRect.xMax + 10f, y, 28f, 28f);
            if (Widgets.ButtonImage(deleteRect, TexButton.Delete) && pawnDef.actionTriggers.Any())
            {
                CQFEditorTools.DrawFloatMenu(pawnDef.actionTriggers, data => pawnDef.actionTriggers.Remove(data), this.TriggerLabel);
            }
            TooltipHandler.TipRegion(deleteRect, "CQF_PawnEditor_Delete".Translate());
            y += 42f;
            foreach (PawnActionTriggerData data in pawnDef.actionTriggers)
            {
                float panelHeight = this.TriggerPanelHeight(data);
                Rect panelRect = new Rect(x, y, inRect.width - x - 20f, panelHeight);
                Widgets.DrawLightHighlight(panelRect);
                Widgets.DrawBox(panelRect, 1, QuestEditor_Dialog.blueTex);
                Rect keyRect = new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 30f);
                Widgets.Label(new Rect(keyRect.x, keyRect.y + 3f, 110f, 24f), "CQF_PawnEditor_TriggerKey".Translate().Colorize(ColorLibrary.PaleBlue));
                data.key = Widgets.TextField(new Rect(keyRect.x + 118f, keyRect.y, keyRect.width - 118f, 30f), data.key);
                Rect modeRect = new Rect(panelRect.x + 10f, keyRect.yMax + 6f, panelRect.width - 20f, 30f);
                if (this.DrawTextButton(modeRect, "CQF_PawnEditor_TriggerMode".Translate(this.ModeLabel(data.mode))))
                {
                    CQFEditorTools.DrawFloatMenu(this.AllowedModes, mode => data.mode = mode, this.ModeLabel);
                }
                this.DrawActions(data, modeRect.yMax + 8f, panelRect);
                y += panelHeight + 10f;
            }
        }

        public override void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest)
        {
            if (pawn?.Map == null)
            {
                return;
            }
            MapComponent_CustomMapData comp = MapComponent_CustomMapData.GetComp(pawn.Map);
            foreach (PawnActionTriggerData data in pawnDef.actionTriggers)
            {
                if (data == null || data.key.NullOrEmpty())
                {
                    continue;
                }
                ThingActionTrigger trigger = comp.Triggers.Find(t => t.key == data.key);
                if (trigger == null)
                {
                    trigger = new ThingActionTrigger { key = data.key };
                    comp.Triggers.Add(trigger);
                }
                trigger.mode = data.mode;
                trigger.actions = data.actions.ListFullCopy();
                if (!trigger.things.Contains(pawn))
                {
                    trigger.things.Add(pawn);
                }
            }
        }

        public override void SaveData(ComplexPawnDef pawnDef, XElement root)
        {
            if (!pawnDef.actionTriggers.NullOrEmpty())
            {
                root.Add(CQFEditorTools.SaveList_Saveable(pawnDef.actionTriggers, "actionTriggers"));
            }
        }

        private string TriggerLabel(PawnActionTriggerData data)
        {
            return data?.key.NullOrEmpty() ?? true ? "CQF_PawnEditor_None".Translate() : data.key;
        }

        private string ModeLabel(ActionTriggerMode mode)
        {
            return ("ActionTriggerMode_" + mode).Translate();
        }

        private void DrawActions(PawnActionTriggerData data, float y, Rect panelRect)
        {
            Rect labelRect = new Rect(panelRect.x + 10f, y + 3f, 255f, 24f);
            string label = "CQF_PawnEditor_TriggerActions".Translate();
            Widgets.Label(labelRect, label.Colorize(ColorLibrary.PaleBlue));
            float buttonX = labelRect.x + Text.CalcSize(label).x + 14f;
            Rect addRect = new Rect(buttonX, y, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                CQFEditorTools.OpenCQFActionSelect(type => data.actions.Add((CQFAction)Activator.CreateInstance(type)));
            }
            TooltipHandler.TipRegion(addRect, "CQF_PawnEditor_Add".Translate());
            Rect deleteRect = new Rect(addRect.xMax + 8f, y, 28f, 28f);
            if (Widgets.ButtonImage(deleteRect, TexButton.Delete) && data.actions.Any())
            {
                CQFEditorTools.DrawFloatMenu(data.actions, action => data.actions.Remove(action), this.ActionLabel);
            }
            TooltipHandler.TipRegion(deleteRect, "CQF_PawnEditor_Delete".Translate());

            float actionY = y + 34f;
            foreach (CQFAction action in data.actions)
            {
                Rect actionRect = new Rect(panelRect.x + 14f, actionY, panelRect.width - 28f, 26f);
                if (Widgets.ButtonText(actionRect, this.ActionLabel(action), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                }
                actionY += 30f;
            }
        }

        private float TriggerPanelHeight(PawnActionTriggerData data)
        {
            return 122f + (data.actions?.Count ?? 0) * 30f;
        }

        private string ActionLabel(CQFAction action)
        {
            return action == null ? "CQF_PawnEditor_None".Translate() : action.GetType().Name.Translate();
        }

        private List<ActionTriggerMode> AllowedModes => new List<ActionTriggerMode> { ActionTriggerMode.Damaged };
    }

    public class TraitData : ISaveable
    {
        public static void DrawList(List<TraitData> list, ref float y, string title = null, string tip = null, bool needBox = false, float x = 10f, float defaultWidth = 180f)
        {
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t =>
            {
                t.degreeDatas.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s));
                });
            });
            float initY = y;
            float width = defaultWidth;
            if (title != null)
            {
                y += 5f;
                Text.Font = GameFont.Medium;
                Rect rectTitle = new Rect(x + 10, y, 1020f, 35f);
                Widgets.Label(rectTitle, title);
                if (tip != null)
                {
                    TooltipHandler.TipRegionByKey(rectTitle, tip);
                }
                Text.Font = GameFont.Small;
                y += 40f;
                float textWidth = Text.CalcSize(title).x + 20f;
                width = textWidth > width ? textWidth : width;
            }
            for (int i = 0; i < list.Count; i++)
            {
                TraitData data = list[i];
                CQFEditorTools.DrawSelectablePercent(y, data.def?.DataAtDegree(data.degree)?.label, ref data.chance, ref data.buffer, () =>
                    Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "CQF_PawnEditor_Select".Translate(), t =>
                    {
                        data.def = t.Key;
                        data.degree = t.Value.degree;
                    })), x + 5f);
                y += 30f;
            }
            y += 5f;
            if (needBox)
            {
                Widgets.DrawBox(new Rect(x, initY, width, y - initY), 1, QuestEditor_Dialog.blueTex);
            }
            y += 10f;
            CQFEditorTools.DrawButtonForList(ref y, list, t => t.def?.DataAtDegree(t.degree)?.label,
                () => Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "CQF_PawnEditor_Select".Translate(), t =>
                {
                    list.Add(new TraitData() { def = t.Key, degree = t.Value.degree, chance = 1f });
                })), x - 5f, width - 70f, new Vector2(70f, 25f));
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            result.Add(new XElement("degree", this.degree));
            result.Add(new XElement("chance", this.chance));
            return result;
        }

        public TraitDef def;
        public int degree;
        public float chance = 1f;
        public string buffer;
    }

    public class SkillData : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            result.Add(new XElement("level", this.level));
            result.Add(new XElement("passion", this.passion));
            return result;
        }

        public SkillDef def;
        public int level;
        public Passion passion = Passion.None;
        public string levelBuffer;
    }

    public class AbilityData : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            return result;
        }

        public AbilityDef def;
    }

    public class HediffData : ISaveable
    {
        public void SetPart(ComplexPawnDef pawnDef, BodyPartRecord record)
        {
            this.part = record?.def;
            this.partLabel = record?.untranslatedCustomLabel;
            this.partIndex = record == null ? -1 : pawnDef.kindDef?.race?.race?.body?.AllParts.IndexOf(record) ?? -1;
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("def", this.def?.defName));
            result.Add(new XElement("part", this.part?.defName));
            result.Add(new XElement("partLabel", this.partLabel));
            result.Add(new XElement("partIndex", this.partIndex));
            result.Add(new XElement("severity", this.severity));
            return result;
        }

        public HediffDef def;
        public BodyPartDef part;
        public string partLabel;
        public int partIndex = -1;
        public float severity = 1f;
        public string buffer;
    }

    public class PawnActionTriggerData : ISaveable
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("key", this.key));
            result.Add(new XElement("mode", this.mode));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }

        public string key;
        public ActionTriggerMode mode = ActionTriggerMode.Damaged;
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
