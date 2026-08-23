using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_QuestBookStepInfo : Window
    {
        public Dialog_QuestBookStepInfo(QuestBookStep step, QuestBookInstance instance)
        {
            this.step = step;
            this.instance = instance;
            doCloseX = true;
            forcePause = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, 540f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            QuestBookStepState state = instance?.GetStepState(step.id);
            bool canCheck = instance?.state == QuestBookState.Active && state?.status == QuestBookStepStatus.Active;
            Widgets.Label(new Rect(4f, 0f, inRect.width - 52f, 32f), "CQF_QuestBook_StepInfo".Translate().Colorize(ColorLibrary.SkyBlue));
            Color oldColor = GUI.color;
            GUI.color = canCheck ? Color.white : Color.gray;
            Rect checkRect = new Rect(inRect.width - 34f, 2f, 28f, 28f);
            if (Widgets.ButtonImage(checkRect, TexButton.Reload) && canCheck)
            {
                Messages.Message(instance.CheckObjectives(step.id) ? "CQF_QuestBook_ObjectivesChecked".Translate() : "CQF_QuestBook_ObjectiveCheckUnavailable".Translate(), MessageTypeDefOf.PositiveEvent);
            }
            GUI.color = oldColor;
            TooltipHandler.TipRegion(checkRect, "CQF_QuestBook_CheckObjectivesTip".Translate());
            Rect scrollRect = new Rect(0f, 38f, inRect.width, inRect.height - 38f);
            float contentWidth = inRect.width - 18f;
            Rect contentRect = new Rect(0f, 0f, contentWidth, Mathf.Max(scrollRect.height, CalculateContentHeight(contentWidth)));
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, contentRect);
            float y = 8f;
            DrawStepCard(ref y, contentRect.width, state);
            DrawRewardCard(ref y, contentRect.width);
            DrawObjectiveCard(ref y, contentRect.width);
            Widgets.EndScrollView();
        }

        private float CalculateContentHeight(float width)
        {
            float height = GetStepCardHeight(width);
            bool hasInfo = step.rewardInfos.Any(info => info?.HasContent == true);
            bool hasRewards = !step.rewards.NullOrEmpty();
            if (hasInfo || hasRewards)
            {
                height += GetRewardCardHeight(width) + 10f;
            }
            height += GetObjectiveCardHeight(width) + CardGap;
            return height;
        }

        private void DrawStepCard(ref float y, float width, QuestBookStepState state)
        {
            float height = GetStepCardHeight(width);
            Rect card = new Rect(8f, y, width - 16f, height);
            Widgets.DrawMenuSection(card);
            Rect iconRect = new Rect(card.x + 12f, card.y + 12f, 56f, 56f);
            DrawStepIcon(iconRect);
            float textX = iconRect.xMax + 14f;
            float textWidth = card.xMax - textX - 14f;
            Widgets.Label(new Rect(textX, card.y + 10f, textWidth, 26f), step.Label.Colorize(ColorLibrary.PaleBlue));
            string stateKey = state == null ? "CQF_QuestBook_State_Locked" : "CQF_QuestBook_State_" + state.status;
            Widgets.Label(new Rect(textX, card.y + 36f, textWidth, 22f), "CQF_QuestBook_State".Translate(stateKey.Translate()).Colorize(GetStateColor(state?.status)));
            if (!step.Description.NullOrEmpty())
            {
                float descriptionHeight = Text.CalcHeight(step.Description, textWidth);
                Widgets.Label(new Rect(textX, card.y + 60f, textWidth, descriptionHeight), step.Description);
            }
            List<Texture2D> detailImages = GetDetailImages();
            if (detailImages.Any())
            {
                float descriptionHeight = step.Description.NullOrEmpty() ? 0f : Text.CalcHeight(step.Description, textWidth);
                float imageY = card.y + 74f + descriptionHeight;
                foreach (Texture2D detailImage in detailImages)
                {
                    Rect imageRect = new Rect(textX, imageY, textWidth, DetailImageHeight);
                    Widgets.DrawBox(imageRect, 1);
                    Widgets.DrawTextureFitted(imageRect.ContractedBy(4f), detailImage, 1f);
                    imageY += DetailImageHeight + 12f;
                }
            }
            y += height + CardGap;
        }

        private float GetStepCardHeight(float width)
        {
            float textWidth = width - 112f;
            float height = Mathf.Max(80f, 70f + (step.Description.NullOrEmpty() ? 0f : Text.CalcHeight(step.Description, textWidth)));
            int detailImageCount = GetDetailImages().Count;
            if (detailImageCount > 0)
            {
                height += detailImageCount * (DetailImageHeight + 12f);
            }
            return height;
        }

        private void DrawRewardCard(ref float y, float width)
        {
            bool hasInfo = step.rewardInfos.Any(info => info?.HasContent == true);
            bool hasRewards = !step.rewards.NullOrEmpty();
            if (!hasInfo && !hasRewards) return;
            float height = GetRewardCardHeight(width);
            Rect card = new Rect(8f, y, width - 16f, height);
            Widgets.DrawMenuSection(card);
            Widgets.Label(new Rect(card.x + 12f, card.y + 8f, card.width - 24f, 24f), "CQF_QuestBook_Rewards".Translate().Colorize(ColorLibrary.PaleBlue));
            float rowY = card.y + 38f;
            if (hasInfo)
            {
                foreach (QuestBookRewardInfo info in step.rewardInfos.Where(info => info?.HasContent == true))
                {
                    float rowHeight = GetRewardInfoRowHeight(info, card.width - 28f);
                    Rect row = DrawListRow(card, rowY, rowHeight);
                    Widgets.DrawHighlightIfMouseover(row);
                    Rect iconRect = new Rect(row.x + 8f, row.y + 8f, 40f, 40f);
                    DrawRewardInfoIcon(iconRect, info);
                    string label = info.Label.NullOrEmpty() ? "CQF_QuestBook_RewardInfoUnnamed".Translate().ToString() : info.Label;
                    float textX = iconRect.xMax + 12f;
                    float textWidth = row.width - (textX - row.x) - 10f;
                    Widgets.Label(new Rect(textX, row.y + 7f, textWidth, 22f), label.Colorize(ColorLibrary.PaleBlue));
                    if (!info.Description.NullOrEmpty()) TooltipHandler.TipRegion(row, info.Description);
                    rowY += rowHeight + 6f;
                }
            }
            if (hasRewards)
            {
                foreach (CQFThingDefCount reward in step.rewards)
                {
                    Rect row = DrawListRow(card, rowY, RewardRowHeight);
                    Widgets.DrawHighlightIfMouseover(row);
                    Rect iconRect = new Rect(row.x + 8f, row.y + 8f, 40f, 40f);
                    if (reward?.thing != null) Widgets.DefIcon(iconRect, reward.thing, reward.stuff);
                    Widgets.Label(new Rect(iconRect.xMax + 12f, row.y + 17f, row.width - 76f, 24f), GetRewardLabel(reward).Colorize(ColorLibrary.PaleBlue));
                    rowY += RewardRowHeight + 6f;
                }
            }
            y += height + CardGap;
        }

        private float GetRewardCardHeight(float width)
        {
            float height = 44f;
            foreach (QuestBookRewardInfo info in step.rewardInfos.Where(info => info?.HasContent == true))
            {
                height += GetRewardInfoRowHeight(info, width - 28f) + 6f;
            }
            if (!step.rewards.NullOrEmpty())
            {
                height += step.rewards.Count * (RewardRowHeight + 6f);
            }
            return height;
        }

        private static float GetRewardInfoRowHeight(QuestBookRewardInfo info, float width)
        {
            return 56f;
        }

        private void DrawObjectiveCard(ref float y, float width)
        {
            float height = GetObjectiveCardHeight(width);
            Rect card = new Rect(8f, y, width - 16f, height);
            Widgets.DrawMenuSection(card);
            Widgets.Label(new Rect(card.x + 14f, card.y + 8f, card.width - 28f, 24f), "CQF_QuestBook_Objectives".Translate().Colorize(ColorLibrary.PaleBlue));
            float rowY = card.y + ObjectiveHeaderHeight;
            for (int index = 0; index < step.objectives.Count; index++)
            {
                QuestBookObjective objective = step.objectives[index];
                QuestBookObjectiveProgress progress = instance?.GetObjectiveProgress(step.id, index);
                float rowHeight = Mathf.Max(50f, Text.CalcHeight(objective.Label, card.width - 118f) + 16f);
                Rect row = DrawListRow(card, rowY, rowHeight);
                Widgets.DrawHighlightIfMouseover(row);
                string count = objective.UsesTargetCount && objective.TargetCount > 1 ? " (" + (progress?.currentCount ?? 0) + "/" + objective.TargetCount + ")" : string.Empty;
                Rect iconRect = new Rect(row.x + 8f, row.y + 7f, 36f, 36f);
                DrawObjectiveIcon(objective, iconRect);
                Widgets.Label(new Rect(iconRect.xMax + 12f, row.y + (row.height - 24f) / 2f, row.width - 100f, 24f), objective.Label + count);
                Rect stateRect = new Rect(row.xMax - 34f, row.y + (row.height - 24f) / 2f, 24f, 24f);
                if (progress?.completed == true)
                {
                    Widgets.CheckboxDraw(stateRect.x, stateRect.y, true, true, stateRect.width);
                }
                else
                {
                    Widgets.DrawBox(stateRect, 1);
                }
                if (!objective.Description.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(row, objective.Description);
                }
                rowY += rowHeight + RowGap;
            }
            y += height + CardGap;
        }

        private float GetObjectiveCardHeight(float width)
        {
            return ObjectiveHeaderHeight + step.objectives.Sum(objective =>
                Mathf.Max(50f, Text.CalcHeight(objective.Label, width - 118f) + 16f) + RowGap);
        }

        private static Rect DrawListRow(Rect card, float y, float height)
        {
            Rect row = new Rect(card.x + 12f, y, card.width - 24f, height);
            Widgets.DrawBoxSolid(row, new Color(0.085f, 0.1f, 0.115f, 0.78f));
            return row;
        }

        private void DrawStepIcon(Rect rect)
        {
            Widgets.DrawTextureFitted(rect, nodeFrame, 1f);
            Rect iconRect = rect.ContractedBy(10f);
            Texture2D texture = step.iconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(step.iconPath, false);
            if (texture != null) Widgets.DrawTextureFitted(iconRect, texture, 1f);
        }

        private static void DrawRewardInfoIcon(Rect rect, QuestBookRewardInfo info)
        {
            Texture2D texture = info.iconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(info.iconPath, false);
            if (texture != null) Widgets.DrawTextureFitted(rect, texture, 1f);
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

        private List<Texture2D> GetDetailImages()
        {
            return (step.detailImagePaths ?? new List<string>())
                .Where(path => !path.NullOrEmpty())
                .Select(path => ContentFinder<Texture2D>.Get(path, false))
                .Where(texture => texture != null)
                .ToList();
        }

        private static string GetRewardLabel(CQFThingDefCount reward)
        {
            return reward?.thing == null ? "CQF_QuestBook_RewardInvalid".Translate() : reward.thing.LabelCap + " x" + reward.count.min;
        }

        private static Color GetStateColor(QuestBookStepStatus? status)
        {
            if (status == QuestBookStepStatus.Completed) return ColorLibrary.Green;
            if (status == QuestBookStepStatus.Failed) return ColorLibrary.RedReadable;
            if (status == QuestBookStepStatus.Active) return ColorLibrary.SkyBlue;
            return Color.gray;
        }

        private readonly QuestBookStep step;
        private readonly QuestBookInstance instance;
        private Vector2 scrollPosition;
        private const float ObjectiveHeaderHeight = 38f;
        private const float CardGap = 8f;
        private const float RowGap = 6f;
        private const float RewardRowHeight = 54f;
        private const float DetailImageHeight = 180f;
        private static readonly Texture2D nodeFrame = ContentFinder<Texture2D>.Get("UI/QuestBook/NodeFrame", true);
    }
}
