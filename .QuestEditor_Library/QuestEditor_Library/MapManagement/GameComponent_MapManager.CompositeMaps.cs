using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    public IReadOnlyList<CQFCompositeMapInstance> CompositeMaps => compositeMaps;

    public CQFCompositeMapInstance? GetCompositeMap(Map? map) =>
        map != null ? compositeByMap.GetValueOrDefault(map) : null;

    public CQFCompositeMapInstance GenerateCompositeMap(CQFCompositeMapDef definition, Map source,
        IntVec3 entranceCell, Quest? quest = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ValidateMap(source);
        if (GameTools.isGeneratingMap || MapGenerator.mapBeingGenerated != null)
            throw new InvalidOperationException("[CQF] Composite map generation requires a finished source map.");
        if (!game.Maps.Contains(source) || !ValidArrival(source, entranceCell) || entranceCell.GetFirstBuilding(source) != null)
            throw new InvalidOperationException("[CQF] Invalid composite map entrance cell: " + entranceCell);
        string[] errors = definition.Validate().ToArray();
        if (errors.Length > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        if (definition.regions.Count > sbyte.MaxValue + 1 - TotalMapCount)
            throw new InvalidOperationException("[CQF] Too many maps to generate " + definition.defName);

        CQFCompositeMapInstance instance = new(definition, source, entranceCell);
        List<CQFCompositeMapParent> parents = new();
        CQFCompositeMapPortal? entrance = null;
        string phase = definition.defName;
        try
        {
            foreach (CQFCompositeMapRegion region in definition.regions)
            {
                phase = region.id;
                CQFCompositeMapParent parent = (CQFCompositeMapParent)WorldObjectMaker.MakeWorldObject(CQFCompositeMapDefs.CQF_CompositeMapRegion);
                parents.Add(parent);
                parent.organizationScope = parents[0].ID;
                parent.mapDataDef = region.map;
                parent.quest = quest!;
                parent.Tile = source.Tile;
                parent.permanent = true;
                parent.mapGenerator = region.map.generator ?? parent.def.mapGenerator;
                IEnumerable<GenStepWithParams> steps = region.map.GetSteps()
                    .Append(new GenStepWithParams(CQFCompositeMapDefs.CQF_CompositeMapVisibility, new GenStepParams()));
                Map map = GameTools.GenerateSubMap(region.map.size, parent, parent.mapGenerator, steps, source);
                if (!parent.GenerationCompleted)
                    throw new InvalidOperationException("[CQF] CQF map generation did not complete for " + region.map.defName);
                instance.Add(region.id, map);
            }
            Map entryMap = instance.EntryMap!;
            phase = definition.entry;
            foreach (CQFCompositeMapConnection link in definition.portals)
            {
                Map from = instance.GetMap(link.from)!;
                Map to = instance.GetMap(link.to)!;
                phase = link.from + " / " + link.to;
                PlacePortal(from, link.fromCell, to, link.toCell);
                if (link.twoWay) PlacePortal(to, link.toCell, from, link.fromCell);
            }
            phase = definition.entry;
            PlacePortal(entryMap, definition.entryCell, source, entranceCell);
            entrance = PlacePortal(source, entranceCell, entryMap, definition.entryCell);
            instance.Bind(entrance);
            compositeMaps.Add(instance);
            foreach (Map map in instance.Maps) compositeByMap.Add(map, instance);
            RegisterCompositeEdges(instance);
            InvalidateCrossMapRoutes();
            if (Current.ProgramState == ProgramState.Playing) Find.ColonistBar.MarkColonistsDirty();
            return instance;
        }
        catch (Exception error)
        {
            compositeMaps.Remove(instance);
            foreach (Map map in instance.Maps)
            {
                compositeByMap.Remove(map);
                ForgetCompositeEdges(map);
            }
            List<Exception> failures = new() { error };
            try { if (entrance?.Spawned == true) entrance.Destroy(); }
            catch (Exception cleanup) { failures.Add(cleanup); }
            foreach (CQFCompositeMapParent parent in parents)
            {
                try { RemoveCompositeRegion(parent); }
                catch (Exception cleanup) { failures.Add(cleanup); }
            }
            throw new AggregateException("[CQF] Composite map generation failed: " + definition.defName + " / " + phase, failures);
        }
    }

    public void ViewCompositeMap(CQFCompositeMapInstance instance)
    {
        if (!compositeMaps.Contains(instance) || instance.EntryMap is not Map entry || entry.Disposed)
            throw new InvalidOperationException("[CQF] Composite map entry is unavailable.");
        ViewCompositeRegion(entry);
    }

    public void DestroyCompositeMap(CQFCompositeMapInstance instance)
    {
        if (!compositeMaps.Contains(instance))
            throw new InvalidOperationException("[CQF] Composite map instance is unavailable.");
        if (instance.UsesRegisteredMaps)
        {
            DestroyEntranceCompositeBranch(instance.RootGroup.Owner!);
            return;
        }
        List<Map> maps = GetCompositeBranchMaps(instance, instance.Groups);
        if (CompositeRemovalBlock(maps) is { } blocked)
            throw new InvalidOperationException(blocked.Translate().ToString());
        if (maps.Contains(game.CurrentMap) && !instance.Source.Disposed)
        {
            Activate(instance.Source);
            CameraJumper.TryJump(instance.EntranceCell, instance.Source, CameraJumper.MovementMode.Cut);
        }
        RemoveCompositeBranchMaps(maps);
        if (instance.Entrance is { Destroyed: false } entrance && !instance.Source.Disposed)
        {
            bool wasHibernating = IsHibernating(instance.Source);
            Activate(instance.Source);
            entrance.Destroy();
            if (wasHibernating) Hibernate(instance.Source);
        }
        foreach (Map map in instance.Maps) compositeByMap.Remove(map);
        CloseCompositeInstance(instance);
        DirtyCompositeColonists();
    }


    internal void ViewCompositeRegion(Map map)
    {
        if (GetCompositeMapForView(map) is not CQFCompositeMapInstance instance || map.Disposed)
            throw new InvalidOperationException("[CQF] Composite map region is unavailable.");
        RememberCompositeCamera();
        Activate(map);
        IntVec3 cell = instance.CameraCell(map);
        CameraJumper.TryJump(cell.InBounds(map) ? cell : map.Center, map, CameraJumper.MovementMode.Cut);
    }

    internal bool FocusCompositeColonist(Pawn pawn)
    {
        if (pawn.MapHeld is not { Disposed: false } map || GetCompositeMapForView(map) == null) return false;
        IntVec3 cell = pawn.PositionHeld;
        if (!cell.InBounds(map)) return false;
        RememberCompositeCamera();
        Activate(map);
        CameraJumper.TryJump(cell, map, CameraJumper.MovementMode.Cut);
        return true;
    }

    internal (bool HasColonists, bool KeptAwake) GetCompositeMapStatus(Map map)
    {
        if (IsHibernating(map)) return (false, false);
        return HasCompositeColonists(map) ? (true, false) : (false, CQFModuleLoader.KeepsMapActive(game, map));
    }

    internal void QueueCompositeTransfer(Action transfer) => compositeTransfers.Add(transfer);

    internal CQFCompositeMapInstance? GetCompositeMapForView(Map? map)
    {
        if (GetCompositeMap(map) is { } member) return member;
        if (map?.Parent is not CustomSite || map.Disposed) return null;
        if (minimap?.Instance is { } shown && shown.ViewableSource == map) return shown;
        foreach (CQFCompositeMapInstance instance in compositeMaps)
            if (instance.ViewableSource == map) return instance;
        return null;
    }

    internal void QueueCompositeEdgeTravel(Pawn pawn, Map source, IntVec3 cell)
    {
        if (GetCompositeMap(source) is not CQFCompositeMapInstance instance ||
            !instance.TryGetEdge(source, cell, out Map target, out IntVec3 arrival))
            return;

        QueueCompositeTransfer(() =>
        {
            if (pawn.Spawned && pawn.Map == source && pawn.Position == cell)
                TryTravelCompositeMap(pawn, target, arrival, edge: true);
        });
    }

    internal bool TryTravelCompositeMap(Pawn pawn, Map? target, IntVec3 arrival, bool edge = false)
    {
        if (target == null || target.Disposed)
        {
            Messages.Message("CQF_Composite_DestinationUnavailable".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        Activate(target);
        if (!CanLand(target, arrival, edge) &&
            !CellFinder.TryFindRandomCellNear(arrival, target, 3, cell => CanLand(target, cell, edge), out arrival))
        {
            Messages.Message("CQF_Composite_ArrivalBlocked".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        Map source = pawn.Map;
        List<Pawn>? pursuers = crossMap.FindPursuers(pawn, source);
        IntVec3 sourceCell = pawn.Position;
        Rot4 rotation = pawn.Rotation;
        bool drafted = pawn.Drafted;
        bool follow = game.CurrentMap == source && Find.Selector.IsSelected(pawn);
        pawn.DeSpawn();
        try
        {
            GenSpawn.Spawn(pawn, arrival, target, rotation);
        }
        catch (Exception error)
        {
            if (!pawn.Spawned) GenSpawn.Spawn(pawn, sourceCell, source, rotation);
            throw new InvalidOperationException("[CQF] Composite map transfer failed for " + pawn.GetUniqueLoadID(), error);
        }

        if (pawn.drafter != null) pawn.drafter.Drafted = drafted;
        Current.Game.GetComponent<GameComponent_MetaLord>().Manager.GetForPawn(pawn)?.PawnArrivedNewLevel(pawn, target);
        crossMap.Pursue(pursuers, pawn, source, target, sourceCell);
        if (follow) CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(pawn), CameraJumper.MovementMode.Cut);
        return true;
    }

    private void ProcessCompositeTransfers()
    {
        if (compositeTransfers.Count == 0) return;
        Action[] pending = compositeTransfers.ToArray();
        compositeTransfers.Clear();
        foreach (Action transfer in pending)
        {
            try { transfer(); }
            catch (Exception error) { Log.Error("[CQF] Composite map transfer failed: " + error); }
        }
    }

    private void UpdateCompositeMaps()
    {
        if (Current.ProgramState != ProgramState.Playing || LongEventHandler.AnyEventNowOrWaiting ||
            (compositeMaps.Count == 0 && minimap == null)) return;
        RememberCompositeCamera();
        try
        {
            if (compositeMaps.Exists(static instance => !instance.UsesRegisteredMaps))
            {
                foreach (Map map in game.Maps)
                    if (map.Parent is CQFCompositeMapParent && GetCompositeMap(map) != null && CanHibernateComposite(map))
                        compositeHibernationCandidates.Add(map);
            }

            HashSet<Map>? collectedMaps = null;
            if (compositeHibernationCandidates.Count > 1)
            {
                CQFModuleLoader.CollectLifetimeMaps(game, compositeLifetimeMaps);
                collectedMaps = compositeLifetimeMaps;
            }
            foreach (Map map in compositeHibernationCandidates) RequestHibernation(map, collectedMaps);
        }
        finally
        {
            compositeHibernationCandidates.Clear();
            compositeLifetimeMaps.Clear();
        }
        Map? currentMap = WorldRendererUtility.DrawingMap ? game.CurrentMap : null;
        UpdateCompositeMinimap(GetCompositeMapForView(currentMap));

    }

    private void UpdateCompositeMinimap(CQFCompositeMapInstance? instance)
    {
        if (minimap != null && minimap.Instance == instance && Find.WindowStack.IsOpen(minimap))
        {
            minimap.SyncGeometry();
            return;
        }
        if (minimap != null) Find.WindowStack.TryRemove(minimap, false);
        minimap = null;
        if (instance == null) return;
        minimap = new CQFCompositeMinimap(this, instance);
        Find.WindowStack.Add(minimap);
    }

    private void RememberCompositeCamera()
    {
        Map map = game.CurrentMap;
        if (WorldRendererUtility.DrawingMap && GetCompositeMapForView(map) is CQFCompositeMapInstance instance)
        {
            IntVec3 cell = Find.CameraDriver.MapPosition;
            if (cell.InBounds(map)) instance.RememberCamera(map, cell);
        }
    }

    private bool CanHibernateComposite(Map map) =>
        (map != game.CurrentMap || !WorldRendererUtility.DrawingMap) && !HasCompositeColonists(map) &&
        GetCompositeMap(map)?.KeepsSourceActive(map) != true;

    private static bool HasCompositeColonists(Map map)
    {
        IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < spawned.Count; i++)
            if (IsCompositeColonist(spawned[i])) return true;
        foreach (Pawn pawn in map.mapPawns.AllPawnsUnspawned)
            if (IsCompositeColonist(pawn)) return true;
        foreach (Thing thing in map.spawnedThings)
            if (thing is Pawn { carryTracker.CarriedThing: Pawn carried } && IsCompositeColonist(carried))
                return true;
        return false;
    }

    private static bool CanLand(Map map, IntVec3 cell, bool edge) => ValidArrival(map, cell) &&
        (!edge || (cell.x > 0 && cell.z > 0 && cell.x < map.Size.x - 1 && cell.z < map.Size.z - 1)) &&
        cell.GetFirstPawn(map) == null;

    private static bool IsCompositeColonist(Pawn pawn) => pawn.IsColonist || pawn.IsSlaveOfColony;

    private void RestoreCompositeMaps()
    {
        foreach (CQFCompositeMapInstance instance in compositeMaps)
        {
            instance.RestoreGroups();
            if (instance.Source == null || instance.EntryMap == null)
                throw new InvalidOperationException("[CQF] A saved composite map has an unresolved definition, source or entry.");
            foreach (CQFCompositeMapGroup group in instance.Groups)
            {
                if (group == instance.RootGroup && !instance.UsesRegisteredMaps) continue;
                if (group.Owner == null || instance.GetMap(group.EntryRegion) is not { } entry ||
                    entry != group.Owner.CustomMap ||
                    (group != instance.RootGroup && (group.ParentRegion == null || instance.GetMap(group.ParentRegion) == null)) ||
                    !compositeByEntrance.TryAdd(group.Owner, group))
                    throw new InvalidOperationException("[CQF] A saved composite map has an unresolved or duplicate entrance.");
            }
            foreach (Map map in instance.Maps)
                if (map == null || !compositeByMap.TryAdd(map, instance))
                    throw new InvalidOperationException("[CQF] A saved composite map has an unresolved or duplicate region.");
            RegisterCompositeEdges(instance);
        }
    }

    private static CQFCompositeMapPortal PlacePortal(Map map, IntVec3 cell, Map otherMap, IntVec3 otherCell)
    {
        if (!ValidArrival(map, cell) || cell.GetFirstBuilding(map) != null || !ValidArrival(otherMap, otherCell))
            throw new InvalidOperationException("[CQF] Invalid composite map portal cells: " + cell + " / " + otherCell);
        CQFCompositeMapPortal portal = (CQFCompositeMapPortal)ThingMaker.MakeThing(CQFCompositeMapDefs.CQF_CompositeMapPortal);
        portal.Bind(otherMap, otherCell);
        GenSpawn.Spawn(portal, cell, map);
        return portal;
    }

    internal static bool ValidArrival(Map map, IntVec3 cell) =>
        cell.InBounds(map) && cell.Standable(map);

    private void RemoveCompositeRegion(CQFCompositeMapParent parent)
    {
        Map? map = game.Maps.FirstOrDefault(candidate => candidate.Parent == parent);
        if (map != null) game.DeinitAndRemoveMap(map, notifyPlayer: false);
        Find.World.pocketMaps.Remove(parent);
    }

    private List<CQFCompositeMapInstance> compositeMaps = new();
    private readonly Dictionary<Map, CQFCompositeMapInstance> compositeByMap = new();
    private readonly List<Action> compositeTransfers = new();
    private readonly List<Map> compositeHibernationCandidates = new();
    private readonly HashSet<Map> compositeLifetimeMaps = new();
    private CQFCompositeMinimap? minimap;

}
