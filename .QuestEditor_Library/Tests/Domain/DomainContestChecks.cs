using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;

internal static class DomainContestChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        Check(typeof(MapComponent_Domain).IsSealed,
            "domain instances cannot introduce a derived map component that vanilla auto-registers in place of the battlefield");
        Check(DomainTerritoryPartition.CalculateFraction(3000f, 1000f) == 0.75f, "absolute current health puts the straight boundary at 75 percent for 3000 versus 1000");
        Check(DomainTerritoryPartition.CalculateFraction(0f, 0f) == 0f, "simultaneous depletion avoids division by zero");
        Check(DomainTerritoryPartition.CalculateFraction(float.MaxValue, float.MaxValue) == 0.5f, "large finite health values do not overflow their total");
        Check(DomainTerritoryPartition.CalculateFraction(1f, 0f) == 1f, "remaining domain pushes the boundary to the map edge");
        bool rejected = false;
        try { DomainTerritoryPartition.CalculateFraction(float.NaN, 1f); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "invalid health is rejected instead of corrupting territory");
        foreach (bool reverse in new[] { false, true })
            foreach (float fraction in new[] { 0f, 0.01f, 0.05f, 0.1f, 0.11f, 0.39f, 0.7f, 0.99f, 1f })
            {
                DomainTerritoryPartition partition = new(10, 10, fraction, reverse);
                HashSet<IntVec3> first = new();
                HashSet<IntVec3> second = new();
                for (int x = 0; x < 10; x++)
                    for (int z = 0; z < 10; z++)
                    {
                        IntVec3 cell = new(x, 0, z);
                        (partition.IsFirst(cell) ? first : second).Add(cell);
                    }
                Check(first.Count == partition.FirstCells && second.Count == partition.SecondCells
                    && first.Count + second.Count == partition.CellCount && !first.Overlaps(second),
                    "fixed map retains every cell exactly once at fraction " + fraction + ", reverse=" + reverse);
                Check(Connected(first) && Connected(second), "both rectangular territories stay connected at fraction " + fraction + ", reverse=" + reverse);
                Check(Enumerable.Range(0, 10).All(x => Enumerable.Range(0, 10).All(z =>
                    partition.IsFirst(new IntVec3(x, 0, z)) == partition.IsFirst(new IntVec3(x, 0, 0)))),
                    "every row shares one straight boundary at fraction " + fraction + ", reverse=" + reverse);
            }
        DomainTerritoryPartition moving = new(60, 60, 0.5f, false);
        float old = moving.Advance(3000f, 1000f, 0.25f);
        Check(old == 30f && Math.Abs(moving.BoundaryX - 30.25f) < 0.0001f && moving.FirstCells == 1800,
            "straight boundary moves inside a cell without consuming individual cells");
        moving.Advance(3000f, 1000f, 0.25f);
        moving.Advance(3000f, 1000f, 0.25f);
        Check(Math.Abs(moving.BoundaryX - 30.75f) < 0.0001f && moving.FirstCells == 1860,
            "crossing a column center changes every row together");
        for (int tick = 0; tick < 120; tick++) moving.Advance(3000f, 1000f, 0.25f);
        Check(moving.FirstFraction == 0.75f && moving.BoundaryX == 45f, "boundary converges to the exact health ratio without overshooting");
        moving.Advance(1000f, 3000f, 0.5f);
        Check(Math.Abs(moving.BoundaryX - 44.5f) < 0.0001f, "damage or healing reverses the whole straight boundary");
        DomainTerritoryPartition shallow = new(60, 7, 0.5f, false);
        shallow.Advance(3000f, 1000f, 0.25f);
        Check(Math.Abs(shallow.BoundaryX - 30.25f) < 0.0001f && shallow.CellCount == 420,
            "boundary speed is independent of map height and preserves map size");
        DomainTerritoryPartition reversed = new(60, 60, 0.5f, true);
        reversed.Advance(3000f, 1000f, 0.25f);
        Check(Math.Abs(reversed.BoundaryX - 29.75f) < 0.0001f, "reversed domains move the boundary toward the opposing side");
        for (int tick = 0; tick < 120; tick++) reversed.Advance(0f, 1000f, 0.5f);
        Check(reversed.BoundaryX == 60f && reversed.SecondCells == 3600, "a defeated domain leaves all original map cells to the survivor");
        CheckPersistence(moving);
        CheckInstances();
        Console.WriteLine("Domain contest checks passed: " + checks);
    }

    private static void CheckInstances()
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.info = new MapInfo { Size = new IntVec3(10, 1, 10) };
        MapComponent_Domain first = new(map);
        MapComponent_Domain second = new(map, true);
        map.components = new List<MapComponent> { first };
        Pawn firstCaster = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        Pawn secondCaster = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        first.Configure(new DomainDef { defName = "CQF_Check_ContestFirst", workerClass = typeof(DomainWorker_Visual), maximumHealth = 3000f });
        second.Configure(new DomainDef { defName = "CQF_Check_ContestSecond", workerClass = typeof(DomainWorker_Visual), maximumHealth = 1000f });
        Set(first, "active", true); Set(second, "active", true);
        Set(first, "caster", firstCaster); Set(second, "caster", secondCaster);
        Set(first, "health", 3000f); Set(second, "health", 1000f);
        Set(first, "challenger", second);
        CheckInstanceQueries(first, second);
        Check(first.HasActiveDomain && !first.Available && !first.CanAcceptChallenger, "shared battlefield remains busy and refuses a third domain");
        Check(first.FindDomainFor(firstCaster) == first && first.FindDomainFor(secondCaster) == second,
            "caster lookup resolves two independent domain instances on one map");
        Check(first.Worker != second.Worker && second.Worker!.Domain == second && second.map == first.map,
            "both domain workers share the battlefield while retaining separate instance state");
        Check(first.HasOtherActiveDomain(second) && first.HasOtherActiveDomain(first), "returning either participant detects the remaining domain");
        Building_Domain anchor = new() { def = new ThingDef() };
        anchor.Initialize(map); anchor.BindDomain(second);
        Check(anchor.Domain == second && anchor.HitPoints == 1000, "challenger anchor resolves its own shared health rather than the first domain");
        second.DrainHealth(250f);
        Check(second.Health == 750f && first.Health == 3000f, "damage to one domain leaves the opposing health unchanged");
        Set(first, "active", false);
        Check(first.HasActiveDomain && !first.Available && !first.HasOtherActiveDomain(second), "challenger victory keeps the shared battlefield active after the first domain ends");
        Set(second, "active", false);
        Check(!first.HasActiveDomain && !first.HasOtherActiveDomain(second), "shared battlefield becomes reclaimable only after both domains end");
        CheckInstancePersistence(first, second, map, firstCaster, secondCaster, anchor);
    }

    private static void CheckInstanceQueries(MapComponent_Domain first, MapComponent_Domain second)
    {
        DomainContest contest = new();
        DomainTerritoryPartition partition = new(10, 10, 0.5f, false);
        typeof(DomainContest).GetField("partition", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(contest, partition);
        Set(first, "contest", contest);
        IntVec3 left = new(2, 0, 5), right = new(7, 0, 5);
        Check(first.DomainAt(left) == first && second.DomainAt(left) == first
            && first.DomainAt(right) == second && second.DomainAt(right) == second,
            "both instance entry points resolve the same natural territory owner");
        Check(second.FindDomainFor(first.Caster) == first && second.FindDomainFor(second.Caster) == second
            && second.FindDomainFor(null) == first && second.FindDomainFor(new Pawn()) == null,
            "challenger caster lookup uses the shared battlefield including null and unrelated pawns");
        Check(second.HasActiveDomain && second.HasOtherActiveDomain(second) && second.HasOtherActiveDomain(first)
            && second.GetActiveWorker<DomainWorker_Visual>() == first.Worker,
            "battlefield activity and worker lookup are independent of the calling instance");
        partition.Capture(right, null);
        partition.Advance(first.Health, second.Health, 10f);
        Check(second.DomainAt(right) == first && first.OwnsCell(right) && !second.OwnsCell(right),
            "challenger query agrees with a captured island and caster ownership");
        partition.Protect(true, new[] { right }, 100);
        partition.Advance(1f, 1000f, 10f);
        Check(second.DomainAt(right) == first && second.DomainAt(IntVec3.Invalid) == null,
            "protected ownership and out-of-bounds queries agree from either side");
        Set(first, "active", false);
        Check(first.DomainAt(left) == second && second.DomainAt(left) == second && second.HasActiveDomain
            && second.GetActiveWorker<DomainWorker_Visual>() == second.Worker && !second.HasOtherActiveDomain(second),
            "challenger victory resolves every query to the surviving domain");
        Set(first, "active", true); Set(second, "active", false);
        Check(second.DomainAt(right) == first && second.HasActiveDomain && second.HasOtherActiveDomain(second)
            && second.GetActiveWorker<DomainWorker_Visual>() == first.Worker,
            "dissolved challenger still queries the surviving primary domain");
        Set(first, "active", false);
        Check(first.DomainAt(left) == null && second.DomainAt(right) == null && !second.HasActiveDomain
            && second.GetActiveWorker<DomainWorker_Visual>() == null,
            "both inactive instances report an empty battlefield");
        Set(first, "active", true); Set(second, "active", true); Set(first, "contest", null!);
    }

    private static void CheckInstancePersistence(MapComponent_Domain first, MapComponent_Domain second, Map map, Pawn firstCaster, Pawn secondCaster, Building_Domain anchor)
    {
        Set(first, "active", true); Set(second, "active", true);
        firstCaster.def = new ThingDef { defName = "CQF_Check_ContestPawn" };
        secondCaster.def = firstCaster.def;
        firstCaster.thingIDNumber = 9101; secondCaster.thingIDNumber = 9102;
        anchor.def.defName = "CQF_Check_ContestAnchor";
        anchor.thingIDNumber = 9103;
        Set(second, "anchor", anchor);
        DefDatabase<DomainDef>.Add(first.Definition);
        DefDatabase<DomainDef>.Add(second.Definition);
        TerrainDef terrain = new() { defName = "CQF_Check_ContestTerrain" };
        DefDatabase<TerrainDef>.Add(terrain);
        DomainTerrainSnapshot snapshot = new();
        typeof(DomainTerrainSnapshot).GetField("width", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(snapshot, 10);
        typeof(DomainTerrainSnapshot).GetField("height", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(snapshot, 10);
        typeof(DomainTerrainSnapshot).GetField("terrains", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(snapshot, Enumerable.Repeat(terrain, 100).ToList());
        DomainContest contest = new();
        typeof(DomainContest).GetField("firstTerrain", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(contest, snapshot);
        typeof(DomainContest).GetField("secondTerrain", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(contest, snapshot);
        typeof(DomainContest).GetField("partition", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(contest, new DomainTerritoryPartition(10, 10, 0.36f, true));
        Set(first, "contest", contest);
        string path = Path.Combine(Path.GetTempPath(), "CQF_ContestInstances_" + Guid.NewGuid().ToString("N") + ".xml");
        MapComponent_Domain? value = first;
        try
        {
            for (int pass = 0; pass < 2; pass++)
            {
                Scribe.saver.InitSaving(path, "contestChecks");
                Scribe_Deep.Look(ref value, "battlefield", map);
                Scribe.saver.FinalizeSaving();
                value = null;
                Scribe.loader.InitLoading(path);
                LoadedObjectDirectory directory = (LoadedObjectDirectory)typeof(CrossRefHandler)
                    .GetField("loadedObjectDirectory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(firstCaster); directory.RegisterLoaded(secondCaster); directory.RegisterLoaded(anchor);
                Scribe_Deep.Look(ref value, "battlefield", map);
                Scribe.loader.FinalizeLoading();
                map.components = new List<MapComponent> { value! };
                Check(value!.Caster == firstCaster && value.Challenger!.Caster == secondCaster
                    && value.Health == (pass == 0 ? 3000f : 0f) && value.Challenger.Health == 750f, "native Scribe restores independent instance health and caster references, pass " + pass);
                Check(value.Challenger!.Anchor == anchor && anchor.Domain == value.Challenger
                    && value.Challenger.Worker!.Domain == value.Challenger, "native Scribe rebuilds challenger worker and anchor bindings, pass " + pass);
                Check(value.Contest!.Partition.FirstFraction == 0.36f && Math.Abs(value.Contest.Partition.BoundaryX - 6.4f) < 0.0001f
                    && value.Contest.Partition.IsFirst(new IntVec3(9, 0, 0)),
                    "native Scribe restores snapshots and border orientation, pass " + pass);
                if (pass == 0)
                {
                    Set(value, "active", false);
                    Set(value, "trapped", true);
                    Set(value, "health", 0f);
                }
                else Check(value.Trapped && !value.Active && !value.Available && value.Challenger.Active,
                    "native Scribe preserves captive caster identity without restoring a dissolved domain");
            }
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop();
            File.Delete(path);
        }
    }

    private static void CheckPersistence(DomainTerritoryPartition original)
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_Contest_" + Guid.NewGuid().ToString("N") + ".xml");
        DomainTerritoryPartition? value = original;
        try
        {
            Scribe.saver.InitSaving(path, "contestChecks");
            Scribe_Deep.Look(ref value, "territory");
            Scribe.saver.FinalizeSaving();
            value = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref value, "territory");
            Scribe.loader.FinalizeLoading();
            Check(value != null && value.FirstFraction == original.FirstFraction && value.BoundaryX == original.BoundaryX
                && value.Reversed == original.Reversed && value.CellCount == original.CellCount,
                "native Scribe preserves the moving boundary across a save and load");
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop();
            File.Delete(path);
        }
    }

    private static bool Connected(HashSet<IntVec3> cells)
    {
        if (cells.Count == 0) return true;
        HashSet<IntVec3> visited = new();
        Queue<IntVec3> pending = new();
        pending.Enqueue(cells.First());
        while (pending.Count > 0)
        {
            IntVec3 cell = pending.Dequeue();
            if (!visited.Add(cell)) continue;
            foreach (IntVec3 offset in GenAdj.CardinalDirections)
                if (cells.Contains(cell + offset) && !visited.Contains(cell + offset)) pending.Enqueue(cell + offset);
        }
        return visited.Count == cells.Count;
    }

    private static void Set(object target, string field, object value)
        => typeof(MapComponent_Domain).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, value);

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static int checks;
}
