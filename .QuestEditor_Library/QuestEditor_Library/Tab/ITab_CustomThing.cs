using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ITab_CustomThing : ITab
    {
        public ITab_CustomThing()
        {
            this.size = new Vector2(560f, 620f);
            this.labelKey = "ITab_CustomThing";
            this.tutorTag = "CustomThing";
        }

        public IDrawTabable Thing
        {
            get
            {
                if (this.SelObject is IDrawTabable thing)
                {
                    return thing;
                }

                if (this.thing is IDrawTabable thing2)
                {
                    return thing2;
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
            using CQFUIScope scope = new CQFUIScope(this.CQFSize.x, this.CQFSize.y);
            using (new CQFEditorContext(this.Thing as Thing))
            {
                this.Thing?.DrawTab();
            }
        }

        public Thing thing;
    }
}
