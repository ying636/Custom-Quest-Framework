using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_Terrain : Designator_Cells
    {
        public Designator_Terrain()
        {
            this.defaultLabel = Designator_Terrain.terrain?.label;
            this.icon = Designator_Terrain.terrain.uiIcon;
            this.defaultDesc = "Designator_TerrainAndRoofDesc".Translate();
            this.useMouseIcon = true;
        }
        public override Color IconDrawColor => terrain?.DrawColor ?? base.IconDrawColor;
        public override bool Visible => DebugSettings.godMode;
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get
            {
                return QEDefOf.CQF_Areas;
            }
        }
        public override bool DragDrawMeasurements => true;
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {   List<FloatMenuOption> results = CQFEditorTools.DrawFloatMenuWithRsult<TerrainDef>(DefDatabase<TerrainDef>.AllDefs.ToList(),def => 
                {
                    Designator_Terrain.terrain = def;
                    this.defaultLabel = def.label;
                    this.icon = def.GetUIIconForStuff(null);
                }
                ,
                def => def.label
                );
                yield return new FloatMenuOption("Select".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_Select<TerrainDef>(DefDatabase<TerrainDef>.AllDefsListForReading,
           x => x.uiIcon, x => x.label, "Select".Translate(),
           x =>
           {
               Designator_Terrain.terrain = x;
               this.defaultLabel = x.label;
               this.icon = x.GetUIIconForStuff(null);
           }, t => t.DrawColor, (t, r) => Widgets.DefIcon(r, t, null,1,null,false, t.DrawColor)));
                });
                yield break;
            }
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (loc.InBounds(Find.CurrentMap))
            {
                TerrainDef def = Designator_Terrain.terrain;
                Find.CurrentMap.terrainGrid.SetTerrain(loc, def);
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public static TerrainDef terrain = TerrainDefOf.Bridge;
    }
}
