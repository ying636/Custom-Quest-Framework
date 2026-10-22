using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using UnityEngine;
using Verse;

internal static class DomainTerritoryRulesChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        foreach (bool reversed in new[] { false, true })
        foreach (bool first in new[] { false, true })
        {
            DomainTerritoryPartition partition = new(60, 60, 0.5f, reversed);
            List<IntVec3> cells = new();
            for (int z = 10; z < 50; z++)
            for (int x = 20; x < 40; x++)
                if (partition.IsFirst(new IntVec3(x, 0, z)) == first) cells.Add(new IntVec3(x, 0, z));
            int id = partition.Protect(first, cells.Concat(cells), 1000);
            Check(partition.Protections.Single().Cells.Count == cells.Count, "duplicate cells are registered once");
            int secondId = partition.Protect(first, cells.Take(20), 900);
            partition.Advance(first ? 1000f : 9000f, first ? 9000f : 1000f, 60f);
            Check(cells.All(cell => partition.IsFirst(cell) == first), "protected territory withstands the advancing frontier");
            Check(Math.Abs((first ? partition.FirstCells : partition.SecondCells) - cells.Count) <= 60,
                "protected area forms a minimum allocation when larger than the health share");
            CheckGeometry(partition);
            CheckPersistence(partition, cells, first);
            partition.PruneProtections(899, true, true);
            Check(partition.Protections.Count == 2, "protection remains before its expiry tick");
            partition.PruneProtections(900, true, true);
            Check(partition.Protections.Count == 1 && cells.All(cell => partition.IsFirst(cell) == first),
                "one overlapping protection expiring preserves the other protection");
            Check(!partition.RemoveProtection(secondId), "already expired protection cannot be removed twice");
            Check(partition.RemoveProtection(id), "explicit removal identifies the original protection");
            partition.Advance(first ? 1000f : 9000f, first ? 9000f : 1000f, 60f);
            Check(!partition.HasProtections && cells.Any(cell => partition.IsFirst(cell) != first), "removal releases cells back to the health-based partition");
            bool opposite = !first;
            IntVec3 oppositeCell = Enumerable.Range(0, 60).Select(x => new IntVec3(x, 0, 30)).First(cell => partition.IsFirst(cell) == opposite);
            partition.Protect(opposite, new[] { oppositeCell }, 2000);
            partition.PruneProtections(1000, first, !first);
            partition.Advance(first ? 1000f : 0f, first ? 0f : 1000f, 60f);
            Check(!partition.HasProtections && partition.FirstCells == (first ? 3600 : 0), "dissolution clears the defeated side's protection before survivor takeover");
        }
        DomainTerritoryPartition invalid = new(60, 60, 0.5f, false);
        Throws(() => invalid.Protect(true, new[] { new IntVec3(40, 0, 20) }, 100), "opposing territory cannot be protected");
        Throws(() => invalid.Protect(true, Array.Empty<IntVec3>(), 100), "empty protection is rejected");
        foreach (bool reversed in new[] { false, true })
        {
            DomainTerritoryPartition partition = new(60, 60, 0.5f, reversed);
            partition.Advance(9900f, 100f, 60f);
            Check(!partition.HasOverrides && Enumerable.Range(0, 60).All(x =>
                Enumerable.Range(0, 60).Select(z => partition.IsFirst(new IntVec3(x, 0, z))).Distinct().Count() == 1),
                "extreme health differences move the main frontier without generating automatic holes");
        }
        Console.WriteLine("Domain territory rule checks passed: " + checks);
    }

    private static void CheckGeometry(DomainTerritoryPartition partition)
    {
        DomainTerritoryGeometry geometry = new(partition);
        List<Vector3> vertices = new(); List<int> triangles = new(); List<Color32> colors = new();
        double sum = 0d;
        for (int side = 0; side < 2; side++)
        {
            geometry.Append(new Rect(0f, 0f, 60f, 60f), side == 0, 0f, vertices, triangles, colors);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                sum += Math.Abs((b.x - a.x) * (double)(c.z - a.z) - (b.z - a.z) * (double)(c.x - a.x)) * 0.5d;
            }
            vertices.Clear(); triangles.Clear(); colors.Clear();
        }
        Check(Math.Abs(sum - 3600d) < 0.01d, "protected cells and frontier meshes cover the whole battlefield exactly once");
    }

    private static void CheckPersistence(DomainTerritoryPartition original, List<IntVec3> cells, bool first)
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_ProtectedTerritory_" + Guid.NewGuid().ToString("N") + ".xml");
        DomainTerritoryPartition? loaded = original;
        try
        {
            Scribe.saver.InitSaving(path, "checks"); Scribe_Deep.Look(ref loaded, "partition"); Scribe.saver.FinalizeSaving();
            loaded = null;
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "partition"); Scribe.loader.FinalizeLoading();
            Check(loaded!.Protections.Count == 2 && cells.All(cell => loaded.IsFirst(cell) == first)
                && loaded.FirstCells == original.FirstCells, "native saving restores protected regions, overlap and actual ownership");
        }
        finally { if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop(); File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++; Console.WriteLine("PASS " + message);
    }

    private static void Throws(Action action, string message)
    {
        try { action(); } catch (ArgumentException) { Check(true, message); return; }
        throw new InvalidOperationException(message);
    }

    private static int checks;
}
