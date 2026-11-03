using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using Verse;

internal static class CaravanActionsNativeChecks
{
    public static void Run()
    {
        typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!.FieldType));
        Game previousGame = Current.Game;
        GameComponent_Editor previousComponent = GameComponent_Editor.Instance;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        game.tickManager = (TickManager)RuntimeHelpers.GetUninitializedObject(typeof(TickManager));
        World world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        world.factionManager = new FactionManager();
        typeof(Game).GetField("worldInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, world);
        Current.Game = game;
        Harmony fixtureHarmony = new("CQF.Checks.CaravanNative");
        MethodInfo gravEngine = AccessTools.Method(typeof(GravshipUtility), nameof(GravshipUtility.PlayerHasGravEngine), new[] { typeof(Map) });
        try
        {
            GameComponent_Editor component = new(game);
            WorldObjectDef ordinary = new() { defName = "CQF_Check_Caravan", worldObjectClass = typeof(Caravan) };
            WorldObjectDef derived = new() { defName = "CQF_Check_CaravanDerived", worldObjectClass = typeof(CaravanActionsProbeCaravan) };
            WorldObjectDef alreadyRegistered = new()
            {
                defName = "CQF_Check_CaravanRegistered", worldObjectClass = typeof(Caravan),
                comps = new() { new WorldObjectCompProperties { compClass = typeof(WorldObjectComp_CaravanActions) } }
            };
            WorldObjectDef unrelated = new() { defName = "CQF_Check_OtherWorldObject", worldObjectClass = typeof(WorldObject) };
            foreach (WorldObjectDef definition in new[] { ordinary, derived, alreadyRegistered, unrelated }) DefDatabase<WorldObjectDef>.Add(definition);
            RuntimeHelpers.RunClassConstructor(typeof(PatchMain).TypeHandle);
            foreach (WorldObjectDef definition in new[] { ordinary, derived, alreadyRegistered })
                Check(definition.comps.Count(properties => properties.compClass == typeof(WorldObjectComp_CaravanActions)) == 1,
                    "native component registration is unique for " + definition.defName);
            Check(unrelated.comps.Count == 0, "non-caravan world objects receive no caravan component");

            CaravanActionsProbeCaravan caravan = (CaravanActionsProbeCaravan)RuntimeHelpers.GetUninitializedObject(typeof(CaravanActionsProbeCaravan));
            caravan.def = derived;
            typeof(WorldObject).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(caravan, new List<WorldObjectComp>());
            caravan.PostMake();
            Check(caravan.GetComponent<WorldObjectComp_CaravanActions>() != null, "native WorldObject.PostMake initializes the caravan component");
            CaravanActionsProbeCondition allowed = new() { Allowed = true };
            CaravanActionsProbeCondition denied = new();
            CaravanActionsProbeAction firstAction = new();
            CaravanActionsProbeAction secondAction = new();
            CaravanActionDef first = new()
            {
                defName = "CQF_Check_FirstCaravanCommand", label = "CQF_Check_FirstCaravanCommand", description = "CQF_Check_FirstCaravanDescription", CD = 12,
                conditions = new() { allowed }, actions = new() { firstAction }
            };
            CaravanActionDef second = new()
            {
                defName = "CQF_Check_SecondCaravanCommand", label = "CQF_Check_SecondCaravanCommand", CD = 20,
                actions = new() { secondAction }
            };
            CaravanActionDef hidden = new() { defName = "CQF_Check_HiddenCaravanCommand", conditions = new() { denied } };
            foreach (CaravanActionDef definition in new[] { first, second, hidden }) DefDatabase<CaravanActionDef>.Add(definition);
            DynamicMethod nativeGizmos = new("CQF_Check_WorldObjectGizmos", typeof(IEnumerable<Gizmo>), new[] { typeof(WorldObject) }, typeof(CaravanActionsNativeChecks).Module, true);
            ILGenerator il = nativeGizmos.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(WorldObject).GetMethod(nameof(WorldObject.GetGizmos))!);
            il.Emit(OpCodes.Ret);
            Func<WorldObject, IEnumerable<Gizmo>> getGizmos = nativeGizmos.CreateDelegate<Func<WorldObject, IEnumerable<Gizmo>>>();
            List<Command_Action> commands = getGizmos(caravan).OfType<Command_Action>().ToList();
            Check(commands.Count == 2 && commands.Select(command => command.defaultLabel).SequenceEqual(new[] { first.label, second.label }),
                "native world-object gizmos expose only matching commands in definition order");
            Check(allowed.Target == caravan && denied.Target == caravan, "conditions evaluate the owning caravan");
            Check(commands[0].defaultDesc == first.description && commands[0].icon == first.Icon && commands.All(command => command.Order == float.MaxValue),
                "command appearance is retained and native sorting keeps commands at the end");
            commands[0].action();
            Check(firstAction.Targets.SequenceEqual(new[] { caravan }) && secondAction.Targets.Count == 0, "the first command executes its own actions against its caravan");
            Check(component.CACDS[first].curTick == 0 && component.CACDS[first].time == first.CD, "commands retain the existing cooldown storage");
            commands = getGizmos(caravan).OfType<Command_Action>().ToList();
            Check(commands[0].Disabled && !commands[1].Disabled, "only the command on cooldown is disabled");
            commands[1].action();
            Check(secondAction.Targets.SequenceEqual(new[] { caravan }), "separate command callbacks keep their own definitions");
            typeof(TickManager).GetField("ticksGameInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game.tickManager, first.CD);
            component.GameComponentTick();
            Check(component.IsAvailable(first) && !component.IsAvailable(second), "the existing component expires each command cooldown independently");
            allowed.Allowed = false;
            Check(getGizmos(caravan).OfType<Command_Action>().Count() == 1, "command visibility reevaluates changed conditions");

            fixtureHarmony.Patch(gravEngine, prefix: new HarmonyMethod(typeof(CaravanActionsNativeChecks), nameof(NoGravEngine)));
            MapParent_Custom parent = (MapParent_Custom)RuntimeHelpers.GetUninitializedObject(typeof(MapParent_Custom));
            parent.def = new WorldObjectDef { defName = "CQF_Check_PocketParent", worldObjectClass = typeof(MapParent_Custom) };
            Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.info = new MapInfo { Size = new IntVec3(4, 1, 4), parent = parent, isPocketMap = true };
            Check(!map.IsPlayerHome && map.IsPocketMap, "exit-grid fixture is a pocket map rather than a player home");
            Check(!new ExitMapGrid(map).MapUsesExitGrid, "the native exit grid rejects CQF pocket maps without a CQF patch");
            Check(typeof(PatchMain).Assembly.GetType("QuestEditor_Library.Patch_MapExit") == null
                && typeof(PatchMain).Assembly.GetType("QuestEditor_Library.Patch_AddCaravanAction") == null,
                "the 1.7 core excludes both replaced Harmony patch classes");
            Check(new Harmony("QF_Patch").GetPatchedMethods().Count() == 17, "the core registers exactly seventeen native method targets");
            Console.WriteLine("Native caravan and pocket-map checks passed.");
        }
        finally
        {
            fixtureHarmony.Unpatch(gravEngine, HarmonyPatchType.Prefix, fixtureHarmony.Id);
            Current.Game = previousGame;
            GameComponent_Editor.Instance = previousComponent;
        }
    }

    private static bool NoGravEngine(ref bool __result)
    {
        __result = false;
        return false;
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
