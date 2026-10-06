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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_DropReward.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
                result.Add(CQFSerialization.SaveList_Saveable(rewards, "rewards"));
            }
            return result;
        }

        public List<CQFThingData> rewards = new List<CQFThingData>();
    }
}
