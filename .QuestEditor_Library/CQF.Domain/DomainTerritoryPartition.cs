using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainTerritoryPartition : IExposable
{
    public DomainTerritoryPartition() { }

    public DomainTerritoryPartition(int width, int height, float firstFraction, bool reverse)
    {
        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (float.IsNaN(firstFraction) || float.IsInfinity(firstFraction) || firstFraction < 0f || firstFraction > 1f)
            throw new ArgumentOutOfRangeException(nameof(firstFraction));
        this.width = width;
        this.height = height;
        this.reverse = reverse;
        this.firstFraction = firstFraction;
        boundary = width * (reverse ? 1f - firstFraction : firstFraction);
    }

    public int CellCount => width * height;
    public int FirstColumns => Math.Max(0, Math.Min(width,
        (int)Math.Ceiling((reverse ? width - (double)BoundaryX : BoundaryX) - 0.5d)));
    public int FirstCells => owners == null ? FirstColumns * height : firstCells;
    public int SecondCells => CellCount - FirstCells;
    public float FirstFraction => firstFraction;
    public float BoundaryX => boundary;
    public bool Reversed => reverse;
    public DomainInvasion? Invasion => invasion;
    public int Revision => revision;
    public IReadOnlyList<IntVec3> ChangedCells => changedCells;
    public IReadOnlyList<Rect> DirtyBounds => dirtyBounds;
    public IReadOnlyList<DomainTerritoryProtection> Protections => protections;
    public bool HasProtections => protections.Count > 0;
    public bool HasOverrides => fixedCells.Count > 0;
    public int Width => width;
    public int Height => height;

    public void EnableInvasions(int seed)
    {
        invasion = new DomainInvasion(seed);
        owners = new bool[CellCount];
        RebuildOwners();
        changedCells.Clear();
    }

    public float SignedDistance(float x, float z)
    {
        int owner = OverrideAt(Mathf.FloorToInt(x), Mathf.FloorToInt(z));
        return owner == 0 ? BaseSignedDistance(x, z) : owner == 1 ? 1f : -1f;
    }

    public float BaseSignedDistance(float x, float z)
    {
        float result = reverse ? x - boundary : boundary - x;
        if (invasion?.Strength > 0f)
        {
            float hole = invasion.SignedDistance(x, z);
            result = invasion.First ? Math.Max(result, hole) : Math.Min(result, -hole);
        }
        return result;
    }

    public bool IsBoundaryCell(IntVec3 cell)
        => Math.Abs(cell.x - (int)Math.Floor(boundary)) <= 1
            || HasOverrides && (OverrideAt(cell.x, cell.z) != 0 || OverrideAt(cell.x - 1, cell.z) != 0
                || OverrideAt(cell.x + 1, cell.z) != 0 || OverrideAt(cell.x, cell.z - 1) != 0 || OverrideAt(cell.x, cell.z + 1) != 0)
            || invasion?.IntersectsBoundary(new Rect(cell.x, cell.z, 1f, 1f)) == true;

    public int ProtectionAt(int x, int z) => protectedOwners != null && x >= 0 && z >= 0 && x < width && z < height
        ? protectedOwners[z * width + x] : 0;

    public int OverrideAt(int x, int z) => overrideOwners != null && x >= 0 && z >= 0 && x < width && z < height
        ? overrideOwners[z * width + x] : 0;

    public int Protect(bool first, IEnumerable<IntVec3> cells, int expiresTick, Thing? source = null)
    {
        List<IntVec3> selected = cells.Distinct().ToList();
        if (selected.Count == 0 || selected.Any(cell => cell.x < 0 || cell.z < 0 || cell.x >= width || cell.z >= height
            || IsFirst(cell) != first)) throw new ArgumentException("[CQF] Territory protection requires cells currently owned by its domain.", nameof(cells));
        DomainTerritoryProtection protection = new(++nextProtectionId, first, selected, expiresTick, source);
        protections.Add(protection);
        RebuildProtections();
        return protection.Id;
    }

    public bool RemoveProtection(int id)
    {
        int index = protections.FindIndex(item => item.Id == id);
        if (index < 0) return false;
        protections.RemoveAt(index);
        RebuildProtections();
        return true;
    }

    public void PruneProtections(int tick, bool firstActive, bool secondActive)
    {
        bool changed = false;
        for (int index = protections.Count - 1; index >= 0; index--)
            if (protections[index].Expired(tick) || !(protections[index].First ? firstActive : secondActive))
            { protections.RemoveAt(index); changed = true; }
        if (changed) RebuildProtections();
    }

    public static float CalculateFraction(float firstHealth, float secondHealth)
    {
        if (float.IsNaN(firstHealth) || float.IsNaN(secondHealth)
            || float.IsInfinity(firstHealth) || float.IsInfinity(secondHealth) || firstHealth < 0f || secondHealth < 0f)
            throw new ArgumentOutOfRangeException(nameof(firstHealth));
        double total = (double)firstHealth + secondHealth;
        return total == 0d ? 0f : (float)(firstHealth / total);
    }

    public bool IsFirst(IntVec3 cell)
    {
        if (cell.x < 0 || cell.z < 0 || cell.x >= width || cell.z >= height)
            throw new ArgumentOutOfRangeException(nameof(cell));
        return SignedDistance(cell.x + 0.5f, cell.z + 0.5f) > 0f;
    }

    public float Advance(float firstHealth, float secondHealth, float maximumDistance)
    {
        if (maximumDistance <= 0f || float.IsNaN(maximumDistance) || float.IsInfinity(maximumDistance))
            throw new ArgumentOutOfRangeException(nameof(maximumDistance));
        float old = BoundaryX;
        float previous = firstFraction;
        float target = CalculateFraction(firstHealth, secondHealth);
        float step = maximumDistance / width;
        firstFraction = Math.Abs(target - firstFraction) <= step ? target
            : firstFraction + Math.Sign(target - firstFraction) * step;
        changedCells.Clear();
        dirtyBounds.Clear();
        bool changed = previous != firstFraction || protectionDirty;
        protectionDirty = false;
        if (invasion != null)
        {
            AddInvasionBounds();
            changed |= invasion.Advance(firstFraction, step, width, height, reverse, firstHealth == 0f || secondHealth == 0f);
            AddInvasionBounds();
        }
        if (!changed) { dirtyBounds.Clear(); return old; }
        boundary = SolveBoundary();
        if (owners != null) RebuildOwners();
        foreach (IntVec3 cell in changedCells) dirtyBounds.Add(new Rect(cell.x, cell.z, 1f, 1f));
        revision++;
        return old;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref width, "width");
        Scribe_Values.Look(ref height, "height");
        Scribe_Values.Look(ref firstFraction, "firstFraction", -1f);
        Scribe_Values.Look(ref reverse, "reverse");
        Scribe_Deep.Look(ref invasion, "invasion");
        Scribe_Collections.Look(ref protections, "protections", LookMode.Deep);
        Scribe_Values.Look(ref nextProtectionId, "nextProtectionId");
        Scribe_Collections.Look(ref firstClaims, "firstClaims", LookMode.Value);
        Scribe_Collections.Look(ref secondClaims, "secondClaims", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit
            && (width <= 0 || height <= 0 || (long)width * height > int.MaxValue
                || float.IsNaN(firstFraction) || float.IsInfinity(firstFraction) || firstFraction < 0f || firstFraction > 1f))
            throw new InvalidOperationException("[CQF] Invalid saved domain territory partition.");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            invasion?.Rebuild(firstFraction, width, height, reverse);
            protections ??= new();
            firstClaims ??= new();
            secondClaims ??= new();
            if (firstClaims.Overlaps(secondClaims)) throw new InvalidOperationException("[CQF] Saved territory claims overlap.");
            RebuildProtections();
            boundary = SolveBoundary();
            if (invasion != null || HasOverrides) { owners = new bool[CellCount]; RebuildOwners(); changedCells.Clear(); }
            protectionDirty = false;
            revision++;
        }
    }

    private float SolveBoundary()
    {
        if (invasion?.Strength > 0f || HasOverrides)
        {
            double desired = Math.Max(fixedFirstCells, Math.Min(CellCount - fixedSecondCells, firstFraction * (double)CellCount));
            float low = 0f, high = width;
            for (int pass = 0; pass < 23; pass++)
            {
                float middle = (low + high) * 0.5f;
                double area = (reverse ? width - middle : middle) * (double)height + (invasion?.AreaOutside(middle, !reverse) ?? 0d);
                foreach (IntVec3 cell in fixedCells)
                {
                    float left = Mathf.Clamp(middle - cell.x, 0f, 1f);
                    double baseArea = (reverse ? 1f - left : left)
                        + (invasion?.AreaOutside(middle, !reverse, new Rect(cell.x, cell.z, 1f, 1f)) ?? 0d);
                    area += (OverrideAt(cell.x, cell.z) == 1 ? 1d : 0d) - baseArea;
                }
                if ((area < desired) != reverse) low = middle;
                else high = middle;
            }
            return (low + high) * 0.5f;
        }
        return width * (reverse ? 1f - firstFraction : firstFraction);
    }

    private void RebuildOwners()
    {
        firstCells = 0;
        for (int z = 0; z < height; z++)
        for (int x = 0; x < width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            bool owner = IsFirst(cell);
            int index = z * width + x;
            if (owner != owners![index]) { owners[index] = owner; changedCells.Add(cell); }
            if (owner) firstCells++;
        }
    }

    private void AddInvasionBounds()
    {
        for (int index = 0; index < invasion!.Shapes.Count; index++)
        {
            Vector4 shape = invasion.Shapes[index];
            if (shape.z > 0f && shape.w > 0f)
                dirtyBounds.Add(new Rect(shape.x - shape.z - 1f, shape.y - shape.w - 1f, shape.z * 2f + 2f, shape.w * 2f + 2f));
        }
    }

    private void RebuildProtections()
    {
        protectedOwners ??= new byte[CellCount];
        Array.Clear(protectedOwners, 0, protectedOwners.Length);
        foreach (DomainTerritoryProtection protection in protections)
        foreach (IntVec3 cell in protection.Cells)
        {
            if (cell.x < 0 || cell.z < 0 || cell.x >= width || cell.z >= height)
                throw new InvalidOperationException("[CQF] Domain protection lies outside its battlefield.");
            int index = cell.z * width + cell.x;
            byte owner = protection.First ? (byte)1 : (byte)2;
            if (protectedOwners[index] != 0 && protectedOwners[index] != owner)
                throw new InvalidOperationException("[CQF] Opposing domain protections overlap.");
            if (protectedOwners[index] == owner) continue;
            protectedOwners[index] = owner;
        }
        RebuildOverrides();
    }

    private void RebuildOverrides()
    {
        overrideOwners ??= new byte[CellCount];
        if (protectedOwners != null) Array.Copy(protectedOwners, overrideOwners, CellCount);
        else Array.Clear(overrideOwners, 0, overrideOwners.Length);
        foreach (IntVec3 cell in firstClaims) SetClaimOwner(cell, 1);
        foreach (IntVec3 cell in secondClaims) SetClaimOwner(cell, 2);
        fixedCells.Clear();
        fixedFirstCells = 0;
        fixedSecondCells = 0;
        for (int z = 0; z < height; z++)
        for (int x = 0; x < width; x++)
        {
            int owner = overrideOwners[z * width + x];
            if (owner == 0) continue;
            fixedCells.Add(new IntVec3(x, 0, z));
            if (owner == 1) fixedFirstCells++; else fixedSecondCells++;
        }
        if (owners == null) { owners = new bool[CellCount]; RebuildOwners(); changedCells.Clear(); }
        protectionDirty = true;
    }

    private int width;
    private int height;
    private float firstFraction;
    private bool reverse;
    private float boundary;
    private DomainInvasion? invasion;
    private bool[]? owners;
    private int firstCells;
    private int revision;
    private readonly List<IntVec3> changedCells = new();
    private readonly List<Rect> dirtyBounds = new(6);
    private List<DomainTerritoryProtection> protections = new();
    private readonly List<IntVec3> fixedCells = new();
    private byte[]? protectedOwners;
    private byte[]? overrideOwners;
    private int fixedFirstCells;
    private int fixedSecondCells;
    private int nextProtectionId;
    private bool protectionDirty;
}
