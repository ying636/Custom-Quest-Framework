#if RIMWORLD_1_7
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    public CQFCompositeMapGroup? GetCompositeGroup(CustomMapEntrance_Composite owner) =>
        compositeByEntrance.GetValueOrDefault(owner);

    public CQFCompositeMapInstance RegisterCompositeEntrance(CustomMapEntrance_Composite owner, Map source, Map destination)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ValidateCompositeLink(source, destination);
        if (owner.Map != source || owner.CustomMap != destination)
            throw new InvalidOperationException("[CQF] Composite entrance does not own the destination map.");
        if (GetCompositeGroup(owner) is { } previous)
        {
            if (GetCompositeMap(destination) is { } current && current.GetGroup(destination) == previous &&
                current.GetMap(previous.EntryRegion) == destination)
                return current;
            throw new InvalidOperationException("[CQF] The entrance already owns a composite map group.");
        }
        if (GetCompositeMap(destination) != null)
            throw new InvalidOperationException("[CQF] The destination already belongs to a composite map.");

        CQFCompositeMapInstance? existing = GetCompositeMap(source);
        CQFCompositeMapInstance instance = existing ?? new CQFCompositeMapInstance(source, owner.Position);
        CQFCompositeMapGroup group = instance.AddGroup(owner, source, destination);
        compositeByEntrance.Add(owner, group);
        compositeByMap.Add(destination, instance);
        InvalidateCrossMapRoutes();
        if (existing == null) compositeMaps.Add(instance);
        NotifyMapActivity(source);
        DirtyCompositeColonists();
        return instance;
    }

    public CQFCompositeMapInstance? RegisterCompositeSubMap(Map source, Map destination, IntVec2? position = null)
    {
        if (GetCompositeMap(source) is not { } instance) return null;
        ValidateCompositeLink(source, destination);
        if (GetCompositeMap(destination) is { } existing)
        {
            if (existing == instance && instance.MatchesRegistration(destination, source, position))
                return instance;
            throw new InvalidOperationException("[CQF] The destination already belongs to a composite map.");
        }
        instance.AppendRegion(source, destination, position);
        compositeByMap.Add(destination, instance);
        NotifyMapActivity(source);
        DirtyCompositeColonists();
        return instance;
    }

    internal string? CompositeBranchRemovalBlock(CustomMapEntrance_Composite owner)
    {
        if (GetCompositeGroup(owner) is not { } group || GetCompositeMap(owner.CustomMap) is not { } instance)
            return null;
        return CompositeRemovalBlock(GetCompositeBranchMaps(instance, instance.Branch(group)));
    }

    internal void DestroyEntranceCompositeBranch(CustomMapEntrance_Composite owner)
    {
        if (GetCompositeGroup(owner) is not { } group || GetCompositeMap(owner.CustomMap) is not { } instance)
            return;
        List<Map> maps = GetCompositeBranchMaps(instance, instance.Branch(group));
        if (CompositeRemovalBlock(maps) is { } blocked)
            throw new InvalidOperationException(blocked.Translate().ToString());
        if (maps.Contains(game.CurrentMap) && owner.Map is { Disposed: false } source)
            CameraJumper.TryJump(owner.Position, Activate(source), CameraJumper.MovementMode.Cut);
        RemoveCompositeBranchMaps(maps);
    }

    private void ValidateCompositeLink(Map source, Map destination)
    {
        ValidateMap(source);
        ValidateMap(destination);
        if (source == destination || (!game.Maps.Contains(source) && !IsHibernating(source)) ||
            (!game.Maps.Contains(destination) && !IsHibernating(destination)))
            throw new InvalidOperationException("[CQF] A composite registration requires distinct, generated source and destination maps.");
    }

    private static List<Map> GetCompositeBranchMaps(CQFCompositeMapInstance instance, IReadOnlyList<CQFCompositeMapGroup> groups)
    {
        List<Map> maps = new();
        foreach (CQFCompositeMapGroup group in groups)
            foreach (CQFCompositeMapRegion region in group.Regions)
                if (instance.GetMap(region.id) is { Disposed: false } map) maps.Add(map);
        return maps;
    }

    private string? CompositeRemovalBlock(List<Map> maps)
    {
        HashSet<MapParent> parents = new(maps.Select(map => map.Parent));
        foreach (Map map in maps)
        {
            if (HasCompositeColonists(map) || (!IsHibernating(map) && map.mapPawns.AnyPawnBlockingMapRemoval))
                return "CQF_Composite_BranchOccupied";
            if (map.Parent is MapParent_Custom parent &&
                (parent.entrance?.thereIsPawnIsEntering == true || parent.exit?.thereIsPawnIsEntering == true))
                return "CQF_Composite_BranchInUse";
        }
        return Find.World.pocketMaps.Any(parent => maps.Contains(parent.sourceMap) && !parents.Contains(parent))
            ? "CQF_Composite_BranchHasReturn" : null;
    }

    private void RemoveCompositeBranchMaps(List<Map> maps)
    {
        for (int i = maps.Count - 1; i >= 0; i--)
        {
            Map map = maps[i];
            if (map.Disposed) continue;
            Activate(map);
            if (map.Parent is CQFCompositeMapParent parent)
                RemoveCompositeRegion(parent);
            else
                PocketMapUtility.DestroyPocketMap(map);
        }
    }

    private void ForgetRegisteredCompositeRegion(Map map)
    {
        if (GetCompositeMap(map) is not { } instance || instance.GetGroup(map) is not { } group) return;
        if (instance.GetMap(group.EntryRegion) == map)
        {
            ForgetCompositeGroups(instance, instance.Branch(group));
            return;
        }

        string id = instance.RegionId(map);
        foreach (CQFCompositeMapGroup child in instance.ChildrenAt(id).ToArray())
            ForgetCompositeGroups(instance, instance.Branch(child));
        compositeByMap.Remove(map);
        instance.RemoveRegion(map);
        DirtyCompositeColonists();
    }

    private void ForgetCompositeGroups(CQFCompositeMapInstance instance, List<CQFCompositeMapGroup> groups)
    {
        bool removeInstance = groups.Contains(instance.RootGroup);
        foreach (CQFCompositeMapGroup group in groups)
        {
            if (group.Owner is { } owner)
            {
                compositeByEntrance.Remove(owner);
                if (owner.CustomMap == instance.GetMap(group.EntryRegion))
                {
                    owner.CustomMap = null!;
                    owner.exit = null!;
                }
            }
            foreach (CQFCompositeMapRegion region in group.Regions)
                if (instance.GetMap(region.id) is { } map) compositeByMap.Remove(map);
        }
        instance.RemoveGroups(groups);
        if (removeInstance) CloseCompositeInstance(instance);
        DirtyCompositeColonists();
    }

    private void CloseCompositeInstance(CQFCompositeMapInstance instance)
    {
        compositeMaps.Remove(instance);
        if (Current.ProgramState != ProgramState.Playing || Find.UIRoot == null) return;
        if (minimap?.Instance == instance) UpdateCompositeMinimap(null);
        if (Find.WindowStack.WindowOfType<CQFCompositeMapWindow>() is { } window && window.Instance == instance)
            Find.WindowStack.TryRemove(window, false);
    }

    private static void DirtyCompositeColonists()
    {
        if (Current.ProgramState == ProgramState.Playing && Find.UIRoot != null) Find.ColonistBar.MarkColonistsDirty();
    }

    private readonly Dictionary<CustomMapEntrance_Composite, CQFCompositeMapGroup> compositeByEntrance = new();
}
#endif
