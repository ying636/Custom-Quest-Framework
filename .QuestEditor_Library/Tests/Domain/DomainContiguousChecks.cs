using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;

internal static class DomainContiguousChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        for (int count = 3; count <= 8; count++)
        foreach (IntVec3 size in new[] { new IntVec3(90, 1, 90), new IntVec3(160, 1, 70), new IntVec3(60, 1, 140) })
        for (int scenario = 0; scenario < 4; scenario++)
        {
            List<IntVec3> centers = Enumerable.Range(0, count).Select(index => scenario == 0 ? size / 2
                : new IntVec3(size.x / 2 + (int)(Math.Cos(index * Math.PI * 2d / count) * (scenario == 1 ? 3 : size.x * 0.25d)), 0,
                    size.z / 2 + (int)(Math.Sin(index * Math.PI * 2d / count) * (scenario == 1 ? 3 : size.z * 0.25d)))).ToList();
            float[] health = Enumerable.Range(0, count).Select(index => scenario < 2 ? 100000f
                : scenario == 2 ? index == count - 1 ? 1000f : 100000f : index % 3 == 0 ? 15000f : index % 3 == 1 ? 45000f : 100000f).ToArray();
            DomainTerritoryPartition partition = new(size.x, size.z, centers, health);
            Validate(partition, health, "n=" + count + " size=" + size + " scenario=" + scenario);
            if (scenario == 2)
                Check(Cells(partition, count - 1).All(cell => cell.x > 0 && cell.z > 0 && cell.x < size.x - 1 && cell.z < size.z - 1),
                    "the weakened block closes into one interior island");
            if (scenario == 0 && size.x == 90) Persistence(partition);
        }
        Random random = new(38291);
        for (int scenario = 0; scenario < 150; scenario++)
        {
            int count = 3 + scenario % 6;
            float[] health = Enumerable.Range(0, count).Select(_ => (float)(100000d * Math.Pow(10d, -random.NextDouble() * 2.5d))).ToArray();
            List<IntVec3> centers = Enumerable.Range(0, count).Select(_ => new IntVec3(random.Next(35, 45), 0, random.Next(35, 45))).ToList();
            DomainTerritoryPartition partition = new(80, 80, centers, health);
            Validate(partition, health, "random=" + scenario);
        }
        for (int count = 3; count <= 8; count++)
        {
            float[] health = Enumerable.Repeat(100000f, count).ToArray(); health[count - 1] = 1000f;
            DomainTerritoryPartition partition = new(100, 100, Enumerable.Repeat(new IntVec3(50, 0, 50), count).ToList(), health);
            Validate(partition, health, "coincident island n=" + count);
            Check(Cells(partition, count - 1).All(cell => cell.x > 0 && cell.z > 0 && cell.x < 99 && cell.z < 99), "a coincident weak domain is a closed interior island");
        }
        foreach (int count in new[] { 3, 8 })
        {
            float[] health = Enumerable.Repeat(100000f, count).ToArray(); health[count - 1] = 1f;
            DomainTerritoryPartition partition = new(100, 100, Enumerable.Repeat(new IntVec3(50, 0, 50), count).ToList(), health);
            Validate(partition, health, "last health point n=" + count);
            IntVec3 protectedCell = Cells(partition, count - 1).First();
            partition.Protect(count - 1, new[] { protectedCell }, 10000);
            partition.Advance(health, 1f, true);
            Check(partition.OwnerIndex(protectedCell) == count - 1, "protection remains valid when its area exceeds its owner's health share");
            for (int side = 1; side < count; side++) health[side] = 0f;
            partition.Prune(10001, health.Select(value => value > 0f).ToList());
            partition.Advance(health, 1f, true);
            Check(partition.CellsFor(0) == partition.CellCount, "the final active side takes the full battlefield");
        }
        for (int scenario = 0; scenario < 72; scenario++)
        {
            int count = 3 + scenario % 6;
            float[] health = Enumerable.Range(0, count).Select(index => index > 0 && (index + scenario) % 3 == 0 ? 0f
                : (float)Math.Max(1d, 100000d * Math.Pow(10d, -random.NextDouble() * 5d))).ToArray();
            DomainTerritoryPartition partition = new(90, 90, Enumerable.Repeat(new IntVec3(45, 0, 45), count).ToList(), health);
            Validate(partition, health, "retired and near-collapse n=" + count + " scenario=" + scenario);
        }
        foreach (int count in new[] { 4, 8 })
        {
            float[] health = Enumerable.Repeat(100000f, count).ToArray();
            DomainTerritoryPartition partition = new(100, 100, Enumerable.Repeat(new IntVec3(50, 0, 50), count).ToList(), health);
            for (int tick = 0; tick < 240; tick++)
            {
                health[count - 1] = tick < 120 ? 1000f : 100000f;
                partition.Advance(health, 1f / 120f);
                if (tick % 12 == 11) Validate(partition, Enumerable.Range(0, count).Select(index => partition.FractionFor(index)).ToArray(), "moving n=" + count + " tick=" + tick);
            }
        }
        Console.WriteLine("Contiguous domain checks passed: " + checks);
    }

    private static void Validate(DomainTerritoryPartition partition, IReadOnlyList<float> health, string scenario)
    {
        double total = health.Sum(value => (double)value);
        for (int side = 0; side < health.Count; side++)
        {
            HashSet<IntVec3> cells = Cells(partition, side);
            if (health[side] == 0f) { Check(cells.Count == 0, "inactive side owns no main ground: " + scenario); continue; }
            Check(cells.Count > 0 && Connected(cells), "each active main territory is cardinally connected: " + scenario + " side=" + side);
            double expected = partition.CellCount * health[side] / total;
            Check(Math.Abs(cells.Count - expected) <= Math.Max(3d, partition.CellCount * 0.015d), "connected territory retains the health quota: " + scenario + " side=" + side + " expected=" + expected + " actual=" + cells.Count);
            int width = cells.Max(cell => cell.x) - cells.Min(cell => cell.x) + 1;
            int height = cells.Max(cell => cell.z) - cells.Min(cell => cell.z) + 1;
            Check(cells.Count < 32 || cells.Count >= width * height * 0.3d && Math.Max(width, height) <= Math.Min(width, height) * 5,
                "the main territory is a compact block rather than a narrow ribbon: " + scenario + " side=" + side);
        }
    }

    private static HashSet<IntVec3> Cells(DomainTerritoryPartition partition, int side)
    {
        HashSet<IntVec3> result = new();
        for (int z = 0; z < partition.Height; z++)
        for (int x = 0; x < partition.Width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            if (partition.OwnerIndex(cell) == side) result.Add(cell);
        }
        return result;
    }

    private static bool Connected(HashSet<IntVec3> cells)
    {
        HashSet<IntVec3> reached = new();
        Queue<IntVec3> pending = new(); pending.Enqueue(cells.First());
        while (pending.Count > 0)
        {
            IntVec3 cell = pending.Dequeue();
            if (!reached.Add(cell)) continue;
            foreach (IntVec3 direction in GenAdj.CardinalDirections)
                if (cells.Contains(cell + direction) && !reached.Contains(cell + direction)) pending.Enqueue(cell + direction);
        }
        if (reached.Count != cells.Count) Console.WriteLine("TRACE disconnected reached=" + reached.Count + " total=" + cells.Count);
        return reached.Count == cells.Count;
    }

    private static void Persistence(DomainTerritoryPartition original)
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_Connected_" + Guid.NewGuid().ToString("N") + ".xml");
        DomainTerritoryPartition? value = original;
        try
        {
            Scribe.saver.InitSaving(path, "connectedChecks"); Scribe_Deep.Look(ref value, "partition"); Scribe.saver.FinalizeSaving();
            value = null;
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref value, "partition"); Scribe.loader.FinalizeLoading();
            Check(value != null && Enumerable.Range(0, original.CellCount).All(index => value.OwnerIndex(new IntVec3(index % original.Width, 0, index / original.Width))
                == original.OwnerIndex(new IntVec3(index % original.Width, 0, index / original.Width))), "saving and loading retains the exact contiguous territory");
        }
        finally { if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop(); File.Delete(path); }
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        checks++; Console.WriteLine("PASS " + message);
    }

    private static int checks;
}
