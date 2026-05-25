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
    public class Designator_MoveOut : Designator_Cells
    {
        public Designator_MoveOut()
        {
            this.defaultLabel = "QE_Designator_MoveOut".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Icon_MoveOut", true);
            this.defaultDesc = "QE_Designator_MoveOutDesc".Translate();
            this.useMouseIcon = true;
        }
        public override bool Visible => true;
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
                yield return new FloatMenuOption("SwitchMoveToRoot".Translate(), () =>
                {
                    this.moveToRoot = !this.moveToRoot;
                    this.icon = ContentFinder<Texture2D>.Get(this.moveToRoot ? "UI/Icon_MoveToRoot" : "UI/Icon_MoveOut", true);
                    this.defaultLabel = this.moveToRoot ? "QE_Designator_MoveToRoot".Translate() : "QE_Designator_MoveOut".Translate();
                });
                yield break;
            }
        }
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
            Pawn targetPawn = loc.GetFirstPawn(this.Map);
            Thing target = targetPawn != null && (targetPawn.Faction == Faction.OfPlayer || targetPawn.IsPrisoner || targetPawn.Downed) ? targetPawn : loc.GetFirstItem(this.Map);
            if (target != null && !this.Map.designationManager.HasMapDesignationOn(target))
            {
                if (target.Map.Parent is MapParent_Custom custom && custom.exit != null)
                {
                    this.Map.designationManager.AddDesignation(new Designation(target, this.moveToRoot ? QEDefOf.QE_MoveToRoot : QEDefOf.QE_MoveOut));
                }
                else
                {
                    Messages.Message("TargetIsntWithinSubMap".Translate(), MessageTypeDefOf.NegativeEvent);
                }
            }
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public bool moveToRoot = false;
    }
}
