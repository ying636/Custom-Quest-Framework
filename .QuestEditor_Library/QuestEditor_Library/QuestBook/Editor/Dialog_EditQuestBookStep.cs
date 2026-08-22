using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_EditQuestBookStep : Window
    {
        public Dialog_EditQuestBookStep(QuestBookStep step)
        {
            this.step = step;
            if (step.labelKey.CanTranslate())
            {
                step.labelKey = step.labelKey.Translate().ToString();
            }
            if (step.descriptionKey.CanTranslate())
            {
                step.descriptionKey = step.descriptionKey.Translate().ToString();
            }
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(760f, 720f);

        public override void DoWindowContents(Rect inRect)
        {
            float contentWidth = inRect.width - 30f;
            Rect scrollRect = new Rect(0f, 0f, inRect.width, inRect.height);
            Rect contentRect = new Rect(0f, 0f, contentWidth, Mathf.Max(inRect.height, CalculateContentHeight(contentWidth)));
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, contentRect);
            float y = 8f;
            DrawSectionTitle(ref y, contentWidth, "CQF_QuestBook_StepProperties".Translate());
            DrawTextField(ref y, contentWidth, "CQF_QuestBook_StepDataName".Translate(), ref step.id);
            DrawTextField(ref y, contentWidth, "CQF_QuestBook_StepName".Translate(), ref step.labelKey);
            DrawTextField(ref y, contentWidth, "CQF_QuestBook_StepDescription".Translate(), ref step.descriptionKey);
            y += 8f;
            DrawIconCard(ref y, contentWidth);
            DrawRewardInfoCard(ref y, contentWidth);
            DrawObjectiveCard(ref y, contentWidth);
            DrawRewardCard(ref y, contentWidth);
            DrawActionCard(ref y, contentWidth, "CQF_QuestBook_ActivateActions".Translate(), step.onActivateActions);
            DrawActionCard(ref y, contentWidth, "CQF_QuestBook_CompleteActions".Translate(), step.onCompleteActions);
            DrawActionCard(ref y, contentWidth, "CQF_QuestBook_FailActions".Translate(), step.onFailActions);
            DrawActionCard(ref y, contentWidth, "CQF_QuestBook_SkipActions".Translate(), step.onSkipActions);
            Widgets.EndScrollView();
        }

        private void DrawSectionTitle(ref float y, float width, string title)
        {
            Widgets.Label(new Rect(8f, y, width - 16f, 32f), title.Colorize(ColorLibrary.SkyBlue));
            y += 42f;
        }

        private void DrawTextField(ref float y, float width, string label, ref string value)
        {
            float rowHeight = 62f;
            Rect row = new Rect(8f, y, width - 16f, rowHeight);
            Widgets.DrawMenuSection(row);
            Widgets.Label(new Rect(row.x + 12f, row.y + 7f, row.width - 24f, 22f), label.Colorize(ColorLibrary.PaleBlue));
            value = Widgets.TextField(new Rect(row.x + 12f, row.y + 30f, row.width - 24f, 26f), value ?? string.Empty);
            y += rowHeight + 8f;
        }

        private void DrawIconCard(ref float y, float width)
        {
            float cardHeight = 142f;
            Rect card = new Rect(8f, y, width - 16f, cardHeight);
            Widgets.DrawMenuSection(card);
            Widgets.Label(new Rect(card.x + 12f, card.y + 8f, card.width - 24f, 24f), "CQF_QuestBook_NodeIcon".Translate().Colorize(ColorLibrary.PaleBlue));
            Rect previewRect = new Rect(card.x + 12f, card.y + 38f, 88f, 88f);
            Widgets.DrawTextureFitted(previewRect, nodeFrame, 1f);
            DrawIcon(previewRect.ContractedBy(13f));
            float buttonX = previewRect.xMax + 20f;
            DrawTextButton(new Rect(buttonX, card.y + 38f, 150f, 28f), "CQF_QuestBook_SelectThingIcon", SelectThingIcon);
            DrawTextButton(new Rect(buttonX, card.y + 72f, 150f, 28f), "CQF_QuestBook_SelectImageIcon", SelectImageIcon);
            DrawTextButton(new Rect(buttonX, card.y + 106f, 100f, 28f), "CQF_QuestBook_Clear", ClearIcon);
            y += cardHeight + 10f;
        }

        private void DrawRewardInfoCard(ref float y, float width)
        {
            step.rewardInfos ??= new List<QuestBookRewardInfo>();
            float cardHeight = GetRewardInfoCardHeight(width);
            Rect card = new Rect(8f, y, width - 16f, cardHeight);
            Widgets.DrawMenuSection(card);
            string title = "CQF_QuestBook_RewardInfo".Translate();
            Widgets.Label(new Rect(card.x + 12f, card.y + 8f, Text.CalcSize(title).x, 24f), title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(card.xMax - 42f, card.y + 5f, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                QuestBookRewardInfo info = new QuestBookRewardInfo();
                step.rewardInfos.Add(info);
                Find.WindowStack.Add(new Dialog_EditQuestBookRewardInfo(info));
            }
            TooltipHandler.TipRegion(addRect, "CQF_QuestBook_AddRewardInfoTip".Translate());
            float rowY = card.y + 40f;
            if (step.rewardInfos.NullOrEmpty())
            {
                Widgets.Label(new Rect(card.x + 12f, rowY + 12f, card.width - 24f, 26f), "CQF_QuestBook_NoRewardInfo".Translate().Colorize(Color.gray));
            }
            for (int index = 0; index < step.rewardInfos.Count; index++)
            {
                QuestBookRewardInfo info = step.rewardInfos[index];
                float rowHeight = GetRewardInfoRowHeight(info, card.width - 24f);
                Rect rowRect = new Rect(card.x + 12f, rowY, card.width - 24f, rowHeight);
                Widgets.DrawBoxSolid(rowRect, new Color(0.08f, 0.1f, 0.12f, 0.72f));
                Widgets.DrawHighlightIfMouseover(rowRect);
                Rect iconRect = new Rect(rowRect.x + 6f, rowRect.y + 6f, 48f, 48f);
                Widgets.DrawBox(iconRect, 1);
                DrawRewardInfoIcon(info, iconRect.ContractedBy(6f));
                Rect contentRect = new Rect(iconRect.xMax + 12f, rowRect.y + 6f, rowRect.width - 106f, rowHeight - 12f);
                string label = info.Label.NullOrEmpty() ? "CQF_QuestBook_RewardInfoUnnamed".Translate().ToString() : info.Label;
                Widgets.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 22f), label.Colorize(ColorLibrary.PaleBlue));
                if (!info.Description.NullOrEmpty())
                {
                    Widgets.Label(new Rect(contentRect.x, contentRect.y + 24f, contentRect.width, contentRect.height - 24f), info.Description);
                }
                if (Widgets.ButtonInvisible(new Rect(rowRect.x, rowRect.y, rowRect.width - 42f, rowRect.height)))
                {
                    Find.WindowStack.Add(new Dialog_EditQuestBookRewardInfo(info));
                }
                Rect deleteRect = new Rect(rowRect.xMax - 30f, rowRect.y + 13f, 28f, 28f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    step.rewardInfos.Remove(info);
                    break;
                }
                TooltipHandler.TipRegion(deleteRect, "CQF_QuestBook_RemoveRewardInfoTip".Translate());
                rowY += rowHeight + 6f;
            }
            y += cardHeight + 10f;
        }

        private static void DrawRewardInfoIcon(QuestBookRewardInfo info, Rect rect)
        {
            if (info.iconThing != null)
            {
                Widgets.DefIcon(rect, info.iconThing);
                return;
            }
            if (!info.iconPath.NullOrEmpty())
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(info.iconPath, false);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(rect, texture, 1f);
                }
            }
        }

        private static void DrawTextButton(Rect rect, string labelKey, Action action)
        {
            if (Widgets.ButtonText(rect, labelKey.Translate()))
            {
                action();
            }
        }

        private void SelectThingIcon()
        {
            Find.WindowStack.Add(new Dialog_Select<ThingDef>(new LabeledTextureSelectDrawer<ThingDef>(
                DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(def => def.uiIcon != null
                        && !def.uiIcon.NullOrBad()
                        && def.uiIcon != BaseContent.PlaceholderImage
                        && def.category != ThingCategory.Mote
                        && def.mote == null
                        && def.projectile == null
                        && def.skyfaller == null
                        && def.pawnFlyer == null
                        && def.gas == null
                        && def.filth == null
                        && def.thingClass != null
                        && !typeof(Mote).IsAssignableFrom(def.thingClass))
                    .OrderBy(def => def.label)
                    .ToList(),
                def => def.uiIcon,
                def => def.label,
                selected =>
                {
                    step.iconThing = selected;
                    step.iconPath = null;
                },
                null,
                (def, rect) => Widgets.DrawTextureFitted(rect, def.uiIcon, 1f)), "CQF_QuestBook_SelectThingIcon".Translate()));
        }

        private void SelectImageIcon()
        {
            Find.WindowStack.Add(new Dialog_SelectDialogImage(path =>
            {
                step.iconPath = path;
                step.iconThing = null;
            }, step.iconPath));
        }

        private void ClearIcon()
        {
            step.iconThing = null;
            step.iconPath = null;
        }

        private void DrawIcon(Rect rect)
        {
            if (step.iconThing != null)
            {
                Widgets.DefIcon(rect, step.iconThing);
                return;
            }
            if (!step.iconPath.NullOrEmpty())
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(step.iconPath, false);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(rect, texture, 1f);
                }
            }
        }

        private void DrawObjectiveCard(ref float y, float width)
        {
            float cardHeight = GetObjectiveCardHeight();
            Rect card = new Rect(8f, y, width - 16f, cardHeight);
            Widgets.DrawMenuSection(card);
            string title = "CQF_QuestBook_Objectives".Translate();
            Rect titleRect = new Rect(card.x + 12f, card.y + 8f, Text.CalcSize(title).x, 30f);
            Widgets.Label(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(titleRect.xMax + 8f, card.y + 5f, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                step.objectives.Add(new QuestBookObjective());
            }
            TooltipHandler.TipRegion(addRect, "CQF_QuestBook_AddObjectiveTip".Translate());
            float modeLabelWidth = Text.CalcSize("CQF_QuestBook_CompletionMode".Translate()).x;
            float modeButtonWidth = 120f;
            Rect modeButtonRect = new Rect(card.xMax - modeButtonWidth - 12f, card.y + 5f, modeButtonWidth, 28f);
            Widgets.Label(new Rect(modeButtonRect.x - modeLabelWidth - 8f, card.y + 8f, modeLabelWidth, 24f), "CQF_QuestBook_CompletionMode".Translate().Colorize(ColorLibrary.PaleBlue));
            if (Widgets.ButtonText(modeButtonRect, ("QuestBookCompletionMode_" + step.completionMode).Translate(), false))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (QuestBookCompletionMode mode in Enum.GetValues(typeof(QuestBookCompletionMode)))
                {
                    QuestBookCompletionMode selectedMode = mode;
                    options.Add(new FloatMenuOption(("QuestBookCompletionMode_" + mode).Translate(), () => step.completionMode = selectedMode));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            float rowY = card.y + 42f;
            foreach (QuestBookObjective objective in step.objectives.ToList())
            {
                Rect row = new Rect(card.x + 12f, rowY, card.width - 58f, 30f);
                string objectiveLabel = objective.Label;
                if (Widgets.ButtonText(row, string.Empty, false))
                {
                    Find.WindowStack.Add(new Dialog_EditQuestBookObjective(objective));
                }
                Rect iconRect = new Rect(row.x + 4f, row.y + 3f, 24f, 24f);
                DrawObjectiveIcon(objective, iconRect);
                Widgets.Label(new Rect(iconRect.xMax + 8f, row.y + 4f, row.width - 40f, 22f), objectiveLabel);
                Rect deleteRect = new Rect(card.xMax - 42f, rowY + 1f, 28f, 28f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    step.objectives.Remove(objective);
                }
                TooltipHandler.TipRegion(deleteRect, "CQF_QuestBook_RemoveObjectiveTip".Translate());
                rowY += 38f;
            }
            y += cardHeight + 10f;
        }

        private static void DrawObjectiveIcon(QuestBookObjective objective, Rect rect)
        {
            if (objective.iconThing != null)
            {
                Widgets.DefIcon(rect, objective.iconThing);
                return;
            }
            if (!objective.iconPath.NullOrEmpty())
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(objective.iconPath, false);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(rect, texture, 1f);
                    return;
                }
            }
            if (objective.targetThingDef != null)
            {
                Widgets.DefIcon(rect, objective.targetThingDef);
                return;
            }
            Widgets.DrawTextureFitted(rect, TexButton.Info, 1f);
        }

        private void DrawActionCard(ref float y, float width, string title, List<CQFAction> actions)
        {
            float cardHeight = GetActionCardHeight(actions);
            Rect card = new Rect(8f, y, width - 16f, cardHeight);
            Widgets.DrawMenuSection(card);
            Rect titleRect = new Rect(card.x + 12f, card.y + 8f, Text.CalcSize(title).x, 30f);
            Widgets.Label(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(titleRect.xMax + 8f, card.y + 5f, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                CQFEditorTools.OpenCQFActionSelect(type => actions.Add((CQFAction)Activator.CreateInstance(type)));
            }
            TooltipHandler.TipRegion(addRect, "CQF_QuestBook_AddActionTip".Translate());
            Rect removeRect = new Rect(addRect.xMax + 8f, addRect.y, 28f, 28f);
            Color oldColor = GUI.color;
            if (!actions.Any())
            {
                GUI.color = Color.gray;
            }
            bool removeClicked = Widgets.ButtonImage(removeRect, TexButton.Delete);
            GUI.color = oldColor;
            if (removeClicked && actions.Any())
            {
                Find.WindowStack.Add(new FloatMenu(actions.Select(action => new FloatMenuOption(action.GetType().Name.Translate(), () => actions.Remove(action))).ToList()));
            }
            TooltipHandler.TipRegion(removeRect, "CQF_QuestBook_RemoveActionTip".Translate());
            float rowY = card.y + 42f;
            foreach (CQFAction action in actions.ToList())
            {
                if (Widgets.ButtonText(new Rect(card.x + 12f, rowY, card.width - 24f, 26f), action.GetType().Name.Translate(), false))
                {
                    Find.WindowStack.Add(new Dialog_EditIDrawable(action));
                }
                rowY += 30f;
            }
            y += cardHeight + 10f;
        }

        private void DrawRewardCard(ref float y, float width)
        {
            float cardHeight = GetRewardCardHeight();
            Rect card = new Rect(8f, y, width - 16f, cardHeight);
            Widgets.DrawMenuSection(card);
            string title = "CQF_QuestBook_StepRewards".Translate();
            Rect titleRect = new Rect(card.x + 12f, card.y + 8f, Text.CalcSize(title).x, 30f);
            Widgets.Label(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(titleRect.xMax + 8f, card.y + 5f, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                CQFRewardEditor.OpenThingSelector(definition => step.rewards.Add(new CQFThingDefCount { thing = definition }));
            }
            TooltipHandler.TipRegion(addRect, "CQF_QuestBook_AddRewardTip".Translate());
            float rowY = card.y + 42f;
            foreach (CQFThingDefCount reward in step.rewards.ToList())
            {
                float itemY = rowY;
                reward.DrawWithSingleCount(ref rowY, card, card.x + 12f);
                Rect deleteRect = new Rect(card.xMax - 42f, itemY + 1f, 28f, 28f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    step.rewards.Remove(reward);
                }
                TooltipHandler.TipRegion(deleteRect, "CQF_QuestBook_RemoveRewardTip".Translate());
                rowY += 8f;
            }
            y += cardHeight + 10f;
        }

        private float CalculateContentHeight(float contentWidth)
        {
            float height = 8f + 42f;
            height += 3f * (62f + 8f);
            height += 8f;
            height += 142f + 10f;
            height += GetRewardInfoCardHeight(contentWidth) + 10f;
            height += GetObjectiveCardHeight() + 10f;
            height += GetRewardCardHeight() + 10f;
            height += GetActionCardHeight(step.onActivateActions) + 10f;
            height += GetActionCardHeight(step.onCompleteActions) + 10f;
            height += GetActionCardHeight(step.onFailActions) + 10f;
            height += GetActionCardHeight(step.onSkipActions) + 10f;
            return height + 8f;
        }

        private float GetObjectiveCardHeight()
        {
            return 48f + step.objectives.Count * 38f;
        }

        private float GetActionCardHeight(List<CQFAction> actions)
        {
            return 48f + actions.Count * 30f;
        }

        private float GetRewardCardHeight()
        {
            return 48f + step.rewards.Count * 38f;
        }

        private float GetRewardInfoCardHeight(float width)
        {
            if (step.rewardInfos.NullOrEmpty()) return 92f;
            return 48f + step.rewardInfos.Sum(info => GetRewardInfoRowHeight(info, width - 24f) + 6f);
        }

        private static float GetRewardInfoRowHeight(QuestBookRewardInfo info, float width)
        {
            float textWidth = width - 66f;
            float descriptionHeight = info.Description.NullOrEmpty() ? 0f : Text.CalcHeight(info.Description, textWidth);
            return Mathf.Max(60f, 30f + descriptionHeight);
        }

        private readonly QuestBookStep step;
        private Vector2 scrollPosition;
        private static readonly Texture2D nodeFrame = ContentFinder<Texture2D>.Get("UI/QuestBook/NodeFrame", true);
    }
}
