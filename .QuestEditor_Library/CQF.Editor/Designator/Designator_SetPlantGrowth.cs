using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SetPlantGrowth : Designator_Cells
    {
        public Designator_SetPlantGrowth()
        {
            this.icon = TexButton.OpenStatsReport;
            this.defaultDesc = "CQFSetPlantGrowthDesc".Translate();
            this.useMouseIcon = true;
        }

        public override string Label => "CQFSetPlantGrowth".Translate(this.growthPercentage);

        public override bool Visible => DebugSettings.godMode;

        public override DrawStyleCategoryDef DrawStyleCategory => QEDefOf.CQF_Areas;

        public override bool DragDrawMeasurements => true;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("CQF_SetPlantGrowthPercentage".Translate(), this.OpenGrowthSlider);
            }
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return;
            }
            Plant plant = loc.GetPlant(Find.CurrentMap);
            if (plant == null || plant.Destroyed)
            {
                return;
            }
            plant.Growth = this.growthPercentage / 100f;
            Find.CurrentMap.mapDrawer.SectionAt(loc).RegenerateAllLayers();
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return false;
            }
            return loc.GetPlant(Find.CurrentMap) != null
                ? AcceptanceReport.WasAccepted
                : "CQF_SetPlantGrowthNoTarget".Translate();
        }

        private void OpenGrowthSlider()
        {
            Find.WindowStack.Add(new Dialog_Slider(
                value => "CQF_SetPlantGrowthPercentagePrompt".Translate(value),
                0,
                100,
                value =>
                {
                    this.growthPercentage = value;
                    Find.DesignatorManager.Select(this);
                },
                this.growthPercentage));
        }

        private int growthPercentage = 100;
    }
}
