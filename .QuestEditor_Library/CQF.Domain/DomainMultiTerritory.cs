using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainMultiTerritory : IExposable
{
    public DomainMultiTerritory() { }

    public DomainMultiTerritory(int width, int height, IEnumerable<IntVec3> centers, IEnumerable<float> health)
    {
        this.width = width;
        this.height = height;
        this.centers = centers.ToList();
        List<float> values = health.ToList();
        ValidateHealth(values);
        double total = values.Sum(value => (double)value);
        fractions = values.Select(value => total > 0 ? (float)(value / total) : 0f).ToList();
        biases = fractions.Select(value => value > 0 ? (float)Math.Log(value) : -1000000f).ToList();
        BuildSamples();
        Solve(96);
        RefreshOwners();
        changedCells.Clear(); dirtyBounds.Clear();
    }

    public int Width => width;
    public int Height => height;
    public int Count => centers.Count;
    public int Revision => revision;
    public IReadOnlyList<IntVec3> Centers => centers;
    public IReadOnlyList<float> Fractions => fractions;
    public IReadOnlyList<IntVec3> ChangedCells => changedCells;
    public IReadOnlyList<Rect> DirtyBounds => dirtyBounds;
    public IReadOnlyList<DomainTerritoryProtection> Protections => protections;
    public IReadOnlyDictionary<IntVec3, int> Claims => claims;
    public bool HasOverrides => claims.Count > 0 || protections.Count > 0;
    public int LastErosionTick => lastErosionTick;

    public int OwnerAt(IntVec3 cell)
    {
        if (cell.x < 0 || cell.z < 0 || cell.x >= width || cell.z >= height) throw new ArgumentOutOfRangeException(nameof(cell));
        int index = cell.z * width + cell.x;
        return overrides![index] != 0 ? overrides[index] - 1 : baseOwners![index];
    }

    public int BaseOwner(float x, float z)
    {
        int owner = -1;
        float best = float.NegativeInfinity;
        for (int side = 0; side < Count; side++)
        {
            if (fractions[side] <= 0f) continue;
            float score = Score(side, x, z);
            if (score > best) { best = score; owner = side; }
        }
        return owner;
    }

    public float Score(int side, float x, float z)
    {
        int left = Math.Max(0, Math.Min(width - 1, Mathf.FloorToInt(x)));
        int bottom = Math.Max(0, Math.Min(height - 1, Mathf.FloorToInt(z)));
        float dx = Mathf.Clamp01(x - left), dz = Mathf.Clamp01(z - bottom);
        int index = bottom * (width + 1) + left;
        float[] values = samples![side];
        float a = values[index], b = values[index + width + 1], c = values[index + width + 2], d = values[index + 1];
        float score = dz >= dx ? a * (1f - dz) + b * (dz - dx) + c * dx
            : a * (1f - dx) + c * dz + d * (dx - dz);
        float[] distances = distanceSamples![side];
        float distance = dz >= dx ? distances[index] * (1f - dz) + distances[index + width + 1] * (dz - dx) + distances[index + width + 2] * dx
            : distances[index] * (1f - dx) + distances[index + width + 2] * dz + distances[index + 1] * (dx - dz);
        return score - Confinement(side) * distance + biases[side];
    }

    public float Distance(int side, float x, float z)
    {
        if (fractions[side] <= 0f) return -8f;
        int rival = -1;
        float best = float.NegativeInfinity;
        for (int index = 0; index < Count; index++)
        {
            if (index == side || fractions[index] <= 0f) continue;
            float score = Score(index, x, z);
            if (score > best) { best = score; rival = index; }
        }
        if (rival < 0) return 8f;
        float value = Score(side, x, z) - best;
        float gx = Score(side, x + 0.25f, z) - Score(rival, x + 0.25f, z)
            - Score(side, x - 0.25f, z) + Score(rival, x - 0.25f, z);
        float gz = Score(side, x, z + 0.25f) - Score(rival, x, z + 0.25f)
            - Score(side, x, z - 0.25f) + Score(rival, x, z - 0.25f);
        return Mathf.Clamp(value / Mathf.Max(0.002f, Mathf.Sqrt(gx * gx + gz * gz) * 2f), -8f, 8f);
    }

    public int OverrideAt(int x, int z) => x < 0 || z < 0 || x >= width || z >= height ? 0 : overrides![z * width + x];
    public int ProtectionAt(int x, int z) => x < 0 || z < 0 || x >= width || z >= height ? 0 : protectedOwners![z * width + x];
    public int ExposureAt(IntVec3 cell) => exposure.TryGetValue(cell, out int age) ? age : 0;
    public int CellsFor(int side) => owners!.Count(owner => owner == side);

    public bool NearBoundary(int x, int z, float distance)
    {
        int radius = Math.Max(0, Mathf.CeilToInt(distance));
        for (int dz = Math.Max(0, z - radius); dz <= Math.Min(height - 1, z + radius); dz++)
        for (int dx = Math.Max(0, x - radius); dx <= Math.Min(width - 1, x + radius); dx++)
            if (oldBoundary![dz * width + dx]) return true;
        return false;
    }

    public bool IsBoundary(IntVec3 cell)
    {
        int owner = BaseOwner(cell.x, cell.z);
        if (BaseOwner(cell.x + 1, cell.z) != owner || BaseOwner(cell.x, cell.z + 1) != owner
            || BaseOwner(cell.x + 1, cell.z + 1) != owner) return true;
        int fixedOwner = OverrideAt(cell.x, cell.z);
        return fixedOwner != 0 || ProtectionAt(cell.x - 1, cell.z) != 0 || ProtectionAt(cell.x + 1, cell.z) != 0
            || ProtectionAt(cell.x, cell.z - 1) != 0 || ProtectionAt(cell.x, cell.z + 1) != 0;
    }

    public void Advance(IReadOnlyList<float> health, float step, bool force = false)
    {
        ValidateHealth(health);
        if (step <= 0 || float.IsNaN(step) || float.IsInfinity(step)) throw new ArgumentOutOfRangeException(nameof(step));
        double total = health.Sum(value => (double)value);
        bool changed = dirty;
        force |= fixedDirty;
        for (int side = 0; side < Count; side++)
        {
            float target = total > 0 ? (float)(health[side] / total) : 0f;
            float previous = fractions[side];
            fractions[side] = health[side] == 0f ? 0f : Mathf.MoveTowards(previous, target, step);
            changed |= Math.Abs(previous - fractions[side]) > 0.000001f;
            if ((previous == 0f) != (fractions[side] == 0f)) force = true;
        }
        changedCells.Clear(); dirtyBounds.Clear();
        if (!changed && !force) return;
        dirty = true;
        if (!force && ++pendingTicks < 6) return;
        pendingTicks = 0;
        double fractionSum = fractions.Sum(value => (double)value);
        if (fractionSum > 0)
            for (int side = 0; side < Count; side++) fractions[side] = (float)(fractions[side] / fractionSum);
        Solve(force ? 96 : 16);
        RefreshOwners();
        dirty = false;
        fixedDirty = false;
        revision++;
    }

    public int Protect(int side, IEnumerable<IntVec3> cells, int expiresTick, Thing? source)
    {
        List<IntVec3> selected = cells.Distinct().ToList();
        if (side < 0 || side >= Count || selected.Count == 0 || selected.Any(cell => OwnerAt(cell) != side))
            throw new ArgumentException("[CQF] Protection requires cells owned by its domain.");
        DomainTerritoryProtection protection = new(++nextProtectionId, side, selected, expiresTick, source);
        protections.Add(protection); RebuildOverrides();
        return protection.Id;
    }

    public bool RemoveProtection(int id, int side)
    {
        int index = protections.FindIndex(item => item.Id == id && item.OwnerIndex == side);
        if (index < 0) return false;
        protections.RemoveAt(index); RebuildOverrides();
        return true;
    }

    public void Prune(int tick, IReadOnlyList<bool> active)
    {
        bool changed = protections.RemoveAll(item => item.Expired(tick) || !active[item.OwnerIndex]) > 0;
        foreach (IntVec3 cell in claims.Where(item => !active[item.Value]).Select(item => item.Key).ToList())
        { claims.Remove(cell); exposure.Remove(cell); changed = true; }
        if (changed) RebuildOverrides();
    }

    public int[] Capture(IReadOnlyList<IntVec3?> positions)
    {
        HashSet<IntVec3> cells = new();
        int[] gained = new int[Count];
        foreach (IntVec3? position in positions)
            if (position.HasValue)
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(position.Value, 2f, true))
                    if (cell.x >= 0 && cell.z >= 0 && cell.x < width && cell.z < height) cells.Add(cell);
        bool changed = false;
        foreach (IntVec3 cell in cells)
        {
            if (ProtectionAt(cell.x, cell.z) != 0) continue;
            int owner = -1, closest = int.MaxValue;
            bool tied = false;
            for (int side = 0; side < Count; side++)
            {
                if (!positions[side].HasValue) continue;
                int distance = (positions[side]!.Value - cell).LengthHorizontalSquared;
                if (distance < closest) { owner = side; closest = distance; tied = false; }
                else if (distance == closest) tied = true;
            }
            if (owner < 0 || tied || closest > 4) continue;
            if (OwnerAt(cell) != owner) gained[owner]++;
            if (!claims.TryGetValue(cell, out int previous) || previous != owner)
            { claims[cell] = owner; exposure.Remove(cell); changed = true; }
        }
        if (changed) RebuildOverrides();
        return gained;
    }

    public void Erode(int tick, bool eligible, IReadOnlyList<int> durations)
    {
        if (lastErosionTick == tick) return;
        lastErosionTick = tick;
        if (!eligible) return;
        bool changed = false;
        foreach (var claim in claims.ToList())
        {
            IntVec3 cell = claim.Key;
            if (ProtectionAt(cell.x, cell.z) != 0 || BaseOwner(cell.x + 0.5f, cell.z + 0.5f) == claim.Value)
            { exposure.Remove(cell); continue; }
            int elapsed = exposure.TryGetValue(cell, out int previous) ? previous + 1 : 1;
            if (elapsed < durations[claim.Value]) exposure[cell] = elapsed;
            else { claims.Remove(cell); exposure.Remove(cell); changed = true; }
        }
        if (changed) RebuildOverrides();
    }

    public void Import(DomainTerritoryPartition source, IReadOnlyList<int> owners, IntVec3 offset)
    {
        lastErosionTick = Math.Max(lastErosionTick, source.LastClaimErosionTick);
        foreach (DomainTerritoryProtection protection in source.Protections)
        {
            int owner = owners[protection.OwnerIndex];
            protections.Add(new DomainTerritoryProtection(protection.Id, owner, protection.Cells.Select(cell => cell + offset), protection.ExpiresTick, protection.Source));
            nextProtectionId = Math.Max(nextProtectionId, protection.Id);
        }
        foreach (var claim in source.ClaimOwners)
        {
            IntVec3 cell = claim.Key + offset;
            claims[cell] = owners[claim.Value];
            int age = source.ClaimExposureAt(claim.Key);
            if (age > 0) exposure[cell] = age;
        }
        RebuildOverrides();
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref width, "width");
        Scribe_Values.Look(ref height, "height");
        Scribe_Collections.Look(ref centers, "centers", LookMode.Value);
        Scribe_Collections.Look(ref fractions, "fractions", LookMode.Value);
        Scribe_Collections.Look(ref biases, "biases", LookMode.Value);
        Scribe_Collections.Look(ref protections, "protections", LookMode.Deep);
        Scribe_Collections.Look(ref claims, "claims", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref exposure, "exposure", LookMode.Value, LookMode.Value);
        Scribe_Values.Look(ref nextProtectionId, "nextProtectionId");
        Scribe_Values.Look(ref lastErosionTick, "lastErosionTick", -1);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        if (fractions == null || biases == null || fractions.Count != Count || biases.Count != Count
            || fractions.Any(value => value < 0 || float.IsNaN(value) || float.IsInfinity(value))
            || biases.Any(value => float.IsNaN(value) || float.IsInfinity(value)))
            throw new InvalidOperationException("[CQF] Invalid saved multiple-domain territory weights.");
        protections ??= new(); claims ??= new(); exposure ??= new();
        if (protections.Any(item => item.OwnerIndex < 0 || item.OwnerIndex >= Count)
            || claims.Any(item => item.Value < 0 || item.Value >= Count || !InBounds(item.Key))
            || exposure.Any(item => item.Value <= 0 || !claims.ContainsKey(item.Key)))
            throw new InvalidOperationException("[CQF] Invalid saved multiple-domain fixed territory.");
        BuildSamples(); RebuildOverrides(); RefreshOwners();
        changedCells.Clear(); dirtyBounds.Clear(); dirty = false; fixedDirty = false;
    }

    private void ValidateHealth(IReadOnlyList<float> values)
    {
        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue || centers == null
            || Count < 2 || Count > MapComponent_Domain.MaximumDomains || values.Count != Count
            || values.Any(value => value < 0 || float.IsNaN(value) || float.IsInfinity(value)))
            throw new ArgumentException("[CQF] Invalid multiple-domain territory dimensions or health.");
    }

    private bool InBounds(IntVec3 cell) => cell.x >= 0 && cell.z >= 0 && cell.x < width && cell.z < height;

    private void BuildSamples()
    {
        ValidateHealth(fractions);
        samples = new float[Count][];
        centerSamples = new float[Count][];
        distanceSamples = new float[Count][];
        centerDistances = new float[Count][];
        owners = new int[width * height];
        baseOwners = new int[owners.Length];
        oldBoundary = new bool[owners.Length];
        protectedOwners = new int[owners.Length]; overrides = new int[owners.Length];
        for (int side = 0; side < Count; side++)
        {
            samples[side] = new float[(width + 1) * (height + 1)];
            centerSamples[side] = new float[owners.Length];
            distanceSamples[side] = new float[samples[side].Length];
            centerDistances[side] = new float[owners.Length];
            for (int z = 0; z <= height; z++)
            for (int x = 0; x <= width; x++)
            {
                double dx = (x - centers[side].x - 0.5d) / width;
                double dz = (z - centers[side].z - 0.5d) / height;
                double angle = Math.Atan2(dz, dx);
                double warp = 1d + 0.065d * Math.Sin(angle * 3d + side * 1.7d) + 0.04d * Math.Sin(angle * 5d - side * 0.9d);
                samples[side][z * (width + 1) + x] = (float)-Math.Log((dx * dx + dz * dz) * warp * warp + 0.00002d);
                distanceSamples[side][z * (width + 1) + x] = (float)(dx * dx + dz * dz);
            }
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                int corner = z * (width + 1) + x;
                centerSamples[side][z * width + x] = (samples[side][corner] + samples[side][corner + width + 2]) * 0.5f;
                centerDistances[side][z * width + x] = (distanceSamples[side][corner] + distanceSamples[side][corner + width + 2]) * 0.5f;
            }
        }
    }

    private void Solve(int passes)
    {
        double[] desired = fractions.Select(value => (double)value * width * height).ToArray();
        int[] fixedCounts = new int[Count];
        foreach (int owner in overrides!) if (owner > 0) fixedCounts[owner - 1]++;
        int free = width * height - fixedCounts.Sum();
        for (int side = 0; side < Count; side++)
        {
            desired[side] = Math.Max(0d, desired[side] - fixedCounts[side]);
            if (fractions[side] <= 0f) biases[side] = -1000000f;
            else if (biases[side] < -100f) biases[side] = (float)Math.Log(Math.Max(0.000001f, fractions[side]));
        }
        double total = desired.Sum();
        if (total > 0) for (int side = 0; side < Count; side++) desired[side] *= free / total;
        if (free == 0) return;
        float[] thresholds = new float[free];
        float[] confinement = Enumerable.Range(0, Count).Select(Confinement).ToArray();
        int[] counts = new int[Count];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int side = 0; side < Count; side++)
            {
                if (fractions[side] <= 0f) continue;
                int length = 0;
                for (int cell = 0; cell < width * height; cell++)
                {
                    if (overrides[cell] != 0) continue;
                    float best = float.NegativeInfinity;
                    for (int rival = 0; rival < Count; rival++)
                        if (rival != side && fractions[rival] > 0f)
                            best = Math.Max(best, centerSamples![rival][cell] - confinement[rival] * centerDistances![rival][cell] + biases[rival]);
                    thresholds[length++] = best - centerSamples![side][cell] + confinement[side] * centerDistances![side][cell];
                }
                int quota = Math.Max(0, Math.Min(free, (int)Math.Round(desired[side])));
                float candidate = quota == 0 ? thresholds.Min() - 0.00001f
                    : quota == free ? thresholds.Max() + 0.00001f
                    : SelectThreshold(thresholds, quota - 1) + 0.000001f;
                if (!float.IsNegativeInfinity(candidate)) biases[side] += (candidate - biases[side]) * 0.8f;
            }
            Array.Clear(counts, 0, counts.Length);
            for (int cell = 0; cell < width * height; cell++)
            {
                if (overrides[cell] != 0) continue;
                int owner = -1; float best = float.NegativeInfinity;
                for (int side = 0; side < Count; side++)
                    if (fractions[side] > 0f && centerSamples![side][cell] - confinement[side] * centerDistances![side][cell] + biases[side] > best)
                    { best = centerSamples[side][cell] - confinement[side] * centerDistances![side][cell] + biases[side]; owner = side; }
                if (owner >= 0) counts[owner]++;
            }
            double error = Enumerable.Range(0, Count).Max(side => Math.Abs(desired[side] - counts[side]));
            if (error <= Math.Max(2d, width * height * 0.001d)) break;
            float origin = biases.Where((_, side) => fractions[side] > 0f).DefaultIfEmpty(0f).Max();
            for (int side = 0; side < Count; side++) if (fractions[side] > 0f) biases[side] -= origin;
        }
    }

    private float Confinement(int side) => Mathf.Clamp01((0.3f - fractions[side]) / 0.3f) * 16f;

    private static float SelectThreshold(float[] values, int target)
    {
        int left = 0, right = values.Length - 1;
        while (left < right)
        {
            float pivot = values[left + (right - left) / 2];
            int low = left, high = right;
            while (low <= high)
            {
                while (values[low] < pivot) low++;
                while (values[high] > pivot) high--;
                if (low > high) break;
                (values[low], values[high]) = (values[high], values[low]);
                low++; high--;
            }
            if (target <= high) right = high;
            else if (target >= low) left = low;
            else break;
        }
        return values[target];
    }
    private void RefreshOwners()
    {
        for (int z = 0; z < height; z++)
        for (int x = 0; x < width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            int index = z * width + x;
            baseOwners![index] = BaseOwner(x + 0.5f, z + 0.5f);
            int owner = overrides![index] > 0 ? overrides[index] - 1 : baseOwners[index];
            if (owners![index] != owner) { owners[index] = owner; changedCells.Add(cell); }
            bool boundary = IsBoundary(cell);
            if (boundary || oldBoundary![index]) dirtyBounds.Add(new Rect(x, z, 1f, 1f));
            oldBoundary![index] = boundary;
        }
    }

    private void RebuildOverrides()
    {
        Array.Clear(protectedOwners!, 0, protectedOwners!.Length);
        foreach (DomainTerritoryProtection protection in protections)
        foreach (IntVec3 cell in protection.Cells)
        {
            if (!InBounds(cell)) throw new InvalidOperationException("[CQF] Protection lies outside its battlefield.");
            int index = cell.z * width + cell.x;
            int owner = protection.OwnerIndex + 1;
            if (protectedOwners[index] != 0 && protectedOwners[index] != owner)
                throw new InvalidOperationException("[CQF] Opposing territory protections overlap.");
            protectedOwners[index] = owner;
        }
        Array.Copy(protectedOwners, overrides!, overrides!.Length);
        foreach (var claim in claims)
        {
            int index = claim.Key.z * width + claim.Key.x;
            if (overrides[index] == 0) overrides[index] = claim.Value + 1;
        }
        dirty = true;
        fixedDirty = true;
    }

    private int width;
    private int height;
    private List<IntVec3> centers = new();
    private List<float> fractions = new();
    private List<float> biases = new();
    private List<DomainTerritoryProtection> protections = new();
    private Dictionary<IntVec3, int> claims = new();
    private Dictionary<IntVec3, int> exposure = new();
    private int nextProtectionId;
    private int lastErosionTick = -1;
    private int pendingTicks;
    private int revision;
    private bool dirty;
    private bool fixedDirty;
    private float[][]? samples;
    private float[][]? centerSamples;
    private float[][]? distanceSamples;
    private float[][]? centerDistances;
    private int[]? owners;
    private int[]? baseOwners;
    private bool[]? oldBoundary;
    private int[]? protectedOwners;
    private int[]? overrides;
    private readonly List<IntVec3> changedCells = new();
    private readonly List<Rect> dirtyBounds = new();
}
