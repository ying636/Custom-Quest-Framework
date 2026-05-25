using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Designator_Disdestroy : Designator_Cells
    {
        public Designator_Disdestroy()
        {
            this.defaultLabel = "QE_Designator_Disdestroy".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Icons/Icon_Disdestroy", true);
            this.defaultDesc = "QE_Designator_Disdestroy_Desc".Translate();
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
        public override void DesignateThing(Thing t)
        {
            this.DesignateSingleCell(t.Position);
        }
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (!t.def.alwaysHaulable)
            {
                return false;
            }
            if (base.Map.designationManager.DesignationAt(t.Position, this.Designation) != null)
            {
                return AcceptanceReport.WasRejected;
            }
            return true;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (!loc.InBounds(this.Map))
            {
                return;
            }
            if (!this.Map.designationManager.HasMapDesignationAt(loc))
            {
                this.Map.designationManager.AddDesignation(new Designation(loc, QEDefOf.QE_Disdestroy));
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }
    }
}
