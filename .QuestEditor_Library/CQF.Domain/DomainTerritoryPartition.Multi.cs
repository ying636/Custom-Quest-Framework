using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainTerritoryPartition
{
    public DomainTerritoryPartition(int width, int height, IReadOnlyList<IntVec3> centers, IReadOnlyList<float> health)
        : this(width, height, 0.5f, false)
    {
        multi = new DomainMultiTerritory(width, height, centers, health);
    }

    public bool IsMultilateral => multi != null;
    public int DomainCount => multi?.Count ?? 2;
    public int LastClaimErosionTick => multi?.LastErosionTick ?? lastErosionTick;
    public IReadOnlyList<IntVec3> OpeningCenters => multi?.Centers ?? new[] { firstCenter, secondCenter };
    public IEnumerable<KeyValuePair<IntVec3, int>> ClaimOwners => multi != null ? multi.Claims
        : firstClaims.Select(cell => new KeyValuePair<IntVec3, int>(cell, 0))
            .Concat(secondClaims.Select(cell => new KeyValuePair<IntVec3, int>(cell, 1)));

    public int OwnerIndex(IntVec3 cell) => multi?.OwnerAt(cell) ?? (IsFirst(cell) ? 0 : 1);
    public int CellsFor(int side) => multi?.CellsFor(side) ?? (side == 0 ? FirstCells : SecondCells);
    public float FractionFor(int side) => multi != null ? multi.Fractions[side] : side == 0 ? firstFraction : 1f - firstFraction;
    public float ScoreFor(int side, float x, float z) => multi?.Score(side, x, z) ?? BaseSignedDistance(x, z) * (side == 0 ? 0.5f : -0.5f);
    public float DistanceFor(int side, float x, float z) => multi?.Distance(side, x, z) ?? BaseSignedDistance(x, z) * (side == 0 ? 1f : -1f);
    public int ClaimExposureAt(IntVec3 cell) => multi?.ExposureAt(cell) ?? (claimExposure.TryGetValue(cell, out int age) ? age : 0);

    public int Protect(int side, IEnumerable<IntVec3> cells, int expiresTick, Thing? source = null)
        => multi != null ? multi.Protect(side, cells, expiresTick, source) : Protect(side == 0, cells, expiresTick, source);

    public bool RemoveProtection(int id, int side)
        => multi != null ? multi.RemoveProtection(id, side) : protections.Any(item => item.Id == id && item.OwnerIndex == side) && RemoveProtection(id);

    public void Prune(int tick, IReadOnlyList<bool> active)
    {
        if (multi != null) multi.Prune(tick, active);
        else { PruneProtections(tick, active[0], active[1]); PruneClaims(active[0], active[1]); }
    }

    public int[] Capture(IReadOnlyList<IntVec3?> centers)
    {
        if (multi != null) return multi.Capture(centers);
        (int first, int second) = Capture(centers[0], centers[1]);
        return new[] { first, second };
    }

    public void ErodeClaims(int tick, bool eligible, IReadOnlyList<int> durations)
    {
        if (multi != null) multi.Erode(tick, eligible, durations);
        else ErodeClaims(tick, eligible, durations[0], durations[1]);
    }

    public void Advance(IReadOnlyList<float> health, float step, bool force = false)
    {
        if (multi != null) multi.Advance(health, step, force);
        else Advance(health[0], health[1], step * width);
    }

    public void Inherit(DomainTerritoryPartition source, IReadOnlyList<int> ownerMap, IntVec3 offset)
    {
        if (multi == null) throw new InvalidOperationException("[CQF] Territory inheritance requires a multiple-domain partition.");
        multi.Import(source, ownerMap, offset);
    }

    private DomainMultiTerritory? multi;
}
