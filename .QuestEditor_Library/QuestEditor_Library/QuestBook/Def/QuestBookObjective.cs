using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class QuestBookObjective : IExposable, ISaveable, IDrawable
    {
        [NoTranslate]
        public string labelKey;
        [NoTranslate]
        public string descriptionKey;
        [NoTranslate]
        public string iconPath;
        public bool optional;

        public string Label => labelKey.NullOrEmpty() ? "CQF_QuestBook_Objective".Translate().ToString() : labelKey.CanTranslate() ? labelKey.Translate().ToString() : labelKey;

        public string Description => descriptionKey.NullOrEmpty() ? string.Empty : descriptionKey.CanTranslate() ? descriptionKey.Translate().ToString() : descriptionKey;

        public virtual bool UsesSignal => false;

        public virtual bool UsesThingTarget => false;

        public virtual bool UsesResearchTarget => false;

        public virtual bool UsesTargetCount => false;

        public virtual bool RequiresCheck => false;

        public virtual string Signal
        {
            get => null;
            set { }
        }

        public virtual ThingDef TargetThingDef
        {
            get => null;
            set { }
        }

        public virtual ResearchProjectDef TargetResearch
        {
            get => null;
            set { }
        }

        public virtual int TargetCount
        {
            get => 1;
            set { }
        }

        public virtual IEnumerable<ThingDef> GetThingTargets()
        {
            yield break;
        }

        public abstract bool Process(QuestBookObjectiveProgress progress, Signal signal);

        public virtual bool Check(QuestBookObjectiveProgress progress)
        {
            return false;
        }

        public virtual void Draw(ref float y, Rect inRect, float x)
        {
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref descriptionKey, "descriptionKey");
            Scribe_Values.Look(ref iconPath, "iconPath");
            Scribe_Values.Look(ref optional, "optional");
        }

        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", GetType().FullName);
            result.Add(new XElement("labelKey", labelKey ?? string.Empty));
            if (!descriptionKey.NullOrEmpty())
            {
                result.Add(new XElement("descriptionKey", descriptionKey));
            }
            if (!iconPath.NullOrEmpty())
            {
                result.Add(new XElement("iconPath", iconPath));
            }
            result.Add(new XElement("optional", optional));
            return result;
        }
    }
}
