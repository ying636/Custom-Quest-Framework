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
    }

    public CQFCompositeMapDef Definition => definition;
    public Map Source => source;
    public string DisplayName => definition.defName.CanTranslate() ? definition.defName.Translate() : definition.LabelCap.ToString();
    public IntVec3 EntranceCell => entranceCell;
    public Map? EntryMap => GetMap(definition.entry);

    public IEnumerable<Map> Maps => maps.Values;
    internal bool MinimapExpanded { get; set; } = true;
    internal CQFCompositeMapPortal? Entrance => entrance;
    internal Vector2 MinimapPosition { get => minimapPosition; set => minimapPosition = value; }

    public Map? GetMap(string id) => maps.GetValueOrDefault(id);

    public void ExposeData()
    {
        Scribe_Defs.Look(ref definition, "definition");
        Scribe_References.Look(ref source, "source");
        Scribe_Values.Look(ref entranceCell, "entranceCell");
        Scribe_References.Look(ref entrance, "entrance");
        Scribe_Collections.Look(ref maps, "maps", LookMode.Value, LookMode.Reference, ref savedIds, ref savedMaps);
        Scribe_Collections.Look(ref camera, "camera", LookMode.Value, LookMode.Value);
        Scribe_Values.Look(ref minimapPosition, "minimapPosition", new Vector2(-1, -1));
        bool expanded = MinimapExpanded;
        Scribe_Values.Look(ref expanded, "minimapExpanded", true);
        MinimapExpanded = expanded;
    }

    internal void Add(string id, Map map) => maps.Add(id, map);
    internal void Bind(CQFCompositeMapPortal portal) => entrance = portal;

    internal void RememberCamera(Map map, IntVec3 cell) => camera[map.uniqueID] = cell;
    internal IntVec3 CameraCell(Map map) => camera.GetValueOrDefault(map.uniqueID,
        map == EntryMap ? definition.entryCell : map.Center);

    internal bool TryGetEdge(Map map, IntVec3 cell, out Map target, out IntVec3 arrival)
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
        target = null!;
        arrival = IntVec3.Invalid;
        return false;
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

    private CQFCompositeMapDef definition = null!;
    private Map source = null!;
    private IntVec3 entranceCell;
    private Vector2 minimapPosition = new(-1, -1);
    private CQFCompositeMapPortal? entrance;
    private Dictionary<string, Map> maps = new(StringComparer.Ordinal);
    private Dictionary<int, IntVec3> camera = new();
    private List<string>? savedIds;
    private List<Map>? savedMaps;
}
