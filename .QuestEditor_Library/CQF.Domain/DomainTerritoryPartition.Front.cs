using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainTerritoryPartition
{
    public DomainTerritoryPartition(int width, int height, float firstFraction, bool reverse,
        IntVec3 firstCenter, IntVec3 secondCenter) : this(width, height, firstFraction, reverse)
    {
        curvedFront = true;
        this.firstCenter = firstCenter;
        this.secondCenter = secondCenter;
        boundary = SolveBoundary();
        RebuildOwners();
        changedCells.Clear();
        dirtyBounds.Clear();
    }

    public bool UsesCurvedFront => curvedFront;
    public float Encirclement => curvedFront ? Mathf.SmoothStep(0f, 1f,
        Mathf.InverseLerp(0.3f, 0.2f, Mathf.Min(firstFraction, 1f - firstFraction))) : 0f;
    public IntVec3 FirstCenter => firstCenter;
    public IntVec3 SecondCenter => secondCenter;

    public bool NearFront(int x, int z, float distance = 2f)
    {
        float a = BaseSignedDistance(x, z), b = BaseSignedDistance(x + 1f, z);
        float c = BaseSignedDistance(x, z + 1f), d = BaseSignedDistance(x + 1f, z + 1f);
        return Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d)) <= distance
            && Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d)) >= -distance;
    }

    private float CurvedDistance(float x, float z)
    {
        if (frontScores == null || x < 0f || z < 0f || x > width || z > height) return FrontScore(x, z) - frontLevel;
        int left = Math.Min(width - 1, Mathf.FloorToInt(x)), bottom = Math.Min(height - 1, Mathf.FloorToInt(z));
        float dx = x - left, dz = z - bottom;
        int index = bottom * (width + 1) + left;
        float a = frontScores[index], b = frontScores[index + width + 1];
        float c = frontScores[index + width + 2], d = frontScores[index + 1];
        return (dz >= dx ? a * (1f - dz) + b * (dz - dx) + c * dx
            : a * (1f - dx) + c * dz + d * (dx - dz)) - frontLevel;
    }

    private float FrontScore(float x, float z)
    {
        float curve = Mathf.Sin(z / height * Mathf.PI * 2f) * width * 0.015f;
        float plane = (reverse ? x : width - x) + curve;
        float blend = Encirclement;
        if (blend <= 0f) return plane;
        bool weakFirst = firstFraction < 0.5f;
        IntVec3 center = weakFirst ? firstCenter : secondCenter;
        float cx = Mathf.Clamp(center.x + 0.5f, 0.5f, width - 0.5f);
        float cz = Mathf.Clamp(center.z + 0.5f, 0.5f, height - 0.5f);
        float dx = (x - cx) / (x < cx ? cx : width - cx);
        float dz = (z - cz) / (z < cz ? cz : height - cz);
        float angle = Mathf.Atan2(dz, dx);
        float radial = Mathf.Sqrt(dx * dx + dz * dz) * Mathf.Min(width, height) * 0.5f
            * (1f + 0.07f * Mathf.Sin(angle * 3f + 0.7f) + 0.045f * Mathf.Sin(angle * 5f - 1.2f));
        return Mathf.Lerp(plane, weakFirst ? -radial : radial, blend);
    }

    private float SolveFront()
    {
        frontScores ??= new float[(width + 1) * (height + 1)];
        float low = float.MaxValue, high = float.MinValue;
        for (int z = 0; z <= height; z++)
        for (int x = 0; x <= width; x++)
        {
            float score = FrontScore(x, z);
            frontScores[z * (width + 1) + x] = score;
            low = Mathf.Min(low, score);
            high = Mathf.Max(high, score);
        }
        double desired = Math.Max(fixedFirstCells, Math.Min(CellCount - fixedSecondCells, firstFraction * (double)CellCount));
        if (desired <= fixedFirstCells) frontLevel = high + 1f;
        else if (desired >= CellCount - fixedSecondCells) frontLevel = low - 1f;
        else
        {
            for (int pass = 0; pass < 22; pass++)
            {
                float level = (low + high) * 0.5f;
                double area = fixedFirstCells;
                for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                {
                    if (OverrideAt(x, z) != 0) continue;
                    int index = z * (width + 1) + x;
                    float a = frontScores[index] - level, b = frontScores[index + width + 1] - level;
                    float c = frontScores[index + width + 2] - level, d = frontScores[index + 1] - level;
                    area += TriangleArea(a, b, c) + TriangleArea(a, c, d);
                }
                if (area > desired) low = level;
                else high = level;
            }
            frontLevel = (low + high) * 0.5f;
        }
        return width * (reverse ? 1f - firstFraction : firstFraction);
    }

    private static float TriangleArea(float a, float b, float c)
    {
        int positive = (a > 0f ? 1 : 0) + (b > 0f ? 1 : 0) + (c > 0f ? 1 : 0);
        if (positive == 0) return 0f;
        if (positive == 3) return 0.5f;
        if (positive == 1)
        {
            if (b > 0f) (a, b) = (b, a);
            else if (c > 0f) (a, c) = (c, a);
            return 0.5f * a / (a - b) * a / (a - c);
        }
        if (b <= 0f) (a, b) = (b, a);
        else if (c <= 0f) (a, c) = (c, a);
        return 0.5f - 0.5f * a / (a - b) * a / (a - c);
    }

    private bool curvedFront;
    private IntVec3 firstCenter;
    private IntVec3 secondCenter;
    private float frontLevel;
    private float[]? frontScores;
    private bool[]? frontCells;
}
