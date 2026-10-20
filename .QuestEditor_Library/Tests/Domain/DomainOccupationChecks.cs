using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using UnityEngine;
using Verse;

internal static class DomainOccupationChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        DomainOccupation timer = new();
        for (int tick = 1; tick < 180; tick++)
            if (timer.Advance(tick, true)) throw new InvalidOperationException("Occupation completed early.");
        Check(timer.Ticks == 179, "enemy presence remains incomplete at 179 native game ticks");
        for (int pass = 0; pass < 256; pass++) timer.Advance(179, true);
        Check(timer.Ticks == 179, "repeated checks during a paused tick do not advance occupation");
        timer = RoundTrip(timer);
        Check(timer.Ticks == 179 && timer.Advance(180, true) && timer.Ticks == 0, "saved progress completes exactly on the 180th tick");
        for (int tick = 181; tick < 240; tick++) timer.Advance(tick, true);
        timer.Advance(240, false);
        Check(timer.Ticks == 0, "returning home or becoming ineligible clears accumulated progress");
        timer.Advance(241, true); timer.Advance(500, true);
        Check(timer.Ticks == 1, "unobserved time cannot be credited as continuous enemy presence");
        for (int tick = 501; tick < 550; tick++) timer.Advance(tick, true);
        timer.Advance(549, false); timer.Advance(549, true);
        Check(timer.Ticks == 0, "a same-tick interruption resets progress without starting it twice");
        foreach (bool reverse in new[] { false, true })
        foreach (bool first in new[] { false, true }) CheckCapture(reverse, first);
        DomainTerritoryPartition blocked = new(60, 60, 0.5f, false);
        IntVec3 center = new(42, 0, 30);
        blocked.Protect(false, new[] { center }, 1000);
        var gained = blocked.Capture(center, null);
        blocked.Advance(1000f, 1000f, 60f);
        Check(gained.First == GenRadial.NumCellsInRadius(2f) - 1 && !blocked.IsFirst(center), "an enemy protected cell is excluded from radius-two conversion");
        blocked = RoundTrip(blocked);
        Check(blocked.HasProtections && blocked.HasOverrides && !blocked.IsFirst(center)
            && blocked.IsFirst(center + new IntVec3(1, 0, 0)), "native saving keeps protection priority over surrounding captured territory");
        DomainTerritoryPartition same = new(60, 60, 0.5f, false);
        gained = same.Capture(new IntVec3(30, 0, 30), new IntVec3(30, 0, 30));
        Check(gained.First == 0 && gained.Second == 0 && !same.HasOverrides, "equal simultaneous captures keep the original owner without a processing-order advantage");
        var simultaneous = same.Capture(new IntVec3(31, 0, 30), new IntVec3(29, 0, 30));
        same.Advance(1000f, 1000f, 60f);
        Check(simultaneous.First > 0 && simultaneous.Second > 0 && same.IsFirst(new IntVec3(31, 0, 30))
            && !same.IsFirst(new IntVec3(29, 0, 30)) && !same.IsFirst(new IntVec3(30, 0, 30)),
            "simultaneous overlapping captures use distance and preserve equidistant ownership");
        Console.WriteLine("Domain occupation checks passed: " + checks);
    }

    private static void CheckCapture(bool reverse, bool first)
    {
        DomainTerritoryPartition partition = new(60, 60, 0.5f, reverse);
        IntVec3 center = new(first != reverse ? 42 : 17, 0, 30);
        HashSet<IntVec3> previous = Cells(partition);
        var gained = partition.Capture(first ? center : null, first ? null : center);
        Check((first ? gained.First : gained.Second) == GenRadial.NumCellsInRadius(2f), "capture converts the native radius-two footprint for either orientation");
        partition.Advance(5000f, 5000f, 60f);
        HashSet<IntVec3> current = Cells(partition);
        previous.SymmetricExceptWith(current);
        Check(previous.SetEquals(partition.ChangedCells), "captured islands and compensation expose every terrain ownership change");
        Check(GenRadial.RadialCellsAround(center, 2f, true).All(cell => partition.IsFirst(cell) == first)
            && !partition.HasProtections && partition.HasOverrides, "captured cells persist independently from temporary protection");
        partition = RoundTrip(partition);
        Check(GenRadial.RadialCellsAround(center, 2f, true).All(cell => partition.IsFirst(cell) == first)
            && (first ? partition.FirstClaimedCells : partition.SecondClaimedCells) == GenRadial.NumCellsInRadius(2f), "native saving restores every claimed cell and its owner");
        partition.Advance(first ? 1f : 10000f, first ? 10000f : 1f, 60f);
        Check(GenRadial.RadialCellsAround(center, 2f, true).All(cell => partition.IsFirst(cell) == first), "health-ratio changes do not silently erase a captured foothold");
        CheckGeometry(partition);
        gained = partition.Capture(first ? null : center, first ? center : null);
        partition.Advance(1000f, 1000f, 60f);
        Check((first ? gained.Second : gained.First) == GenRadial.NumCellsInRadius(2f)
            && GenRadial.RadialCellsAround(center, 2f, true).All(cell => partition.IsFirst(cell) != first), "opposing occupation recaptures the same footprint");
        partition.PruneClaims(first, !first);
        partition.Advance(first ? 1000f : 0f, first ? 0f : 1000f, 60f);
        Check(!partition.HasOverrides && partition.FirstCells == (first ? 3600 : 0), "dissolution removes captured cells before survivor takeover");
    }

    private static void CheckGeometry(DomainTerritoryPartition partition)
    {
        DomainTerritoryGeometry geometry = new(partition);
        List<Vector3> vertices = new(); List<int> triangles = new(); List<Color32> colors = new();
        double area = 0d;
        for (int side = 0; side < 2; side++)
        {
            geometry.Append(new Rect(0f, 0f, 60f, 60f), side == 0, 0f, vertices, triangles, colors);
            for (int index = 0; index < triangles.Count; index += 3)
            {
                Vector3 a = vertices[triangles[index]], b = vertices[triangles[index + 1]], c = vertices[triangles[index + 2]];
                area += Math.Abs((b.x - a.x) * (double)(c.z - a.z) - (b.z - a.z) * (double)(c.x - a.x)) * 0.5d;
            }
            vertices.Clear(); triangles.Clear(); colors.Clear();
        }
        Check(Math.Abs(area - 3600d) < 0.01d, "captured and natural terrain meshes cover the battlefield without overlaps or gaps");
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

    private static T RoundTrip<T>(T original) where T : class, IExposable, new()
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_Occupation_" + Guid.NewGuid().ToString("N") + ".xml");
        T? loaded = original;
        try
        {
            Scribe.saver.InitSaving(path, "checks"); Scribe_Deep.Look(ref loaded, "state"); Scribe.saver.FinalizeSaving();
            loaded = null; Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "state"); Scribe.loader.FinalizeLoading();
            return loaded!;
        }
        finally { if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop(); File.Delete(path); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++; Console.WriteLine("PASS " + message);
    }

    private static int checks;
}
