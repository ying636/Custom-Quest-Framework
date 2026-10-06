using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Window_DesignatorTerrainPalette : Window_DesignatorPalette<TerrainDef>
    {
        public Window_DesignatorTerrainPalette(Designator_Terrain designator)
        {
            this.designator = designator;
        }

        protected override string PaletteTitle => "CQF_TerrainPalette".Translate();

        protected override IReadOnlyList<TerrainDef> AllItems => DefDatabase<TerrainDef>.AllDefsListForReading;

        protected override IReadOnlyList<TerrainDef> RecentItems => Designator_Terrain.RecentSelections;

        protected override string GetLabel(TerrainDef item)
        {
            return item.label ?? item.defName;
        }

        protected override string GetTip(TerrainDef item)
        {
            string label = this.GetLabel(item);
            return item.description.NullOrEmpty() ? label : label + "\n\n" + item.description;
        }

        protected override void DrawIcon(TerrainDef item, Rect rect)
        {
            Widgets.DefIcon(rect, item, null, 1f, null, true, item.DrawColor);
        }

        protected override void SelectItem(TerrainDef item)
        {
            this.designator.SelectTerrain(item);
        }

        protected override bool IsSelected(TerrainDef item)
        {
            return item == Designator_Terrain.terrain;
        }

        private readonly Designator_Terrain designator;
    }
}
