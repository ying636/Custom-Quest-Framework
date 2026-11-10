using System.Collections;
using System.Reflection;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public static class CounterTransitionTimingChecks
{
    private static string ResultPath => Path.Combine(Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE")
        ?? GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");

    public static void Install()
    {
        foreach (DomainDef definition in DefDatabase<DomainDef>.AllDefsListForReading) definition.preload = true;
        Harmony harmony = new("CQF.CounterTransitionTimingChecks");
        harmony.Patch(AccessTools.Method(typeof(DomainTransition), nameof(DomainTransition.BindOpening)),
            postfix: new HarmonyMethod(typeof(CounterTransitionTimingChecks), nameof(AfterBind)));
        harmony.Patch(AccessTools.Method(typeof(DomainTransition), nameof(DomainTransition.Draw)),
            prefix: new HarmonyMethod(typeof(CounterTransitionTimingChecks), nameof(BeforeDraw)));
        harmony.Patch(AccessTools.Method(typeof(DomainOpening), nameof(DomainOpening.BeginClosing)),
            prefix: new HarmonyMethod(typeof(CounterTransitionTimingChecks), nameof(BeforeClosing)));
        harmony.Patch(AccessTools.Method(typeof(DomainOpening_Basic), nameof(DomainOpening_Basic.CompleteOpening)),
            prefix: new HarmonyMethod(typeof(CounterTransitionTimingChecks), nameof(BeforeOpening)));
        Type blood = GenTypes.GetTypeInAnyAssembly("RSH.Thing_BloodSeaDomainOpening")
            ?? throw new InvalidOperationException("Blood sea opening type was not loaded.");
        harmony.Patch(AccessTools.Method(blood, nameof(DomainOpening.CompleteOpening)),
            prefix: new HarmonyMethod(typeof(CounterTransitionTimingChecks), nameof(BeforeOpening)));
        harmony.Patch(AccessTools.Method(typeof(CounterOpeningChecksDriver), "NextCase"),
            prefix: new HarmonyMethod(typeof(CounterTransitionTimingChecks), nameof(BeforeNextCase)));
    }

    private static void AfterBind(DomainTransition __instance)
    {
        if (__instance.OpeningOwner?.IsCounterOpening != true) return;
        if (LongEventHandler.AnyEventNowOrWaiting) Fail("fullscreen opening was bound inside a loading event");
        else File.AppendAllText(ResultPath, "PASS fullscreen opening starts after loading has finished\n");
        if (GenCommandLine.CommandLineArgPassed("cqfcounterrepairdelay"))
            LongEventHandler.QueueLongEvent(() => Thread.Sleep(1400), "GeneratingMap", false, null);
    }

    private static void BeforeClosing(DomainOpening __instance)
    {
        if (!__instance.IsCounterOpening || __instance.Definition.defName != "RSH_BloodSeaDomain") return;
        IDictionary templates = (IDictionary)(typeof(CQFDomainRuntime).GetField("mapTemplates", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(CQFDomainRuntime.Instance) ?? throw new InvalidOperationException("Domain template cache was unavailable."));
        templates.Remove(__instance.Definition);
        File.AppendAllText(ResultPath, "TRACE removed incoming template before closure\n");
    }

    private static void BeforeOpening(DomainOpening __instance)
    {
        if (!__instance.IsCounterOpening) return;
        if (LongEventHandler.AnyEventNowOrWaiting) Fail("fullscreen opening was called inside a loading event");
        else File.AppendAllText(ResultPath, "PASS fullscreen opening starts after loading has finished\n");
    }

    private static void BeforeDraw(DomainTransition __instance)
    {
        if (__instance.OpeningOwner is not { IsCounterOpening: true } opening || LongEventHandler.AnyEventNowOrWaiting
            || Event.current?.type != EventType.Repaint || !WorldRendererUtility.DrawingMap || !observed.Add(__instance)) return;
        float progress = __instance.Progress;
        File.AppendAllText(ResultPath, "TRACE first visible frame " + opening.Definition.defName + " progress=" + progress + "\n");
        if (progress > 0.1f) Fail("opening time advanced before its first visible frame: " + progress);
        else File.AppendAllText(ResultPath, "PASS fullscreen opening begins at its first visible frame\n");
    }

    private static bool BeforeNextCase()
    {
        if (!failed) return true;
        File.AppendAllText(ResultPath, "FAILED\n");
        Application.Quit(1);
        return false;
    }

    private static void Fail(string message)
    {
        failed = true;
        File.AppendAllText(ResultPath, "FAIL " + message + "\n");
        Log.Error("[CQF.CounterTransitionTimingChecks] " + message);
    }

    private static readonly HashSet<DomainTransition> observed = new();
    private static bool failed;
}
