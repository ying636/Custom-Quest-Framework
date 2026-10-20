using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using UnityEngine;
using Verse;

internal static class DomainInvasionChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        foreach (bool reversed in new[] { false, true })
        foreach (bool strongFirst in new[] { false, true })
        {
            float a = strongFirst ? 8000f : 2000f, b = strongFirst ? 2000f : 8000f;
            DomainTerritoryPartition partition = new(60, 60, 0.5f, reversed);
            partition.EnableInvasions(74183);
            HashSet<IntVec3> previous = Cells(partition);
            for (int tick = 0; tick < 180; tick++)
            {
                partition.Advance(a, b, 0.5f);
                HashSet<IntVec3> current = Cells(partition);
                previous.SymmetricExceptWith(current);
                if (!previous.SetEquals(partition.ChangedCells)) throw new InvalidOperationException("Changed-cell list missed a hole or frontier change.");
                previous = current;
            }
            Check(partition.Invasion!.First == strongFirst && partition.Invasion.Strength > 0.49f,
                "dominant side develops actual islands, reverse=" + reversed + ", first=" + strongFirst);
            Check(partition.Invasion.Shapes.Count == 3 && partition.Invasion.Shapes.All(shape =>
                partition.SignedDistance(shape.x, shape.y) > 0f == strongFirst), "all three hole centers belong to the invading side");
            Check(partition.Invasion.Shapes.All(shape => (reversed ? shape.x > partition.BoundaryX : shape.x < partition.BoundaryX) != strongFirst),
                "invasion centers lie beyond the main frontier inside opposing territory");
            double rectangle = (reversed ? 60d - partition.BoundaryX : partition.BoundaryX) * 60d;
            double total = rectangle + partition.Invasion.AreaOutside(partition.BoundaryX, !reversed);
            Check(Math.Abs(total - partition.FirstFraction * 3600d) < 0.001d,
                "frontier compensates island area to retain the health ratio");
            Check(partition.FirstCells == Cells(partition).Count && partition.FirstCells + partition.SecondCells == 3600,
                "cell totals include disconnected invaded cells without overlaps or gaps");
            CheckGeometry(partition);
            CheckPersistence(partition);
            for (int tick = 0; tick < 180; tick++) partition.Advance(5000f, 5000f, 0.5f);
            Check(partition.Invasion.Strength == 0f && partition.BoundaryX == 30f && partition.FirstCells == 1800,
                "recovery closes every island and restores the balanced frontier");
            for (int tick = 0; tick < 180; tick++) partition.Advance(b, a, 0.5f);
            Check(partition.Invasion.First != strongFirst && partition.Invasion.Strength > 0.49f,
                "reversed health advantage invades the other side without stale ownership");
            partition.Advance(0f, 1000f, 60f);
            Check(partition.Invasion.Strength == 0f && partition.FirstCells == 0 && partition.SecondCells == 3600,
                "domain dissolution clears islands and immediately assigns the whole battlefield to the survivor");
        }
        DomainTerritoryPartition balanced = new(60, 60, 0.5f, false);
        balanced.EnableInvasions(8);
        for (int tick = 0; tick < 100; tick++) balanced.Advance(6000f, 4000f, 0.5f);
        Check(balanced.Invasion!.Strength == 0f, "moderate health differences retain the ordinary frontier");
        for (int tick = 0; tick < 100; tick++) balanced.Advance(6000f, 4000f, 0.5f);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int tick = 0; tick < 256; tick++) balanced.Advance(6000f, 4000f, 0.5f);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "stable territory updates allocate no managed memory");
        for (int tick = 0; tick < 180; tick++) balanced.Advance(8000f + MathF.Sin(tick * 0.1f) * 100f, 2000f, 0.5f);
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int tick = 0; tick < 256; tick++) balanced.Advance(8000f + MathF.Sin(tick * 0.1f) * 100f, 2000f, 0.5f);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        timer.Stop();
        Check(allocated == 0, "moving invasion geometry and ownership reuse their existing buffers");
        Console.WriteLine("PROFILE moving invasion update=" + timer.Elapsed.TotalMilliseconds / 256d + " ms, allocated=" + allocated);
        Console.WriteLine("Domain invasion checks passed: " + checks);
    }

    private static void CheckGeometry(DomainTerritoryPartition partition)
    {
        DomainTerritoryGeometry geometry = new(partition);
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Color32> colors = new();
        Vector4 hole = partition.Invasion!.Shapes[1];
        Rect rect = new(Mathf.Floor(hole.x - hole.z), Mathf.Floor(hole.y - hole.w), Mathf.Ceil(hole.z * 2f) + 2f, Mathf.Ceil(hole.w * 2f) + 2f);
        double area = 0d;
        for (int side = 0; side < 2; side++)
        {
            geometry.Append(rect, side == 0, 0f, vertices, triangles, colors);
            for (int triangle = 0; triangle < triangles.Count; triangle += 3)
            {
                Vector3 a = vertices[triangles[triangle]], b = vertices[triangles[triangle + 1]], c = vertices[triangles[triangle + 2]];
                double twice = (b.x - a.x) * (double)(c.z - a.z) - (b.z - a.z) * (double)(c.x - a.x);
                area += Math.Abs(twice) * 0.5d;
            }
            vertices.Clear(); triangles.Clear(); colors.Clear();
        }
        Check(Math.Abs(area - rect.width * rect.height) < 0.01d, "both curved terrain meshes cover the same region without gaps or overlap");
    }

    private static void CheckPersistence(DomainTerritoryPartition original)
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_Invasion_" + Guid.NewGuid().ToString("N") + ".xml");
        DomainTerritoryPartition? loaded = original;
        try
        {
            Scribe.saver.InitSaving(path, "checks");
            Scribe_Deep.Look(ref loaded, "partition");
            Scribe.saver.FinalizeSaving();
            loaded = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "partition");
            Scribe.loader.FinalizeLoading();
            Check(loaded!.Invasion!.First == original.Invasion!.First && loaded.Invasion.Strength == original.Invasion.Strength
                && loaded.Invasion.Shapes.SequenceEqual(original.Invasion.Shapes) && Cells(loaded).SetEquals(Cells(original)),
                "native saving restores island geometry, owner, growth and all occupied cells");
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop();
            File.Delete(path);
        }
    }

    private static HashSet<IntVec3> Cells(DomainTerritoryPartition partition)
    {
        HashSet<IntVec3> result = new();
        for (int z = 0; z < 60; z++)
        for (int x = 0; x < 60; x++)
        {
            IntVec3 cell = new(x, 0, z);
            if (partition.IsFirst(cell)) result.Add(cell);
        }
        return result;
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static int checks;
}
