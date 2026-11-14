using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutPlanningContext
{
    public CQFLayoutPlanningContext(IntVec3 size, CustomMapStep_Layout settings, IEnumerable<CustomMapDataDef> definitions,
        IEnumerable<CQFMapRegion> regions, Func<IntVec3, bool>? canPlace = null, Func<IntVec3, bool>? canRoad = null,
        Func<IntVec3, bool>? canWalk = null)
    {
        Size = size; Settings = settings; Definitions = definitions.ToList(); Regions = regions.ToList();
        Random = new Random(settings.seed); placeable = canPlace ?? (_ => true); roadable = canRoad ?? (_ => true); walkable = canWalk ?? (_ => true);
        Bounds = new CellRect(settings.edgeMargin, settings.edgeMargin, size.x - settings.edgeMargin * 2, size.z - settings.edgeMargin * 2);
        if (Bounds.Width <= 0 || Bounds.Height <= 0) throw new CQFLayoutException("CQF_Layout_MapTooSmall");
        foreach (CQFMapRegion region in Regions.Where(r => r.protect)) Plan.Protected.UnionWith(region.cells);
        if (!string.IsNullOrWhiteSpace(settings.allowedRegion))
        {
            List<CQFMapRegion> matches = Regions.Where(r => r.id == settings.allowedRegion || r.key == settings.allowedRegion).ToList();
            if (matches.Count != 1) throw new CQFLayoutException("CQF_Region_Reference: " + settings.allowedRegion);
            Plan.Allowed.UnionWith(matches[0].cells.Where(Bounds.Contains));
        }
        else Plan.Allowed.UnionWith(Bounds.Cells);
    }

    public IntVec3 Size { get; }
    public CellRect Bounds { get; }
    public CustomMapStep_Layout Settings { get; }
    public List<CustomMapDataDef> Definitions { get; }
    public List<CQFMapRegion> Regions { get; }
    public Random Random { get; }
    public CQFLayoutPlan Plan { get; } = new CQFLayoutPlan();
    public CQFMapRegion? Reference
    {
        get => reference;
        set
        {
            reference = value;
            referenceCells = value == null ? new HashSet<IntVec3>() : new HashSet<IntVec3>(value.cells);
            referenceBoundary = value?.Boundary ?? new List<IntVec3>();
        }
    }
    public HashSet<IntVec3> Blocked { get; } = new HashSet<IntVec3>();
    public Dictionary<CustomMapDataDef, int> Counts { get; } = new Dictionary<CustomMapDataDef, int>();

    public bool CanPlace(CQFLayoutPlacement part)
    {
        CellRect rect = part.Bounds;
        if (!rect.Cells.All(c => Plan.Allowed.Contains(c) && !Plan.Protected.Contains(c) && !Blocked.Contains(c) && placeable(c))) return false;
        bool perimeter = part.Role == "Wall" || part.Role == "Corner" || part.Role == "Gate";
        if (Plan.Parts.Any(p => p.Bounds.ExpandedBy(perimeter && (p.Role == "Wall" || p.Role == "Corner" || p.Role == "Gate") ? 0 : Settings.spacing).Overlaps(rect))) return false;
        if (Reference == null) return true;
        if (Settings.relation == "Inside") return rect.Cells.All(referenceCells.Contains);
        if (Settings.relation == "Anywhere") return true;
        if (Settings.directions.Count > 0)
        {
            IntVec3 delta = part.Center - Reference.Bounds.CenterCell;
            Rot4 direction = Math.Abs(delta.x) > Math.Abs(delta.z) ? delta.x >= 0 ? Rot4.East : Rot4.West : delta.z >= 0 ? Rot4.North : Rot4.South;
            if (!Settings.directions.Contains(direction)) return false;
        }
        if (rect.Cells.Any(referenceCells.Contains)) return false;
        if (Settings.relation == "Outside") return true;
        double squared = referenceBoundary.Min(c =>
        {
            int dx = Math.Max(Math.Max(rect.minX - c.x, c.x - rect.maxX), 0);
            int dz = Math.Max(Math.Max(rect.minZ - c.z, c.z - rect.maxZ), 0);
            return (double)dx * dx + (double)dz * dz;
        });
        return squared >= (double)Settings.distance.min * Settings.distance.min && squared <= (double)Settings.distance.max * Settings.distance.max;
    }

    public bool CanRoad(IntVec3 center, int width, HashSet<IntVec3>? exceptions = null)
    {
        int offset = (width - 1) / 2;
        foreach (IntVec3 cell in new CellRect(center.x - offset, center.z - offset, width, width).Cells)
        {
            if (!Plan.Allowed.Contains(cell) || Blocked.Contains(cell) || Plan.Protected.Contains(cell) || !roadable(cell)) return false;
            if (Plan.Parts.Any(p => p.Bounds.Contains(cell)) && exceptions?.Contains(cell) != true) return false;
        }
        return true;
    }

    public bool CanWalk(IntVec3 cell) => cell.x >= 0 && cell.z >= 0 && cell.x < Size.x && cell.z < Size.z && walkable(cell);

    public List<IntVec3> ShuffledCells(int stride = 3)
    {
        List<IntVec3> cells = new List<IntVec3>();
        for (int x = Bounds.minX; x <= Bounds.maxX; x += stride)
            for (int z = Bounds.minZ; z <= Bounds.maxZ; z += stride) cells.Add(new IntVec3(x, 0, z));
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Next(i + 1); (cells[i], cells[j]) = (cells[j], cells[i]);
        }
        return cells;
    }

    public void Add(CQFLayoutPlacement part)
    {
        Plan.Parts.Add(part);
        Counts.TryGetValue(part.Source.Origin, out int count); Counts[part.Source.Origin] = count + 1;
    }

    private readonly Func<IntVec3, bool> placeable;
    private readonly Func<IntVec3, bool> roadable;
    private readonly Func<IntVec3, bool> walkable;
    private CQFMapRegion? reference;
    private HashSet<IntVec3> referenceCells = new HashSet<IntVec3>();
    private List<IntVec3> referenceBoundary = new List<IntVec3>();
}
