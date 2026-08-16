using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ITab_CustomText : ITab
    {
        public ITab_CustomText()
        {
            this.size = new Vector2(460f, 400f);
            this.labelKey = "ITab_CustomText";
            this.tutorTag = "CustomText";
        }

        public CompCustomText Comp
        {
            get
            {
                if (this.SelObject is ThingWithComps thing && thing.TryGetComp<CompCustomText>() is CompCustomText comp)
                {
                    return comp;
                }
                return null;
            }
        }
        public override bool IsVisible => DebugSettings.godMode;
        protected override bool StillValid => DebugSettings.godMode;

        protected override void FillTab()
        {
            if (this.Comp == null)
            {
                return;
            }

            float y = 38f;
            float width = this.size.x - 16f;
            this.DrawTextSection(ref y, width, "CQF_CustomName".Translate(), ref this.Comp.useCustomName, ref this.Comp.customName, false);
            this.DrawTextSection(ref y, width, "CQF_CustomDescription".Translate(), ref this.Comp.useCustomDescription, ref this.Comp.customDescription, true);
            this.DrawTextSection(ref y, width, "CQF_CustomInspectText".Translate(), ref this.Comp.useCustomInspectText, ref this.Comp.customInspectText, true);
        }

        private void DrawTextSection(ref float y, float width, string label, ref bool enabled, ref string text, bool multiline)
        {
            float editorHeight = multiline ? 58f : 30f;
            float sectionHeight = 38f + (enabled ? editorHeight + 10f : 0f);
            Rect sectionRect = new Rect(8f, y, width, sectionHeight);
            Rect headerRect = new Rect(sectionRect.x, sectionRect.y, sectionRect.width, 32f);
            Widgets.DrawBoxSolid(sectionRect, new Color(0.07f, 0.08f, 0.09f, 0.72f));
            Widgets.DrawBoxSolid(headerRect, new Color(0.14f, 0.17f, 0.2f, 0.9f));
            Widgets.Label(new Rect(headerRect.x + 10f, headerRect.y + 4f, headerRect.width - 50f, 25f), label);
            Widgets.Checkbox(new Vector2(headerRect.xMax - 30f, headerRect.y + 4f), ref enabled, 24f);

            if (enabled)
            {
                Rect editorRect = new Rect(sectionRect.x + 8f, headerRect.yMax + 6f, sectionRect.width - 16f, editorHeight);
                text = multiline ? Widgets.TextArea(editorRect, text ?? string.Empty) : Widgets.TextField(editorRect, text ?? string.Empty);
            }

            Widgets.DrawBox(sectionRect, 1);
            y += sectionHeight + 8f;
        }
    }
}
