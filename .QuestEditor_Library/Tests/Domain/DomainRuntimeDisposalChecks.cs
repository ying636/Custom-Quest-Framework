using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;

internal static class DomainRuntimeDisposalChecks
{
    public static void Run()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        Game previousGame = Current.Game;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        game.components = new();
        Current.Game = game;
        string path = Path.Combine(Path.GetTempPath(), "CQF-EmptyDomainDisposal-" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            GameComponent_MapManager host = new(game);
            CQFDomainRuntime fresh = new(host);
            fresh.Dispose();
            fresh.Dispose();
            Check(true, "unused domain runtime supports repeated disposal");
            File.WriteAllText(path, "<save><runtime /></save>", System.Text.Encoding.UTF8);
            CQFDomainRuntime? aborted = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref aborted, "runtime", host);
            Check(Field("domainMaps").GetValue(aborted) == null && Field("mapTemplates").GetValue(aborted) == null,
                "native Scribe reproduces null collections before load completion");
            aborted!.Dispose();
            Scribe.ForceStop();
            Check(true, "domain cleanup succeeds when loading stops before post-load initialization");

            CQFDomainRuntime? loaded = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "runtime", host);
            Scribe.loader.FinalizeLoading();
            Check(loaded!.DomainMaps.Count == 0 && Field("mapTemplates").GetValue(loaded) != null,
                "empty current-format domain state restores both runtime collections");
            loaded.Dispose();
            Check(true, "empty loaded domain state disposes successfully");

            CQFDomainRuntime runtime = new(host);
            Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.components = new();
            MapComponent_Domain domain = new(map);
            MapComponent_Domain challenger = new(map, true);
            map.components.Add(domain);
            DomainDisposalProbeWorker failing = new() { ThrowOnDispose = true };
            DomainDisposalProbeWorker following = new();
            typeof(MapComponent_Domain).GetField("worker", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(domain, failing);
            typeof(MapComponent_Domain).GetField("worker", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(challenger, following);
            typeof(MapComponent_Domain).GetField("challenger", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(domain, challenger);
            ((List<Map>)Field("domainMaps").GetValue(runtime)!).AddRange(new[] { null!, map });
            bool diagnosed = false;
            try { runtime.Dispose(); }
            catch (AggregateException error) { diagnosed = error.ToString().Contains("CQF_Check_WorkerDisposalFailure"); }
            Check(diagnosed && failing.Disposals == 1 && following.Disposals == 1,
                "one worker cleanup failure is diagnosed while the challenger is still released");
            Check(runtime.DomainMaps.Count == 0, "failed resource cleanup still clears domain ownership");
            runtime.Dispose();
            Check(failing.Disposals == 1 && following.Disposals == 1, "domain resource cleanup runs only once after failure");
            Console.WriteLine("Domain runtime disposal checks passed.");
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive) Scribe.ForceStop();
            Current.Game = previousGame;
            File.Delete(path);
        }
    }

    private static FieldInfo Field(string name) => typeof(CQFDomainRuntime).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }
}
