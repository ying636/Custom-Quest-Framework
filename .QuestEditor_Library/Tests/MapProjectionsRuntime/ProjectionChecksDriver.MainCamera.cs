using HarmonyLib;
using LudeonTK;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private float MainCameraZoomTarget => (float)AccessTools.Field(typeof(CameraDriver), "desiredSize").GetValue(Current.CameraDriver)!;

    private IEnumerable<object?> CheckMainCamera()
    {
        GameComponent_CQFProjectionUI ui = GameComponent_CQFProjectionUI.Instance!;
        Check(!ui.ShowControls, "projection gameplay controls are hidden by default");
        List<DebugActionNode> nodes = new() { Dialog_Debug.rootNode };
        DebugActionNode? toggle = null;
        while (nodes.Count > 0)
        {
            DebugActionNode node = nodes[^1];
            nodes.RemoveAt(nodes.Count - 1);
            if (node.sourceAttribute?.name == "CQF_MP_ToggleControls") toggle = node;
            nodes.AddRange(node.children);
        }
        Check(toggle?.action != null && toggle.LabelNow == "CQF_MP_ToggleControls".Translate().ToString()
            && toggle.LabelNow != "CQF_MP_ToggleControls", "native debug action exposes the localized projection panel switch");
        toggle!.action();
        Check(ui.ShowControls, "debug action shows the projection controls");
        guiAction = () =>
        {
            try
            {
                Bootstrap.ObserveCamera = true;
                foreach ((KeyCode key, Vector3 direction) in new[] { (KeyCode.A, Vector3.left), (KeyCode.D, Vector3.right),
                    (KeyCode.W, Vector3.forward), (KeyCode.S, Vector3.back) })
                {
                    Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 32f);
                    Find.CameraDriver.Update();
                    ClickPlaying(actor!.DrawPos);
                    Vector3 before = Current.Camera.transform.position;
                    Rect sourceView = control!.ViewRect;
                    Bootstrap.HeldKey = key;
                    Vector2 point = control.ScreenRect(Current.Camera).center;
                    for (int i = 0; i < 6; i++) FirePlayingScreen(point, EventType.KeyDown, key: key);
                    Bootstrap.HeldKey = KeyCode.None;
                    Check(Vector3.Dot(Current.Camera.transform.position - before, direction) > 0f
                        && control.ViewRect == sourceView && Find.Selector.IsSelected(actor),
                        "native " + key + " input moves the main camera while keeping the projected pawn selected");
                }
                Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 32f);
                Find.CameraDriver.Update();
                ClickPlaying(actor!.DrawPos);
                Vector2 center = control!.ScreenRect(Current.Camera).center;
                SetPointer(center, EventType.KeyDown);
                Event.current.keyCode = KeyBindingDefOf.MapZoom_In.MainKey;
                Bootstrap.PressedKey = Event.current.keyCode;
                Current.CameraDriver.CameraDriverOnGUI();
                Current.CameraDriver.Update();
                float baselineZoom = MainCameraZoomTarget;
                EventType baselineEvent = Event.current.type;
                Bootstrap.PressedKey = null;
                Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 32f);
                Find.CameraDriver.Update();
                FirePlayingScreen(center, EventType.KeyDown, key: KeyBindingDefOf.MapZoom_In.MainKey);
                Check(Mathf.Abs(MainCameraZoomTarget - baselineZoom) < 0.001f && CQFMapProjectionContext.Current == null,
                    "projection preserves native zoom shortcut handling with the installed camera configuration: " + baselineEvent);
                float cameraSize = MainCameraZoomTarget;
                FirePlayingScreen(center, EventType.ScrollWheel, delta: new Vector2(0f, 6f));
                Check(MainCameraZoomTarget > cameraSize && Bootstrap.CameraInputUsedHost,
                    "native zoom-out uses the host map and real main camera during projected GUI");
                Find.CameraDriver.SetRootPosAndSize(new Vector3(120f, 0f, 120f), 32f);
                Find.CameraDriver.Update();
                SetPointer(center, EventType.Repaint);
                using (CQFMapProjectionContext context = new(control, Current.Camera))
                {
                    Check(Find.CurrentMap == source, "projection interaction scope still exposes the source map");
                }
                Current.CameraDriver.CameraDriverOnGUI();
                Check(CQFMapProjectionContext.Current == null && Find.CurrentMap == host && Bootstrap.CameraInputUsedHost,
                    "completed GUI scopes expose the host to native main camera input without camera patches");
                Current.CameraDriver.Update();
                Check(Current.Camera.transform.position.x > source!.Size.x && Current.Camera.transform.position.z > source.Size.z,
                    "main camera movement remains bounded by the host map rather than the smaller source map");
                Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 32f);
                Find.CameraDriver.Update();
                ClickPlaying(actor.DrawPos);
            }
            finally { Bootstrap.HeldKey = null; Bootstrap.ObserveCamera = false; }
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        string directory = Path.Combine(Bootstrap.Output, "MainCamera");
        Directory.CreateDirectory(directory);
        foreach (object? wait in Frames(24)) yield return wait;
        float beforeZoom = Current.Camera.orthographicSize;
        Rect sourceBeforeZoom = control!.ViewRect;
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "01-BeforeZoom.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        guiAction = () => FirePlaying(actor!.DrawPos, EventType.ScrollWheel, delta: new Vector2(0f, -4f));
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        foreach (object? wait in Frames(24)) yield return wait;
        Check(Current.Camera.orthographicSize < beforeZoom && control.ViewRect == sourceBeforeZoom,
            "main camera visibly zooms in after native smoothing while the source view stays fixed");
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "02-ZoomedIn.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        beforeZoom = Current.Camera.orthographicSize;
        guiAction = () => FirePlaying(actor!.DrawPos, EventType.ScrollWheel, delta: new Vector2(0f, 8f));
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        foreach (object? wait in Frames(24)) yield return wait;
        Check(Current.Camera.orthographicSize > beforeZoom && control.ViewRect == sourceBeforeZoom,
            "main camera visibly zooms out after native smoothing while the source view stays fixed");
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "03-ZoomedOut.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        foreach (object? wait in Frames(4)) yield return wait;
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "ControlsVisible.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        toggle.action();
        Check(!ui.ShowControls && CQFMapProjectionContext.Focused == control, "hiding the projection panel keeps gameplay focus");
        foreach (object? wait in Frames(4)) yield return wait;
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "ControlsHidden.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        toggle.action();
    }
}
