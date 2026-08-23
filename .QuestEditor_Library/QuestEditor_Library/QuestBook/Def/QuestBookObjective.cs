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
            DrawCommonStart(ref y, inRect);
            DrawSpecial(ref y, inRect, x);
            DrawCommonRules(ref y, inRect);
        }

        public virtual void DrawSpecial(ref float y, Rect inRect, float x)
        {
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

        protected void DrawCommonStart(ref float y, Rect inRect)
        {
            float width = inRect.width - 16f;
            Widgets.Label(new Rect(8f, y, width, 32f), "CQF_QuestBook_ObjectiveEditor".Translate().Colorize(ColorLibrary.SkyBlue));
            y += 38f;
            DrawSection(ref y, width, "CQF_QuestBook_ObjectiveBasic", 154f, card =>
            {
                float rowY = card.y + 46f;
                DrawTextField(card, ref rowY, "CQF_QuestBook_ObjectiveName", ref labelKey, false);
                DrawTextField(card, ref rowY, "CQF_QuestBook_ObjectiveDescription", ref descriptionKey, true);
            });
            DrawSection(ref y, width, "CQF_QuestBook_ObjectiveIcon", 160f, card =>
            {
                Rect previewRect = new Rect(card.x + 14f, card.y + 44f, 64f, 64f);
                Widgets.DrawBox(previewRect, 1);
                DrawObjectiveIcon(previewRect.ContractedBy(8f));
                float buttonX = previewRect.xMax + 18f;
                DrawTextButton(new Rect(buttonX, previewRect.y, 168f, 26f), "CQF_QuestBook_SelectThingIcon", SelectThingIcon);
                DrawTextButton(new Rect(buttonX, previewRect.y + 32f, 168f, 26f), "CQF_QuestBook_SelectImageIcon", SelectImageIcon);
                DrawTextButton(new Rect(buttonX, previewRect.y + 64f, 100f, 26f), "CQF_QuestBook_Clear", ClearIcon);
            });
        }

        protected void DrawCommonRules(ref float y, Rect inRect)
        {
            DrawSection(ref y, inRect.width - 16f, "CQF_QuestBook_ObjectiveRules", 82f, card =>
            {
                Rect toggleRect = new Rect(card.x + 14f, card.y + 48f, card.width - 28f, 28f);
                Widgets.DrawHighlightIfMouseover(toggleRect);
                Widgets.CheckboxLabeled(toggleRect, "CQF_QuestBook_Optional".Translate(), ref optional, placeCheckboxNearText: false);
                TooltipHandler.TipRegion(toggleRect, "CQF_QuestBook_OptionalTip".Translate());
            });
        }

        protected void DrawDetectionSection(ref float y, Rect inRect, int fieldCount, Action<Rect> contentDrawer)
        {
            DrawSection(ref y, inRect.width - 16f, "CQF_QuestBook_ObjectiveDetection", 52f + fieldCount * 36f + 10f, card =>
            {
                float rowY = card.y + 48f;
                Widgets.Label(new Rect(card.x + 14f, rowY + 2f, 164f, 24f), "CQF_QuestBook_ObjectiveType".Translate());
                Widgets.Label(new Rect(card.x + 184f, rowY + 2f, card.width - 198f, 24f), GetType().Name.Translate().Colorize(ColorLibrary.SkyBlue));
                rowY += 36f;
                contentDrawer(card);
            });
        }

        protected void DrawSection(ref float y, float width, string titleKey, float height, Action<Rect> contentDrawer)
        {
            Rect card = new Rect(8f, y, width, height);
            Widgets.DrawMenuSection(card);
            Widgets.Label(new Rect(card.x + 14f, card.y + 10f, card.width - 28f, 28f), titleKey.Translate().Colorize(ColorLibrary.PaleBlue));
            contentDrawer(card);
            y += height + 12f;
        }

        protected void DrawTextField(Rect card, ref float y, string labelKey, ref string value, bool multiline)
        {
            float fieldX = card.x + 184f;
            float fieldWidth = card.width - 198f;
            float fieldHeight = multiline ? 54f : 28f;
            Widgets.Label(new Rect(card.x + 14f, y + 2f, 164f, 24f), labelKey.Translate());
            Rect field = new Rect(fieldX, y, fieldWidth, fieldHeight);
            value = multiline ? Widgets.TextArea(field, value ?? string.Empty) : Widgets.TextField(field, value ?? string.Empty);
            y += multiline ? 64f : 36f;
        }

        protected void DrawRowLabel(Rect card, float y, string labelKey)
        {
            Widgets.Label(new Rect(card.x + 14f, y + 2f, 164f, 24f), labelKey.Translate());
        }

        protected void DrawTextButton(Rect rect, string labelKey, Action action)
        {
            if (Widgets.ButtonText(rect, labelKey.Translate()))
            {
                action();
            }
        }

        protected void DrawObjectiveIcon(Rect rect)
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

        private void SelectThingIcon()
        {
            QuestBookTextureEntry.OpenSelect(path =>
            {
                iconPath = path;
                iconManuallySelected = true;
            }, "CQF_QuestBook_SelectThingIcon");
        }

        private void SelectImageIcon()
        {
            Find.WindowStack.Add(new Dialog_SelectDialogImage(path =>
            {
                iconPath = path;
                iconManuallySelected = true;
            }, iconPath));
        }

        private void ClearIcon()
        {
            iconPath = null;
            iconManuallySelected = false;
        }
    }
}
