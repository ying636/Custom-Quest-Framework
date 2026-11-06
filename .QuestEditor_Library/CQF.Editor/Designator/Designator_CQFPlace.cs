using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class Designator_CQFPlace : Designator_Place
    {
        public bool CanRotate => PlacingDef is ThingDef { category: ThingCategory.Building, rotatable: true };
        public override DrawStyleCategoryDef? DrawStyleCategory => CanRotate ? null : QEDefOf.CQF_Areas;

        public sealed override void DesignateSingleCell(IntVec3 loc)
        {
            CQFEditorSymmetry symmetry = CQFEditorSymmetry.For(this.Map);
            if (!symmetry.Active) this.PlaceAt(loc, this.placingRot);
            else symmetry.Place(loc, this.placingRot, (ThingDef)this.PlacingDef, this.PlaceAt, this.StuffDef);
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            using CQFSymmetryScope scope = new CQFSymmetryScope(CQFEditorSymmetry.For(this.Map));
            base.DesignateMultiCell(cells);
        }

        public override void DoExtraGuiControls(float leftX, float bottomY)
        {
            if (CanRotate)
            {
                base.DoExtraGuiControls(leftX, bottomY);
                return;
            }
            if (PlacingDef.PlaceWorkers != null)
            {
                foreach (PlaceWorker worker in PlacingDef.PlaceWorkers)
                {
                    worker.DrawOnGUIExtra(PlacingDef);
                }
            }
            (areaControls ??= new Designator_CQFAreaControls()).DoExtraGuiControls(leftX, bottomY);
        }

        public override void SelectedProcessInput(Event ev)
        {
            if (!CanRotate)
            {
                (areaControls ??= new Designator_CQFAreaControls()).SelectedProcessInput(ev);
                return;
            }
            base.SelectedProcessInput(ev);
        }

        protected abstract void PlaceAt(IntVec3 loc, Rot4 rotation);

        protected override void DrawGhost(Color ghostCol)
        {
            base.DrawGhost(ghostCol);
            CQFEditorSymmetry symmetry = CQFEditorSymmetry.For(this.Map);
            if (!symmetry.Active) return;
            ThingDef def = (ThingDef)this.PlacingDef;
            IntVec3 source = UI.MouseCell();
            HashSet<CellRect> visited = new HashSet<CellRect> { GenAdj.OccupiedRect(source, this.placingRot, def.size) };
            foreach (CQFSymmetryTransform transform in symmetry.Transforms())
            {
                IntVec3 position = transform.Position(source, this.placingRot, def);
                Rot4 rotation = transform.Rotation(this.placingRot);
                CellRect rect = GenAdj.OccupiedRect(position, rotation, def.size);
                if (!visited.Add(rect) || !rect.InBounds(this.Map)) continue;
                Color color = symmetry.PlacementReport(def, position, rotation, this.StuffDef).Accepted ? ghostCol : Color.red;
                GhostDrawer.DrawGhostThing(position, rotation, def, this.ThingStyleDefForPreview?.Graphic, color,
                    AltitudeLayer.Blueprint, null, true, this.StuffDef);
            }
        }

        private Designator_CQFAreaControls? areaControls;
    }
}
