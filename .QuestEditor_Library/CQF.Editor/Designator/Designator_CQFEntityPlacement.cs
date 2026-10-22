using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Designator_CQFEntityPlacement : Designator_Place
    {
        public Designator_CQFEntityPlacement(PawnSpawnData recipe, Map map, Quest? quest = null)
        {
            this.recipe = recipe;
            this.map = map;
            this.quest = quest;
            defaultLabel = "CQF_EntityBook_PlacementTitle".Translate(CQFEntityBookCatalog.LabelFor(recipe));
            defaultDesc = "CQF_EntityBook_PlacementDescription".Translate();
            icon = QEDefOf.QE_Spawner_Editor.GetUIIconForStuff(null);
            useMouseIcon = true;
        }

        public override bool Visible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;
        public override BuildableDef PlacingDef => QEDefOf.QE_Spawner_Editor;
        public override ThingStyleDef? ThingStyleDefForPreview => null;
        public override ThingDef? StuffDef => null;
        public override DrawStyleCategoryDef? DrawStyleCategory => null;

        public override bool CanRemainSelected() => Visible && !map.Disposed && ReferenceEquals(Find.CurrentMap, map);

        public override AcceptanceReport CanDesignateCell(IntVec3 loc) => CQFEntityMapPlacement.CanPlace(loc, map, recipe);

        public override void DesignateSingleCell(IntVec3 loc)
        {
            try
            {
                Spawner marker = CQFEntityMapPlacement.Place(recipe, loc, map);
                Find.DesignatorManager.Deselect();
                Find.Selector.ClearSelection();
                Find.Selector.Select(marker);
                Find.WindowStack.Add(new Window_CQFEntityPlacement(marker, quest));
            }
            catch (Exception error) { CQFEntityBookCatalog.ReportError(error); }
        }

        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
        }

        private readonly PawnSpawnData recipe;
        private readonly Map map;
        private readonly Quest? quest;
    }
}
