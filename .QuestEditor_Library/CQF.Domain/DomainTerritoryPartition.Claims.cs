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
                if (!firstClaims.Contains(cell)) claimExposure.Remove(cell);
                changed |= secondClaims.Remove(cell);
                changed |= firstClaims.Add(cell);
                if (!previous) firstGained++;
            }
            else
            {
                if (!secondClaims.Contains(cell)) claimExposure.Remove(cell);
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
        if (!firstActive && firstClaims.Count > 0)
        {
            foreach (IntVec3 cell in firstClaims) claimExposure.Remove(cell);
            firstClaims.Clear(); changed = true;
        }
        if (!secondActive && secondClaims.Count > 0)
        {
            foreach (IntVec3 cell in secondClaims) claimExposure.Remove(cell);
            secondClaims.Clear(); changed = true;
        }
        if (changed) RebuildOverrides();
    }

    public void ErodeClaims(int tick, bool eligible, int firstDuration, int secondDuration)
    {
        if (firstDuration <= 0 || secondDuration <= 0) throw new ArgumentOutOfRangeException(nameof(firstDuration));
        if (lastErosionTick == tick) return;
        lastErosionTick = tick;
        if (!eligible) return;
        bool changed = ErodeClaims(firstClaims, true, firstDuration);
        changed |= ErodeClaims(secondClaims, false, secondDuration);
        if (changed) RebuildOverrides();
    }

    private bool ErodeClaims(HashSet<IntVec3> claims, bool first, int duration)
    {
        captureCells.Clear();
        foreach (IntVec3 cell in claims)
        {
            if (ProtectionAt(cell.x, cell.z) != 0 || (BaseSignedDistance(cell.x + 0.5f, cell.z + 0.5f) > 0f) == first)
            {
                claimExposure.Remove(cell);
                continue;
            }
            int exposure = claimExposure.TryGetValue(cell, out int elapsed) ? elapsed + 1 : 1;
            if (exposure < duration) claimExposure[cell] = exposure;
            else captureCells.Add(cell);
        }
        foreach (IntVec3 cell in captureCells) { claims.Remove(cell); claimExposure.Remove(cell); }
        return captureCells.Count > 0;
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
    private Dictionary<IntVec3, int> claimExposure = new();
    private int lastErosionTick = -1;
    private readonly List<IntVec3> captureCells = new(26);
}
