using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookStep : IExposable, ISaveable
    {
        public string id = "step_" + Guid.NewGuid().ToString("N");
        [NoTranslate]
        public string labelKey;
        [NoTranslate]
        public string descriptionKey;
        public QuestBookCompletionMode completionMode = QuestBookCompletionMode.All;
        public List<QuestBookObjective> objectives = new List<QuestBookObjective>();
        public List<CQFThingDefCount> rewards = new List<CQFThingDefCount>();
        public List<QuestBookRewardInfo> rewardInfos = new List<QuestBookRewardInfo>();
        public List<CQFAction> onActivateActions = new List<CQFAction>();
        public List<CQFAction> onCompleteActions = new List<CQFAction>();
        public List<CQFAction> onFailActions = new List<CQFAction>();
        public List<CQFAction> onSkipActions = new List<CQFAction>();
        [NoTranslate]
        public List<string> nextStepIds = new List<string>();
        public Vector2 position = Vector2.zero;
        public ThingDef iconThing;
        [NoTranslate]
        public string iconPath;
        [NoTranslate]
        public List<string> detailImagePaths = new List<string>();

        public string Label => labelKey.NullOrEmpty() ? id : labelKey.CanTranslate() ? labelKey.Translate().ToString() : labelKey;

        public string Description => descriptionKey.NullOrEmpty() ? string.Empty : descriptionKey.CanTranslate() ? descriptionKey.Translate().ToString() : descriptionKey;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref descriptionKey, "descriptionKey");
            Scribe_Values.Look(ref completionMode, "completionMode", QuestBookCompletionMode.All);
            Scribe_Collections.Look(ref objectives, "objectives", LookMode.Deep);
            Scribe_Collections.Look(ref rewards, "rewards", LookMode.Deep);
            Scribe_Collections.Look(ref rewardInfos, "rewardInfos", LookMode.Deep);
            Scribe_Collections.Look(ref onActivateActions, "onActivateActions", LookMode.Deep);
            Scribe_Collections.Look(ref onCompleteActions, "onCompleteActions", LookMode.Deep);
            Scribe_Collections.Look(ref onFailActions, "onFailActions", LookMode.Deep);
            Scribe_Collections.Look(ref onSkipActions, "onSkipActions", LookMode.Deep);
            Scribe_Collections.Look(ref nextStepIds, "nextStepIds", LookMode.Value);
            Scribe_Values.Look(ref position, "position");
            Scribe_Defs.Look(ref iconThing, "iconThing");
            Scribe_Values.Look(ref iconPath, "iconPath");
            Scribe_Collections.Look(ref detailImagePaths, "detailImagePaths", LookMode.Value);
            detailImagePaths ??= new List<string>();
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("id", id));
            result.Add(new XElement("labelKey", labelKey));
            if (!descriptionKey.NullOrEmpty())
            {
                result.Add(new XElement("descriptionKey", descriptionKey));
            }
            result.Add(new XElement("completionMode", completionMode));
            if (!objectives.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(objectives, "objectives"));
            }
            if (!rewards.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(rewards, "rewards"));
            }
            if (!rewardInfos.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(rewardInfos, "rewardInfos"));
            }
            if (!onActivateActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onActivateActions, "onActivateActions"));
            }
            if (!onCompleteActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onCompleteActions, "onCompleteActions"));
            }
            if (!onFailActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onFailActions, "onFailActions"));
            }
            if (!onSkipActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onSkipActions, "onSkipActions"));
            }
            if (!nextStepIds.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList(nextStepIds, "nextStepIds"));
            }
            if (position != Vector2.zero)
            {
                result.Add(new XElement("position", position));
            }
            if (iconThing != null)
            {
                result.Add(new XElement("iconThing", iconThing.defName));
            }
            if (!iconPath.NullOrEmpty())
            {
                result.Add(new XElement("iconPath", iconPath));
            }
            if (!detailImagePaths.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList(detailImagePaths, "detailImagePaths"));
            }
            return result;
        }
    }
}
