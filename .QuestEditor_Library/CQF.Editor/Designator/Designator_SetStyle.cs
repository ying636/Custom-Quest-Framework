using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SetStyle : Designator_Cells
    {
        public Designator_SetStyle()
        {
            this.defaultDesc = "CQFSetStyleDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Icons/Icon_ChangeStyle");
            this.useMouseIcon = true;
        }
        public StyleCategoryDef? Style
        {
            get 
            {
                return style;
            }
        }
        public override string Label => clearStyle
            ? "CQF_ClearStyle".Translate()
            : "CQFSetStyle".Translate(this.Style?.label ?? "Null".Translate());
        public override bool Visible => DebugSettings.godMode;
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get
            {
                return DrawStyleCategoryDefOf.Areas;
            }
        }
        public override bool DragDrawMeasurements => true;
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("CQF_ClearStyle".Translate(), () =>
                {
                    style = null;
                    clearStyle = true;
                    Find.DesignatorManager.Select(this);
                });
                foreach(StyleCategoryDef def in DefDatabase<StyleCategoryDef>.AllDefsListForReading)
                {
                    yield return new FloatMenuOption(def.label, () => 
                    {
                        style = def;
                        clearStyle = false;
                        Find.DesignatorManager.Select(this);
                    });
                }
                yield break;
            }
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            AcceptanceReport report = this.CanDesignateCell(loc);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            foreach (Thing thing in loc.GetThingList(Find.CurrentMap))
            {
                if (!this.CanApplyStyle(thing)) continue;
                thing.SetStyleDef(clearStyle ? null : this.Style!.GetStyleForThingDef(thing.def));
                thing.Notify_ColorChanged();
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (Find.CurrentMap == null || !loc.InBounds(Find.CurrentMap))
                return "OutOfBounds".Translate();
            if (!clearStyle && this.Style == null)
                return "CQF_SetStyleSelectFirst".Translate();
            return loc.GetThingList(Find.CurrentMap).Exists(this.CanApplyStyle)
                ? AcceptanceReport.WasAccepted
                : "CQF_SetStyleNoTarget".Translate();
        }

        private bool CanApplyStyle(Thing thing)
        {
            if (thing.Destroyed || !(thing is ThingWithComps styledThing) || styledThing.compStyleable == null)
                return false;
            return clearStyle
                ? thing.StyleDef != null
                : this.Style?.GetStyleForThingDef(thing.def) != null;
        }

        public static StyleCategoryDef? style = null;
        private static bool clearStyle;
    }
}
