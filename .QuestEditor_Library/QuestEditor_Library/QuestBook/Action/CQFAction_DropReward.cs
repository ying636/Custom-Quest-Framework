using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_DropReward : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Misc;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            float contentWidth = Mathf.Max(280f, inRect.width - x - 12f);
            Rect sectionRect = new Rect(x, y, contentWidth, 32f);
            Widgets.DrawMenuSection(sectionRect);
            string title = "CQF_QuestBook_Rewards".Translate();
            Widgets.Label(new Rect(sectionRect.x + 12f, sectionRect.y + 5f, sectionRect.width - 52f, 24f), title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(sectionRect.xMax - 36f, sectionRect.y + 3f, 28f, 28f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                CQFRewardEditor.OpenRewardSelector(reward => rewards.Add(reward));
            }
            TooltipHandler.TipRegion(addRect, "CQF_QuestBook_AddRewardTip".Translate());
            y += sectionRect.height + 8f;

            if (rewards.NullOrEmpty())
            {
                Rect emptyRect = new Rect(x, y, contentWidth, 38f);
                Widgets.DrawMenuSection(emptyRect);
                Widgets.Label(new Rect(emptyRect.x + 12f, emptyRect.y + 9f, emptyRect.width - 24f, 22f), "CQF_QuestBook_NoRewards".Translate().Colorize(Color.gray));
                y += emptyRect.height + 10f;
                return;
            }

            foreach (CQFThingData reward in rewards.ToList())
            {
                float cardY = y;
                float contentY = cardY + 28f;
                string rewardType = reward == null ? "CQF_QuestBook_RewardInvalid".Translate() : reward.GetType().Name.Translate();
                Widgets.Label(new Rect(x + 12f, cardY + 4f, contentWidth - 56f, 22f), rewardType.Colorize(ColorLibrary.PaleBlue));
                if (reward != null)
                {
                    reward.Draw(ref contentY, inRect, x + 8f);
                }
                else
                {
                    Widgets.Label(new Rect(x + 20f, contentY + 5f, contentWidth - 80f, 22f), "CQF_QuestBook_RewardInvalid".Translate().Colorize(ColorLibrary.RedReadable));
                    contentY += 30f;
                }
                float cardHeight = Mathf.Max(54f, contentY - cardY + 6f);
                Widgets.DrawBox(new Rect(x, cardY, contentWidth, cardHeight), 1, QuestEditor_Dialog.blueTex);
                Rect deleteRect = new Rect(x + contentWidth - 36f, cardY + 2f, 28f, 28f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    rewards.Remove(reward);
                }
                TooltipHandler.TipRegion(deleteRect, "CQF_QuestBook_RemoveRewardTip".Translate());
                y = cardY + cardHeight + 10f;
            }
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            CQFRewardDelivery.TryDrop(rewards, quest);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref rewards, "rewards", LookMode.Deep);
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!rewards.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(rewards, "rewards"));
            }
            return result;
        }

        public List<CQFThingData> rewards = new List<CQFThingData>();
    }
}
