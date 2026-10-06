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
        internal object CQFSelectedObject => this.SelObject;
        internal Vector2 CQFSize => this.size;

        public override bool IsVisible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;
        protected override bool StillValid => CQFEditorBridge.IsLoaded && DebugSettings.godMode;

        protected override void FillTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.ITab_CustomText.FillTab()", this, arguments);
        }
        internal void DrawTextSection(ref float y, float width, string label, ref bool enabled, ref string text, bool multiline)

        {
            object[] arguments = new object[]
            {
                y,
                width,
                label,
                enabled,
                text,
                multiline
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.ITab_CustomText.DrawTextSection(Ref:float,None:float,None:string,Ref:bool,Ref:string,None:bool)", this, arguments);
            y = (float)arguments[0];
            enabled = (bool)arguments[3];
            text = (string)arguments[4];
        }
}
}
