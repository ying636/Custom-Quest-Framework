using Verse;

namespace QuestEditor_Library;

public sealed class CQFLayoutRoadPlanner
{
    public void Connect(CQFLayoutPlanningContext context)
    {
        HashSet<IntVec3> network = new HashSet<IntVec3>();
        Dictionary<string, List<IntVec3>> referenceStarts = new Dictionary<string, List<IntVec3>>(StringComparer.Ordinal);
        foreach (string id in context.Plan.Parts.Select(p => p.ReferenceId).Where(s => s.Length > 0).Distinct())
        {
            CQFMapRegion region = context.Regions.Single(r => r.id == id);
            if (region.connections.Count == 0) throw new CQFLayoutException("CQF_Layout_MissingConnection: " + id);
            List<IntVec3> starts = new List<IntVec3>();
            foreach (CQFMapRegionConnection connection in region.connections)
            {
                if (!context.CanWalk(connection.cell)) throw new CQFLayoutException("CQF_Layout_BlockedConnection: " + id + " " + connection.cell);
                IntVec3 start = connection.cell + connection.direction.FacingCell * (1 + (context.Settings.roadWidth - 1) / 2);
                if (context.CanRoad(start, context.Settings.roadWidth)) starts.Add(start);
            }
            if (starts.Count == 0) throw new CQFLayoutException("CQF_Layout_BlockedConnection: " + id);
            referenceStarts.Add(id, starts);
        }
        foreach (CQFLayoutPlacement part in context.Plan.Parts)
        {
            if (part.Role == "Wall" || part.Role == "Corner") continue;
            List<IntVec3>? route = null;
            foreach (CQFMapRegionConnection entry in part.Entrances())
            {
                if (part.ImpassableCells.Contains(entry.cell - part.Origin)) continue;
                IntVec3 start = entry.cell + entry.direction.FacingCell;
                int limit = Math.Max(part.Bounds.Width, part.Bounds.Height) + context.Settings.roadWidth;
                for (int i = 0; i < limit && part.Bounds.ExpandedBy((context.Settings.roadWidth - 1) / 2).Contains(start); i++) start += entry.direction.FacingCell;
                if (!context.CanRoad(start, context.Settings.roadWidth)) continue;
                List<IntVec3> approach = new List<IntVec3>();
                IntVec3 cursor = entry.cell + entry.direction.FacingCell;
                bool clear = true;
                for (int i = 0; cursor != start && i < limit; i++, cursor += entry.direction.FacingCell)
                {
                    if (part.ObjectCells.Contains(cursor - part.Origin)) { clear = false; break; }
                    if (!context.Plan.Allowed.Contains(cursor) || context.Plan.Protected.Contains(cursor) || context.Blocked.Contains(cursor)) { clear = false; break; }
                    approach.Add(cursor);
                }
                if (!clear) continue;
                HashSet<IntVec3> targets = part.ReferenceId.Length > 0 ? new HashSet<IntVec3>(referenceStarts[part.ReferenceId]) : network;
                route = targets.Count == 0 ? new List<IntVec3> { start } : FindPath(context, start, targets);
                if (route == null) continue;
                context.Plan.Roads.UnionWith(approach);
                foreach (IntVec3 cell in route)
                {
                    int offset = (context.Settings.roadWidth - 1) / 2;
                    context.Plan.Roads.UnionWith(new CellRect(cell.x - offset, cell.z - offset, context.Settings.roadWidth, context.Settings.roadWidth).Cells);
                }
                network.UnionWith(route);
                break;
            }
            if (route == null) throw new CQFLayoutException("CQF_Layout_RoadUnreachable: " + part.Source.defName);
        }
    }

    public void Validate(Map map, CQFLayoutPlan plan, IReadOnlyList<CQFMapRegion> regions)
    {
        Dictionary<string, HashSet<IntVec3>> reachable = new Dictionary<string, HashSet<IntVec3>>(StringComparer.Ordinal);
        foreach (CQFLayoutPlacement part in plan.Parts.Where(p => p.Role != "Wall" && p.Role != "Corner"))
        {
            List<IntVec3> entrances = part.Entrances().Select(c => c.cell).Where(c => c.InBounds(map) && c.Walkable(map)).ToList();
            if (entrances.Count == 0) throw new CQFLayoutException("CQF_Layout_RoadUnreachable: " + part.Source.defName);
            if (!reachable.TryGetValue(part.ReferenceId, out HashSet<IntVec3>? visited))
            {
                visited = new HashSet<IntVec3>();
                Queue<IntVec3> queue = new Queue<IntVec3>();
                IEnumerable<IntVec3> starts = part.ReferenceId.Length == 0 ? entrances.Take(1)
                    : regions.Single(r => r.id == part.ReferenceId).connections.Select(c => c.cell);
                foreach (IntVec3 start in starts.Where(c => c.InBounds(map) && c.Walkable(map)))
                    if (visited.Add(start)) queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    IntVec3 cell = queue.Dequeue();
                    foreach (IntVec3 direction in GenAdj.CardinalDirections)
                    {
                        IntVec3 next = cell + direction;
                        if (next.InBounds(map) && next.Walkable(map) && visited.Add(next)) queue.Enqueue(next);
                    }
                }
                reachable.Add(part.ReferenceId, visited);
            }
            if (!entrances.Any(visited.Contains)) throw new CQFLayoutException("CQF_Layout_RoadUnreachable: " + part.Source.defName);
        }
    }

    private List<IntVec3>? FindPath(CQFLayoutPlanningContext context, IntVec3 start, HashSet<IntVec3> targets)
    {
        Queue<IntVec3> queue = new Queue<IntVec3>();
        Dictionary<IntVec3, IntVec3> previous = new Dictionary<IntVec3, IntVec3> { [start] = start };
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            IntVec3 cell = queue.Dequeue();
            if (targets.Contains(cell))
            {
                List<IntVec3> result = new List<IntVec3> { cell };
                while (previous[cell] != cell) { cell = previous[cell]; result.Add(cell); }
                return result;
            }
            foreach (IntVec3 direction in GenAdj.CardinalDirections)
            {
                IntVec3 next = cell + direction;
                if (previous.ContainsKey(next) || !context.CanRoad(next, context.Settings.roadWidth)) continue;
                previous.Add(next, cell); queue.Enqueue(next);
            }
        }
        return null;
    }
}
