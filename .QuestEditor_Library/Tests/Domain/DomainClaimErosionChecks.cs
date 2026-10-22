using QuestEditor_Library;
using Verse;

internal static class DomainClaimErosionChecks
{
    public static void Run()
    {
        Check(new DomainDef().contestClaimErosionTicks == 600, "default exposure duration is ten native game seconds");
        foreach (bool reverse in new[] { false, true })
        foreach (bool first in new[] { false, true })
        {
            DomainTerritoryPartition partition = new(60, 60, 0.5f, reverse);
            IntVec3 center = new(first != reverse ? 42 : 17, 0, 30);
            partition.Capture(first ? center : null, first ? null : center);
            partition.Advance(1000f, 1000f, 60f);
            for (int tick = 1; tick < 600; tick++) partition.ErodeClaims(tick, true, 600, 600);
            Check(partition.IsFirst(center) == first, "both orientations retain enemy-surrounded claims through tick 599");
            for (int pass = 0; pass < 100; pass++) partition.ErodeClaims(599, true, 600, 600);
            partition = RoundTrip(partition);
            Check(partition.IsFirst(center) == first, "paused checks and native saving preserve incomplete erosion");
            HashSet<IntVec3> previous = Cells(partition);
            partition.ErodeClaims(600, true, 600, 600);
            partition.Advance(1000f, 1000f, 60f);
            previous.SymmetricExceptWith(Cells(partition));
            Check(!partition.HasOverrides && partition.IsFirst(center) != first
                && partition.FirstClaimedCells == 0 && partition.SecondClaimedCells == 0,
                "tick 600 releases all exposed cells back to the main territory");
            Check(previous.SetEquals(partition.ChangedCells), "erosion and frontier compensation report all terrain changes, including the final claim");
        }
        DomainTerritoryPartition protectedLand = new(60, 60, 0.5f, false);
        IntVec3 point = new(42, 0, 30);
        protectedLand.Capture(point, null); protectedLand.Advance(1000f, 1000f, 60f);
        for (int tick = 1; tick < 600; tick++) protectedLand.ErodeClaims(tick, true, 600, 600);
        int protection = protectedLand.Protect(true, new[] { point }, 10000);
        protectedLand.ErodeClaims(600, true, 600, 600); protectedLand.Advance(1000f, 1000f, 60f);
        Check(protectedLand.IsFirst(point) && protectedLand.FirstClaimedCells == 1
            && !protectedLand.IsFirst(point + new IntVec3(1, 0, 0)), "protection saves only its actual cells while neighboring exposed claims fall");
        protectedLand.RemoveProtection(protection); protectedLand.Advance(1000f, 1000f, 60f);
        for (int tick = 601; tick < 1200; tick++) protectedLand.ErodeClaims(tick, true, 600, 600);
        Check(protectedLand.IsFirst(point), "protection resets exposure instead of causing immediate loss when it ends");
        protectedLand.ErodeClaims(1200, true, 600, 600); protectedLand.Advance(1000f, 1000f, 60f);
        Check(!protectedLand.HasOverrides && !protectedLand.IsFirst(point), "unprotected land requires a fresh ten seconds before falling");

        DomainTerritoryPartition rescued = new(60, 60, 0.5f, false);
        rescued.Capture(point, null); rescued.Advance(1000f, 1000f, 60f);
        for (int tick = 1; tick < 600; tick++) rescued.ErodeClaims(tick, true, 600, 600);
        rescued.Advance(9000f, 1000f, 60f); rescued.ErodeClaims(600, true, 600, 600);
        rescued.Advance(1000f, 1000f, 60f);
        for (int tick = 601; tick < 1200; tick++) rescued.ErodeClaims(tick, true, 600, 600);
        Check(rescued.IsFirst(point), "the friendly main frontier resets exposure when it reaches occupied land");
        for (int tick = 1200; tick < 1500; tick++) rescued.ErodeClaims(tick, false, 600, 600);
        Check(rescued.IsFirst(point), "arrival locks and suspended contests do not advance exposure");
        rescued.ErodeClaims(1500, true, 600, 600); rescued.Advance(1000f, 1000f, 60f);
        Check(!rescued.HasOverrides, "suspended ticks grant no erosion credit and active combat resumes the remaining timer");

        DomainTerritoryPartition recaptured = new(60, 60, 0.5f, false);
        recaptured.Capture(point, null); recaptured.Advance(1000f, 1000f, 60f);
        for (int tick = 1; tick < 600; tick++) recaptured.ErodeClaims(tick, true, 600, 600);
        recaptured.Capture(null, point); recaptured.Capture(point, null); recaptured.Advance(1000f, 1000f, 60f);
        recaptured.ErodeClaims(600, true, 600, 600);
        Check(recaptured.IsFirst(point), "a genuine recapture starts a new exposure timer");
        recaptured.PruneClaims(false, true); recaptured.Advance(0f, 1000f, 60f);
        recaptured = RoundTrip(recaptured);
        Check(!recaptured.HasOverrides && recaptured.FirstCells == 0, "dissolution saves without dangling exposure records");
        Console.WriteLine("Domain claim erosion checks passed: " + checks);
    }

    private static HashSet<IntVec3> Cells(DomainTerritoryPartition partition)
    {
        HashSet<IntVec3> result = new();
        for (int z = 0; z < partition.Height; z++)
        for (int x = 0; x < partition.Width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            if (partition.IsFirst(cell)) result.Add(cell);
        }
        return result;
    }

    private static T RoundTrip<T>(T original) where T : class, IExposable, new()
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_ClaimErosion_" + Guid.NewGuid().ToString("N") + ".xml");
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
