using System;
using System.Collections.Generic;
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
        public static IReadOnlyList<TerrainDef> RecentSelections => recentSelections;
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("Select".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_Select<TerrainDef>(new TextureSelectDrawer<TerrainDef>(DefDatabase<TerrainDef>.AllDefsListForReading, x => x.uiIcon, x => x.label, x =>
           {
               this.SelectTerrain(x);
           }, t => t.DrawColor, (t, r) => Widgets.DefIcon(r, t, null,1,null,false, t.DrawColor), null, null, null, null, null), "Select".Translate()));
                });
                yield return new FloatMenuOption("CQF_OpenFloatingPalette".Translate(), () =>
                {
                    Find.WindowStack.Add(new Window_DesignatorTerrainPalette(this));
                });
                yield break;
            }
        }
        public void SelectTerrain(TerrainDef def)
        {
            Designator_Terrain.terrain = def;
            this.defaultLabel = def.label;
            this.icon = def.GetUIIconForStuff(null);
            this.RecordRecentSelection(def);
            Find.DesignatorManager.Select(this);
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

        private void RecordRecentSelection(TerrainDef def)
        {
            recentSelections.Remove(def);
            recentSelections.Insert(0, def);
            if (recentSelections.Count > RecentSelectionLimit)
            {
                recentSelections.RemoveRange(RecentSelectionLimit, recentSelections.Count - RecentSelectionLimit);
            }
        }

        public static TerrainDef terrain = TerrainDefOf.Bridge;

        private const int RecentSelectionLimit = 5;

        private static readonly List<TerrainDef> recentSelections = new List<TerrainDef>();
    }
}
