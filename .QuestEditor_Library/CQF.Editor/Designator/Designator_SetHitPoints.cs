using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SetHitPoints : Designator_Cells
    {
        public Designator_SetHitPoints()
        {
            this.icon = TexButton.OpenStatsReport;
            this.defaultDesc = "CQFSetHitPointsDesc".Translate();
            this.useMouseIcon = true;
        }

        public override string Label => "CQFSetHitPoints".Translate(this.ModeLabel);

        public override bool Visible => DebugSettings.godMode;

        public override DrawStyleCategoryDef DrawStyleCategory => QEDefOf.CQF_Areas;

        public override bool DragDrawMeasurements => true;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("CQF_SetHitPointsPercentage".Translate(), this.OpenPercentageSlider);
                yield return new FloatMenuOption("-10", () => this.SetAdjustmentMode(-10));
                yield return new FloatMenuOption("+10", () => this.SetAdjustmentMode(10));
                yield return new FloatMenuOption("-100", () => this.SetAdjustmentMode(-100));
                yield return new FloatMenuOption("+100", () => this.SetAdjustmentMode(100));
            }
        }

        private string ModeLabel => this.usePercentage
            ? this.percentage.ToString() + "%"
            : this.adjustment.ToString("+0;-0;0");

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return;
            }
            foreach (Thing thing in loc.GetThingList(Find.CurrentMap))
            {
                if (!thing.def.useHitPoints || thing.Destroyed)
                {
                    continue;
                }
                int hitPoints = this.usePercentage
                    ? Mathf.RoundToInt(thing.MaxHitPoints * this.percentage / 100f)
                    : thing.HitPoints + this.adjustment;
                int oldHitPoints = thing.HitPoints;
                thing.HitPoints = Mathf.Clamp(hitPoints, 1, thing.MaxHitPoints);
                if (thing is Building building)
                {
                    BuildingsDamageSectionLayerUtility.Notify_BuildingHitPointsChanged(building, oldHitPoints);
                }
                Find.CurrentMap.mapDrawer.MapMeshDirty(thing.Position, MapMeshFlagDefOf.Things);
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return false;
            }
            return loc.GetThingList(Find.CurrentMap).Exists(thing => thing.def.useHitPoints && !thing.Destroyed)
                ? AcceptanceReport.WasAccepted
                : "CQF_SetHitPointsNoTarget".Translate();
        }

        private void OpenPercentageSlider()
        {
            Find.WindowStack.Add(new Dialog_Slider(
                value => "CQF_SetHitPointsPercentagePrompt".Translate(value),
                1,
                100,
                value =>
                {
                    this.percentage = value;
                    this.usePercentage = true;
                    Find.DesignatorManager.Select(this);
                },
                this.percentage));
        }

        private void SetAdjustmentMode(int value)
        {
            this.adjustment = value;
            this.usePercentage = false;
        }

        private bool usePercentage = true;
        private int percentage = 100;
        private int adjustment;
    }
}
