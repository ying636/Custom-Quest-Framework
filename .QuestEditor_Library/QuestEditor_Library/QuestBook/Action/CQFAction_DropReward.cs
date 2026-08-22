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
            string title = "CQF_QuestBook_Rewards".Translate();
            Rect titleRect = new Rect(x, y, Text.CalcSize(title).x, 28f);
            Widgets.Label(titleRect, title.Colorize(ColorLibrary.PaleBlue));
            Rect addRect = new Rect(titleRect.xMax + 8f, y, 26f, 26f);
            if (Widgets.ButtonImage(addRect, TexButton.Plus))
            {
                CQFRewardEditor.OpenRewardSelector(reward => rewards.Add(reward));
            }
            TooltipHandler.TipRegion(addRect, "CQF_QuestBook_AddRewardTip".Translate());
            y += 34f;
            foreach (CQFThingData reward in rewards.ToList())
            {
                float itemY = y;
                reward.Draw(ref y, inRect, x);
                Rect deleteRect = new Rect(inRect.width - 40f, itemY + 1f, 28f, 28f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    rewards.Remove(reward);
                }
                TooltipHandler.TipRegion(deleteRect, "CQF_QuestBook_RemoveRewardTip".Translate());
                y += 8f;
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
