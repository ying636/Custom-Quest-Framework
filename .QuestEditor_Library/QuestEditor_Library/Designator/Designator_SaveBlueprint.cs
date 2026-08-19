using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SaveBlueprint : Designator
    {
        public Designator_SaveBlueprint(Designator_Blueprint blueprintDesignator, SaveMode saveMode)
        {
            this.blueprintDesignator = blueprintDesignator;
            this.saveMode = saveMode;
            this.defaultLabel = "CQF_SaveBlueprint".Translate();
            this.defaultDesc = "CQF_SaveBlueprintDesc".Translate();
            string iconPath = saveMode == SaveMode.Rectangle
                ? "UI/Icon_SaveZoneAsDef_Rectangle"
                : "UI/Icon_SaveZoneAsDef_Round";
            this.icon = ContentFinder<Texture2D>.Get(iconPath, false) ?? TexButton.NewFile;
            this.useMouseIcon = true;
        }

        public override bool DragDrawMeasurements => true;

        public override DrawStyleCategoryDef DrawStyleCategory => this.saveMode == SaveMode.Rectangle
            ? DrawStyleCategoryDefOf.Areas
            : DrawStyleCategoryDefOf.Paint;

        public void BeginSelection()
        {
            if (Find.CurrentMap == null)
            {
                Messages.Message("CQF_NoCurrentMap".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            if (this.saveMode == SaveMode.Rectangle)
            {
                Find.DesignatorManager.Select(this);
                return;
            }
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = false,
                canTargetLocations = true
            }, this.SelectCenter, target => this.DrawSaveMouseAttachment(), target => true);
        }

        public void SaveWholeMap()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("CQF_NoCurrentMap".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            BlueprintRepository.CreateFromMap(map, map.AllCells.ToList(), this.blueprintDesignator.SelectBlueprint);
        }

        public override void DesignateSingleCell(IntVec3 cell)
        {
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            if (this.saveMode == SaveMode.Rectangle)
            {
                this.SaveCells(cells.ToList());
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return Find.CurrentMap != null && loc.InBounds(Find.CurrentMap);
        }

        public override void DrawMouseAttachments()
        {
            GenUI.DrawMouseAttachment(this.icon, string.Empty, this.iconAngle, this.iconOffset);
        }

        private void SelectCenter(LocalTargetInfo centerTarget)
        {
            IntVec3 center = centerTarget.Cell;
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = false,
                canTargetLocations = true
            }, target => this.SaveFromCenter(center, target.Cell),
            target => this.DrawCenteredPreview(center, target.Cell), target => true);
        }

        private void SaveFromCenter(IntVec3 center, IntVec3 target)
        {
            if (this.saveMode == SaveMode.Round)
            {
                float radius = center.DistanceTo(target);
                if (radius > GenRadial.MaxRadialPatternRadius)
                {
                    radius = GenRadial.MaxRadialPatternRadius;
                    Messages.Message("OutOfVanillaRange".Translate(GenRadial.MaxRadialPatternRadius),
                        MessageTypeDefOf.CautionInput);
                }
                this.SaveCells(GenRadial.RadialCellsAround(center, radius, true).ToList());
                return;
            }
            CellRect rect = CellRect.CenteredOn(center, (int)center.DistanceTo(target));
            this.SaveCells(rect.Cells.ToList());
        }

        private void DrawCenteredPreview(IntVec3 center, IntVec3 target)
        {
            this.DrawSaveMouseAttachment();
            GenDraw.DrawTargetHighlight(center);
            GenDraw.DrawTargetHighlight(target);
            if (this.saveMode == SaveMode.Round)
            {
                float radius = center.DistanceTo(target);
                GenDraw.DrawRadiusRing(center, radius > GenRadial.MaxRadialPatternRadius
                    ? GenRadial.MaxRadialPatternRadius
                    : radius);
                return;
            }
            CellRect rect = CellRect.CenteredOn(center, (int)center.DistanceTo(target));
            GenDraw.DrawFieldEdges(rect.Cells.ToList());
        }

        private void SaveCells(List<IntVec3> cells)
        {
            BlueprintRepository.CreateFromMap(Find.CurrentMap, cells, this.blueprintDesignator.SelectBlueprint);
        }

        private void DrawSaveMouseAttachment()
        {
            GenUI.DrawMouseAttachment(this.icon, string.Empty, this.iconAngle, this.iconOffset);
        }

        private readonly Designator_Blueprint blueprintDesignator;
        private readonly SaveMode saveMode;
    }
}
