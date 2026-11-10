using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld.Planet;
using Verse;

internal static class DomainOpeningFusionChecks
{
    public static void Run()
    {
        Game previous = Current.Game;
        FieldInfo prefsField = AccessTools.Field(typeof(Prefs), "data");
        object? previousPrefs = prefsField.GetValue(null);
        prefsField.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefsField.FieldType));
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        content.assemblies = new ModAssemblyHandler(content);
        content.assemblies.loadedAssemblies.Add(typeof(CQFDomainRuntime).Assembly);
        content.assemblies.loadedAssemblies.Add(typeof(GameComponent_MapManager).Assembly);
        LoadedModManager.RunningModsListForReading.Add(content);
        GenTypes.ClearCache();
        Harmony capture = new("CQF.Checks.OpeningFusions");
        capture.Patch(AccessTools.Method(typeof(Log), nameof(Log.Error)), prefix: new HarmonyMethod(typeof(DomainOpeningFusionChecks), nameof(CaptureError)));
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        List<Map> maps = new();
        AccessTools.Field(typeof(Game), "maps").SetValue(game, maps);
        Current.Game = game;
        Map source = CreateMap();
        source.components.Add(new MapComponent_Domain(source));
        maps.Add(source);
        FusionProbeOpening opening = new();
        AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(opening, (sbyte)0);
        GameComponent_MapManager host = new(game);
        try
        {
            CQFDomainRuntime empty = CreateRuntime(host, opening);
            Process(empty);
            Check(errors.Count == 0 && Pool(empty).Count == 0, "empty pool is valid while an opening is active");
            CQFDomainRuntime missingPool = CreateRuntime(host, opening);
            PoolField.SetValue(missingPool, null);
            Process(missingPool);
            Check(Pool(missingPool).Count == 0 && errors.Count == 1 && errors[0].EndsWith("domainMaps=null"),
                "missing pool is initialized with an explicit diagnostic");
            Process(missingPool);
            Check(errors.Count == 1, "repeated fusion checks do not repeat a repaired pool diagnostic");
            errors.Clear();
            CQFDomainRuntime missingEntry = CreateRuntime(host, opening);
            Map valid = CreateMap();
            valid.components.Add(new MapComponent_Domain(valid));
            maps.Add(valid);
            Pool(missingEntry).Add(null!);
            Pool(missingEntry).Add(valid);
            Process(missingEntry);
            Check(Pool(missingEntry).SequenceEqual(new[] { valid }) && errors.Count == 1 && errors[0].EndsWith("null=1"),
                "missing map reference is reported and valid pool entries survive");
            errors.Clear();
            CQFDomainRuntime detached = CreateRuntime(host, opening);
            Map sleeping = CreateMap();
            sleeping.components = null!;
            Pool(detached).Add(sleeping);
            Process(detached);
            Check(errors.Count == 0 && Pool(detached).Contains(sleeping), "fusion checks do not inspect or remove detached pooled maps");
            CQFDomainRuntime disposed = CreateRuntime(host, opening);
            Map destroyed = CreateMap();
            destroyed.components = null!;
            AccessTools.PropertySetter(typeof(Map), nameof(Map.Disposed)).Invoke(destroyed, new object[] { true });
            maps.Add(destroyed);
            Pool(disposed).Add(destroyed);
            Process(disposed);
            Check(errors.Count == 0, "disposed active-map entries are skipped before component access");
            maps.Remove(destroyed);
            CQFDomainRuntime componentMissing = CreateRuntime(host, opening);
            Map incomplete = CreateMap();
            incomplete.uniqueID = 8765;
            maps.Add(incomplete);
            Pool(componentMissing).Add(incomplete);
            Process(componentMissing);
            Check(errors.Count == 1 && errors[0].Contains("MapComponent_Domain=null, map=8765"),
                "missing active-map component receives a precise diagnostic without aborting fusion detection");
            maps.Remove(incomplete);
            errors.Clear();
            CQFDomainRuntime detachedOpening = CreateRuntime(host, opening);
            AccessTools.Field(typeof(Thing), "mapIndexOrState").SetValue(opening, (sbyte)-1);
            Process(detachedOpening);
            Check(errors.Count == 0, "detached opening is excluded even if its custom Active property remains true");
            CheckLoading(host);
            Console.WriteLine("Opening fusion checks passed: " + checks);
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop();
            Current.Game = previous;
            prefsField.SetValue(null, previousPrefs);
            LoadedModManager.RunningModsListForReading.Remove(content);
            GenTypes.ClearCache();
            capture.UnpatchAll("CQF.Checks.OpeningFusions");
        }
    }

    private static void CheckLoading(GameComponent_MapManager host)
    {
        errors.Clear();
        string path = Path.Combine(Path.GetTempPath(), "CQF_FusionReferences_" + Guid.NewGuid().ToString("N") + ".xml");
        CQFDomainRuntime? runtime = new(host);
        Map unresolved = CreateMap();
        unresolved.uniqueID = 876543;
        Pool(runtime).Add(unresolved);
        try
        {
            Scribe.saver.InitSaving(path, "fusionChecks");
            Scribe_Deep.Look(ref runtime, "runtime", host);
            Scribe.saver.FinalizeSaving();
            runtime = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref runtime, "runtime", host);
            Scribe.loader.FinalizeLoading();
            Check(runtime != null && Pool(runtime).Count == 0 && errors.Any(error => error.Contains("CQFDomainRuntime.domainMaps: null=1")),
                "native Scribe resolves a missing saved map reference and post-load cleanup reports it");
            errors.Clear();
            runtime = null;
            Scribe.loader.InitLoading(path);
            LoadedObjectDirectory directory = (LoadedObjectDirectory)AccessTools.Field(typeof(CrossRefHandler), "loadedObjectDirectory")
                .GetValue(Scribe.loader.crossRefs)!;
            directory.RegisterLoaded(unresolved);
            Scribe_Deep.Look(ref runtime, "runtime", host);
            Scribe.loader.FinalizeLoading();
            Check(runtime != null && Pool(runtime).Single() == unresolved && errors.Count == 0,
                "native Scribe preserves a valid domain map reference without a diagnostic");
            runtime = new(host);
            Scribe.saver.InitSaving(path, "fusionChecks");
            Scribe_Deep.Look(ref runtime, "runtime", host);
            Scribe.saver.FinalizeSaving();
            runtime = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref runtime, "runtime", host);
            Scribe.loader.FinalizeLoading();
            Check(runtime != null && Pool(runtime).Count == 0 && errors.Count == 0,
                "native Scribe accepts an empty saved domain pool without a diagnostic");
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop();
            File.Delete(path);
        }
    }

    private static CQFDomainRuntime CreateRuntime(GameComponent_MapManager host, DomainOpening opening)
    {
        CQFDomainRuntime runtime = new(host);
        ((HashSet<DomainOpening>)AccessTools.Field(typeof(CQFDomainRuntime), "activeOpenings").GetValue(runtime)!).Add(opening);
        return runtime;
    }

    private static void Process(CQFDomainRuntime runtime)
    {
        AccessTools.Field(typeof(CQFDomainRuntime), "fusionCheckRequested").SetValue(runtime, true);
        try { AccessTools.Method(typeof(CQFDomainRuntime), "ProcessOpeningFusions").Invoke(runtime, null); }
        catch (TargetInvocationException error) { throw error.InnerException!; }
    }

    private static List<Map> Pool(CQFDomainRuntime runtime) => (List<Map>)PoolField.GetValue(runtime)!;

    private static Map CreateMap()
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.info = new MapInfo { parent = new MapParent() };
        map.components = new List<MapComponent>();
        return map;
    }

    private static bool CaptureError(string text)
    {
        errors.Add(text);
        Console.WriteLine("EXPECTED ERROR " + text);
        return false;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks++;
        Console.WriteLine("PASS " + description);
    }

    private static readonly FieldInfo PoolField = AccessTools.Field(typeof(CQFDomainRuntime), "domainMaps");
    private static readonly List<string> errors = new();
    private static int checks;
}
