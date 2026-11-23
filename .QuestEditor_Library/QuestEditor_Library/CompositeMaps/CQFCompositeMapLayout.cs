using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositeMapLayout
{
    internal CQFCompositeMapLayout(bool full)
    {
        this.full = full;
    }

    internal Dictionary<CQFCompositeMapGroup, Rect> BranchPositions => branchPositions;

    internal Dictionary<string, Rect> GetPositions(CQFCompositeMapInstance instance, CQFCompositeMapGroup group, Rect available)
    {
        if (displayedGroup == group && area == available && layoutVersion == group.LayoutVersion) return positions;
        IReadOnlyList<CQFCompositeMapRegion> regions = group.Regions;
        HashSet<IntVec2> occupied = new(regions.Select(region => region.position));
        Dictionary<CQFCompositeMapGroup, IntVec2> branchCells = new();
        foreach (CQFCompositeMapRegion region in regions)
            foreach (CQFCompositeMapGroup branch in instance.ChildrenAt(region.id))
                branchCells.Add(branch, PlaceBranch(region.position, occupied));
        int minX = occupied.Min(cell => cell.x);
        int maxZ = occupied.Max(cell => cell.z);
        float columns = (float)occupied.Max(cell => cell.x) - minX + 1;
        float rows = (float)maxZ - occupied.Min(cell => cell.z) + 1;

        float width = Mathf.Min(available.width / columns, available.height / rows * (full ? 1.5f : 2f));
        float height = Mathf.Min(available.height / rows, width);
        float x = available.x + (available.width - width * columns) / 2;
        float y = available.y + (available.height - height * rows) / 2;
        float gutter = Mathf.Min(full ? 10f : 4f, height * 0.20f);
        positions.Clear();
        branchPositions.Clear();

        foreach (CQFCompositeMapRegion region in regions)
            positions[region.id] = At(region.position);
        foreach ((CQFCompositeMapGroup branch, IntVec2 cell) in branchCells)
            branchPositions.Add(branch, At(cell));
        area = available;
        displayedGroup = group;
        layoutVersion = group.LayoutVersion;
        return positions;

        Rect At(IntVec2 cell) => new Rect(x + ((float)cell.x - minX) * width,
            y + ((float)maxZ - cell.z) * height, width, height).ContractedBy(gutter);
    }

    private static IntVec2 PlaceBranch(IntVec2 origin, HashSet<IntVec2> occupied)
    {
        for (int distance = 1; ; distance++)
        {
            for (int offset = 0; offset <= distance; offset++)
            {
                IntVec2 candidate = new(origin.x + offset, origin.z - distance);
                if (occupied.Add(candidate)) return candidate;
                if (occupied.Add(candidate = new IntVec2(origin.x + distance, origin.z + offset))) return candidate;
                if (occupied.Add(candidate = new IntVec2(origin.x - offset, origin.z + distance))) return candidate;
                if (occupied.Add(candidate = new IntVec2(origin.x - distance, origin.z - offset))) return candidate;
                if (offset == 0 || offset == distance) continue;
                if (occupied.Add(candidate = new IntVec2(origin.x - offset, origin.z - distance))) return candidate;
                if (occupied.Add(candidate = new IntVec2(origin.x + distance, origin.z - offset))) return candidate;
                if (occupied.Add(candidate = new IntVec2(origin.x + offset, origin.z + distance))) return candidate;
                if (occupied.Add(candidate = new IntVec2(origin.x - distance, origin.z + offset))) return candidate;
            }
        }
    }

    private readonly bool full;
    private readonly Dictionary<string, Rect> positions = new(StringComparer.Ordinal);
    private readonly Dictionary<CQFCompositeMapGroup, Rect> branchPositions = new();
    private Rect? area;
    private CQFCompositeMapGroup? displayedGroup;
    private int layoutVersion;
}
