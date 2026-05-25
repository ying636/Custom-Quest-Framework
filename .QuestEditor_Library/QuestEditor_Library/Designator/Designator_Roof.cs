using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_Roof : Designator_Cells
    {
        public Designator_Roof()
        {
            this.defaultLabel = Designator_Roof.roof?.label;
            this.icon = TexButton.ShowRoofOverlay;
            this.defaultDesc = "Designator_TerrainAndRoofDesc".Translate();
            this.useMouseIcon = true;
        }
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
                List<FloatMenuOption> results = CQFEditorTools.DrawFloatMenuWithRsult<RoofDef>(DefDatabase<RoofDef>.AllDefs.ToList(), def =>
                 {
                     Designator_Roof.roof = def;
                     this.defaultLabel = def.label;
                     this.icon = TexButton.ShowRoofOverlay;
                 }
                ,
                def => def.label
                );
                foreach (FloatMenuOption result in results)
                {
                    yield return result;
                }
                yield return new FloatMenuOption("RemoveRoof".Translate(), () =>
                 {
                     Designator_Roof.roof = null;
                     this.defaultLabel = "RemoveRoof".Translate();
                 });
                yield break;
            }
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                RoofDef def = Designator_Roof.roof;
                Find.CurrentMap.roofGrid.SetRoof(loc, def);
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public static RoofDef roof = RoofDefOf.RoofConstructed;
    }
}
