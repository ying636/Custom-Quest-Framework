using Verse;

namespace QuestEditor_Library;

public class CQFLayoutWorker_City : CQFLayoutWorker
{
    public override IEnumerable<IntVec3> Locations(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int index)
    {
        int grid = context.Settings.gridSize;
        List<IntVec3> cells = new List<IntVec3>();
        for (int x = context.Bounds.minX + grid / 2; x <= context.Bounds.maxX - grid / 2; x += grid)
            for (int z = context.Bounds.minZ + grid / 2; z <= context.Bounds.maxZ - grid / 2; z += grid)
                cells.Add(new IntVec3(x, 0, z));
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = context.Random.Next(i + 1); (cells[i], cells[j]) = (cells[j], cells[i]);
        }
        return cells;
    }

    public override bool Accept(CQFLayoutPlanningContext context, CQFLayoutPlacement part)
    {
        int maximum = context.Settings.gridSize - context.Settings.roadWidth - 2;
        return part.Bounds.Width <= maximum && part.Bounds.Height <= maximum;
    }

    public override void Finish(CQFLayoutPlanningContext context)
    {
        if (!context.Settings.connectRoads) return;
        foreach (IntVec3 cell in context.Bounds.Cells)
        {
            bool street = (cell.x - context.Bounds.minX) % context.Settings.gridSize == 0 || (cell.z - context.Bounds.minZ) % context.Settings.gridSize == 0;
            if (!street || !context.CanRoad(cell, context.Settings.roadWidth)) continue;
            int offset = (context.Settings.roadWidth - 1) / 2;
            context.Plan.Roads.UnionWith(new CellRect(cell.x - offset, cell.z - offset, context.Settings.roadWidth, context.Settings.roadWidth).Cells);
        }
    }
}
