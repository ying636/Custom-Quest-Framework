using Verse;

namespace QuestEditor_Library
{
    internal sealed class Designator_CQFAreaControls : Designator
    {
        public override bool Visible => false;
        public override DrawStyleCategoryDef DrawStyleCategory => QEDefOf.CQF_Areas;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return false;
        }
    }
}
