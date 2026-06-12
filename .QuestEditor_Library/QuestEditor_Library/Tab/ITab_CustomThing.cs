using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
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
        public override bool IsVisible => DebugSettings.godMode;
        protected override bool StillValid => DebugSettings.godMode;
        protected override void FillTab()
        {
            this?.Thing?.DrawTab();
        }

        public Thing thing;
    }
}
