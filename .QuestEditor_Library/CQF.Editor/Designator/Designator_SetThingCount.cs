using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SetThingCount : Designator_Cells
    {
        public Designator_SetThingCount()
        {
            this.icon = TexButton.OpenStatsReport;
            this.defaultDesc = "CQFSetThingCountDesc".Translate();
            this.useMouseIcon = true;
        }

        public override string Label
        {
            get
            {
                switch (this.mode)
                {
                    case ThingCountMode.Add:
                        return "CQFSetThingCount".Translate(this.AdjustmentLabelKey.Translate());
                    case ThingCountMode.Subtract:
                        return "CQFSetThingCount".Translate(this.AdjustmentLabelKey.Translate());
                    default:
                        return "CQFSetThingCount".Translate(this.customCount);
                }
            }
        }

        public override bool Visible => DebugSettings.godMode;

        public override DrawStyleCategoryDef DrawStyleCategory => QEDefOf.CQF_Areas;

        public override bool DragDrawMeasurements => true;

        private string AdjustmentLabelKey => this.adjustment switch
        {
            1 => "CQF_SetThingCountAdd1",
            -1 => "CQF_SetThingCountSubtract1",
            10 => "CQF_SetThingCountAdd10",
            -10 => "CQF_SetThingCountSubtract10",
            75 => "CQF_SetThingCountAdd75",
            -75 => "CQF_SetThingCountSubtract75",
            _ => "CQF_SetThingCountCustom"
        };

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("CQF_SetThingCountCustom".Translate(), this.OpenCustomCountSlider);
                yield return new FloatMenuOption("CQF_SetThingCountAdd1".Translate(), () => this.SetAdjustmentMode(1));
                yield return new FloatMenuOption("CQF_SetThingCountSubtract1".Translate(), () => this.SetAdjustmentMode(-1));
                yield return new FloatMenuOption("CQF_SetThingCountAdd10".Translate(), () => this.SetAdjustmentMode(10));
                yield return new FloatMenuOption("CQF_SetThingCountSubtract10".Translate(), () => this.SetAdjustmentMode(-10));
                yield return new FloatMenuOption("CQF_SetThingCountAdd75".Translate(), () => this.SetAdjustmentMode(75));
                yield return new FloatMenuOption("CQF_SetThingCountSubtract75".Translate(), () => this.SetAdjustmentMode(-75));
            }
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return;
            }
            foreach (Thing thing in loc.GetThingList(Find.CurrentMap))
            {
                if (!this.IsAdjustableItem(thing))
                {
                    continue;
                }
                int oldStackCount = thing.stackCount;
                int targetStackCount = this.GetTargetStackCount(thing);
                if (oldStackCount == targetStackCount)
                {
                    continue;
                }
                thing.stackCount = targetStackCount;
                Find.CurrentMap.mapDrawer.MapMeshDirty(thing.Position, MapMeshFlagDefOf.Things);
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!loc.InBounds(Find.CurrentMap))
            {
                return false;
            }
            return loc.GetThingList(Find.CurrentMap).Exists(this.IsAdjustableItem)
                ? AcceptanceReport.WasAccepted
                : "CQF_SetThingCountNoTarget".Translate();
        }

        private void OpenCustomCountSlider()
        {
            Find.WindowStack.Add(new Dialog_SetThingCount(
                value =>
                {
                    this.customCount = value;
                    this.mode = ThingCountMode.Custom;
                    Find.DesignatorManager.Select(this);
                },
                this.customCount));
        }

        private void SetAdjustmentMode(int value)
        {
            this.adjustment = value;
            this.mode = value > 0 ? ThingCountMode.Add : ThingCountMode.Subtract;
            Find.DesignatorManager.Select(this);
        }

        private int GetTargetStackCount(Thing thing)
        {
            int targetStackCount = this.mode == ThingCountMode.Custom
                ? this.customCount
                : thing.stackCount + this.adjustment;
            return Mathf.Clamp(targetStackCount, 1, thing.def.stackLimit);
        }

        private bool IsAdjustableItem(Thing thing)
        {
            return thing != null
                && !thing.Destroyed
                && thing.def.category == ThingCategory.Item
                && thing.def.stackLimit > 1;
        }

        private ThingCountMode mode = ThingCountMode.Custom;
        private int customCount = 1;
        private int adjustment;

        private enum ThingCountMode
        {
            Custom,
            Add,
            Subtract
        }
    }
}
