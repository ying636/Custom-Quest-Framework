using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainTerritoryPartition
{
    public int FirstClaimedCells => firstClaims.Count;
    public int SecondClaimedCells => secondClaims.Count;

    public (int First, int Second) Capture(IntVec3? firstCenter, IntVec3? secondCenter)
    {
        if (owners == null) { owners = new bool[CellCount]; RebuildOwners(); changedCells.Clear(); }
        captureCells.Clear();
        AddCaptureCells(firstCenter);
        AddCaptureCells(secondCenter);
        int firstGained = 0, secondGained = 0;
        bool changed = false;
        foreach (IntVec3 cell in captureCells)
        {
            if (ProtectionAt(cell.x, cell.z) != 0) continue;
            int firstDistance = firstCenter.HasValue ? (cell - firstCenter.Value).LengthHorizontalSquared : int.MaxValue;
            int secondDistance = secondCenter.HasValue ? (cell - secondCenter.Value).LengthHorizontalSquared : int.MaxValue;
            if (firstDistance == secondDistance) continue;
            bool owner = firstDistance < secondDistance;
            bool previous = IsFirst(cell);
            if (owner)
            {
                changed |= secondClaims.Remove(cell);
                changed |= firstClaims.Add(cell);
                if (!previous) firstGained++;
            }
            else
            {
                changed |= firstClaims.Remove(cell);
                changed |= secondClaims.Add(cell);
                if (previous) secondGained++;
            }
        }
        if (changed) RebuildOverrides();
        return (firstGained, secondGained);
    }

    public void PruneClaims(bool firstActive, bool secondActive)
    {
        bool changed = false;
        if (!firstActive && firstClaims.Count > 0) { firstClaims.Clear(); changed = true; }
        if (!secondActive && secondClaims.Count > 0) { secondClaims.Clear(); changed = true; }
        if (changed) RebuildOverrides();
    }

    private void AddCaptureCells(IntVec3? center)
    {
        if (!center.HasValue) return;
        if (center.Value.x < 0 || center.Value.z < 0 || center.Value.x >= width || center.Value.z >= height)
            throw new ArgumentOutOfRangeException(nameof(center));
        int count = GenRadial.NumCellsInRadius(2f);
        for (int index = 0; index < count; index++)
        {
            IntVec3 cell = center.Value + GenRadial.RadialPattern[index];
            if (cell.x >= 0 && cell.z >= 0 && cell.x < width && cell.z < height && !captureCells.Contains(cell)) captureCells.Add(cell);
        }
    }

    private void SetClaimOwner(IntVec3 cell, byte owner)
    {
        if (cell.x < 0 || cell.z < 0 || cell.x >= width || cell.z >= height)
            throw new InvalidOperationException("[CQF] Saved territory claim lies outside its battlefield.");
        int index = cell.z * width + cell.x;
        if (overrideOwners![index] == 0) overrideOwners[index] = owner;
    }

    private HashSet<IntVec3> firstClaims = new();
    private HashSet<IntVec3> secondClaims = new();
    private readonly List<IntVec3> captureCells = new(26);
}
