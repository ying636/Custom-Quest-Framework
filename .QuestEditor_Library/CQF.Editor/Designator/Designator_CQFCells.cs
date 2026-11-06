using Verse;

namespace QuestEditor_Library
{
    public abstract class Designator_CQFCells : Designator_Cells
    {
        public sealed override void DesignateSingleCell(IntVec3 loc)
        {
            CQFEditorSymmetry symmetry = CQFEditorSymmetry.For(this.Map);
            if (!symmetry.Active) this.ApplyCell(loc);
            else symmetry.ApplyCells(loc, this.ApplyCell, this.CanDesignateCell);
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            using CQFSymmetryScope scope = new CQFSymmetryScope(CQFEditorSymmetry.For(this.Map));
            base.DesignateMultiCell(cells);
        }

        protected bool Visit(Thing thing) => CQFEditorSymmetry.For(this.Map).Visit(thing);

        protected abstract void ApplyCell(IntVec3 loc);
    }
}
