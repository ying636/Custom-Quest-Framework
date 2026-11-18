using System.Text.Json;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

[StaticConstructorOnStartup]
public static class Bootstrap
{
    static Bootstrap()
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfprojectionchecks")) return;
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
        new Harmony("local.CQF.MapProjectionChecks").Patch(AccessTools.PropertyGetter(typeof(Input), nameof(Input.mousePosition)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(Mouse)));
        Harmony cameraInput = new("local.CQF.MapProjectionChecks.CameraInput");
        cameraInput.Patch(AccessTools.Method(typeof(Input), nameof(Input.GetKey), new[] { typeof(KeyCode) }),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(Key)));
        cameraInput.Patch(AccessTools.Method(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) }),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(KeyDown)));
        cameraInput.Patch(AccessTools.PropertyGetter(typeof(UnityGUIBugsFixer), nameof(UnityGUIBugsFixer.CurrentEventDelta)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(DragDelta)));
        cameraInput.Patch(AccessTools.Method(typeof(CameraDriver), nameof(CameraDriver.CameraDriverOnGUI)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(ObserveCameraInput)) { priority = Priority.Last });
        Harmony trace = new("local.CQF.MapProjectionChecks.ItemMenus");
        trace.Patch(AccessTools.Method(typeof(FloatMenuMap), nameof(FloatMenuMap.DoWindowContents)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(TraceMenu)));
        trace.Patch(AccessTools.Method(typeof(FloatMenuOption), nameof(FloatMenuOption.Chosen)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(TraceChosen)));
        trace.Patch(AccessTools.Method(typeof(Selector), nameof(Selector.ClearSelection)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(TraceClear)));
        trace.Patch(AccessTools.Method(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI)),
            prefix: new HarmonyMethod(typeof(Bootstrap), nameof(TraceReadout)) { priority = Priority.Last });
        if (GenCommandLine.CommandLineArgPassed("cqfprojectionprofile"))
            trace.Patch(AccessTools.Method(typeof(QuestEditor_Library.CQFMapProjectionRenderer), "Render"),
                prefix: new HarmonyMethod(typeof(Bootstrap), nameof(CountRender)),
                finalizer: new HarmonyMethod(typeof(Bootstrap), nameof(EndRender)));
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            GameObject owner = new("CQF_MapProjectionChecks");
            UnityEngine.Object.DontDestroyOnLoad(owner);
            owner.AddComponent<ProjectionChecksDriver>();
        });
    }

    public static string Output => GenCommandLine.TryGetCommandLineArg("cqfprojectionoutput", out string value)
        ? value : throw new InvalidOperationException("cqfprojectionoutput");

    private static bool Mouse(ref Vector3 __result)
    {
        Vector3? position = MousePosition ?? HoverPosition;
        if (!position.HasValue) return true;
        __result = position.Value;
        return false;
    }

    private static bool Key(KeyCode key, ref bool __result)
    {
        if (!HeldKey.HasValue) return true;
        __result = key != KeyCode.None && key == HeldKey.Value;
        return false;
    }

    private static bool DragDelta(ref Vector2 __result)
    {
        if (!CameraDragDelta.HasValue) return true;
        __result = CameraDragDelta.Value;
        CameraDragDelta = Vector2.zero;
        return false;
    }

    private static bool KeyDown(KeyCode key, ref bool __result)
    {
        if (!PressedKey.HasValue) return true;
        __result = key != KeyCode.None && key == PressedKey.Value;
        return false;
    }

    private static void ObserveCameraInput(CameraDriver __instance)
    {
        if (!ObserveCamera || __instance != Current.CameraDriver) return;
        CameraInputUsedHost = QuestEditor_Library.CQFMapProjectionContext.Current == null
            && Find.CurrentMap == QuestEditor_Library.CQFMapProjectionContext.HostMap && Find.Camera == Current.Camera;
        File.AppendAllText(Path.Combine(Output, "CameraInput.txt"), Event.current.type + " raw=" + Event.current.rawType
            + " key=" + Event.current.keyCode + " zoomKey=" + KeyBindingDefOf.MapZoom_In.MainKey
            + " zoomEvent=" + KeyBindingDefOf.MapZoom_In.KeyDownEvent + " target="
            + AccessTools.Field(typeof(CameraDriver), "desiredSize").GetValue(__instance) + " host=" + CameraInputUsedHost + "\n");
    }

    private static void TraceMenu()
    {
        if (TraceItemMenus) File.AppendAllText(Path.Combine(Output, "ItemTrace.txt"), "MENU " + Event.current.type + " at=" + Event.current.mousePosition
            + " map=" + Find.CurrentMap + " selected=" + Find.Selector.SingleSelectedThing + "\n");
    }

    private static void TraceChosen(FloatMenuOption __instance)
    {
        if (TraceItemMenus) File.AppendAllText(Path.Combine(Output, "ItemTrace.txt"), "CHOSEN " + __instance.Label + " disabled=" + __instance.Disabled + "\n");
    }

    private static void TraceClear()
    {
        if (TraceItemMenus) File.AppendAllText(Path.Combine(Output, "ItemTrace.txt"), "CLEAR " + Environment.StackTrace + "\n");
    }

    private static void TraceReadout()
    {
        if (HoverPosition.HasValue && Event.current.type == EventType.Repaint)
            ReadoutOffsets.Add(new Vector2(GUI.matrix.m03, GUI.matrix.m13));
    }

    private static void CountRender(out Waylith.Profiling.FrameProfiler.ScopeHandle __state)
    {
        __state = default;
        if (!ProfilerSampling) return;
        ProfilerRenderCalls++;
        __state = Waylith.Profiling.FrameProfiler.Scope("CQF_ProjectionRender");
    }

    private static Exception? EndRender(Exception? __exception, Waylith.Profiling.FrameProfiler.ScopeHandle __state)
    {
        __state.Dispose();
        return __exception;
    }

    public static Vector3? MousePosition;
    public static Vector3? HoverPosition;
    public static bool TraceItemMenus;
    public static readonly List<Vector2> ReadoutOffsets = new();
    public static bool ProfilerSampling;
    public static int ProfilerRenderCalls;
    public static KeyCode? HeldKey;
    public static KeyCode? PressedKey;
    public static Vector2? CameraDragDelta;
    public static bool ObserveCamera;
    public static bool CameraInputUsedHost;
}
