using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LudeonTK;
using QuestEditor_Library;
using Verse;

internal static class MapManagerLifetimeChecks
{
    public static void Run()
    {
        Game previousGame = Current.Game;
        ProgramState previousState = Current.ProgramState;
        FieldInfo factory = AccessTools.Field(typeof(CQFModuleLoader), "mapModuleFactory");
        object? previousFactory = factory.GetValue(null);
        Harmony harmony = new("CQF.Checks.MapLifetime");
        harmony.Patch(AccessTools.Method(typeof(Log), nameof(Log.Error)), prefix: new HarmonyMethod(typeof(MapManagerLifetimeChecks), nameof(CaptureError)));
        harmony.Patch(AccessTools.Method(typeof(Map), nameof(Map.Dispose)), prefix: new HarmonyMethod(typeof(MapManagerLifetimeChecks), nameof(DisposeMap)));
        harmony.Patch(AccessTools.Method(typeof(Game), nameof(Game.Dispose)), prefix: new HarmonyMethod(typeof(Patch_MapHibernationDispose), nameof(Patch_MapHibernationDispose.Prefix)));
        try
        {
            factory.SetValue(null, null);
            Current.Game = null!;
            Current.ProgramState = ProgramState.Entry;
            DebugActionNode root = DebugActions_MapHibernation.MapHibernation();
            Check(root.childGetter().Count == 0 && !root.VisibleNow, "main-menu debug enumeration is safe and hides map hibernation");
            Game missing = CreateGame();
            Current.Game = missing;
            Current.ProgramState = ProgramState.Playing;
            Check(root.childGetter().Count == 0 && !root.VisibleNow && missing.components.Count == 0,
                "missing gameplay component is diagnosed without creating replacement state");
            Check(errors.Any(error => error.Contains("Map hibernation component is unavailable")), "missing gameplay component retains its diagnostic");

            Game first = CreateGame();
            Current.Game = first;
            GameComponent_MapManager firstManager = new(first);
            first.components.Add(firstManager);
            Map selected = CreateMap(1);
            first.Maps.Add(selected);
            DebugActionNode command = root.childGetter().Single();
            Check(root.VisibleNow && command.VisibleNow, "current-game map commands are visible");
            Game replacement = CreateGame();
            Current.Game = replacement;
            GameComponent_MapManager replacementManager = new(replacement);
            replacement.components.Add(replacementManager);
            command.action();
            Check(!command.VisibleNow && !firstManager.IsHibernateRequested(selected), "cached debug callbacks cannot mutate a previous game");
            Current.Game = first;
            command.action();
            Check(firstManager.IsHibernateRequested(selected), "current-game debug callbacks still queue hibernation");
            firstManager.NotifyMapActivity(selected);
            firstManager.DisposeMaps();
            command.action();
            Check(GameComponent_MapManager.Instance == null && !root.VisibleNow && !firstManager.IsHibernateRequested(selected),
                "disposed managers remain unavailable while Current.Game still references the exiting game");

            MapDisposalProbeModule module = new();
            CQFModuleLoader.RegisterMapModuleFactory(_ => module);
            Game exiting = CreateGame();
            Current.Game = exiting;
            GameComponent_MapManager manager = new(exiting);
            exiting.components.Add(manager);
            Map active = CreateMap(10);
            Map broken = CreateMap(20);
            Map sleeping = CreateMap(30);
            exiting.Maps.Add(active);
            ((List<Map>)AccessTools.Field(typeof(GameComponent_MapManager), "hibernatingMaps").GetValue(manager)!).AddRange(new[] { broken, sleeping });
            exiting.Dispose();
            Check(disposedMaps.Contains(broken) && disposedMaps.Contains(sleeping), "one detached map cleanup failure does not skip later maps");
            Check(disposedMaps.Contains(active), "the real native Game.Dispose continues after CQF module and detached-map cleanup failures");
            Check(errors.Any(error => error.Contains("CQF_Check_ModuleDisposalFailure")) && errors.Any(error => error.Contains("CQF_Check_MapDisposalFailure")),
                "both cleanup failures retain complete diagnostic exceptions");
            exiting.Dispose();
            Check(module.Disposals == 1 && !manager.HasHibernatingMaps && GameComponent_MapManager.Instance == null,
                "repeated game disposal does not repeat CQF cleanup or revive its cached manager");
            Console.WriteLine("Map manager lifetime checks passed.");
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            factory.SetValue(null, previousFactory);
            Current.Game = previousGame;
            Current.ProgramState = previousState;
        }
    }

    private static Game CreateGame()
    {
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        game.components = new();
        AccessTools.Field(typeof(Game), "maps").SetValue(game, new List<Map>());
        return game;
    }

    private static Map CreateMap(int id)
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.uniqueID = id;
        return map;
    }

    private static bool CaptureError(string text)
    {
        errors.Add(text);
        Console.WriteLine("EXPECTED " + text);
        return false;
    }

    private static bool DisposeMap(Map __instance)
    {
        disposedMaps.Add(__instance);
        if (__instance.uniqueID == 20) throw new InvalidOperationException("CQF_Check_MapDisposalFailure");
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }

    private static readonly List<string> errors = new();
    private static readonly List<Map> disposedMaps = new();
}
