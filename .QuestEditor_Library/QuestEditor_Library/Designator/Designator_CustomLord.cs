using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    [StaticConstructorOnStartup]
    public class Designator_CustomLord : Designator
    {
        public Designator_CustomLord()
        {
            this.defaultLabel = "DesignatorLord".Translate();
            this.icon = Designator_CustomLord.icon_Lord;
            this.defaultDesc = "DesignatorLordDesc".Translate();
            this.tutorTag = "Lord";
        }
        public override bool Visible => DebugSettings.godMode;
        public override void ProcessInput(UnityEngine.Event ev)
        {
            Find.WindowStack.Add(new Window_CustomLord(Find.CurrentMap));
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return false;
        }

        public static readonly Texture2D icon_Lord = ContentFinder<Texture2D>.Get("UI/Icons/Icon_Lord");
    }
}
