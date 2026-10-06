using System;
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
        public bool iconManuallySelected;
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual void DrawSpecial(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawSpecial(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref descriptionKey, "descriptionKey");
            Scribe_Values.Look(ref iconPath, "iconPath");
            Scribe_Values.Look(ref iconManuallySelected, "iconManuallySelected");
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
            result.Add(new XElement("iconManuallySelected", iconManuallySelected));
            result.Add(new XElement("optional", optional));
            return result;
        }

        protected internal void DrawCommonStart(ref float y, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawCommonStart(Ref:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        protected internal void DrawCommonRules(ref float y, Rect inRect)

        {
            object[] arguments = new object[]
            {
                y,
                inRect
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawCommonRules(Ref:float,None:UnityEngine.Rect)", this, arguments);
            y = (float)arguments[0];
        }
        public delegate void DetectionContentDrawer(Rect card, ref float y);

        protected internal void DrawDetectionSection(ref float y, Rect inRect, DetectionContentDrawer contentDrawer)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                contentDrawer
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawDetectionSection(Ref:float,None:UnityEngine.Rect,None:QuestEditor_Library.QuestBookObjective.DetectionContentDrawer)", this, arguments);
            y = (float)arguments[0];
        }
        protected internal void DrawSection(ref float y, float width, string titleKey, float height, Action<Rect> contentDrawer)

        {
            object[] arguments = new object[]
            {
                y,
                width,
                titleKey,
                height,
                contentDrawer
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawSection(Ref:float,None:float,None:string,None:float,None:System.Action<UnityEngine.Rect>)", this, arguments);
            y = (float)arguments[0];
        }
        protected internal void DrawTextField(Rect card, ref float y, string labelKey, ref string value, bool multiline)

        {
            object[] arguments = new object[]
            {
                card,
                y,
                labelKey,
                value,
                multiline
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawTextField(None:UnityEngine.Rect,Ref:float,None:string,Ref:string,None:bool)", this, arguments);
            y = (float)arguments[1];
            value = (string)arguments[3];
        }
        protected internal void DrawRowLabel(Rect card, float y, string labelKey)

        {
            object[] arguments = new object[]
            {
                card,
                y,
                labelKey
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawRowLabel(None:UnityEngine.Rect,None:float,None:string)", this, arguments);
        }
        protected internal void DrawTextButton(Rect rect, string labelKey, Action action)

        {
            object[] arguments = new object[]
            {
                rect,
                labelKey,
                action
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawTextButton(None:UnityEngine.Rect,None:string,None:System.Action)", this, arguments);
        }
        protected internal void DrawObjectiveIcon(Rect rect)
        {
            if (!iconPath.NullOrEmpty())
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(iconPath, false);
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(rect, texture, 1f);
                    return;
                }
            }
            if (TargetThingDef != null)
            {
                Widgets.DefIcon(rect, TargetThingDef);
                return;
            }
            Widgets.DrawTextureFitted(rect, TexButton.Info, 1f);
        }
        internal void SelectThingIcon()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.SelectThingIcon()", this, arguments);
        }
        internal void SelectImageIcon()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.SelectImageIcon()", this, arguments);
        }
        internal void ClearIcon()
        {
            iconPath = null;
            iconManuallySelected = false;
        }
    }
}
