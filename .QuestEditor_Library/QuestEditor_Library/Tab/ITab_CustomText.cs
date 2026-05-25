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
            this.size = new Vector2(400f, 300f);
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
            if (this.Comp != null)
            {
                Rect rect = new Rect(5f, 5f, 350f, 25f);
                Widgets.CheckboxLabeled(rect, "UseCustomName".Translate(), ref this.Comp.useCustomName);
                rect.y += 30f;
                if (this.Comp.useCustomName)
                {
                    CQFEditorTools.DrawLabelAndText_Line(rect.y, "CQF_CustomName".Translate(), ref this.Comp.customName, 5f, 350f);
                    rect.y += 30f;
                }
                Widgets.CheckboxLabeled(rect, "UseCustomDescription".Translate(), ref this.Comp.useCustomDescription);
                rect.y += 30f;
                if (this.Comp.useCustomDescription)
                {
                    CQFEditorTools.DrawLabelAndText_Line(rect.y, "CQF_CustomDescription".Translate(), ref this.Comp.customDescription, 5f, 350f);
                    rect.y += 30f;
                }
            }
        }
    }
}
