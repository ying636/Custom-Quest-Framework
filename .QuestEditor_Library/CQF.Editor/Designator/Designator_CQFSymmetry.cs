using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Designator_CQFSymmetry : Designator
    {
        public Designator_CQFSymmetry()
        {
            this.defaultLabel = "CQF_SymmetryTitle".Translate();
            this.defaultDesc = "CQF_SymmetryDescription".Translate();
            this.icon = TexUI.RotRightTex;
        }

        public override bool Visible => DebugSettings.godMode;

        public override void ProcessInput(Event ev)
        {
            if (this.Visible && this.CheckCanInteract() && this.Map != null)
                Find.WindowStack.Add(new Dialog_CQFSymmetry(this.Map));
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc) => false;
    }
}
