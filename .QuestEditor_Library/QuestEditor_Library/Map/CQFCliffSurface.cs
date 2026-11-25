#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCliffSurface
{
    public CQFCliffSurface(Map map, CellRect area, TerrainDef empty, CQFBackgroundCliff cliff)
    {
        this.Area = area;
        this.depth = Mathf.Clamp(cliff.depth, .12f, 32f);
        this.tint = cliff.color;
        this.stride = area.Width + 1;
        this.open = new bool[area.Width * area.Height];
        this.distance = new float[this.stride * (area.Height + 1)];
        Array.Fill(this.distance, this.depth + 2f);

        for (int z = 0; z < area.Height; z++)
        {
            for (int x = 0; x < area.Width; x++)
            {
                var cell = new IntVec3(area.minX + x, 0, area.minZ + z);
                if (cell.GetTerrain(map) != empty) continue;
                this.open[z * area.Width + x] = true;
                for (int side = 0; side < Directions.Length; side++)
                {
                    if ((cliff.edges & (CQFCliffEdges)(1 << side)) == 0) continue;
                    IntVec3 neighbor = cell + Directions[side];
                    if (!neighbor.InBounds(map) || neighbor.GetTerrain(map) == empty) continue;
                    var edge = EdgeCorners[side];
                    this.distance[(z + edge.AZ) * this.stride + x + edge.AX] = 0f;
                    this.distance[(z + edge.BZ) * this.stride + x + edge.BX] = 0f;
                    this.HasEdges = true;
                }
            }
        }

        if (this.HasEdges)
        {
            this.Sweep(1);
            this.Sweep(-1);
        }
    }

    public CellRect Area { get; }
    public bool HasEdges { get; }

    public bool Covers(int x, int z)
    {
        if (!this.OpenAt(x, z)) return false;
        int index = z * this.stride + x;
        float nearest = Mathf.Min(Mathf.Min(this.distance[index], this.distance[index + 1]),
            Mathf.Min(this.distance[index + this.stride], this.distance[index + this.stride + 1]));
        return nearest < this.depth;
    }

    public bool TryGetRimCorners(int x, int z, out Vector4 corners)
    {
        corners = default;
        if (this.OpenAt(x, z)) return false;
        corners = new Vector4(this.RimDistance(x, z), this.RimDistance(x + 1, z),
            this.RimDistance(x, z + 1), this.RimDistance(x + 1, z + 1));
        return corners.x + corners.y + corners.z + corners.w < 4f;
    }

    public Color Evaluate(int x, int z, float u, float v, out float distanceFromEdge)
    {
        int index = z * this.stride + x;
        float southWest = this.distance[index];
        float southEast = this.distance[index + 1];
        float northWest = this.distance[index + this.stride];
        float northEast = this.distance[index + this.stride + 1];
        distanceFromEdge = Mathf.Lerp(Mathf.Lerp(southWest, southEast, u), Mathf.Lerp(northWest, northEast, u), v);
        float fade = Mathf.InverseLerp(this.depth * .58f, this.depth, distanceFromEdge);
        Color color = this.tint;
        color.a *= 1f - Mathf.SmoothStep(0f, 1f, fade);
        return color;
    }

    public bool VisibleAt(Vector3 point)
    {
        int x = Mathf.FloorToInt(point.x) - this.Area.minX;
        int z = Mathf.FloorToInt(point.z) - this.Area.minZ;
        if (!this.Covers(x, z)) return false;
        return this.Evaluate(x, z, point.x - this.Area.minX - x, point.z - this.Area.minZ - z, out _).a > .10f;
    }

    private void Sweep(int step)
    {
        int firstX = step > 0 ? 0 : this.Area.Width;
        int firstZ = step > 0 ? 0 : this.Area.Height;
        for (int j = 0; j <= this.Area.Height; j++)
        {
            int z = firstZ + j * step;
            for (int i = 0; i <= this.Area.Width; i++)
            {
                int x = firstX + i * step;
                int index = z * this.stride + x;
                float value = this.distance[index];
                if (value == 0f) continue;
                int px = x - step;
                int pz = z - step;
                int horizontal = Mathf.Min(x, px);
                int vertical = Mathf.Min(z, pz);
                if (this.OpenAt(horizontal, z - 1) || this.OpenAt(horizontal, z))
                    value = Mathf.Min(value, this.distance[index - step] + 1f);
                if (this.OpenAt(x - 1, vertical) || this.OpenAt(x, vertical))
                    value = Mathf.Min(value, this.distance[index - step * this.stride] + 1f);
                if (this.OpenAt(horizontal, vertical))
                    value = Mathf.Min(value, this.distance[index - step * this.stride - step] + Diagonal);
                int next = x + step;
                if (this.OpenAt(Mathf.Min(x, next), vertical))
                    value = Mathf.Min(value, this.distance[index - step * this.stride + step] + Diagonal);
                this.distance[index] = value;
            }
        }
    }

    private bool OpenAt(int x, int z) => (uint)x < (uint)this.Area.Width && (uint)z < (uint)this.Area.Height
        && this.open[z * this.Area.Width + x];

    private float RimDistance(int x, int z) =>
        (uint)x <= (uint)this.Area.Width && (uint)z <= (uint)this.Area.Height &&
        this.distance[z * this.stride + x] == 0f ? 0f : 1f;

    private const float Diagonal = 1.41421356f;
    private readonly bool[] open;
    private readonly float[] distance;
    private readonly int stride;
    private readonly float depth;
    private readonly Color tint;
    private static readonly IntVec3[] Directions = { IntVec3.North, IntVec3.East, IntVec3.South, IntVec3.West };
    private static readonly (int AX, int AZ, int BX, int BZ)[] EdgeCorners = { (0, 1, 1, 1), (1, 0, 1, 1), (0, 0, 1, 0), (0, 0, 0, 1) };
}
#endif
