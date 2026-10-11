using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Designator_RotateThing : Designator_TransformThing
    {
        public Designator_RotateThing()
        {
            this.defaultLabel = "CQF_RotateThing".Translate();
            this.defaultDesc = "CQF_RotateThingDesc".Translate();
            this.icon = TexUI.RotRightTex;
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                yield return new FloatMenuOption("CQF_RotateThingClockwise".Translate(), () => this.SetDirection(RotationDirection.Clockwise));
                yield return new FloatMenuOption("CQF_RotateThingCounterclockwise".Translate(), () => this.SetDirection(RotationDirection.Counterclockwise));
            }
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            this.ChooseThing(loc, this.DesignateThing);
        }

        public override void DesignateThing(Thing thing)
        {
            this.TransformThing(thing, thing.Position, thing.Rotation.Rotated(this.direction));
        }

        private void SetDirection(RotationDirection value)
        {
            this.direction = value;
            this.icon = value == RotationDirection.Clockwise ? TexUI.RotRightTex : TexUI.RotLeftTex;
        }

        private RotationDirection direction = RotationDirection.Clockwise;
    }
}
