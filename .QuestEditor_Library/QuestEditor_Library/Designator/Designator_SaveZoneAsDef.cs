using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_SaveZoneAsDef : Designator
    {
        public Designator_SaveZoneAsDef()
        {
            this.useMouseIcon = true;
        }
        public override string Label => ("Designator_" + this.saveMode.ToString()).Translate();
        public override string Desc => ("Designator_" + this.saveMode.ToString() + "Desc").Translate();
        public override bool Visible => DebugSettings.godMode; 
        public override bool DragDrawMeasurements => true;
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get
            {
                return this.saveMode == SaveMode.Rectangle ? DrawStyleCategoryDefOf.Areas : DrawStyleCategoryDefOf.Paint;
            }
        }
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions 
        {
            get 
            {
                yield return new FloatMenuOption("SaveMode_Round".Translate(),() => this.saveMode = SaveMode.Round);
                yield return new FloatMenuOption("SaveMode_Rectangle".Translate(), () => this.saveMode = SaveMode.Rectangle);
                yield return new FloatMenuOption("SaveMode_RectangleUseCentre".Translate(), () => this.saveMode = SaveMode.RectangleUseCentre);
                yield return new FloatMenuOption("CQF_SaveWholeMap".Translate(), () => Find.WindowStack.Add(new QuestEditor_SaveMapToFile()));
                yield break;
            }
        }
        public override void ProcessInput(UnityEngine.Event ev)
        {
            base.ProcessInput(ev);
            if (this.saveMode != SaveMode.Rectangle)
            {
                Find.DesignatorManager.Deselect();
                Find.Targeter.BeginTargeting(new TargetingParameters() { canTargetPawns = false, canTargetLocations = true }, l =>
                    {
                        Find.Targeter.BeginTargeting(new TargetingParameters() { canTargetPawns = false, canTargetLocations = true }, l2 =>
                        {
                            if (this.saveMode == SaveMode.Round)
                            {
                                int size = (int)l2.Cell.DistanceTo(l.Cell)*2;
                                if (size > GenRadial.MaxRadialPatternRadius) 
                                {
                                    size = (int)GenRadial.MaxRadialPatternRadius;
                                    Messages.Message("OutOfVanillaRange".Translate(GenRadial.MaxRadialPatternRadius), MessageTypeDefOf.CautionInput);
                                }
                                Find.WindowStack.Add(new QuestEditor_SaveMapToFile(GenRadial.RadialCellsAround(l.Cell, l2.Cell.DistanceTo(l.Cell), true).ToList(), l.Cell, new IntVec3(size-1, 1, size-1), this.saveMode)); ;
                            }
                            else
                            {
                                IntVec3 target = l2.Cell;
                                CellRect rect = CellRect.CenteredOn(l.Cell, (int)l.Cell.DistanceTo(target));
                                Find.WindowStack.Add(new QuestEditor_SaveMapToFile(rect.Cells.ToList(), l.Cell, new IntVec3(rect.maxX - rect.minX, 1, rect.maxZ - rect.minZ), this.saveMode));
                            }
                        }, lh =>
                         {
                             if (this.saveMode == SaveMode.Round)
                             {
                                 GenDraw.DrawTargetHighlight(l.Cell);
                                 float size = lh.Cell.DistanceTo(l.Cell);
                                 if (size > GenRadial.MaxRadialPatternRadius)
                                 {
                                     size = (int)GenRadial.MaxRadialPatternRadius;
                                     Messages.Message("OutOfVanillaRange".Translate(GenRadial.MaxRadialPatternRadius), MessageTypeDefOf.CautionInput);
                                 }
                                 GenDraw.DrawTargetHighlight(lh.Cell);
                                 GenDraw.DrawRadiusRing(l.Cell, size > GenRadial.MaxRadialPatternRadius ? GenRadial.MaxRadialPatternRadius : size);
                             }
                             else
                             {
                                 GenDraw.DrawTargetHighlight(l.Cell);
                                 IntVec3 target = lh.Cell;
                                 CellRect rect = CellRect.CenteredOn(l.Cell, (int)l.Cell.DistanceTo(target));
                                 GenDraw.DrawTargetHighlight(lh.Cell);
                                 GenDraw.DrawFieldEdges(rect.Cells.ToList());
                             }
                         }, lv => true);
                    });
            }
        }
        public override void DesignateThing(Thing t)
        {
        }
        public override void DesignateSingleCell(IntVec3 c)
        {
        }
        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            if (this.saveMode == SaveMode.Rectangle) 
            {
                int? minX = null;
                int? maxX = null;
                int? minZ = null;
                int? maxZ = null;
                cells.ToList().ForEach((c) =>
                {
                    maxX = maxX != null && c.x < maxX ? maxX : c.x;
                    minX = minX != null && c.x > minX ? minX : c.x;
                    minZ = minZ != null && c.z > minZ ? minZ : c.z;
                    maxZ = maxZ != null && c.z < maxZ ? maxZ : c.z;
                });
                IntVec3 centre = new IntVec3((minX.Value + maxX.Value)/2, 0,((maxZ.Value + minZ.Value) / 2));
                Find.WindowStack.Add(new QuestEditor_SaveMapToFile(cells.ToList(),centre,new IntVec3(maxX.Value - minX.Value,1,maxZ.Value - minZ.Value),this.saveMode));
            }
            base.DesignateMultiCell(cells);
        }
        public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            string iconPath = this.saveMode == SaveMode.Round ? "UI/Icon_SaveZoneAsDef_Round" : "UI/Icon_SaveZoneAsDef_Rectangle";
            this.icon = ContentFinder<Texture2D>.Get(iconPath);
            base.DrawIcon(rect, buttonMat, parms);
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public SaveMode saveMode = SaveMode.Round;
    }

    public enum SaveMode
    {
        None,
        Round,
        Rectangle,
        RectangleUseCentre
    }
}
