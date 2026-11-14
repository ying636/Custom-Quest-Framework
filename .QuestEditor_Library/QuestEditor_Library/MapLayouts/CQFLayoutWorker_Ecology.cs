using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutWorker_Ecology : CQFLayoutWorker_Scatter
{
    public override void Prepare(CQFLayoutPlanningContext context)
    {
        if (context.Plan.Ecology == null) throw new CQFLayoutException("CQF_Ecology_MissingPlan");
        connected = Flood(context.Size, context.CanWalk, context.Reference?.connections.Count > 0 ? context.Reference.connections.Select(c => c.cell)
            : new CellRect(0, 0, context.Size.x, context.Size.z).EdgeCells);
        waterDistances = Distances(context.Size, context.Plan.Ecology.Water);
        rockDistances = Distances(context.Size, context.Plan.Ecology.Rocks);
    }

    public override bool Accept(CQFLayoutPlanningContext context, CQFLayoutPlacement part)
    {
        CQFPointOfInterestSettings rule = part.Rule!.pointOfInterest;
        rule.Validate();
        CQFEcologyPlan ecology = context.Plan.Ecology!;
        List<IntVec3> cells = part.Bounds.Cells.ToList();
        if (cells.Any(c => !ecology.Terrains.TryGetValue(c, out TerrainDef? terrain) || terrain.IsWater || rule.allowedTerrains.Count > 0 && !rule.allowedTerrains.Contains(terrain))) return false;
        if (rule.requireReachable && !Entrances(part).Any(connected.Contains)) return false;
        if (rule.habitat == "Shore" || rule.habitat == "RockEdge")
        {
            Dictionary<IntVec3, int> distances = rule.habitat == "Shore" ? waterDistances : rockDistances;
            int distance = cells.Min(c => distances.TryGetValue(c, out int value) ? value : int.MaxValue);
            if (distance < rule.featureDistance.min || distance > rule.featureDistance.max) return false;
        }
        double inner = (double)cells.Count(ecology.Trees.Contains) / cells.Count;
        if (rule.habitat == "Open" && inner > 0.04) return false;
        if (rule.habitat == "ForestClearing")
        {
            List<IntVec3> ring = part.Bounds.ExpandedBy(Math.Max(1, rule.featureDistance.max)).Cells.Where(c => ecology.Terrains.ContainsKey(c) && !part.Bounds.Contains(c)).ToList();
            if (inner > 0.1 || ring.Count == 0 || (double)ring.Count(ecology.Trees.Contains) / ring.Count < 0.03) return false;
        }
        return true;
    }

    public static void ValidateGenerated(Map map, CQFLayoutPlan plan, IReadOnlyList<CQFMapRegion> regions)
    {
        Dictionary<string, HashSet<IntVec3>> connections = new Dictionary<string, HashSet<IntVec3>>();
        foreach (CQFLayoutPlacement part in plan.Parts.Where(p => p.Rule?.pointOfInterest.requireReachable == true))
        {
            if (!connections.TryGetValue(part.ReferenceId, out HashSet<IntVec3>? reached))
            {
                CQFMapRegion? reference = regions.FirstOrDefault(r => r.id == part.ReferenceId);
                reached = Flood(map.Size, c => c.Walkable(map), reference?.connections.Count > 0 ? reference.connections.Select(c => c.cell) : new CellRect(0, 0, map.Size.x, map.Size.z).EdgeCells);
                connections.Add(part.ReferenceId, reached);
            }
            if (!Entrances(part).Any(reached.Contains)) throw new CQFLayoutException("CQF_Layout_RoadUnreachable: " + part.Source.defName);
        }
    }

    private static IEnumerable<IntVec3> Entrances(CQFLayoutPlacement part)
    {
        List<IntVec3> marked = part.Entrances().Select(c => c.cell).ToList();
        return (marked.Count > 0 ? marked : part.Bounds.EdgeCells).Where(c => !part.ImpassableCells.Contains(c - part.Origin));
    }

    private static HashSet<IntVec3> Flood(IntVec3 size, Func<IntVec3, bool> passable, IEnumerable<IntVec3> starts)
    {
        HashSet<IntVec3> result = new HashSet<IntVec3>(); Queue<IntVec3> queue = new Queue<IntVec3>();
        foreach (IntVec3 cell in starts) if (cell.x >= 0 && cell.z >= 0 && cell.x < size.x && cell.z < size.z && passable(cell) && result.Add(cell)) queue.Enqueue(cell);
        while (queue.Count > 0)
        {
            IntVec3 cell = queue.Dequeue();
            foreach (IntVec3 direction in GenAdj.CardinalDirections)
            {
                IntVec3 next = cell + direction;
                if (next.x < 0 || next.z < 0 || next.x >= size.x || next.z >= size.z || !passable(next) || !result.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return result;
    }

    private Dictionary<IntVec3, int> Distances(IntVec3 size, IEnumerable<IntVec3> starts)
    {
        Dictionary<IntVec3, int> result = new Dictionary<IntVec3, int>(); Queue<IntVec3> queue = new Queue<IntVec3>();
        foreach (IntVec3 cell in starts) if (!result.ContainsKey(cell)) { result.Add(cell, 0); queue.Enqueue(cell); }
        while (queue.Count > 0)
        {
            IntVec3 cell = queue.Dequeue();
            foreach (IntVec3 direction in GenAdj.CardinalDirections)
            {
                IntVec3 next = cell + direction;
                if (next.x < 0 || next.z < 0 || next.x >= size.x || next.z >= size.z || result.ContainsKey(next)) continue;
                result.Add(next, result[cell] + 1); queue.Enqueue(next);
            }
        }
        return result;
    }

    private HashSet<IntVec3> connected = new HashSet<IntVec3>();
    private Dictionary<IntVec3, int> waterDistances = new Dictionary<IntVec3, int>();
    private Dictionary<IntVec3, int> rockDistances = new Dictionary<IntVec3, int>();
}
