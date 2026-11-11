using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using UnityEngine;
using Verse;

internal static class DomainMultiChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        CQFDomainRuntime runtime = new(null!);
        DomainDef definition = new() { size = new IntVec3(60, 1, 60) };
        int[] expected = { 90, 111, 128, 143, 156, 169, 180 };
        for (int count = 2; count <= 8; count++)
        {
            IntVec3 size = runtime.BattlefieldSize(Enumerable.Repeat(definition, count).ToList());
            Check(size == new IntVec3(expected[count - 2], 1, expected[count - 2]), "declining scale keeps the total map growing at " + count);
            Check(DomainContest.HealthLossMultiplier(count) == count - 1, "attrition grows with the active domain count");
            if (count < 3) continue;
            List<IntVec3> centers = Enumerable.Range(0, count).Select(index => new IntVec3(45 + (int)(Math.Cos(index * Math.PI * 2 / count) * 18), 0,
                45 + (int)(Math.Sin(index * Math.PI * 2 / count) * 18))).ToList();
            DomainTerritoryPartition partition = new(90, 90, centers, Enumerable.Repeat(100000f, count).ToList());
            int[] owned = Enumerable.Range(0, count).Select(partition.CellsFor).ToArray();
            Console.WriteLine("TRACE equal areas count=" + count + ": " + string.Join(",", owned));
            Check(owned.Sum() == 8100 && owned.All(value => value > 0), "every cell has exactly one owner and every live domain has territory");
            Check(owned.All(value => Math.Abs(value - 8100d / count) < 8100d * 0.02d), "equal health produces approximately equal multi-domain areas");
            double area = Enumerable.Range(0, count).Sum(side => Area(partition, side));
            Check(Math.Abs(area - 8100d) < 0.1d, "multi-domain triangle clipping is complementary without holes or overlapping area");
            IntVec3 protectedCell = Cells(partition, count - 1).First();
            int protection = partition.Protect(count - 1, new[] { protectedCell }, 10000);
            float[] health = Enumerable.Repeat(100000f, count).ToArray(); health[count - 1] = 1000f;
            partition.Advance(health, 1f, true);
            Check(partition.OwnerIndex(protectedCell) == count - 1, "an indexed protection survives a severe health disadvantage");
            Check(partition.RemoveProtection(protection, count - 1), "only the owning domain removes its indexed protection");
            partition.Advance(health, 1f, true);
            HashSet<IntVec3> weak = Cells(partition, count - 1);
            Console.WriteLine("TRACE weak count=" + count + ": " + weak.Count);
            Check(weak.Count > 0 && weak.All(cell => cell.x > 0 && cell.z > 0 && cell.x < 89 && cell.z < 89), "a weak domain keeps an interior island");
            IntVec3 invasion = Cells(partition, 0).First();
            List<IntVec3?> positions = Enumerable.Repeat<IntVec3?>(null, count).ToList(); positions[count - 1] = invasion;
            int[] gains = partition.Capture(positions);
            partition.Advance(health, 1f, true);
            Check(gains[count - 1] > 0 && partition.OwnerIndex(invasion) == count - 1, "any indexed side can capture enemy ground");
            for (int tick = 0; tick < 600; tick++) partition.ErodeClaims(tick, true, Enumerable.Repeat(600, count).ToList());
            partition.Advance(health, 1f, true);
            Check(partition.OwnerIndex(invasion) != count - 1, "captured ground erodes after the configured exposure");
            health[count - 1] = 0f;
            partition.Prune(1000, health.Select(value => value > 0).ToList());
            partition.Advance(health, 1f, true);
            Check(partition.CellsFor(count - 1) == 0, "a dissolved domain has no remaining territory");
        }
        for (int count = 3; count <= 8; count++)
        foreach (bool coincident in new[] { false, true })
        {
            List<IntVec3> centers = Enumerable.Range(0, count).Select(index => coincident ? new IntVec3(90, 0, 90)
                : new IntVec3(90 + (int)(Math.Cos(index * Math.PI * 2d / count) * 6d), 0, 90 + (int)(Math.Sin(index * Math.PI * 2d / count) * 6d))).ToList();
            DomainTerritoryPartition partition = new(180, 180, centers, Enumerable.Repeat(100000f, count).ToList());
            int[] counts = Enumerable.Range(0, count).Select(partition.CellsFor).ToArray();
            Console.WriteLine("TRACE cluster n=" + count + " coincident=" + coincident + ": " + string.Join(",", counts));
            Check(counts.All(value => Math.Abs(value - 32400d / count) < 32400d * 0.015d), "clustered or identical opening positions still receive health-proportional territory");
            float[] health = Enumerable.Repeat(100000f, count).ToArray(); health[count - 1] = 1000f;
            partition.Advance(health, 1f, true);
            HashSet<IntVec3> island = Cells(partition, count - 1);
            Check(island.Count > 0 && island.All(cell => cell.x > 0 && cell.z > 0 && cell.x < 179 && cell.z < 179), "clustered weak territory closes into an interior island");
        }        List<DomainDef> unequal = new() { new() { size = new IntVec3(1000, 1, 800) }, new() { size = new IntVec3(1, 1, 1) } };
        IntVec3 guarded = runtime.BattlefieldSize(unequal, new IntVec3(1000, 1, 800));
        Check(guarded.x > 1000 && guarded.z > 800, "a tiny new domain cannot shrink an existing large battlefield");
        Console.WriteLine("Multiple domain checks passed: " + checks);
    }

    private static HashSet<IntVec3> Cells(DomainTerritoryPartition partition, int side)
    {
        HashSet<IntVec3> cells = new();
        for (int z = 0; z < partition.Height; z++)
        for (int x = 0; x < partition.Width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            if (partition.OwnerIndex(cell) == side) cells.Add(cell);
        }
        return cells;
    }

    private static double Area(DomainTerritoryPartition partition, int side)
    {
        List<Vector3> vertices = new(); List<int> triangles = new(); List<Color32> colors = new();
        new DomainTerritoryGeometry(partition).Append(new Rect(0, 0, partition.Width, partition.Height), side, 0, vertices, triangles, colors);
        double area = 0;
        for (int index = 0; index < triangles.Count; index += 3)
        {
            Vector3 a = vertices[triangles[index]], b = vertices[triangles[index + 1]], c = vertices[triangles[index + 2]];
            area += Math.Abs((b.x - a.x) * (double)(c.z - a.z) - (b.z - a.z) * (double)(c.x - a.x)) * 0.5d;
        }
        return area;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks++; Console.WriteLine("PASS " + description);
    }

    private static int checks;
}
