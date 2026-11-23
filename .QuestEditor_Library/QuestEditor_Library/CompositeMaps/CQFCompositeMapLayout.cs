using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositeMapLayout
{
    internal CQFCompositeMapLayout(CQFCompositeMapDef definition, bool full)
    {
        this.full = full;
        regions = definition.regions;
        minX = regions.Min(region => region.position.x);
        maxZ = regions.Max(region => region.position.z);
        columns = (float)regions.Max(region => region.position.x) - minX + 1;
        rows = (float)maxZ - regions.Min(region => region.position.z) + 1;
        positions = new(regions.Count, StringComparer.Ordinal);
    }

    internal Dictionary<string, Rect> GetPositions(Rect available)
    {
        if (area == available) return positions;

        float width = Mathf.Min(available.width / columns, available.height / rows * (full ? 1.5f : 2f));
        float height = Mathf.Min(available.height / rows, width);
        float x = available.x + (available.width - width * columns) / 2;
        float y = available.y + (available.height - height * rows) / 2;
        float gutter = full ? 10 : 4;

        foreach (CQFCompositeMapRegion region in regions)
            positions[region.id] = new Rect(x + ((float)region.position.x - minX) * width,
                y + ((float)maxZ - region.position.z) * height, width, height).ContractedBy(gutter);
        area = available;
        return positions;
    }

    private readonly bool full;
    private readonly List<CQFCompositeMapRegion> regions;
    private readonly int minX;
    private readonly int maxZ;
    private readonly float columns;
    private readonly float rows;
    private readonly Dictionary<string, Rect> positions;
    private Rect? area;
}
