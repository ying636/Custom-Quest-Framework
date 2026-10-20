using UnityEngine;

namespace QuestEditor_Library;

public sealed class DomainTerritoryGeometry
{
    public DomainTerritoryGeometry(DomainTerritoryPartition partition) { Partition = partition; }

    public DomainTerritoryPartition Partition { get; }

    public void Append(Rect rect, bool first, float altitude, List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        if (!Partition.HasOverrides) { AppendCell(rect, first, altitude, vertices, triangles, colors); return; }
        for (int z = Mathf.FloorToInt(rect.yMin); z < Mathf.CeilToInt(rect.yMax); z++)
        for (int x = Mathf.FloorToInt(rect.xMin); x < Mathf.CeilToInt(rect.xMax); x++)
        {
            Rect tile = Rect.MinMaxRect(Math.Max(x, rect.xMin), Math.Max(z, rect.yMin), Math.Min(x + 1, rect.xMax), Math.Min(z + 1, rect.yMax));
            AppendCell(tile, first, altitude, vertices, triangles, colors);
        }
    }

    public void Rebuild(Mesh mesh, bool first, int width, int height, float altitude)
    {
        vertices.Clear(); triangles.Clear(); colors.Clear();
        xs.Clear(); zs.Clear();
        xs.Add(0f); xs.Add(width); xs.Add(Partition.BoundaryX);
        zs.Add(0f); zs.Add(height);
        if (Partition.Invasion?.Strength > 0f)
            foreach (Vector4 shape in Partition.Invasion.Shapes)
            {
                xs.Add(Mathf.Clamp(shape.x - shape.z, 0f, width)); xs.Add(Mathf.Clamp(shape.x + shape.z, 0f, width));
                zs.Add(Mathf.Clamp(shape.y - shape.w, 0f, height)); zs.Add(Mathf.Clamp(shape.y + shape.w, 0f, height));
            }
        xs.Sort(); zs.Sort();
        for (int z = 1; z < zs.Count; z++)
        for (int x = 1; x < xs.Count; x++)
            if (xs[x] > xs[x - 1] && zs[z] > zs[z - 1])
                Append(new Rect(xs[x - 1], zs[z - 1], xs[x] - xs[x - 1], zs[z] - zs[z - 1]), first, altitude, vertices, triangles, colors);
        FinalizeMesh(mesh);
    }

    internal void RebuildUnclaimed(Mesh mesh, Rect bounds, float altitude)
    {
        vertices.Clear(); triangles.Clear(); colors.Clear();
        for (int z = Math.Max(0, Mathf.FloorToInt(bounds.yMin)); z < Math.Min(Partition.Height, Mathf.CeilToInt(bounds.yMax)); z++)
        for (int x = Math.Max(0, Mathf.FloorToInt(bounds.xMin)); x < Math.Min(Partition.Width, Mathf.CeilToInt(bounds.xMax)); x++)
        {
            if (Partition.OverrideAt(x, z) != 0) continue;
            float left = Math.Max(x, bounds.xMin), right = Math.Min(x + 1, bounds.xMax);
            float bottom = Math.Max(z, bounds.yMin), top = Math.Min(z + 1, bounds.yMax);
            clipped[0] = new Vector2(left, bottom); clipped[1] = new Vector2(left, top);
            clipped[2] = new Vector2(right, top); clipped[3] = new Vector2(right, bottom);
            AppendPolygon(4, altitude, vertices, triangles, colors);
        }
        FinalizeMesh(mesh);
    }

    private void FinalizeMesh(Mesh mesh)
    {
        mesh.Clear();
        if (vertices.Count == 0) return;
        UnityEngine.Rendering.IndexFormat format = vertices.Count > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        if (mesh.indexFormat != format) mesh.indexFormat = format;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
    }

    private void AppendCell(Rect rect, bool first, float altitude, List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        int owner = Partition.OverrideAt(Mathf.FloorToInt(rect.center.x), Mathf.FloorToInt(rect.center.y));
        if (owner != 0)
        {
            if ((owner == 1) == first)
            {
                clipped[0] = new Vector2(rect.xMin, rect.yMin); clipped[1] = new Vector2(rect.xMin, rect.yMax);
                clipped[2] = new Vector2(rect.xMax, rect.yMax); clipped[3] = new Vector2(rect.xMax, rect.yMin);
                AppendPolygon(4, altitude, vertices, triangles, colors);
            }
            return;
        }
        bool curved = Partition.Invasion?.IntersectsBoundary(rect) == true;
        if (!curved)
        {
            bool a = Partition.BaseSignedDistance(rect.xMin, rect.yMin) > 0f;
            bool b = Partition.BaseSignedDistance(rect.xMin, rect.yMax) > 0f;
            bool c = Partition.BaseSignedDistance(rect.xMax, rect.yMax) > 0f;
            bool d = Partition.BaseSignedDistance(rect.xMax, rect.yMin) > 0f;
            if (a == b && b == c && c == d)
            {
                if (a == first)
                {
                    clipped[0] = new Vector2(rect.xMin, rect.yMin);
                    clipped[1] = new Vector2(rect.xMin, rect.yMax);
                    clipped[2] = new Vector2(rect.xMax, rect.yMax);
                    clipped[3] = new Vector2(rect.xMax, rect.yMin);
                    AppendPolygon(4, altitude, vertices, triangles, colors);
                }
                return;
            }
        }
        int columns = curved ? Math.Max(1, Mathf.CeilToInt(rect.width * 4f)) : 1;
        int rows = curved ? Math.Max(1, Mathf.CeilToInt(rect.height * 4f)) : 1;
        for (int z = 0; z < rows; z++)
        for (int x = 0; x < columns; x++)
        {
            float left = rect.xMin + rect.width * x / columns;
            float right = rect.xMin + rect.width * (x + 1) / columns;
            float bottom = rect.yMin + rect.height * z / rows;
            float top = rect.yMin + rect.height * (z + 1) / rows;
            Vector2 a = new(left, bottom), b = new(left, top), c = new(right, top), d = new(right, bottom);
            AppendTriangle(a, b, c, first, curved, altitude, vertices, triangles, colors);
            AppendTriangle(a, c, d, first, curved, altitude, vertices, triangles, colors);
        }
    }

    private void AppendTriangle(Vector2 a, Vector2 b, Vector2 c, bool first, bool curved, float altitude,
        List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        triangle[0] = a; triangle[1] = b; triangle[2] = c;
        for (int index = 0; index < 3; index++)
        {
            Vector2 point = triangle[index];
            float distance = curved ? Partition.BaseSignedDistance(point.x, point.y)
                : Partition.Reversed ? point.x - Partition.BoundaryX : Partition.BoundaryX - point.x;
            distances[index] = first ? distance : -distance;
        }
        int count = 0;
        for (int index = 0; index < 3; index++)
        {
            int previous = (index + 2) % 3;
            float before = distances[previous], after = distances[index];
            if ((before >= 0f) != (after >= 0f))
                clipped[count++] = Vector2.Lerp(triangle[previous], triangle[index], before / (before - after));
            if (after >= 0f) clipped[count++] = triangle[index];
        }
        if (count >= 3) AppendPolygon(count, altitude, vertices, triangles, colors);
    }

    private void AppendPolygon(int count, float altitude, List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        int start = vertices.Count;
        for (int index = 0; index < count; index++)
        {
            vertices.Add(new Vector3(clipped[index].x, altitude, clipped[index].y));
            colors.Add(new Color32(255, 255, 255, 255));
        }
        for (int index = 1; index < count - 1; index++)
        {
            triangles.Add(start); triangles.Add(start + index); triangles.Add(start + index + 1);
        }
    }

    private readonly Vector2[] triangle = new Vector2[3];
    private readonly Vector2[] clipped = new Vector2[4];
    private readonly float[] distances = new float[3];
    private readonly List<float> xs = new(9);
    private readonly List<float> zs = new(8);
    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();
    private readonly List<Color32> colors = new();
}
