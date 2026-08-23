using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditQuestBookObjective : Window
    {
        public Dialog_EditQuestBookObjective(QuestBookObjective objective, System.Action<QuestBookObjective> replaceAction = null)
        {
            this.objective = objective;
            this.replaceAction = replaceAction;
            if (objective.labelKey.CanTranslate())
            {
                objective.labelKey = objective.labelKey.Translate().ToString();
            }
            if (objective.descriptionKey.CanTranslate())
            {
                objective.descriptionKey = objective.descriptionKey.Translate().ToString();
            }
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            float contentWidth = inRect.width - 24f;
            float contentHeight = CalculateContentHeight();
            Rect outRect = new Rect(0f, 0f, inRect.width, inRect.height);
            Rect viewRect = new Rect(0f, 0f, contentWidth, Mathf.Max(contentHeight, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float y = 8f;
            DrawHeader(ref y, contentWidth);
            DrawIdentitySection(ref y, contentWidth);
            DrawIconSection(ref y, contentWidth);
            DrawDetectionSection(ref y, contentWidth);
            DrawRuleSection(ref y, contentWidth);
            Widgets.EndScrollView();
        }

        private void DrawHeader(ref float y, float width)
        {
            Widgets.Label(new Rect(12f, y, width - 24f, 32f), "CQF_QuestBook_ObjectiveEditor".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 38f;
        }

        private void DrawIdentitySection(ref float y, float width)
        {
            Rect card = BeginSection(ref y, width, IdentitySectionHeight, "CQF_QuestBook_ObjectiveBasic");
            float rowY = card.y + SectionHeaderHeight;
            DrawTextField(card, ref rowY, "CQF_QuestBook_ObjectiveName", ref objective.labelKey, false);
            DrawTextField(card, ref rowY, "CQF_QuestBook_ObjectiveDescription", ref objective.descriptionKey, true);
        }

        private void DrawDetectionSection(ref float y, float width)
        {
            Rect card = BeginSection(ref y, width, GetDetectionSectionHeight(), "CQF_QuestBook_ObjectiveDetection");
            float rowY = card.y + SectionHeaderHeight;
            DrawObjectiveSelector(card, ref rowY);
            DrawTargetSelector(card, ref rowY);
            objective.Draw(ref rowY, card, card.x + FieldPadding);
        }

        private void DrawIconSection(ref float y, float width)
        {
            const float cardHeight = 156f;
            Rect card = BeginSection(ref y, width, cardHeight, "CQF_QuestBook_ObjectiveIcon");
            Rect previewRect = new Rect(card.x + 14f, card.y + SectionHeaderHeight + 4f, 64f, 64f);
            Widgets.DrawBox(previewRect, 1);
            DrawObjectiveIcon(objective, previewRect.ContractedBy(8f));
            float buttonX = previewRect.xMax + 18f;
            DrawTextButton(new Rect(buttonX, previewRect.y, 150f, 28f), "CQF_QuestBook_SelectThingIcon", SelectThingIcon);
            DrawTextButton(new Rect(buttonX, previewRect.y + 34f, 150f, 28f), "CQF_QuestBook_SelectImageIcon", SelectImageIcon);
            DrawTextButton(new Rect(buttonX, previewRect.y + 68f, 100f, 28f), "CQF_QuestBook_Clear", ClearIcon);
        }

        private static void DrawTextButton(Rect rect, string labelKey, Action action)
        {
            if (Widgets.ButtonText(rect, labelKey.Translate())) action();
        }

        private void SelectThingIcon()
        {
            QuestBookTextureEntry.OpenSelect(path => objective.iconPath = path, "CQF_QuestBook_SelectThingIcon");
        }

        private void SelectImageIcon()
        {
            Find.WindowStack.Add(new Dialog_SelectDialogImage(path =>
            {
                objective.iconPath = path;
            }, objective.iconPath));
        }

        private void ClearIcon()
        {
            objective.iconPath = null;
        }

        private static void DrawObjectiveIcon(QuestBookObjective objective, Rect rect)
        {
            if (!objective.iconPath.NullOrEmpty())
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(objective.iconPath, false);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(rect, texture, 1f);
                    return;
                }
            }
            if (objective.TargetThingDef != null)
            {
                Widgets.DefIcon(rect, objective.TargetThingDef);
                return;
            }
            Widgets.DrawTextureFitted(rect, TexButton.Info, 1f);
        }

        private void DrawRuleSection(ref float y, float width)
        {
            Rect card = BeginSection(ref y, width, RuleSectionHeight, "CQF_QuestBook_ObjectiveRules");
            DrawToggleRow(new Rect(card.x + 14f, card.y + SectionHeaderHeight, card.width - 28f, ToggleRowHeight), "CQF_QuestBook_Optional", "CQF_QuestBook_OptionalTip", ref objective.optional);
        }

        private Rect BeginSection(ref float y, float width, float height, string titleKey)
        {
            Rect card = new Rect(8f, y, width - 16f, height);
            Widgets.DrawMenuSection(card);
            Widgets.Label(new Rect(card.x + 14f, card.y + 10f, card.width - 28f, 28f), titleKey.Translate().Colorize(ColorLibrary.PaleBlue));
            y += height + SectionGap;
            return card;
        }

        private void DrawTextField(Rect card, ref float y, string labelKey, ref string value, bool multiline)
        {
            float rowHeight = multiline ? MultilineRowHeight : FieldRowHeight;
            Widgets.Label(new Rect(card.x + FieldPadding, y + 2f, LabelWidth, 24f), labelKey.Translate());
            Rect field = new Rect(card.x + FieldPadding + LabelWidth, y, card.width - LabelWidth - FieldPadding * 2f, multiline ? 58f : 28f);
            value = multiline ? Widgets.TextArea(field, value ?? string.Empty) : Widgets.TextField(field, value ?? string.Empty);
            y += rowHeight;
        }

        private void DrawObjectiveSelector(Rect card, ref float y)
        {
            Widgets.Label(new Rect(card.x + FieldPadding, y + 2f, LabelWidth, 24f), "CQF_QuestBook_ObjectiveType".Translate());
            string objectiveName = objective.GetType().Name.Translate();
            Rect button = new Rect(card.x + FieldPadding + LabelWidth, y, card.width - LabelWidth - FieldPadding * 2f, 28f);
                if (Widgets.ButtonText(button, objectiveName, false, true))
                {
                    List<Type> objectiveTypes = typeof(QuestBookObjective).AllSubclassesNonAbstract();
                    Find.WindowStack.Add(new Dialog_Select<Type>(new TextSelectDrawer<Type>(objectiveTypes,
                        type => type.Name.Translate(), type =>
                        {
                            QuestBookObjective replacement = Activator.CreateInstance(type) as QuestBookObjective;
                            if (replacement == null)
                            {
                                Log.Error("CQF task book objective type could not be created: " + type);
                                return;
                            }
                            replacement.labelKey = objective.labelKey;
                            replacement.descriptionKey = objective.descriptionKey;
                            replacement.iconPath = objective.iconPath;
                            replacement.optional = objective.optional;
                            replaceAction?.Invoke(replacement);
                            objective = replacement;
                        }, null, null, null, type => type.Name, null, null), "CQF_QuestBook_ObjectiveChecker".Translate()));
                }
            y += FieldRowHeight;
        }

        private void DrawTargetSelector(Rect card, ref float y)
        {
            if (objective.UsesThingTarget)
            {
                Widgets.Label(new Rect(card.x + FieldPadding, y + 2f, LabelWidth, 24f), "CQF_QuestBook_TargetThing".Translate());
                string label = objective.TargetThingDef == null ? "CQF_QuestBook_None".Translate() : objective.TargetThingDef.LabelCap;
                Rect button = new Rect(card.x + FieldPadding + LabelWidth, y, card.width - LabelWidth - FieldPadding * 2f, 28f);
                if (Widgets.ButtonText(button, label, false, true))
                {
                    List<ThingDef> selectableDefs = objective.GetThingTargets()
                        .Where(def => QuestBookTextureEntry.GetThingTexturePath(def) != null)
                        .OrderBy(def => def.label)
                        .ToList();
                    Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(
                        selectableDefs,
                        def => ContentFinder<Texture2D>.Get(QuestBookTextureEntry.GetThingTexturePath(def), false),
                        def => def.label,
                        def =>
                        {
                            objective.TargetThingDef = def;
                            objective.iconPath = QuestBookTextureEntry.GetThingTexturePath(def);
                        }), "CQF_QuestBook_TargetThing".Translate()));
                }
                y += FieldRowHeight;
                return;
            }
            if (objective.UsesResearchTarget)
            {
                Widgets.Label(new Rect(card.x + FieldPadding, y + 2f, LabelWidth, 24f), "CQF_QuestBook_TargetResearch".Translate());
                string label = objective.TargetResearch == null ? "CQF_QuestBook_None".Translate() : objective.TargetResearch.LabelCap;
                Rect button = new Rect(card.x + FieldPadding + LabelWidth, y, card.width - LabelWidth - FieldPadding * 2f, 28f);
                if (Widgets.ButtonText(button, label, false, true))
                {
                    List<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                        .OrderBy(def => def.label)
                        .ToList();
                    Find.WindowStack.Add(new Dialog_Select<ResearchProjectDef>(new TextSelectDrawer<ResearchProjectDef>(
                        projects, def => def.LabelCap, def => objective.TargetResearch = def, null, def => def.description,
                        null, def => def.defName, null, null), "CQF_QuestBook_TargetResearch".Translate()));
                }
                y += FieldRowHeight;
            }
        }

        private void DrawToggleRow(Rect rect, string labelKey, string tipKey, ref bool value)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            Widgets.CheckboxLabeled(rect, labelKey.Translate(), ref value, placeCheckboxNearText: false);
            TooltipHandler.TipRegion(rect, tipKey.Translate());
        }

        private float CalculateContentHeight()
        {
            return 8f + 38f + IdentitySectionHeight + SectionGap + 156f + SectionGap + GetDetectionSectionHeight() + SectionGap + RuleSectionHeight + 8f;
        }

        private float GetDetectionSectionHeight()
        {
            int rowCount = 1;
            if (objective.UsesSignal)
            {
                rowCount++;
            }
            if (objective.UsesThingTarget || objective.UsesResearchTarget)
            {
                rowCount++;
            }
            if (objective.UsesTargetCount)
            {
                rowCount++;
            }
            return SectionHeaderHeight + rowCount * FieldRowHeight + 8f;
        }

        private const float FieldPadding = 14f;
        private const float FieldRowHeight = 36f;
        private const float IdentitySectionHeight = 164f;
        private const float LabelWidth = 170f;
        private const float MultilineRowHeight = 70f;
        private const float RuleSectionHeight = 82f;
        private const float SectionGap = 12f;
        private const float SectionHeaderHeight = 46f;
        private const float ToggleRowHeight = 30f;
        private QuestBookObjective objective;
        private readonly System.Action<QuestBookObjective> replaceAction;
        private Vector2 scrollPosition;
    }
}
