using System.Text.Json;
using System.Diagnostics;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Waylith.Profiling;
using UnityStubs.Rendering;
using MeshDrawCommand = Waylith.Rendering.MeshDrawCommand;

namespace CQF.ProjectionQualityChecks;

[StaticConstructorOnStartup]
public static class Bootstrap
{
    static Bootstrap()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfprojectionquality")) return;
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "Process.json"), JsonSerializer.Serialize(new
        {
            PID = Environment.ProcessId, ExecutablePath = Environment.ProcessPath, CommandLine = Environment.GetCommandLineArgs(),
            SaveData = GenFilePaths.SaveDataFolderPath, SteamAppId = Environment.GetEnvironmentVariable("SteamAppId"),
            SteamGameId = Environment.GetEnvironmentVariable("SteamGameId"), SteamOverlayGameId = Environment.GetEnvironmentVariable("SteamOverlayGameId"),
            Version = VersionControl.CurrentVersionStringWithRev
        }, new JsonSerializerOptions { WriteIndented = true }));
        Prefs.VolumeMaster = 0f;
        Prefs.EdgeScreenScroll = false;
        Harmony trace = new("local.CQF.ProjectionQualityChecks");
        trace.Patch(AccessTools.Method(typeof(CQFMapProjectionRenderer), "Render"),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(BeginRender)), finalizer: new HarmonyMethod(typeof(Bootstrap), nameof(EndRender)));
        trace.Patch(AccessTools.Method(typeof(LegacyProjectionRenderer), "Render"),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(BeginRender)), finalizer: new HarmonyMethod(typeof(Bootstrap), nameof(EndRender)));
        trace.Patch(AccessTools.Method(typeof(CQFMapProjectionRenderer), nameof(CQFMapProjectionRenderer.Draw)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(DrawLegacy)));
        if (GenCommandLine.CommandLineArgPassed("cqfprojectionpoolpaired"))
            trace.Patch(AccessTools.Method(typeof(CQFMapProjectionRenderer), "SubmitCommands"),
                prefix: new HarmonyMethod(typeof(Bootstrap), nameof(BeginSubmit)), finalizer: new HarmonyMethod(typeof(Bootstrap), nameof(EndSubmit)));
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            GameObject owner = new("CQF_ProjectionQualityChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<QualityChecksDriver>();
        });
    }

    public static string Output => GenCommandLine.TryGetCommandLineArg("cqfprojectionoutput", out string value)
        ? value : throw new InvalidOperationException("cqfprojectionoutput");

    private static void BeginRender(out FrameProfiler.ScopeHandle __state)
    {
        __state = default;
        if (!Sampling) return;
        RenderCalls++;
        if (Profiling) __state = FrameProfiler.Scope("CQF_ProjectionQualityRender");
    }

    private static Exception? EndRender(Exception? __exception, FrameProfiler.ScopeHandle __state)
    {
        __state.Dispose();
        return __exception;
    }

    private static bool DrawLegacy(Map host, CQFMapProjection ___projection)
    {
        if (!LegacyMode || GenCommandLine.CommandLineArgPassed("cqfprojectionpoolpaired")) return true;
        LegacyRenderer ??= new LegacyProjectionRenderer(___projection);
        LegacyRenderer.Draw(host);
        return false;
    }

    private static bool BeginSubmit(ICameraTextureRenderer backend, RenderTexture ___texture, Camera ___camera,
        List<MeshDrawCommand> ___orderedCommands, out (long Alloc, long Time) __state)
    {
        __state = Sampling ? (GC.GetAllocatedBytesForCurrentThread(), Stopwatch.GetTimestamp()) : default;
        if (!LegacyMode) return true;
        backend.Render(___texture, ___orderedCommands, CameraViewFactory.GetView(___camera), ___camera.rect, Color.clear);
        return false;
    }

    private static Exception? EndSubmit(Exception? __exception, (long Alloc, long Time) __state)
    {
        if (__state.Time != 0)
        {
            SubmitCalls++;
            SubmitAlloc += GC.GetAllocatedBytesForCurrentThread() - __state.Alloc;
            SubmitTicks += Stopwatch.GetTimestamp() - __state.Time;
        }
        return __exception;
    }

    public static bool Sampling;
    public static bool Profiling;
    public static int RenderCalls;
    public static bool LegacyMode;
    public static LegacyProjectionRenderer? LegacyRenderer;
    public static long SubmitCalls;
    public static long SubmitAlloc;
    public static long SubmitTicks;
}
