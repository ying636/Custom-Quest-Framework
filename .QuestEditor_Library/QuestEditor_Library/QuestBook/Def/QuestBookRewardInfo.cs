using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookRewardInfo : IExposable, ISaveable
    {
        public string Label => labelKey.NullOrEmpty() ? string.Empty : labelKey.CanTranslate() ? labelKey.Translate().ToString() : labelKey;

        public string Description => descriptionKey.NullOrEmpty() ? string.Empty : descriptionKey.CanTranslate() ? descriptionKey.Translate().ToString() : descriptionKey;

        public bool HasContent => !Label.NullOrEmpty() || !Description.NullOrEmpty() || !iconPath.NullOrEmpty();

        public void ExposeData()
        {
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref descriptionKey, "descriptionKey");
            Scribe_Values.Look(ref iconPath, "iconPath");
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("labelKey", labelKey ?? string.Empty));
            if (!descriptionKey.NullOrEmpty())
            {
                result.Add(new XElement("descriptionKey", descriptionKey));
            }
            if (!iconPath.NullOrEmpty())
            {
                result.Add(new XElement("iconPath", iconPath));
            }
            return result;
        }

        [NoTranslate]
        public string labelKey;
        [NoTranslate]
        public string descriptionKey;
        [NoTranslate]
        public string iconPath;

    }
}
