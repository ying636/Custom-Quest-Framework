using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutWorker_Fortified : CQFLayoutWorker
{
    public override void Prepare(CQFLayoutPlanningContext context)
    {
        perimeter.Clear();
        List<CQFLayoutPartRule> rules = context.Settings.parts.Count > 0 ? context.Settings.parts : context.Settings.layout!.parts;
        foreach (string role in new[] { "Wall", "Corner", "Gate" })
            if (rules.Count(r => r.role == role) != 1 || rules.Single(r => r.role == role).optional)
                throw new CQFLayoutException("CQF_Layout_FortifiedRole: " + role);
        int grid = context.Settings.gridSize;
        int columns = context.Reference == null ? context.Bounds.Width / grid : (context.Reference.Bounds.Width + 2 * context.Settings.distance.min + grid - 1) / grid + 2;
        int rows = context.Reference == null ? context.Bounds.Height / grid : (context.Reference.Bounds.Height + 2 * context.Settings.distance.min + grid - 1) / grid + 2;
        if (columns < 3 || rows < 3 || columns * grid > context.Bounds.Width || rows * grid > context.Bounds.Height)
            throw new CQFLayoutException("CQF_Layout_MapTooSmall");
        IntVec3 center = context.Reference?.Bounds.CenterCell ?? context.Bounds.CenterCell;
        enclosure = new CellRect(center.x - columns * grid / 2, center.z - rows * grid / 2, columns * grid, rows * grid);
        for (int x = 0; x < columns; x++)
            for (int z = 0; z < rows; z++)
            {
                if (x != 0 && z != 0 && x != columns - 1 && z != rows - 1) continue;
                bool corner = (x == 0 || x == columns - 1) && (z == 0 || z == rows - 1);
                string role = corner ? "Corner" : z == 0 && x == columns / 2 ? "Gate" : "Wall";
                Rot4 direction = z == 0 ? Rot4.South : x == columns - 1 ? Rot4.East : z == rows - 1 ? Rot4.North : Rot4.West;
                perimeter.Add((role, new CellRect(enclosure.minX + x * grid, enclosure.minZ + z * grid, grid, grid).CenterCell, direction));
            }
    }

    public override int Count(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int requested)
        => rule.role == "Wall" || rule.role == "Corner" || rule.role == "Gate" ? perimeter.Count(p => p.Role == rule.role) : requested;

    public override IEnumerable<IntVec3> Locations(CQFLayoutPlanningContext context, CQFLayoutPartRule rule, int index)
    {
        if (rule.role == "Gate" || rule.role == "Corner" || rule.role == "Wall") return perimeter.Where(p => p.Role == rule.role).Select(p => p.Cell);
        if (rule.role == "Center") return new[] { enclosure.CenterCell };
        return context.ShuffledCells().Where(enclosure.ContractedBy(context.Settings.gridSize).Contains);
    }

    public override bool Accept(CQFLayoutPlanningContext context, CQFLayoutPlacement part)
    {
        if (part.Role != "Gate" && part.Role != "Wall" && part.Role != "Corner") return enclosure.ContractedBy(context.Settings.gridSize).Contains(new IntVec3(part.Bounds.minX, 0, part.Bounds.minZ))
            && enclosure.ContractedBy(context.Settings.gridSize).Contains(new IntVec3(part.Bounds.maxX, 0, part.Bounds.maxZ));
        return part.Bounds.Width == context.Settings.gridSize && part.Bounds.Height == context.Settings.gridSize
            && perimeter.Any(p => p.Role == part.Role && p.Cell == part.Center && p.Direction == part.Rotation);
    }

    public override void Finish(CQFLayoutPlanningContext context)
    {
        if (perimeter.Any(slot => !context.Plan.Parts.Any(p => p.Role == slot.Role && p.Center == slot.Cell)))
            throw new CQFLayoutException("CQF_Layout_FortifiedGap");
    }

    private CellRect enclosure;
    private readonly List<(string Role, IntVec3 Cell, Rot4 Direction)> perimeter = new List<(string, IntVec3, Rot4)>();
}
