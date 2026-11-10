using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using UnityEngine;
using Verse;

internal static class DomainFrontChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        CQFDomainRuntime runtime = new(null!);
        bool rejected = false;
        try { runtime.GetMaps(null!); } catch (ArgumentNullException) { rejected = true; }
        Check(rejected, "a missing domain definition is rejected rather than matching every map with no challenger");
        foreach (var size in new[] { (60, 60, 60, 60, 90, 90), (60, 100, 120, 40, 135, 105), (61, 63, 62, 64, 93, 96) })
        {
            DomainDef first = new() { size = new IntVec3(size.Item1, 1, size.Item2) };
            DomainDef second = new() { size = new IntVec3(size.Item3, 1, size.Item4) };
            Check(runtime.BattlefieldSize(first, second) == new IntVec3(size.Item5, 1, size.Item6), "battlefield dimensions use the summed dimensions times 0.75 with ceiling");
            Check(runtime.BattlefieldSize(first, second) == runtime.BattlefieldSize(second, first), "battlefield size is independent of which domain opens first");
        }
        foreach (bool reverse in new[] { false, true })
        foreach (float fraction in new[] { 0f, 0.02f, 0.1f, 0.2f, 0.25f, 0.3f, 0.5f, 0.7f, 0.75f, 0.8f, 0.9f, 0.98f, 1f })
        {
            DomainTerritoryPartition partition = new(90, 74, fraction, reverse, new IntVec3(reverse ? 60 : 30, 0, 35), new IntVec3(reverse ? 30 : 60, 0, 38));
            Check(partition.FirstCells + partition.SecondCells == partition.CellCount, "curved front assigns every physical cell exactly once");
            Check(Math.Abs(Area(partition, true) - fraction * partition.CellCount) < 0.04,
                "curved geometry preserves the continuous health area at " + fraction);
            Check(Math.Abs(Area(partition, true) + Area(partition, false) - partition.CellCount) < 0.04,
                "complementary front meshes have no holes or overlapping area");
            if (fraction > 0f && fraction < 1f)
            {
                bool weakFirst = fraction < 0.5f;
                HashSet<IntVec3> weak = Cells(partition, weakFirst);
                Check(Connected(weak) && Connected(Cells(partition, !weakFirst)), "both main territories remain connected while wrapping");
                if (Math.Min(fraction, 1f - fraction) <= 0.20001f)
                    Check(weak.Count > 0 && weak.All(cell => cell.x > 0 && cell.z > 0 && cell.x < 89 && cell.z < 73),
                        "weak main territory closes into a surrounded island");
            }
        }
        DomainTerritoryPartition moving = new(90, 90, 0.5f, false, new(30, 0, 45), new(60, 0, 45));
        for (int tick = 0; tick < 120; tick++) moving.Advance(1000f, 9000f, 0.75f);
        Check(moving.Encirclement == 1f && Math.Abs(moving.FirstFraction - 0.1f) < 0.00001f, "weak territory wraps through a continuous area transition");
        IntVec3 protectedCell = Cells(moving, true).First();
        moving.Protect(true, new[] { protectedCell }, 1000);
        moving.Advance(0.1f, 10000f, 90f);
        Check(moving.IsFirst(protectedCell) && Area(moving, true) >= 0.9999, "protection remains effective beyond a collapsing island area quota");
        CheckPersistence(moving);
        moving.RemoveProtection(moving.Protections[0].Id);
        for (int tick = 0; tick < 120; tick++) moving.Advance(1000f, 1000f, 0.75f);
        Check(moving.Encirclement == 0f && moving.FirstFraction == 0.5f && moving.FirstCenter == new IntVec3(30, 0, 45),
            "healing reopens the front while its original core remains fixed");
        Console.WriteLine("Domain front checks passed: " + checks);
    }

    private static HashSet<IntVec3> Cells(DomainTerritoryPartition partition, bool first)
    {
        HashSet<IntVec3> cells = new();
        for (int z = 0; z < partition.Height; z++)
        for (int x = 0; x < partition.Width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            if (partition.IsFirst(cell) == first) cells.Add(cell);
        }
        return cells;
    }

    private static bool Connected(HashSet<IntVec3> cells)
    {
        if (cells.Count == 0) return true;
        HashSet<IntVec3> seen = new();
        Queue<IntVec3> pending = new();
        pending.Enqueue(cells.First()); seen.Add(cells.First());
        while (pending.Count > 0)
        {
            IntVec3 cell = pending.Dequeue();
            foreach (IntVec3 offset in new[] { new IntVec3(1, 0, 0), new IntVec3(-1, 0, 0), new IntVec3(0, 0, 1), new IntVec3(0, 0, -1) })
            {
                IntVec3 adjacent = cell + offset;
                if (cells.Contains(adjacent) && seen.Add(adjacent)) pending.Enqueue(adjacent);
            }
        }
        return cells.Count == seen.Count;
    }

    private static double Area(DomainTerritoryPartition partition, bool first)
    {
        List<Vector3> vertices = new(); List<int> triangles = new(); List<Color32> colors = new();
        new DomainTerritoryGeometry(partition).Append(new Rect(0f, 0f, partition.Width, partition.Height), first, 0f, vertices, triangles, colors);
        double area = 0;
        for (int index = 0; index < triangles.Count; index += 3)
        {
            Vector3 a = vertices[triangles[index]], b = vertices[triangles[index + 1]], c = vertices[triangles[index + 2]];
            area += Math.Abs(((double)b.x - a.x) * (c.z - a.z) - ((double)c.x - a.x) * (b.z - a.z)) * 0.5;
        }
        return area;
    }

    private static void CheckPersistence(DomainTerritoryPartition partition)
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_Front_" + Guid.NewGuid().ToString("N") + ".xml");
        DomainTerritoryPartition? value = partition;
        try
        {
            Scribe.saver.InitSaving(path, "front"); Scribe_Deep.Look(ref value, "partition"); Scribe.saver.FinalizeSaving();
            value = null; Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref value, "partition"); Scribe.loader.FinalizeLoading();
            Check(value!.UsesCurvedFront && value.FirstCenter == partition.FirstCenter && value.SecondCenter == partition.SecondCenter
                && Cells(value, true).SetEquals(Cells(partition, true)), "native save restores island shape, core locations and protected ownership");
        }
        finally { if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop(); File.Delete(path); }
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
        checks++;
    }

    private static int checks;
}
