using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public static class StartupChecksProfiler
{
    public static void Install()
    {
        Harmony profiler = new("CQF.Checks.StartupProfiling");
        foreach (Type type in new[] { typeof(WorldGenerator), typeof(MapGenerator) })
        {
            string name = type == typeof(WorldGenerator) ? "GenerateWorld" : "GenerateMap";
            profiler.Patch(AccessTools.Method(type, name),
                prefix: new HarmonyMethod(typeof(StartupChecksProfiler), nameof(Begin)),
                postfix: new HarmonyMethod(typeof(StartupChecksProfiler), nameof(End)));
        }
        profiler.Patch(AccessTools.Method(typeof(WorldGenerator), "GenerateWorld"),
            prefix: new HarmonyMethod(typeof(StartupChecksProfiler), nameof(FixSeed)));
        profiler.Patch(AccessTools.Method(typeof(GameInitData), nameof(GameInitData.ChooseRandomStartingTile)),
            prefix: new HarmonyMethod(typeof(StartupChecksProfiler), nameof(BeginTileChoice)),
            postfix: new HarmonyMethod(typeof(StartupChecksProfiler), nameof(EndTileChoice)));
        HashSet<MethodInfo> steps = new();
        foreach (GenStepDef step in DefDatabase<GenStepDef>.AllDefsListForReading)
            steps.Add(AccessTools.Method(step.genStep.GetType(), "Generate", new[] { typeof(Map), typeof(GenStepParams) }));
        foreach (WorldGenStepDef step in DefDatabase<WorldGenStepDef>.AllDefsListForReading)
            steps.Add(AccessTools.Method(step.worldGenStep.GetType(), "GenerateFresh", new[] { typeof(string), typeof(PlanetLayer) }));
        foreach (GameSetupStepDef step in DefDatabase<GameSetupStepDef>.AllDefsListForReading)
            steps.Add(AccessTools.Method(step.setupStep.GetType(), "GenerateFresh", Type.EmptyTypes));
        HashSet<MethodInfo> declaredSteps = new();
        foreach (MethodInfo step in steps)
        {
            if (step == null) throw new MissingMethodException("Startup profiler could not resolve a generation step.");
            if (step.IsAbstract) continue;
            declaredSteps.Add(step.DeclaringType!.GetMethod(step.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null, step.GetParameters().Select(parameter => parameter.ParameterType).ToArray(), null)
                ?? throw new MissingMethodException("Startup profiler could not resolve a declared generation step: " + step));
        }
        foreach (MethodInfo step in declaredSteps)
        {
            profiler.Patch(step, prefix: new HarmonyMethod(typeof(StartupChecksProfiler), nameof(Begin)),
                postfix: new HarmonyMethod(typeof(StartupChecksProfiler), nameof(EndStep)));
        }
    }

    private static void FixSeed(ref string seedString) => seedString = "CQF.Startup.Profile.20261019";

    private static void BeginTileChoice() => Rand.PushState(463170);

    private static void EndTileChoice()
    {
        Rand.PopState();
        File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"),
            "TRACE fixed starting tile=" + Current.Game.InitData.startingTile + "\n");
    }

    private static void Begin(out Stopwatch __state) => __state = Stopwatch.StartNew();

    private static void EndStep(object __instance, Stopwatch __state)
        => File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"),
            "STEP " + __instance.GetType().Assembly.GetName().Name + "/" + __instance.GetType().FullName
            + "=" + __state.ElapsedMilliseconds + " ms\n");

    private static void End(MethodBase __originalMethod, Stopwatch __state)
        => File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt"),
            "TRACE " + __originalMethod.DeclaringType!.Name + "." + __originalMethod.Name + "=" + __state.ElapsedMilliseconds + " ms\n");
}
