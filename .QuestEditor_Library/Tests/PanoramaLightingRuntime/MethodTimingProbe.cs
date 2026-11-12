using System.Diagnostics;
using HarmonyLib;
using QuestEditor_Library;

namespace CQF.PanoramaLightingChecks;

public static class MethodTimingProbe
{
    public static void Install()
    {
        harmony.Patch(AccessTools.Method(typeof(MapDrawLayer_CQFPanoramaLighting), "DrawLayer"),
            prefix: new HarmonyMethod(typeof(MethodTimingProbe), nameof(Begin)),
            postfix: new HarmonyMethod(typeof(MethodTimingProbe), nameof(End)));
    }

    public static void Reset()
    {
        Calls = 0;
        Ticks = 0;
        Allocated = 0;
    }

    public static void Remove()
    {
        harmony.UnpatchAll(harmony.Id);
    }

    public static double Calibrate()
    {
        long elapsed = 0;
        for (int i = 0; i < 10000; i++)
        {
            long start = Stopwatch.GetTimestamp();
            _ = GC.GetAllocatedBytesForCurrentThread();
            elapsed += Stopwatch.GetTimestamp() - start;
            _ = GC.GetAllocatedBytesForCurrentThread();
        }
        return elapsed * 1000000d / Stopwatch.Frequency / 10000d;
    }

    private static void Begin(out ProbeState __state)
    {
        __state = new ProbeState(Stopwatch.GetTimestamp(), GC.GetAllocatedBytesForCurrentThread());
    }

    private static void End(ProbeState __state)
    {
        long elapsed = Stopwatch.GetTimestamp() - __state.Timestamp;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - __state.Allocation;
        if (!Enabled) return;
        Calls++;
        Ticks += elapsed;
        Allocated += allocated;
    }

    public static bool Enabled;
    public static long Calls;
    public static long Ticks;
    public static long Allocated;
    private static readonly Harmony harmony = new("local.CQF.PanoramaLightingTiming");
}

public readonly record struct ProbeState(long Timestamp, long Allocation);
