using UnityEngine;

namespace QuestEditor_Library;

public sealed partial class DomainMultiTerritory
{
    private void BuildSamples()
    {
        ValidateHealth(fractions);
        Vector2[] cores = BuildCores();
        RefreshConfinement();
        samples = new float[Count][];
        centerSamples = new float[Count][];
        distanceSamples = new float[Count][];
        centerDistances = new float[Count][];
        owners = new int[width * height];
        baseOwners = new int[owners.Length];
        cornerOwners = null;
        rivalOwners = null;
        ownerScores = null;
        rivalScores = null;
        oldBoundary = new bool[owners.Length];
        protectedOwners = new int[owners.Length];
        overrides = new int[owners.Length];
        for (int side = 0; side < Count; side++)
        {
            samples[side] = new float[(width + 1) * (height + 1)];
            centerSamples[side] = new float[owners.Length];
            distanceSamples[side] = new float[samples[side].Length];
            centerDistances[side] = new float[owners.Length];
            Vector2 core = Warp(cores[side].x, cores[side].y);
            for (int z = 0; z <= height; z++)
            for (int x = 0; x <= width; x++)
            {
                Vector2 point = Warp(x, z);
                float scale = Math.Min(width, height);
                float dx = (point.x - core.x) / scale;
                float dz = (point.y - core.y) / scale;
                float distance = dx * dx + dz * dz;
                int corner = z * (width + 1) + x;
                samples[side][corner] = -distance;
                distanceSamples[side][corner] = distance;
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

    private Vector2[] BuildCores()
    {
        int rows = 1;
        double best = double.PositiveInfinity;
        for (int candidate = 1; candidate <= Count; candidate++)
        {
            double cost = 0d;
            for (int row = 0; row < candidate; row++)
            {
                int columns = Count / candidate + (row < Count % candidate ? 1 : 0);
                double aspect = width * (double)candidate / (height * columns);
                cost += columns * Math.Pow(Math.Log(aspect), 2d);
            }
            if (cost >= best) continue;
            best = cost; rows = candidate;
        }
        List<Vector2> slots = new();
        for (int row = 0; row < rows; row++)
        {
            int columns = Count / rows + (row < Count % rows ? 1 : 0);
            for (int column = 0; column < columns; column++)
                slots.Add(new Vector2(width * (column + 0.5f) / columns, height * (row + 0.5f) / rows));
        }
        int states = 1 << Count;
        double[] costs = Enumerable.Repeat(double.PositiveInfinity, states).ToArray();
        int[] choices = new int[states];
        int[] counts = new int[states];
        costs[0] = 0d;
        for (int mask = 1; mask < states; mask++) counts[mask] = counts[mask >> 1] + (mask & 1);
        for (int mask = 0; mask < states - 1; mask++)
        {
            int side = counts[mask];
            for (int slot = 0; slot < Count; slot++)
            {
                if ((mask & (1 << slot)) != 0) continue;
                double dx = (slots[slot].x - centers[side].x - 0.5d) / width;
                double dz = (slots[slot].y - centers[side].z - 0.5d) / height;
                double cost = costs[mask] + dx * dx + dz * dz;
                int next = mask | (1 << slot);
                if (cost >= costs[next]) continue;
                costs[next] = cost; choices[next] = slot;
            }
        }
        Vector2[] cores = new Vector2[Count];
        int pending = states - 1;
        for (int side = Count - 1; side >= 0; side--)
        {
            int slot = choices[pending];
            cores[side] = slots[slot]; pending ^= 1 << slot;
        }
        return cores;
    }

    private Vector2 Warp(float x, float z)
    {
        double nx = x / width, nz = z / height;
        return new Vector2(x + width * 0.025f * (float)(Math.Sin(Math.PI * nx) * Math.Sin(Math.PI * 2d * nz)),
            z + height * 0.025f * (float)(Math.Sin(Math.PI * nz) * Math.Sin(Math.PI * 2d * nx)));
    }

    private void RefreshConfinement()
    {
        confinement ??= new float[Count];
        float strongest = fractions.Max();
        for (int side = 0; side < Count; side++)
            confinement[side] = strongest > 0f && fractions[side] > 0f
                ? Mathf.Min(16f, (Mathf.Sqrt(strongest / fractions[side]) - 1f) * 3f) * confinementScale : 0f;
    }

    private bool MainTerritoriesConnected()
    {
        int length = width * height;
        topologyOwners ??= new int[length];
        topologyVisited ??= new bool[length];
        topologyQueue ??= new int[length];
        Array.Clear(topologyVisited, 0, length);
        bool[] seen = new bool[Count];
        topologyDefects = 0; topologySplitOwner = -1;
        for (int index = 0; index < length; index++)
        {
            int owner = -1; float best = float.NegativeInfinity;
            for (int side = 0; side < Count; side++)
            {
                if (fractions[side] <= 0f) continue;
                float score = centerSamples![side][index] - confinement![side] * centerDistances![side][index] + biases[side];
                if (score > best) { best = score; owner = side; }
            }
            topologyOwners[index] = owner;
        }
        for (int start = 0; start < length; start++)
        {
            if (topologyVisited[start] || topologyOwners[start] < 0) continue;
            int side = topologyOwners[start];
            if (seen[side])
            {
                topologyDefects++; topologySplitOwner = side; topologyError = "split side=" + side;
            }
            seen[side] = true;
            int head = 0, tail = 0;
            int minX = width, maxX = 0, minZ = height, maxZ = 0;
            topologyVisited[start] = true; topologyQueue[tail++] = start;
            while (head < tail)
            {
                int cell = topologyQueue[head++];
                minX = Math.Min(minX, cell % width); maxX = Math.Max(maxX, cell % width);
                minZ = Math.Min(minZ, cell / width); maxZ = Math.Max(maxZ, cell / width);
                for (int direction = 0; direction < 4; direction++)
                {
                    int neighbor = direction == 0 ? cell - 1 : direction == 1 ? cell + 1 : direction == 2 ? cell - width : cell + width;
                    if (neighbor < 0 || neighbor >= length || direction == 0 && cell % width == 0 || direction == 1 && cell % width == width - 1
                        || topologyVisited[neighbor] || topologyOwners[neighbor] != side) continue;
                    topologyVisited[neighbor] = true; topologyQueue[tail++] = neighbor;
                }
            }
            int blockWidth = maxX - minX + 1, blockHeight = maxZ - minZ + 1;
            if (tail >= 32 && (tail < blockWidth * blockHeight * 0.3d || Math.Max(blockWidth, blockHeight) > Math.Min(blockWidth, blockHeight) * 5))
            {
                topologyDefects++; topologySplitOwner = side; topologyError = "narrow side=" + side;
            }
        }
        for (int side = 0; side < Count; side++)
            if (fractions[side] > 0f && !seen[side]) { topologyDefects++; topologySplitOwner = side; topologyError = "missing side=" + side; }
        return topologyDefects == 0;
    }

    private bool PolishConnectivity()
    {
        for (int pass = 0; pass < Count; pass++)
        {
            if (MainTerritoriesConnected()) return QuotasAccurate();
            int side = topologySplitOwner, previousDefects = topologyDefects;
            float original = biases[side], best = original;
            int bestDefects = previousDefects;
            float unit = 1f / (Math.Min(width, height) * (float)Math.Min(width, height));
            for (int step = 0; step < 6; step++)
            for (int sign = -1; sign <= 1; sign += 2)
            {
                biases[side] = original + unit * (1 << step) * sign;
                bool connected = MainTerritoriesConnected();
                if (connected && QuotasAccurate()) return true;
                if (topologyDefects < bestDefects && QuotasAccurate()) { bestDefects = topologyDefects; best = biases[side]; }
            }
            biases[side] = best;
            if (bestDefects >= previousDefects) return false;
        }
        return MainTerritoriesConnected() && QuotasAccurate();
    }

    private bool QuotasAccurate()
    {
        int[] counts = new int[Count];
        for (int cell = 0; cell < width * height; cell++)
        {
            int owner = overrides![cell] > 0 ? overrides[cell] - 1 : topologyOwners![cell];
            if (owner >= 0) counts[owner]++;
        }
        for (int side = 0; side < Count; side++)
            if (Math.Abs(counts[side] - quotaTargets![side]) > Math.Max(3d, width * height * 0.005d)) return false;
        return true;
    }

    private float[]? confinement;
    private float confinementScale = 1f;
    private int[]? topologyOwners;
    private bool[]? topologyVisited;
    private int[]? topologyQueue;
    private string? topologyError;
    private int topologyDefects;
    private int topologySplitOwner;
    private double[]? quotaTargets;
}
