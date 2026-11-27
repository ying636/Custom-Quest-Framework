#if RIMWORLD_1_7
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    internal CQFCrossMapCoordinator CrossMap => crossMap;

    public IReadOnlyList<Map> GetReachableCQFMaps(Map source)
    {
        if (!IsRegisteredGameMap(source)) return Array.Empty<Map>();
        var routes = crossMapRoutes ??= BuildCrossMapRoutes();
        var found = new HashSet<Map> { source };
        List<Map> result = new();
        Queue<Map> queue = new();
        queue.Enqueue(source);
        while (queue.TryDequeue(out Map? map))
        {
            if (!routes.TryGetValue(map, out List<CQFCrossMapStep>? steps)) continue;
            foreach (CQFCrossMapStep step in steps)
            {
                if (!IsCrossMapPassageOpen(step)) continue;
                if (!found.Add(step.Destination)) continue;
                result.Add(step.Destination);
                queue.Enqueue(step.Destination);
            }
        }
        return result;
    }

    public Map? GetCQFMapAbove(Map map, bool outside)
    {
        Map? parent = (map.Parent as PocketMapParent)?.sourceMap;
        if (GetCompositeMap(map) is { } instance && instance.GetGroup(map) is { } group)
        {
            if (outside) return instance.Source;
            if (map.Parent is not CQFCompositeMapParent && parent != null) return parent;
            if (group == instance.RootGroup) return instance.Source;
            return group.ParentRegion is { } region ? instance.GetMap(region) : null;
        }
        return outside && parent != null ? LevelPather.GetRoot(map) : parent;
    }

    public bool RequestCrossMapMove(Pawn pawn, Map source, Map destination) =>
        crossMap.RequestMove(this, pawn, source, destination);

    public bool AssignCrossMapHaul(Map source, Thing item, Map destination)
    {
        if (!TryGetCrossMapStep(source, destination, null, out _)) return false;
        crossMap.DesignateHaul(item, destination);
        return true;
    }

    internal void InvalidateCrossMapRoutes() => crossMapRoutes = null;

    internal Map? ResolveCrossMapTarget(TargetInfo target)
    {
        if (target.Map is { } map) return map;
        if (!target.HasThing) return null;
        foreach (Map sleeping in hibernatingMaps)
            if (sleeping.spawnedThings.Contains(target.Thing)) return sleeping;
        return null;
    }

    internal bool TryGetCrossMapStep(Map source, Map destination, Pawn? pawn, out CQFCrossMapStep first,
        IntVec3? preferredExit = null) => FindCrossMapStep(source, destination, pawn, out first, preferredExit, null, false);

    internal bool HasOrganizationRoute(MetaLord owner, Map source, Map destination) =>
        source == destination || FindCrossMapStep(source, destination, null, out _, null, owner, true);

    internal bool TryGetOrganizationStep(MetaLord owner, Map source, Map destination, Pawn pawn, out CQFCrossMapStep first) =>
        FindCrossMapStep(source, destination, pawn, out first, null, owner, false);

    private bool FindCrossMapStep(Map source, Map destination, Pawn? pawn, out CQFCrossMapStep first,
        IntVec3? preferredExit, MetaLord? owner, bool topologyOnly)
    {
        first = default;
        if (source == destination || !IsRegisteredGameMap(source) || !IsRegisteredGameMap(destination)) return false;
        var routes = crossMapRoutes ??= BuildCrossMapRoutes();
        if (preferredExit is { } exit && routes.TryGetValue(source, out List<CQFCrossMapStep>? sourceSteps))
        {
            int best = int.MaxValue;
            foreach (CQFCrossMapStep step in sourceSteps)
            {
                if (step.Destination != destination || !CanUse(step)) continue;
                int distance = step.Departure.DistanceToSquared(exit);
                if (distance >= best) continue;
                best = distance;
                first = step;
            }
            return best != int.MaxValue;
        }
        var visited = new HashSet<Map> { source };
        var queue = new Queue<(Map Map, CQFCrossMapStep First)>();
        queue.Enqueue((source, default));
        while (queue.TryDequeue(out var next))
        {
            if (!routes.TryGetValue(next.Map, out List<CQFCrossMapStep>? steps)) continue;
            foreach (CQFCrossMapStep step in steps)
            {
                if (visited.Contains(step.Destination) || !CanUse(step)) continue;
                visited.Add(step.Destination);
                CQFCrossMapStep initial = next.Map == source ? step : next.First;
                if (step.Destination == destination)
                {
                    first = owner != null && initial.EdgeApproach is { } approach
                        ? initial with { Departure = approach }
                        : initial;
                    return true;
                }
                queue.Enqueue((step.Destination, initial));
            }
        }
        return false;

        bool CanUse(CQFCrossMapStep step)
        {
            if (owner != null && !owner.IncludesMap(step.Destination)) return false;
            if (topologyOnly) return !step.Destination.Disposed && step.Portal is not { Destroyed: true };
            if (!IsCrossMapPassageOpen(step) || (pawn != null && step.Portal is CQFMapPortal portal && !portal.IsAllowed(pawn))) return false;
            if (owner == null || pawn == null || step.Source != source || pawn.Map != source) return true;
            if (step.Portal is CQFMapPortal passage && (!passage.IsAvailable(pawn) || !passage.IsEnterable(out _))) return false;
            if (step.EdgeApproach.HasValue && !step.Departure.Standable(source)) return false;
            LocalTargetInfo target = step.Portal is { } thing ? new LocalTargetInfo(thing) : new LocalTargetInfo(step.EdgeApproach ?? step.Departure);
            return pawn.CanReach(target, step.Portal == null ? PathEndMode.OnCell : PathEndMode.Touch, Danger.Deadly);
        }
    }

    internal bool ContainsCrossMapThing(Map map, Thing thing) =>
        !map.Disposed && (thing.Map == map || (IsHibernating(map) && map.spawnedThings.Contains(thing)));

    internal void DetachCrossMapThing(Map map, Thing thing)
    {
        if (!ContainsCrossMapThing(map, thing)) throw new InvalidOperationException("[CQF] The source map lost the traveling object.");
        if (!IsHibernating(map))
        {
            RemoveThing();
            return;
        }
        if (game.Maps.Count > sbyte.MaxValue)
            throw new InvalidOperationException("[CQF] No map index is available for the transfer.");
        game.Maps.Add(map);
        try
        {
            CQFHibernatingMap.SetMapIndex(map, (sbyte)(game.Maps.Count - 1));
            RemoveThing();
        }
        finally
        {
            CQFHibernatingMap.SetMapIndex(map, -1);
            game.Maps.Remove(map);
        }

        void RemoveThing()
        {
            Lord? lord = (thing as Pawn)?.GetLord();
            thing.DeSpawn();
            if (thing is Pawn pawn && lord?.ownedPawns.Contains(pawn) == true)
                lord.Notify_PawnLost(pawn, PawnLostCondition.ExitedMap);
        }
    }

    internal bool TryCrossMapArrival(Map map, Thing thing, IntVec3 preferred, out IntVec3 cell)
    {
        cell = preferred;
        bool Valid(IntVec3 candidate) => candidate.InBounds(map) && candidate.Standable(map) &&
            (thing is not Pawn || candidate.GetFirstPawn(map) == null);
        return Valid(cell) || CellFinder.TryFindRandomCellNear(preferred, map, 4, Valid, out cell);
    }

    private bool IsRegisteredGameMap(Map map) => !map.Disposed && (game.Maps.Contains(map) || IsHibernating(map));

    private static bool IsCrossMapPassageOpen(CQFCrossMapStep step) => !step.Destination.Disposed &&
        (step.Portal switch
        {
            CustomMapEntrance entrance => !entrance.Destroyed && entrance.opended,
            CustomMapExit exit => !exit.Destroyed && exit.entrance is { Destroyed: false, opended: true },
            { Destroyed: true } => false,
            _ => true
        });

    private Dictionary<Map, List<CQFCrossMapStep>> BuildCrossMapRoutes()
    {
        Dictionary<Map, List<CQFCrossMapStep>> routes = new();
        foreach (Map map in game.Maps.Concat(hibernatingMaps))
        {
            if (map.Parent is MapParent_Custom { sourceMap: { } source, entrance: { Destroyed: false } entrance, Exit: { Destroyed: false } exit }
                && ContainsCrossMapThing(source, entrance) && ContainsCrossMapThing(map, exit))
            {
                Add(source, map, Departure(entrance, source), exit.Position, entrance);
                Add(map, source, Departure(exit, map), entrance.Position, exit);
            }
        }
        foreach (CQFCompositeMapInstance instance in compositeMaps)
        {
            foreach (CustomMapEntrance_Composite entrance in instance.StandardEntrances)
            {
                if (entrance is not { Destroyed: false, FixedSource: { } source, CustomMap: { } destination } ||
                    !ContainsCrossMapThing(source, entrance)) continue;
                if (entrance.FixedOneWay)
                {
                    Add(source, destination, Departure(entrance, source), entrance.GetDestinationLocation(), entrance);
                    continue;
                }
                if (entrance.exit is not { Destroyed: false } exit || !ContainsCrossMapThing(destination, exit)) continue;
                Add(source, destination, Departure(entrance, source), exit.Position, entrance);
                Add(destination, source, Departure(exit, destination), entrance.Position, exit);
            }
            foreach (CQFCompositeMapConnection edge in instance.Edges)
            {
                if (instance.GetMap(edge.from) is not { } from || instance.GetMap(edge.to) is not { } to) continue;
                int offset = edge.length / 2;
                IntVec3 fromCell = CQFCompositeMapDef.EdgeCell(from.Size, edge.fromEdge, edge.fromStart + offset);
                IntVec3 toCell = CQFCompositeMapDef.EdgeCell(to.Size, edge.toEdge, edge.toStart + offset);
                IntVec3 fromApproach = CQFCompositeMapDef.EdgeCell(from.Size, edge.fromEdge, edge.fromStart + offset, 1);
                IntVec3 toApproach = CQFCompositeMapDef.EdgeCell(to.Size, edge.toEdge, edge.toStart + offset, 1);
                Add(from, to, fromCell, toApproach, null, fromApproach);
                if (edge.twoWay)
                    Add(to, from, toCell, fromApproach, null, toApproach);
            }
        }
        return routes;

        static IntVec3 Departure(Thing portal, Map map) =>
            ThingUtility.InteractionCellWhenAt(portal.def, portal.Position, portal.Rotation, map);

        void Add(Map from, Map to, IntVec3 departure, IntVec3 arrival, Thing? portal, IntVec3? edgeApproach = null)
        {
            if (from.Disposed || to.Disposed || !departure.InBounds(from) || !arrival.InBounds(to)) return;
            if (!routes.TryGetValue(from, out List<CQFCrossMapStep>? edges)) routes.Add(from, edges = new());
            edges.Add(new CQFCrossMapStep(from, to, departure, arrival, portal, edgeApproach));
        }
    }

    private CQFCrossMapCoordinator crossMap = new();
    private Dictionary<Map, List<CQFCrossMapStep>>? crossMapRoutes;
}
#endif
