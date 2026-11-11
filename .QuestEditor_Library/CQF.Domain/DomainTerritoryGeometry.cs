using UnityEngine;

namespace QuestEditor_Library;

public sealed class DomainTerritoryGeometry
{
    public DomainTerritoryGeometry(DomainTerritoryPartition partition) { Partition = partition; }

    public DomainTerritoryPartition Partition { get; }

    public void Append(Rect rect, bool first, float altitude, List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        if (!Partition.HasOverrides && !Partition.UsesCurvedFront) { AppendCell(rect, first, altitude, vertices, triangles, colors); return; }
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
        Append(new Rect(0f, 0f, width, height), first, altitude, vertices, triangles, colors);
        FinalizeMesh(mesh);
    }

    public void Append(Rect rect, int side, float altitude, List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        if (!Partition.IsMultilateral) { Append(rect, side == 0, altitude, vertices, triangles, colors); return; }
        for (int z = Mathf.FloorToInt(rect.yMin); z < Mathf.CeilToInt(rect.yMax); z++)
        for (int x = Mathf.FloorToInt(rect.xMin); x < Mathf.CeilToInt(rect.xMax); x++)
        {
            Rect tile = Rect.MinMaxRect(Math.Max(x, rect.xMin), Math.Max(z, rect.yMin), Math.Min(x + 1, rect.xMax), Math.Min(z + 1, rect.yMax));
            int fixedOwner = Partition.OverrideAt(x, z);
            if (fixedOwner != 0 || !Partition.IsBoundaryCell(new Verse.IntVec3(x, 0, z)))
            {
                int owner = fixedOwner == 0 ? Partition.OwnerIndex(new Verse.IntVec3(x, 0, z)) : fixedOwner - 1;
                if (owner != side) continue;
                clipped[0] = new(tile.xMin, tile.yMin); clipped[1] = new(tile.xMin, tile.yMax);
                clipped[2] = new(tile.xMax, tile.yMax); clipped[3] = new(tile.xMax, tile.yMin);
                AppendPolygon(4, altitude, vertices, triangles, colors);
                continue;
            }
            Vector2 a = new(tile.xMin, tile.yMin), b = new(tile.xMin, tile.yMax);
            Vector2 c = new(tile.xMax, tile.yMax), d = new(tile.xMax, tile.yMin);
            AppendMultiTriangle(a, b, c, side, altitude, vertices, triangles, colors);
            AppendMultiTriangle(a, c, d, side, altitude, vertices, triangles, colors);
        }
    }

    public void Rebuild(Mesh mesh, int side, int width, int height, float altitude)
    {
        vertices.Clear(); triangles.Clear(); colors.Clear();
        Append(new Rect(0f, 0f, width, height), side, altitude, vertices, triangles, colors);
        FinalizeMesh(mesh);
    }

    internal void RebuildUnclaimed(Mesh mesh, Rect bounds, float altitude)
    {
        vertices.Clear(); triangles.Clear(); colors.Clear();
        for (int z = Math.Max(0, Mathf.FloorToInt(bounds.yMin)); z < Math.Min(Partition.Height, Mathf.CeilToInt(bounds.yMax)); z++)
        for (int x = Math.Max(0, Mathf.FloorToInt(bounds.xMin)); x < Math.Min(Partition.Width, Mathf.CeilToInt(bounds.xMax)); x++)
        {
            if (Partition.OverrideAt(x, z) != 0) continue;
            if (Partition.UsesCurvedFront && !Partition.NearFront(x, z, 2.5f)) continue;
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
        if (Partition.UsesCurvedFront)
        {
            Vector2 a = new(rect.xMin, rect.yMin), b = new(rect.xMin, rect.yMax);
            Vector2 c = new(rect.xMax, rect.yMax), d = new(rect.xMax, rect.yMin);
            AppendTriangle(a, b, c, first, altitude, vertices, triangles, colors);
            AppendTriangle(a, c, d, first, altitude, vertices, triangles, colors);
            return;
        }
        float left = first != Partition.Reversed ? rect.xMin : Math.Max(rect.xMin, Partition.BoundaryX);
        float right = first != Partition.Reversed ? Math.Min(rect.xMax, Partition.BoundaryX) : rect.xMax;
        if (left >= right) return;
        clipped[0] = new Vector2(left, rect.yMin); clipped[1] = new Vector2(left, rect.yMax);
        clipped[2] = new Vector2(right, rect.yMax); clipped[3] = new Vector2(right, rect.yMin);
        AppendPolygon(4, altitude, vertices, triangles, colors);
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

    private void AppendTriangle(Vector2 a, Vector2 b, Vector2 c, bool first, float altitude,
        List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        input[0] = a; input[1] = b; input[2] = c;
        int count = 0;
        Vector2 previous = c;
        float previousDistance = Partition.BaseSignedDistance(c.x, c.y) * (first ? 1f : -1f);
        for (int index = 0; index < 3; index++)
        {
            Vector2 current = input[index];
            float distance = Partition.BaseSignedDistance(current.x, current.y) * (first ? 1f : -1f);
            if ((distance > 0f) != (previousDistance > 0f))
                clipped[count++] = Vector2.Lerp(previous, current, previousDistance / (previousDistance - distance));
            if (distance > 0f) clipped[count++] = current;
            previous = current;
            previousDistance = distance;
        }
        if (count >= 3) AppendPolygon(count, altitude, vertices, triangles, colors);
    }

    private void AppendMultiTriangle(Vector2 a, Vector2 b, Vector2 c, int side, float altitude,
        List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        if (Partition.FractionFor(side) <= 0f) return;
        multiInput[0] = a; multiInput[1] = b; multiInput[2] = c;
        int count = 3;
        for (int rival = 0; rival < Partition.DomainCount && count >= 3; rival++)
        {
            if (rival == side || Partition.FractionFor(rival) <= 0f) continue;
            int output = 0;
            Vector2 previous = multiInput[count - 1];
            float previousDistance = Partition.ScoreFor(side, previous.x, previous.y) - Partition.ScoreFor(rival, previous.x, previous.y);
            for (int index = 0; index < count; index++)
            {
                Vector2 current = multiInput[index];
                float distance = Partition.ScoreFor(side, current.x, current.y) - Partition.ScoreFor(rival, current.x, current.y);
                if ((distance >= 0f) != (previousDistance >= 0f))
                    multiOutput[output++] = Vector2.Lerp(previous, current, previousDistance / (previousDistance - distance));
                if (distance >= 0f) multiOutput[output++] = current;
                previous = current; previousDistance = distance;
            }
            count = output;
            Array.Copy(multiOutput, multiInput, count);
        }
        if (count < 3) return;
        Array.Copy(multiInput, clipped, count);
        AppendPolygon(count, altitude, vertices, triangles, colors);
    }

    private readonly Vector2[] input = new Vector2[3];
    private readonly Vector2[] clipped = new Vector2[16];
    private readonly Vector2[] multiInput = new Vector2[16];
    private readonly Vector2[] multiOutput = new Vector2[16];
    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();
    private readonly List<Color32> colors = new();
}
