using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using Verse;

internal static class HibernationSchedulingChecks
{
    public static void Run()
    {
        Game previous = Current.Game;
        ProgramState previousState = Current.ProgramState;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        game.components = new();
        game.currentMapIndex = -1;
        game.tickManager = new TickManager();
        Current.Game = game;
        Current.ProgramState = ProgramState.Entry;
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        content.assemblies = new ModAssemblyHandler(content);
        content.assemblies.loadedAssemblies.Add(typeof(GameComponent_MapManager).Assembly);
        content.assemblies.loadedAssemblies.Add(typeof(HibernationSchedulingChecks).Assembly);
        LoadedModManager.RunningModsListForReading.Add(content);
        GenTypes.ClearCache();
        Map active = CreateMap(1);
        Map sleeping = CreateMap(2);
        Map tail = CreateMap(3);
        Map empty = CreateMap(4);
        List<Map> maps = new() { active, sleeping, tail, empty };
        typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, maps);
        for (int i = 0; i < maps.Count; i++) SetFixtureRegionIndex(maps[i], (sbyte)i);
        GameComponent_MapManager manager = new(game);
        game.components.Add(manager);
        Harmony harmony = new("CQF.Checks.HibernationScheduling");
        harmony.Patch(typeof(Log).GetMethod(nameof(Log.Error)), prefix: new HarmonyMethod(typeof(ChecksLogHandler).GetMethod(nameof(ChecksLogHandler.RecordError))));
        harmony.Patch(AccessTools.Method(typeof(TickManager), nameof(TickManager.RemoveAllFromMap)),
            prefix: Hook(nameof(CountTickRemoval)));
        harmony.Patch(AccessTools.Method(typeof(Map), nameof(Map.ExposeData)), prefix: Hook(nameof(ExposeFixtureMap)));
        harmony.Patch(AccessTools.Method(typeof(Map), nameof(Map.FinalizeLoading)), prefix: Hook(nameof(FinalizeFixtureMap)));
        try
        {
            PauseProbeThing activeProbe = SpawnProbe(active, TickerType.Normal, 10);
            PauseProbeThing tailProbe = SpawnProbe(tail, TickerType.Normal, 30);
            PauseProbeThing[] probes = new[] { TickerType.Normal, TickerType.Rare, TickerType.Long }
                .Select((type, index) => SpawnProbe(sleeping, type, 20 + index)).ToArray();
            game.currentMapIndex = 2;
            manager.Hibernate(sleeping);
            Check(manager.IsHibernateRequested(sleeping) && !manager.IsHibernating(sleeping), "requests defer until component update");
            manager.GameComponentUpdate();
            Check(maps.SequenceEqual(new[] { active, tail, empty }) && manager.TotalMapCount == 4,
                "hibernating map leaves native Maps and remains owned by the single manager");
            Check(probes.All(probe => !probe.Spawned && probe.Map == null && !probe.Destroyed),
                "hibernating things are natively unspawned with null Map and are not destroyed");
            Check(Regions(sleeping).All(region => region.Map == null) && sleeping.regionGrid.allDistricts.All(district => district.Map == null),
                "hibernating regions and districts have no active map");
            Check(tailProbe.Map == tail && tailProbe.Spawned && Regions(tail).All(region => region.Map == tail)
                && tail.regionGrid.allDistricts.All(district => district.Map == tail),
                "removing a middle map fixes later thing region and district indices once");
            Check(game.CurrentMap == tail && game.currentMapIndex == 1,
                "detaching an earlier map keeps selection on the same active map");
            game.currentMapIndex = -1;
            Check(activeProbe.Map == active && !sleeping.regionAndRoomUpdater.Enabled, "earlier map is untouched and detached region updater stops");
            bool canFire = true;
            Check(!Patch_DisableIncident.prefix(null!, ref canFire, new IncidentParms { target = sleeping }) && !canFire,
                "existing incident entry rejects an explicitly supplied hibernating target");
            Tick(2100);
            Check(probes.All(probe => probe.Ticks == 0) && activeProbe.Ticks == 2100 && tailProbe.Ticks == 2100,
                "native normal rare long tick lists stop detached things while active maps continue");
            manager.Activate(sleeping);
            Check(maps.Last() == sleeping && probes.All(probe => probe.Spawned && probe.Map == sleeping)
                && Regions(sleeping).All(region => region.Map == sleeping), "activation appends the same map and restores native indices");
            Tick(2100);
            Check(probes[0].Ticks == 2100 && probes[1].Ticks >= 8 && probes[2].Ticks >= 1 && activeProbe.Ticks == 4200,
                "all native ticker types resume once after activation");
            manager.Activate(sleeping);
            Tick(10);
            Check(probes[0].Ticks == 2110, "repeated activation does not duplicate registrations");
            manager.Hibernate(sleeping);
            manager.NotifyMapActivity(sleeping);
            manager.GameComponentUpdate();
            Check(!manager.IsHibernating(sleeping), "activity on an active map cancels a pending request");
            manager.Hibernate(sleeping);
            manager.GameComponentUpdate();
            manager.NotifyMapActivity(sleeping);
            Check(manager.IsHibernating(sleeping) && !maps.Contains(sleeping), "native activity notification does not implicitly wake a detached map");
            manager.Activate(sleeping);
            CheckSaving(game, manager, sleeping, empty);
            GameComponent_MapManager restored = GameComponent_MapManager.Instance!;
            int beforeEmpty = tickRemovalCalls;
            restored.Activate(empty);
            restored.Hibernate(empty);
            restored.GameComponentUpdate();
            restored.Activate(empty);
            Check(tickRemovalCalls == beforeEmpty, "empty map detach and wake never scan global thing tick lists");
            restored.Hibernate(empty);
            restored.GameComponentUpdate();
            for (int i = 0; i < 16; i++) restored.GameComponentUpdate();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 256; i++) restored.GameComponentUpdate();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(allocated == 0, "hibernating map maintenance allocates zero bytes across 256 idle updates (actual: " + allocated + ")");
            restored.ForgetMap(empty);
            Check(!restored.IsHibernating(empty), "map disposal notification releases manager ownership");
            Map[] registered = maps.ToArray();
            maps.Clear();
            maps.AddRange(Enumerable.Repeat(active, 128));
            bool rejected = false;
            try { GameTools.GenerateSubMap(IntVec3.Zero, null!, null!, Array.Empty<GenStepWithParams>(), active); }
            catch (InvalidOperationException error) { rejected = error.Message.Contains("map index limit"); }
            finally { maps.Clear(); maps.AddRange(registered); }
            Check(rejected, "CQF map capacity guard runs without an AddMap patch");
            Check(!typeof(GameComponent_MapManager).Assembly.GetTypes().Any(type => type.Name is
                "Patch_HibernatingThingMap" or "Patch_HibernatingThingSpawned" or "Patch_HibernatingRegionMap"
                or "Patch_HibernatingThingTick" or "Patch_MapHibernationSave" or "Patch_MapHibernationMapLimit"
                or "Patch_HibernatingMapTick" or "Patch_HibernatingTickRegistration" or "Patch_HibernatingMapRemoval"
                or "Patch_ActivateMapOnSpawn" or "Patch_ActivateMapOnDespawn"), "object attributes map ticking registration saving and spawn/despawn need no hibernation patches");
            restored.Activate(active);
            CheckStoredFormats(game);
            Check(ChecksLogHandler.ErrorCount == 0, "native scheduling and Scribe ownership checks log no errors");
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            Scribe.ForceStop();
            Current.ProgramState = previousState;
            Current.Game = previous;
        }
    }

    private static void CheckSaving(Game game, GameComponent_MapManager manager, Map sleeping, Map pending)
    {
        manager.Hibernate(sleeping);
        manager.GameComponentUpdate();
        pending.regionAndRoomUpdater.Enabled = false;
        manager.Hibernate(pending);
        Map[] before = game.Maps.ToArray();
        string file = Path.Combine(Path.GetTempPath(), "CQF_DetachedMapChecks_" + Guid.NewGuid().ToString("N") + ".xml");
        Scribe.saver.InitSaving(file, "checks");
        Scribe_Deep.Look(ref manager, "manager", game);
        foreach (Map map in before) Scribe.saver.loadIDsErrorsChecker.RegisterDeepSaved(map, "externalMapFixture");
        manager.GameComponentUpdate();
        Check(manager.IsHibernateRequested(pending) && game.Maps.SequenceEqual(before),
            "save preserves active list and does not flush pending requests");
        Scribe.saver.FinalizeSaving();
        XDocument xml = XDocument.Load(file);
        Check(xml.Descendants("CQF_ownedHibernatingMaps").Elements("li").Single().Element("map")!.Element("uniqueID")!.Value == "2"
            && xml.Descendants("CQF_pendingHibernation").Elements("li").Single().Value == pending.GetUniqueLoadID(),
            "Scribe owns detached map data and separately references pending active maps");
        Check(savedUnspawned && savedRegionsValid && !sleeping.spawnedThings[0].Spawned && !game.Maps.Contains(sleeping),
            "map serialization scope supplies regional context without spawning things or retaining active membership");
        int initialFinalizations = finalizations;
        World world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        world.factionManager = new FactionManager();
        typeof(Game).GetField("worldInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, world);
        Scribe.loader.InitLoading(file);
        LoadedObjectDirectory directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
        foreach (Map map in before) directory.RegisterLoaded(map);
        GameComponent_MapManager loaded = null!;
        Scribe_Deep.Look(ref loaded, "manager", game);
        Scribe.loader.FinalizeLoading();
        game.components.Clear();
        game.components.Add(loaded);
        loaded.LoadedGame();
        Map detached = loaded.HibernatingMaps.Single(map => map.uniqueID == sleeping.uniqueID);
        Check(!ReferenceEquals(detached, sleeping) && !game.Maps.Contains(detached) && loaded.IsHibernating(pending)
            && detached.spawnedThings.Count == 3 && detached.spawnedThings.All(thing => !thing.Spawned && thing.Map == null),
            "Scribe reconstructs manager-owned map fixtures and restores all pending and detached states");
        Check(finalizations == initialFinalizations + 1 && finalizedBeforeDetach,
            "loaded map initialization and its queued callbacks finish before detaching again");
        loaded.Activate(detached);
        loaded.Activate(pending);
        Check(detached.regionAndRoomUpdater.Enabled && !pending.regionAndRoomUpdater.Enabled,
            "save/load retains the original region updater settings");
        Check(detached.spawnedThings.OfType<PauseProbeThing>().First(probe => probe.def.tickerType == TickerType.Normal).Ticks == 2110,
            "native Thing serialization retains detached object identity and probe state");
        game.currentMapIndex = -1;
        foreach (Map map in game.Maps.ToArray()) loaded.Hibernate(map);
        loaded.GameComponentUpdate();
        Check(game.Maps.Count == 0 && game.CurrentMap == null && loaded.HibernatingMaps.Count == 4,
            "all maps can leave the native list while remaining component-owned");
        string allFile = Path.ChangeExtension(file, ".all.xml");
        Scribe.saver.InitSaving(allFile, "checks");
        Scribe_Deep.Look(ref loaded, "manager", game);
        Scribe.saver.FinalizeSaving();
        Check(game.Maps.Count == 0 && XDocument.Load(allFile).Descendants("CQF_ownedHibernatingMaps").Elements("li").Count() == 4,
            "all-detached save owns every map without retaining a map in the native list");
        Console.WriteLine("Detached map fixture save: " + file);
    }

    private static void CheckStoredFormats(Game game)
    {
        Map[] before = game.Maps.ToArray();
        int errors = ChecksLogHandler.ErrorCount;
        string file = Path.Combine(Path.GetTempPath(), "CQF_MapSchemaChecks_" + Guid.NewGuid().ToString("N") + ".xml");
        File.WriteAllText(file, "<checks><manager><CQF_hibernatingMaps><li>Map_1</li></CQF_hibernatingMaps></manager></checks>");
        Scribe.loader.InitLoading(file);
        GameComponent_MapManager loaded = null!;
        Scribe_Deep.Look(ref loaded, "manager", game);
        Scribe.loader.FinalizeLoading();
        loaded.LoadedGame();
        Check(loaded.HibernatingMaps.Count == 0 && game.Maps.SequenceEqual(before),
            "legacy reference-only node is not interpreted as owned map data and native maps remain active");
        Check(ChecksLogHandler.ErrorCount == errors, "legacy field collision produces no missing-map errors");
        loaded.DisposeMaps();
        File.WriteAllText(file, "<checks><manager><CQF_ownedHibernatingMaps><li /></CQF_ownedHibernatingMaps></manager></checks>");
        Scribe.loader.InitLoading(file);
        loaded = null!;
        Scribe_Deep.Look(ref loaded, "manager", game);
        Scribe.loader.FinalizeLoading();
        bool rejected = false;
        try { loaded.LoadedGame(); }
        catch (InvalidOperationException error) { rejected = error.Message.Contains("CQF_ownedHibernatingMaps"); }
        finally { loaded.DisposeMaps(); }
        Check(rejected, "malformed owned-map data still raises an explicit diagnostic instead of being discarded");
    }

    private static Map CreateMap(int id)
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        ConfigureFixtureMap(map, id);
        return map;
    }

    private static void ConfigureFixtureMap(Map map, int id)
    {
        map.uniqueID = id;
        map.info = new MapInfo { Size = new IntVec3(2, 1, 2) };
        map.events = new MapEvents(map);
        map.spawnedThings = new ThingOwner<Thing>(map);
        map.regionAndRoomUpdater = new RegionAndRoomUpdater(map) { Enabled = true };
        map.components = new();
        map.cellIndices = new CellIndices(map);
        map.regionGrid = new RegionGrid(map);
        Region[] grid = (Region[])typeof(RegionGrid).GetField("regionGrid", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(map.regionGrid)!;
        grid[0] = (Region)RuntimeHelpers.GetUninitializedObject(typeof(Region));
        map.regionGrid.allDistricts.Add(new District());
    }

    private static PauseProbeThing SpawnProbe(Map map, TickerType ticker, int id)
    {
        string name = "CQF_Check_Pause" + ticker;
        ThingDef? definition = DefDatabase<ThingDef>.GetNamedSilentFail(name);
        if (definition == null)
        {
            definition = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            definition.defName = name;
            definition.tickerType = ticker;
            definition.stackLimit = 1;
            definition.category = ThingCategory.Item;
            DefDatabase<ThingDef>.Add(definition);
        }
        PauseProbeThing probe = new() { def = definition, thingIDNumber = id };
        map.spawnedThings.TryAdd(probe, canMergeWithExistingStacks: false);
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(probe, (sbyte)map.Index);
        Find.TickManager.RegisterAllTickabilityFor(probe);
        return probe;
    }

    private static bool ExposeFixtureMap(Map __instance)
    {
        int id = __instance.uniqueID;
        Scribe_Values.Look(ref id, "uniqueID", -1);
        if (Scribe.mode == LoadSaveMode.LoadingVars) ConfigureFixtureMap(__instance, id);
        List<PauseProbeThing> probes = __instance.spawnedThings.OfType<PauseProbeThing>().ToList();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            savedUnspawned = probes.All(probe => !probe.Spawned && probe.Map == null);
            savedRegionsValid = Regions(__instance).All(region => region.Map == __instance);
        }
        Scribe_Collections.Look(ref probes, "things", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
            foreach (PauseProbeThing probe in probes) __instance.spawnedThings.TryAdd(probe, canMergeWithExistingStacks: false);
        return false;
    }

    private static bool FinalizeFixtureMap(Map __instance)
    {
        finalizations++;
        SetFixtureRegionIndex(__instance, (sbyte)__instance.Index);
        foreach (Thing probe in __instance.spawnedThings)
        {
            typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(probe, (sbyte)__instance.Index);
            Find.TickManager.RegisterAllTickabilityFor(probe);
        }
        LongEventHandler.ExecuteWhenFinished(() =>
            finalizedBeforeDetach = Current.Game.Maps.Contains(__instance) && Regions(__instance).All(region => region.Map == __instance));
        return false;
    }

    private static IEnumerable<Region> Regions(Map map) => map.regionGrid.AllRegions_NoRebuild_InvalidAllowed;

    private static void SetFixtureRegionIndex(Map map, sbyte index)
    {
        foreach (Region region in Regions(map)) region.mapIndex = index;
        foreach (District district in map.regionGrid.allDistricts) district.mapIndex = index;
    }

    private static void Tick(int count)
    {
        TickList[] lists = new[] { "tickListNormal", "tickListRare", "tickListLong" }
            .Select(name => (TickList)typeof(TickManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Find.TickManager)!).ToArray();
        for (int i = 0; i < count; i++)
        {
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 1);
            foreach (TickList list in lists) list.Tick();
        }
    }

    private static HarmonyMethod Hook(string name) => new(typeof(HibernationSchedulingChecks).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!);

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    private static void CountTickRemoval() => tickRemovalCalls++;

    private static int tickRemovalCalls;
    private static int finalizations;
    private static bool savedUnspawned;
    private static bool savedRegionsValid;
    private static bool finalizedBeforeDetach;
}
