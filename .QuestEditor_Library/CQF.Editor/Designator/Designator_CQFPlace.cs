using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public abstract class Designator_CQFPlace : Designator_Place
    {
        public bool CanRotate => PlacingDef is ThingDef { category: ThingCategory.Building, rotatable: true };
        public override DrawStyleCategoryDef? DrawStyleCategory => CanRotate ? null : QEDefOf.CQF_Areas;

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

        private Designator_CQFAreaControls? areaControls;
    }
}
