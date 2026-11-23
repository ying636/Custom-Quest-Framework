#if RIMWORLD_1_7
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    public override void GameComponentTick()
    {
        if (disposed || activeCompositeEdges.Count == 0) return;
        int tick = game.tickManager.TicksGame;
        if (tick >= nextCompositeEdgeTick) TickCompositeEdges(tick);
    }

    internal void NotifyCompositeEdgeSpawned(Pawn pawn, Map map)
    {
        if (pawn.Faction != Faction.OfPlayer || !compositeEdges.TryGetValue(map, out CQFCompositeEdgeMap? edge)) return;
        edge.NextCheckTick = nextCompositeEdgeTick = 0;
        if (edge.Cells.Contains(pawn.Position))
            compositeEdgeSpawns[pawn] = (map, pawn.Position);
    }

    private void TickCompositeEdges(int tick)
    {
        int nextTick = tick + CompositeEdgeCheckInterval;
        foreach (CQFCompositeEdgeMap edge in activeCompositeEdges)
        {
            Map map = edge.Map;
            if (map.Disposed) continue;
            if (tick >= edge.NextCheckTick) edge.NextCheckTick = tick + CheckCompositeEdgeMap(edge, tick);
            nextTick = Math.Min(nextTick, edge.NextCheckTick);
        }
        nextCompositeEdgeTick = nextTick;
        compositeEdgeSpawns.Clear();
    }

    private int CheckCompositeEdgeMap(CQFCompositeEdgeMap edge, int tick)
    {
        int nextDelay = CompositeEdgeCheckInterval;
        int maxX = edge.Size.x - 1, maxZ = edge.Size.z - 1;
        List<Pawn> pawns = edge.PlayerPawns;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn? pawn = pawns[i];
            if (pawn == null) continue;
            IntVec3 cell = pawn.Position;
            int distance = Math.Min(Math.Min(cell.x, maxX - cell.x), Math.Min(cell.z, maxZ - cell.z));
            if (distance < nextDelay) nextDelay = Math.Max(1, distance);
            if (distance == 0 && pawn.pather?.LastMovedTick == tick && edge.Cells.Contains(cell))
                CheckCompositeEdgePawn(pawn, edge.Map);
        }
        return nextDelay;
    }

    private void RegisterCompositeEdges(CQFCompositeMapInstance instance)
    {
        foreach (CQFCompositeMapConnection link in instance.Edges)
        {
            RegisterCompositeEdge(instance.GetMap(link.from), link.fromEdge, link.fromStart, link.length);
            if (link.twoWay)
                RegisterCompositeEdge(instance.GetMap(link.to), link.toEdge, link.toStart, link.length);
        }
    }

    private void RegisterCompositeEdge(Map? map, CQFCompositeMapEdge edge, int start, int length)
    {
        if (map == null) return;
        if (!compositeEdges.TryGetValue(map, out CQFCompositeEdgeMap? indexed))
        {
            indexed = new CQFCompositeEdgeMap(map);
            compositeEdges.Add(map, indexed);
            map.events.ThingFactionChanged += OnCompositeEdgeFactionChanged;
            if (!IsHibernating(map)) ActivateCompositeEdges(map);
        }
        for (int i = 0; i < length; i++)
        {
            IntVec3 cell = CQFCompositeMapDef.EdgeCell(indexed.Size, edge, start + i);
            if (cell.InBounds(map)) indexed.Cells.Add(cell);
        }
    }

    private void ActivateCompositeEdges(Map map)
    {
        if (!compositeEdges.TryGetValue(map, out CQFCompositeEdgeMap? edge)) return;
        edge.NextCheckTick = nextCompositeEdgeTick = 0;
        activeCompositeEdges.Add(edge);
    }

    private void DetachCompositeEdges(Map map)
    {
        if (compositeEdges.TryGetValue(map, out CQFCompositeEdgeMap? edge)) activeCompositeEdges.Remove(edge);
        if (activeCompositeEdges.Count == 0) compositeEdgeSpawns.Clear();
    }

    private void ForgetCompositeEdges(Map map)
    {
        DetachCompositeEdges(map);
        if (compositeEdges.Remove(map)) map.events.ThingFactionChanged -= OnCompositeEdgeFactionChanged;
    }

    private void OnCompositeEdgeFactionChanged(Faction previous, Faction current)
    {
        Faction player = Faction.OfPlayer;
        if (previous != player && current != player) return;
        nextCompositeEdgeTick = 0;
        foreach (CQFCompositeEdgeMap edge in activeCompositeEdges) edge.NextCheckTick = 0;
    }

    private void CheckCompositeEdgePawn(Pawn pawn, Map map)
    {
        if (!pawn.pather.ChangedCellRecently(0) || !pawn.IsColonistPlayerControlled) return;
        IntVec3 cell = pawn.Position;
        if (compositeEdgeSpawns.TryGetValue(pawn, out var spawn) && spawn.Map == map && spawn.Cell == cell) return;
        QueueCompositeEdgeTravel(pawn, map, cell);
    }

    private const int CompositeEdgeCheckInterval = 8;
    private readonly Dictionary<Map, CQFCompositeEdgeMap> compositeEdges = new();
    private readonly List<CQFCompositeEdgeMap> activeCompositeEdges = new();
    private readonly Dictionary<Pawn, (Map Map, IntVec3 Cell)> compositeEdgeSpawns = new();
    private int nextCompositeEdgeTick;
}
#endif
