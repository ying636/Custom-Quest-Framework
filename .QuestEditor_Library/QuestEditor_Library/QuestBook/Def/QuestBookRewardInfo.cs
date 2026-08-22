using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookRewardInfo : IExposable, ISaveable
    {
        public string Label => labelKey.NullOrEmpty() ? string.Empty : labelKey.CanTranslate() ? labelKey.Translate().ToString() : labelKey;

        public string Description => descriptionKey.NullOrEmpty() ? string.Empty : descriptionKey.CanTranslate() ? descriptionKey.Translate().ToString() : descriptionKey;

        public bool HasContent => !Label.NullOrEmpty() || !Description.NullOrEmpty() || iconThing != null || !iconPath.NullOrEmpty();

        public void ExposeData()
        {
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref descriptionKey, "descriptionKey");
            Scribe_Defs.Look(ref iconThing, "iconThing");
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
            if (iconThing != null)
            {
                result.Add(new XElement("iconThing", iconThing.defName));
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
        public ThingDef iconThing;
        [NoTranslate]
        public string iconPath;

    }
}
