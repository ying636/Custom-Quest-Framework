#if RIMWORLD_1_7
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapProjectionContext : IDisposable
{
    public CQFMapProjectionContext(CQFMapProjection projection, Camera hostCamera, Camera? renderCamera = null)
    {
        previous = Current;
        Projection = projection;
        HostCamera = hostCamera;
        RenderCamera = renderCamera;
        InputCamera = renderCamera == null ? projection.GetInputCamera(hostCamera) : null;
        AllowsOutsidePointer = Find.Selector.dragBox.active || Find.DesignatorManager.Dragger.Dragging;
        Current = this;
        invalidateCameraPlus?.Invoke();
    }

    public CQFMapProjection Projection { get; }
    public Camera HostCamera { get; }
    public Camera? RenderCamera { get; }
    public Camera? InputCamera { get; }
    public bool AllowsOutsidePointer { get; }
    public bool PointerInside
    {
        get
        {
            Ray ray = HostCamera.ScreenPointToRay(UI.MousePositionOnUI * Prefs.UIScale);
            return Projection.Contains(new Vector3(ray.origin.x, 0f, ray.origin.z));
        }
    }
    public static CQFMapProjectionContext? Current { get; private set; }
    public static CQFMapProjection? Focused { get; private set; }
    public static bool IsRendering => Current?.RenderCamera != null;
    public static Map? HostMap => Verse.Current.Game is { currentMapIndex: >= 0 } game ? game.Maps[game.currentMapIndex] : null;

    public void Dispose()
    {
        if (ReferenceEquals(Current, this))
        {
            Current = previous;
            invalidateCameraPlus?.Invoke();
        }
    }

    public static void Focus(Map host, CQFMapProjection? projection)
    {
        if (projection != null)
        {
            projection.Validate(host);
            if (!projection.CanControl) throw new InvalidOperationException("[CQF] Cannot control a display-only or disabled map projection.");
            GameComponent_MapManager.Instance!.Activate(projection.sourceMap!);
        }
        if (ReferenceEquals(Focused, projection) && focusedHost == host) return;
        if (Focused != null || projection != null)
        {
            Find.Selector.ClearSelection();
            Find.Selector.dragBox.active = false;
            Find.Selector.gotoController.Deactivate();
            Find.Targeter.StopTargeting();
            Find.DesignatorManager.Deselect();
        }
        Focused = projection;
        focusedHost = projection == null ? null : host;
    }

    public static CQFMapProjectionContext? BeginGUI()
    {
        CQFMapProjectionNavigation.BeginEvent();
        if (GameComponent_CQFProjectionUI.Instance?.Session != null) return null;
        Map? host = HostMap;
        if (host == null || !WorldRendererUtility.DrawingMap || focusedHost != null && focusedHost != host
            || Focused != null && (!Focused.CanControl || !Find.CameraDriver.CurrentViewRect.Overlaps(Focused.DestinationBounds)))
        {
            ReleaseFocus();
            return null;
        }
        Event ev = Event.current;
        CQFMapProjectionNavigation.HandleInput(host);
        if (ev.type == EventType.MouseDown && ev.button <= 1 && Find.WindowStack.GetWindowAt(UI.MousePositionOnUIInverted) == null)
        {
            Vector3 mouse = UI.UIToMapPosition(UI.MousePositionOnUI);
            CQFMapProjection? hit = host.GetComponent<MapComponent_CQFMapProjections>().Projections
                .LastOrDefault(projection => projection.enabled && projection.Contains(mouse));
            if (hit != null && hit.CanControl) Focus(host, hit);
            else if (Focused != null && !Find.Selector.dragBox.active) Focus(host, null);
        }
        return Focused == null ? null : new CQFMapProjectionContext(Focused, Find.Camera);
    }

    public static void Release(CQFMapProjection projection)
    {
        CQFMapProjectionNavigation.Release(projection);
        while (Current is { } context && ReferenceEquals(context.Projection, projection)) context.Dispose();
        if (ReferenceEquals(Focused, projection)) ReleaseFocus();
    }

    public static void ReleaseHost(Map host)
    {
        if (focusedHost == host || Focused?.sourceMap == host) ReleaseFocus();
    }

    public static void ReturnToHost()
    {
        CQFMapProjectionNavigation.CancelDrag();
        while (Current is { } context) context.Dispose();
        ReleaseFocus();
    }

    public static void NotifyViewChanged(CQFMapProjection projection)
    {
        if (Current?.Projection != projection) return;
        if (Current.RenderCamera == null) projection.GetInputCamera(Current.HostCamera);
        invalidateCameraPlus?.Invoke();
    }

    private static Action? CreateCameraPlusInvalidator()
    {
        Type? fastUI = AccessTools.TypeByName("CameraPlus.FastUI");
        if (fastUI == null) return null;
        try
        {
            List<FieldInfo> frames = new();
            foreach (string name in new[] { "lastUpdateFrameForCurUICellSize", "lastUpdateFrameForMouseMapPosition", "lastUpdateFrameForMousePositionOnUIInverted" })
            {
                FieldInfo? field = AccessTools.Field(fastUI, name);
                if (field == null || !field.IsStatic || field.FieldType != typeof(int)) throw new MissingFieldException(fastUI.FullName, name);
                frames.Add(field);
            }
            Type cache = AccessTools.TypeByName("CameraPlus.MarkerDecisionCache") ?? throw new TypeLoadException("CameraPlus.MarkerDecisionCache");
            MethodInfo clear = AccessTools.Method(cache, "Clear", Type.EmptyTypes) ?? throw new MissingMethodException(cache.FullName, "Clear");
            Action clearDecisions = clear.CreateDelegate<Action>();
            object invalidFrame = -1;
            return () =>
            {
                foreach (FieldInfo frame in frames) frame.SetValue(null, invalidFrame);
                clearDecisions();
            };
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Camera+ projection cache isolation could not be initialized: " + error);
            return null;
        }
    }

    private static void ReleaseFocus()
    {
        if (Focused == null) return;
        Focused = null;
        focusedHost = null;
        if (Verse.Current.ProgramState == ProgramState.Playing && Find.MapUI != null)
        {
            Find.Selector.ClearSelection();
            Find.Targeter.StopTargeting();
            Find.DesignatorManager.Deselect();
            Find.Selector.dragBox.active = false;
            Find.Selector.gotoController.Deactivate();
        }
    }

    private readonly CQFMapProjectionContext? previous;
    private static readonly Action? invalidateCameraPlus = CreateCameraPlusInvalidator();
    private static Map? focusedHost;
}
#endif
