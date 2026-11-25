using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCompositeMapInstance : IExposable
{
    public CQFCompositeMapInstance() { }

    internal CQFCompositeMapInstance(CQFCompositeMapDef definition, Map source, IntVec3 entranceCell)
    {
        this.definition = definition;
        this.source = source;
        this.entranceCell = entranceCell;
        groups.Add(new CQFCompositeMapGroup(definition.entry, definition.appendedLayout));
        RebuildGroups();
    }

    internal CQFCompositeMapInstance(Map source, IntVec3 entranceCell)
    {
        this.source = source;
        this.entranceCell = entranceCell;
    }

    public CQFCompositeMapDef? Definition => definition;
    public CQFCompositeMapDatabase MapDatabase => mapDatabase;
    public Map Source => source;
    public CQFCompositeMapGroup RootGroup => groups[0];
    public IReadOnlyList<CQFCompositeMapGroup> Groups => groups;
    public string DisplayName => definition is { } def
        ? (def.defName.CanTranslate() ? def.defName.Translate() : def.LabelCap.ToString())
        : RootGroup.Owner!.LabelCap.ToString();
    public IntVec3 EntranceCell => entranceCell;
    public Map? EntryMap => GetMap(RootGroup.EntryRegion);
    public IEnumerable<Map> Maps => maps.Values;
    public bool UsesRegisteredMaps => definition == null;
    internal IReadOnlyList<CQFCompositeMapConnection> Edges => RootGroup.Edges;
    internal bool MinimapExpanded { get; set; } = true;
    internal CQFCompositeMapPortal? Entrance => entrance;
    internal Map? ViewableSource => UsesRegisteredMaps && source is { Disposed: false, Parent: CustomSite } ? source : null;
    internal Vector2 MinimapPosition { get => minimapPosition; set => minimapPosition = value; }

    public Map? GetMap(string id) => maps.GetValueOrDefault(id);
    public CQFCompositeMapGroup? GetGroup(Map map) => groupsByMap.GetValueOrDefault(map);

    public void ExposeData()
    {
        Scribe_Defs.Look(ref definition, "definition");
        Scribe_References.Look(ref source, "source");
        Scribe_Values.Look(ref entranceCell, "entranceCell");
        Scribe_References.Look(ref entrance, "entrance");
        Scribe_Collections.Look(ref maps, "maps", LookMode.Value, LookMode.Reference, ref savedIds, ref savedMaps);
        Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
        groups ??= new();
        Scribe_Deep.Look(ref mapDatabase!, "mapDatabase");
        mapDatabase ??= new();
        Scribe_Collections.Look(ref camera, "camera", LookMode.Value, LookMode.Value);
        Scribe_Values.Look(ref minimapPosition, "minimapPosition", new Vector2(-1, -1));
        bool expanded = MinimapExpanded;
        Scribe_Values.Look(ref expanded, "minimapExpanded", true);
        MinimapExpanded = expanded;
    }

    internal void Add(string id, Map map)
    {
        maps.Add(id, map);
        groupsByMap.Add(map, RootGroup);
        idsByMap.Add(map, id);
        mapDatabase.Register(RootGroup.Regions.First(region => region.id == id), map, RootGroup);
    }

    internal void Bind(CQFCompositeMapPortal portal) => entrance = portal;

    internal void RestoreGroups()
    {
        if (groups.Count == 0 && definition != null)
            groups.Add(new CQFCompositeMapGroup(definition.entry, definition.appendedLayout));
        if (groups.Count == 0)
            throw new InvalidOperationException("[CQF] Composite map has no serialized layout.");
        RebuildGroups();
        if (maps.Count != groupsByMap.Count)
            throw new InvalidOperationException("[CQF] Composite map has missing or unresolved layout members.");
    }

    internal CQFCompositeMapGroup AddGroup(CustomMapEntrance_Composite owner, Map from, Map map)
    {
        string id = map.GetUniqueLoadID();
        CQFCompositeMapGroup group = new(id, owner.Placement, owner, idsByMap.GetValueOrDefault(from));
        group.AddRegion(id, null, default(IntVec2));
        maps.Add(id, map);
        groups.Add(group);
        RebuildGroups();
        return group;
    }

    internal void AppendRegion(Map from, Map map, IntVec2? position)
    {
        CQFCompositeMapGroup group = groupsByMap[from];
        string id = map.GetUniqueLoadID();
        group.AddRegion(id, idsByMap[from], position);
        maps.Add(id, map);
        RebuildGroups();
    }

    internal bool MatchesRegistration(Map map, Map from, IntVec2? position) =>
        GetGroup(map) == GetGroup(from) && GetGroup(map)!.Regions.Any(region =>
            region.id == idsByMap[map] && region.sourceRegion == idsByMap[from] &&
            (!position.HasValue || position.Value == region.position));

    internal string RegionId(Map map) => idsByMap[map];

    internal void RemoveRegion(Map map)
    {
        string id = idsByMap[map];
        groupsByMap[map].RemoveRegion(id);
        maps.Remove(id);
        camera.Remove(map.uniqueID);
        RebuildGroups();
    }

    internal List<CQFCompositeMapGroup> Branch(CQFCompositeMapGroup group)
    {
        List<CQFCompositeMapGroup> branch = new() { group };
        foreach (CQFCompositeMapGroup child in groups)
            if (child.ParentRegion is { } parent && GetMap(parent) is { } host && branch.Contains(GetGroup(host)!))
                branch.Add(child);
        return branch;
    }

    internal void RemoveGroups(List<CQFCompositeMapGroup> branch)
    {
        foreach (CQFCompositeMapGroup group in branch)
        {
            foreach (CQFCompositeMapRegion region in group.Regions)
                if (maps.Remove(region.id, out Map? map)) camera.Remove(map.uniqueID);
            groups.Remove(group);
        }
        RebuildGroups();
    }

    internal IReadOnlyList<CQFCompositeMapGroup> ChildrenAt(string id) =>
        children.GetValueOrDefault(id) is { } branches ? branches : Array.Empty<CQFCompositeMapGroup>();

    internal string GroupName(CQFCompositeMapGroup group) =>
        group == RootGroup ? DisplayName : group.Owner!.LabelCap.ToString();

    internal bool KeepsSourceActive(Map map) => linkedSources.Contains(map);
    internal void RememberCamera(Map map, IntVec3 cell) => camera[map.uniqueID] = cell;
    internal IntVec3 CameraCell(Map map) => camera.GetValueOrDefault(map.uniqueID,
        map == source ? entranceCell : map == EntryMap && definition != null ? definition.entryCell : map.Center);

    internal bool TryGetEdge(Map map, IntVec3 cell, out Map target, out IntVec3 arrival)
    {
        if (definition != null)
        {
            foreach (CQFCompositeMapConnection link in definition.edges)
            {
                Map? from = GetMap(link.from);
                Map? to = GetMap(link.to);
                if (from == null || to == null) continue;
                if (from == map && TryResolveEdge(map, cell, link.fromEdge, link.fromStart, to, link.toEdge, link.toStart, link.length, out arrival))
                {
                    target = to;
                    return true;
                }
                if (link.twoWay && to == map && TryResolveEdge(map, cell, link.toEdge, link.toStart, from, link.fromEdge, link.fromStart, link.length, out arrival))
                {
                    target = from;
                    return true;
                }
            }
        }
        target = null!;
        arrival = IntVec3.Invalid;
        return false;
    }

    private void RebuildGroups()
    {
        groupsByMap.Clear();
        idsByMap.Clear();
        children.Clear();
        linkedSources.Clear();
        mapDatabase.ClearIndex();
        for (int i = 0; i < groups.Count; i++)
        {
            CQFCompositeMapGroup group = groups[i];
            group.Rebuild(i == 0 ? definition : null);
            foreach (CQFCompositeMapRegion region in group.Regions)
            {
                if (GetMap(region.id) is not { } map) continue;
                groupsByMap.Add(map, group);
                idsByMap.Add(map, region.id);
                mapDatabase.Register(region, map, group);
                if (map.Parent is MapParent_Custom { sourceMap: { } upstream } && map.Parent is not CQFCompositeMapParent)
                    linkedSources.Add(upstream);
            }
            if (group.ParentRegion is not { } parent) continue;
            if (!children.TryGetValue(parent, out List<CQFCompositeMapGroup>? branches))
                children.Add(parent, branches = new());
            branches.Add(group);
        }
        mapDatabase.PruneBindings();
    }

    private static bool TryResolveEdge(Map source, IntVec3 cell, CQFCompositeMapEdge edge, int start,
        Map target, CQFCompositeMapEdge targetEdge, int targetStart, int length, out IntVec3 arrival)
    {
        int coordinate = edge is CQFCompositeMapEdge.North or CQFCompositeMapEdge.South ? cell.x : cell.z;
        int offset = coordinate - start;
        if (offset >= 0 && offset < length && cell == CQFCompositeMapDef.EdgeCell(source.Size, edge, coordinate))
        {
            arrival = CQFCompositeMapDef.EdgeCell(target.Size, targetEdge, targetStart + offset, 1);
            return true;
        }
        arrival = IntVec3.Invalid;
        return false;
    }

    private CQFCompositeMapDef? definition;
    private Map source = null!;
    private IntVec3 entranceCell;
    private Vector2 minimapPosition = new(-1, -1);
    private CQFCompositeMapPortal? entrance;
    private Dictionary<string, Map> maps = new(StringComparer.Ordinal);
    private List<CQFCompositeMapGroup> groups = new();
    private CQFCompositeMapDatabase mapDatabase = new();
    private Dictionary<int, IntVec3> camera = new();
    private List<string>? savedIds;
    private List<Map>? savedMaps;
    private readonly Dictionary<Map, CQFCompositeMapGroup> groupsByMap = new();
    private readonly Dictionary<Map, string> idsByMap = new();
    private readonly Dictionary<string, List<CQFCompositeMapGroup>> children = new(StringComparer.Ordinal);
    private readonly HashSet<Map> linkedSources = new();
}
