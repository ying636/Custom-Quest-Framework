using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CQF.EntityBookRuntimeChecks;

public sealed class EntityBookDevtools : IDisposable
{
    public EntityBookDevtools(string directory)
    {
        Directory.CreateDirectory(directory);
        Assembly runtime = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "AutoTestRunner");
        object artifacts = runtime.GetType("LogSyncMod.AutoTest.AutoTestReportWriter", true)!.GetMethod("CreateRunArtifacts")!
            .Invoke(null, new object[] { directory, "CQF.EntityBookChecks", Guid.NewGuid().ToString("N") })!;
        service = Activator.CreateInstance(runtime.GetType("LogSyncMod.AutoTest.AutoTestScreenshotService", true)!, artifacts,
            new Func<string, string, string>((left, right) => left + " | " + right), new Func<string, string>(value => value))!;
        resultType = runtime.GetType("LogSyncMod.AutoTest.AutoTestResult", true)!;
        Type dispatch = AccessTools.TypeByName("RimWorldWaylith.Imgui.ImguiDispatch") ?? throw new TypeLoadException("Native ImguiDispatch is unavailable.");
        harmony = new Harmony("CQF.EntityBookRuntimeChecks");
        harmony.Patch(AccessTools.Method(dispatch, "Dispatch"), new HarmonyMethod(typeof(EntityBookDevtools), nameof(DispatchPrefix)),
            new HarmonyMethod(typeof(EntityBookDevtools), nameof(DispatchPostfix)));
    }

    public int MouseDowns => mouseDowns;
    public int MouseUps => mouseUps;
    public bool ClickPending => pending != 0;
    public IReadOnlyList<string> Captures => captures;

    public void Click(Vector2 uiPoint)
    {
        if (pending != 0) throw new InvalidOperationException("A native GUI click is still pending.");
        point = new Vector2(uiPoint.x * Screen.width / UI.screenWidth, uiPoint.y * Screen.height / UI.screenHeight);
        pending = 2;
        clickDeadline = Time.realtimeSinceStartup + 6f;
    }

    public void CheckPendingClick()
    {
        if (pending > 0 && Time.realtimeSinceStartup > clickDeadline) throw new TimeoutException("Native ImguiDispatch mouse-down/up timed out.");
    }

    public void Capture(string name, Func<bool> guard)
    {
        if (pendingPng != null) throw new InvalidOperationException("A DevTools screenshot is still pending.");
        object result = Activator.CreateInstance(resultType, true)!;
        foreach ((string field, string value) in new[] { ("CaseId", name), ("CaseType", "EntityBook"), ("Scope", "UI"), ("DefName", name), ("Label", name), ("Status", "Passed") })
            resultType.GetField(field)!.SetValue(result, value);
        object[] arguments = { result, false, true, true, captures.Count, guard };
        if (!(bool)service.GetType().GetMethod("TryCaptureInfoCard")!.Invoke(service, arguments)!)
            throw new InvalidOperationException("RimDevTools screenshot service rejected " + name);
        pendingPng = (string?)resultType.GetField("ScreenshotPath")!.GetValue(result);
        if (string.IsNullOrWhiteSpace(pendingPng)) throw new IOException("RimDevTools did not return a screenshot path.");
        captures.Add(pendingPng);
        captureDeadline = Time.realtimeSinceStartup + 10f;
    }

    public bool CompleteCapture()
    {
        if (pendingPng == null) return true;
        if (!File.Exists(pendingPng) || new FileInfo(pendingPng).Length < 512)
        {
            if (Time.realtimeSinceStartup > captureDeadline) throw new TimeoutException("RimDevTools did not write a fresh PNG: " + pendingPng);
            return false;
        }
        using FileStream image = File.OpenRead(pendingPng);
        byte[] signature = new byte[8];
        if (image.Read(signature, 0, signature.Length) != signature.Length || !signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            throw new IOException("RimDevTools screenshot is not a PNG: " + pendingPng);
        pendingPng = null;
        return true;
    }

    public void Dispose()
    {
        pending = 0;
        harmony.UnpatchAll("CQF.EntityBookRuntimeChecks");
    }

    private static void DispatchPrefix(ref Vector2 physMouse, ref bool drawing, out Event? __state)
    {
        __state = null;
        if (!drawing || pending == 0) return;
        __state = Event.current;
        EventType type = pending == 2 ? EventType.MouseDown : EventType.MouseUp;
        physMouse = point;
        drawing = false;
        Event.current = new Event { type = type, rawType = type, button = 0, clickCount = 1, mousePosition = point };
        if (type == EventType.MouseDown) mouseDowns++;
        else mouseUps++;
        pending--;
    }

    private static void DispatchPostfix(Event? __state)
    {
        if (__state != null) Event.current = __state;
    }

    private readonly Harmony harmony;
    private readonly object service;
    private readonly Type resultType;
    private readonly List<string> captures = new();
    private string? pendingPng;
    private float captureDeadline;
    private float clickDeadline;
    private static Vector2 point;
    private static int pending;
    private static int mouseDowns;
    private static int mouseUps;
}
