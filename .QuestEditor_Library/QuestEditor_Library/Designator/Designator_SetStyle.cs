using System;
using System.Collections.Generic;
using System.Linq;
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
        public StyleCategoryDef Style 
        {
            get 
            {
                return style;
            }
        }
        public override string Label => "CQFSetStyle".Translate(this.Style?.label ?? "Null".Translate());
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
                foreach(StyleCategoryDef def in DefDatabase<StyleCategoryDef>.AllDefsListForReading)
                {
                    yield return new FloatMenuOption(def.label, () => 
                    {
                        style = def;
                    });
                }
                yield break;
            }
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                loc.GetThingList(Find.CurrentMap).ForEach(t => 
                {
                    if (this.Style.GetStyleForThingDef(t.def) is ThingStyleDef style) 
                    {
                        t.SetStyleDef(style);
                        Find.CurrentMap.mapDrawer.MapMeshDirty(t.Position, MapMeshFlagDefOf.Things);
                    }
                });
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public static StyleCategoryDef style = null;
    }
}
