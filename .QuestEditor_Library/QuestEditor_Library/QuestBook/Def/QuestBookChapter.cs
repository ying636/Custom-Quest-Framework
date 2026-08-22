using System.Collections.Generic;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookChapter : IExposable, ISaveable
    {
        public string id;
        [NoTranslate]
        public string labelKey;
        [NoTranslate]
        public string descriptionKey;
        public List<QuestBookStep> steps = new List<QuestBookStep>();
        public List<CQFAction> onUnlockActions = new List<CQFAction>();
        public List<CQFAction> onCompleteActions = new List<CQFAction>();

        public string Label => labelKey.NullOrEmpty() ? id : labelKey.CanTranslate() ? labelKey.Translate().ToString() : labelKey;

        public string Description => descriptionKey.NullOrEmpty() ? string.Empty : descriptionKey.CanTranslate() ? descriptionKey.Translate().ToString() : descriptionKey;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref descriptionKey, "descriptionKey");
            Scribe_Collections.Look(ref steps, "steps", LookMode.Deep);
            Scribe_Collections.Look(ref onUnlockActions, "onUnlockActions", LookMode.Deep);
            Scribe_Collections.Look(ref onCompleteActions, "onCompleteActions", LookMode.Deep);
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
            if (!steps.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(steps, "steps"));
            }
            if (!onUnlockActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onUnlockActions, "onUnlockActions"));
            }
            if (!onCompleteActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onCompleteActions, "onCompleteActions"));
            }
            return result;
        }
    }
}
