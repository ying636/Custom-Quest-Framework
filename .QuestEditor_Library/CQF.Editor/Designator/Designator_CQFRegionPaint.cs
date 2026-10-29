using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class Designator_CQFRegionPaint : Designator_Cells
{
    public Designator_CQFRegionPaint(Map map, CQFMapRegion region, bool erase, bool paint)
    {
        targetMap = map; this.region = region; this.erase = erase; this.paint = paint;
        defaultLabel = (erase ? "CQF_Region_Erase" : "CQF_Region_Paint").Translate();
        defaultDesc = defaultLabel;
        icon = TexButton.OpenDebugActionsMenu;
    }

    public override bool Visible => DebugSettings.godMode;
    public override DrawStyleCategoryDef DrawStyleCategory => paint ? DrawStyleCategoryDefOf.Paint : DrawStyleCategoryDefOf.Areas;
    public override bool DragDrawMeasurements => true;
    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get { yield return new FloatMenuOption("CQF_Region_Title".Translate(), () => Find.WindowStack.Add(new Dialog_CQFMapRegions(targetMap, region))); }
    }

    public override bool CanRemainSelected() => Find.CurrentMap == targetMap;
    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => Find.CurrentMap == targetMap && loc.InBounds(targetMap);

    public override void DesignateSingleCell(IntVec3 loc)
    {
        if (!CanDesignateCell(loc).Accepted) return;
        CQFMapRegionCollection registry = MapComponent_CustomMapData.GetComp(targetMap).Regions;
        if (erase)
        {
            region.cells.Remove(loc);
            region.connections.RemoveAll(c => c.cell == loc);
            if (region.cells.Count == 0) registry.Remove(region);
        }
        else if (!region.cells.Contains(loc))
        {
            region.cells.Add(loc);
            if (!registry.All.Contains(region)) registry.Add(region, region.scope, targetMap);
        }
    }

    public override void SelectedUpdate()
    {
        foreach (CQFMapRegion other in MapComponent_CustomMapData.GetComp(targetMap).Regions.All)
            GenDraw.DrawFieldEdges(other.cells, other == region ? CQFUIStyle.Accent : CQFUIStyle.Link);
        foreach (CQFMapRegionConnection connection in region.connections) GenDraw.DrawTargetHighlight(connection.cell);
    }

    private readonly Map targetMap;
    private readonly CQFMapRegion region;
    private readonly bool erase;
    private readonly bool paint;
}
