using Verse;

namespace QuestEditor_Library;

public sealed class CQFCompositeMapDef : Def
{
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        foreach (string error in Validate()) yield return error;
    }

    internal IEnumerable<string> Validate()
    {
        if (regions.Count < 2) yield return defName + ": at least two regions are required.";
        Dictionary<string, CQFCompositeMapRegion> ids = new(StringComparer.Ordinal);
        HashSet<IntVec2> positions = new();
        foreach (CQFCompositeMapRegion region in regions)
        {
            if (region == null || string.IsNullOrWhiteSpace(region.id) || !ids.TryAdd(region.id, region))
            {
                yield return defName + ": empty or duplicate region ID.";
                continue;
            }
            if (region.map == null || region.map.size.x < 3 || region.map.size.y != 1 || region.map.size.z < 3)
                yield return defName + ": invalid map template or size for " + region.id;
            if (!positions.Add(region.position))
                yield return defName + ": duplicate minimap position for " + region.id;
        }
        if (!ids.TryGetValue(entry, out CQFCompositeMapRegion? entryRegion))
            yield return defName + ": unknown entry region " + entry;
        else if (entryRegion.map != null && !InBounds(entryCell, entryRegion.map.size))
            yield return defName + ": invalid entry cell " + entryCell;
        HashSet<(string, IntVec3)> outgoing = new();
        HashSet<(string, IntVec3)> portalCells = new();
        foreach (CQFCompositeMapConnection link in edges.Concat(portals))
        {
            if (link == null || !ids.TryGetValue(link.from, out CQFCompositeMapRegion? from) ||
                !ids.TryGetValue(link.to, out CQFCompositeMapRegion? to) || from == to || from.map == null || to.map == null)
            {
                yield return defName + ": invalid connection endpoint.";
                continue;
            }
            if (edges.Contains(link))
            {
                if (link.length < 1 || link.fromStart < 0 || link.toStart < 0 ||
                    link.length > EdgeSize(from.map.size, link.fromEdge) - link.fromStart ||
                    link.length > EdgeSize(to.map.size, link.toEdge) - link.toStart ||
                    ((int)link.fromEdge + 2) % 4 != (int)link.toEdge)
                {
                    yield return defName + ": invalid opposing edge segments for " + link.from + " / " + link.to;
                    continue;
                }
                for (int i = 0; i < link.length; i++)
                {
                    IntVec3 a = EdgeCell(from.map.size, link.fromEdge, link.fromStart + i);
                    IntVec3 b = EdgeCell(to.map.size, link.toEdge, link.toStart + i);
                    if (!outgoing.Add((link.from, a)))
                        yield return defName + ": overlapping outgoing edge in " + link.from + " at " + a;
                    if (link.twoWay && !outgoing.Add((link.to, b)))
                        yield return defName + ": overlapping outgoing edge in " + link.to + " at " + b;
                }
            }
            else
            {
                if (!InBounds(link.fromCell, from.map.size) || !InBounds(link.toCell, to.map.size))
                    yield return defName + ": portal cell out of bounds for " + link.from + " / " + link.to;
                if (!portalCells.Add((link.from, link.fromCell)))
                    yield return defName + ": duplicate portal in " + link.from + " at " + link.fromCell;
                if (link.twoWay && !portalCells.Add((link.to, link.toCell)))
                    yield return defName + ": duplicate portal in " + link.to + " at " + link.toCell;
            }
        }
        if (portalCells.Contains((entry, entryCell)))
            yield return defName + ": the entry cell overlaps a portal.";
    }

    internal static IntVec3 EdgeCell(IntVec3 size, CQFCompositeMapEdge edge, int offset, int inset = 0) => edge switch
    {
        CQFCompositeMapEdge.North => new IntVec3(offset, 0, size.z - 1 - inset),
        CQFCompositeMapEdge.East => new IntVec3(size.x - 1 - inset, 0, offset),
        CQFCompositeMapEdge.South => new IntVec3(offset, 0, inset),
        CQFCompositeMapEdge.West => new IntVec3(inset, 0, offset),
        _ => throw new ArgumentOutOfRangeException(nameof(edge))
    };

    private static int EdgeSize(IntVec3 size, CQFCompositeMapEdge edge) => edge switch
    {
        CQFCompositeMapEdge.North or CQFCompositeMapEdge.South => size.x,
        CQFCompositeMapEdge.East or CQFCompositeMapEdge.West => size.z,
        _ => 0
    };

    private static bool InBounds(IntVec3 cell, IntVec3 size) =>
        cell.y == 0 && cell.x >= 0 && cell.x < size.x && cell.z >= 0 && cell.z < size.z;

    public List<CQFCompositeMapRegion> regions = new();
    public List<CQFCompositeMapConnection> edges = new();
    public List<CQFCompositeMapConnection> portals = new();
    public string entry = "";
    public IntVec3 entryCell = IntVec3.Invalid;
    public bool showEntranceTitle;
    public CQFCompositeMapPlacement appendedLayout;
}
