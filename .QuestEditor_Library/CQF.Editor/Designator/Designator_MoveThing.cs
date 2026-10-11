using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Designator_MoveThing : Designator_TransformThing
    {
        public Designator_MoveThing()
        {
            this.defaultLabel = "CQF_MoveThing".Translate();
            this.defaultDesc = "CQF_MoveThingDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff");
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return this.targetThing == null
                ? base.CanDesignateCell(loc)
                : this.GetPlacementReport(this.targetThing, loc, this.targetThing.Rotation);
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            if (this.targetThing == null)
            {
                this.ChooseThing(loc, this.DesignateThing);
            }
            else if (this.TransformThing(this.targetThing, loc, this.targetThing.Rotation))
            {
                this.targetThing = null;
            }
        }

        public override void DesignateThing(Thing thing)
        {
            if (this.CanDesignateThing(thing).Accepted)
            {
                this.targetThing = thing;
            }
        }

        public override void DrawMouseAttachments()
        {
            if (this.targetThing == null)
            {
                base.DrawMouseAttachments();
            }
            else
            {
                GenUI.DrawMouseAttachment(this.icon, this.targetThing.LabelCap);
            }
        }

        public override void Deselected()
        {
            this.targetThing = null;
        }

        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            if (this.targetThing != null && (!this.targetThing.Spawned || this.targetThing.Destroyed || this.targetThing.Map != this.Map))
            {
                this.targetThing = null;
                Messages.Message("CQF_EditorTargetUnavailable".Translate(), RimWorld.MessageTypeDefOf.RejectInput, false);
            }
        }

        private Thing? targetThing;
    }
}
