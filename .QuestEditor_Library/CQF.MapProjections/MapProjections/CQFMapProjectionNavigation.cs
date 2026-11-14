#if RIMWORLD_1_7
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public static class CQFMapProjectionNavigation
{
    public static bool BlocksHostCamera { get; private set; }
    public static bool HoldsHostCamera => Current.ProgramState == ProgramState.Playing && WorldRendererUtility.DrawingMap
        && GameComponent_CQFProjectionUI.Instance?.Session == null && (dragged != null
            || CQFMapProjectionContext.Focused is { CanControl: true } projection
            && projection.ScreenRect(Current.Camera).Contains(UI.MousePositionOnUIInverted)
            && Find.WindowStack.GetWindowAt(UI.MousePositionOnUIInverted) == null);

    public static void BeginEvent() => BlocksHostCamera = false;

    public static void HandleInput(Map host)
    {
        if (Find.WindowStack.AnyWindowAbsorbingAllInput) { CancelDrag(); return; }
        Event ev = Event.current;
        Camera camera = Current.Camera;
        if (dragged is { CanControl: true } projection && CQFMapProjectionContext.Focused == projection)
        {
            BlocksHostCamera = true;
            if (ev.type == EventType.MouseDrag && ev.button == 2)
            {
                Vector2 delta = ev.mousePosition - lastPointer;
                Rect screen = projection.ScreenRect(camera), view = projection.ViewRect;
                projection.PanView(new Vector2(-delta.x * view.width / screen.width, delta.y * view.height / screen.height));
                lastPointer = ev.mousePosition;
                ev.Use();
            }
            else if (ev.type == EventType.MouseUp && ev.button == 2) { CancelDrag(); ev.Use(); }
            return;
        }
        CancelDrag();
        if (Find.WindowStack.GetWindowAt(UI.MousePositionOnUIInverted) != null) return;
        Ray ray = camera.ScreenPointToRay(UI.MousePositionOnUI * Prefs.UIScale);
        Vector3 position = new(ray.origin.x, 0f, ray.origin.z);
        CQFMapProjection? hit = host.GetComponent<MapComponent_CQFMapProjections>().Projections.LastOrDefault(p => p.enabled && p.Contains(position));
        if (hit is not { CanControl: true }) return;
        BlocksHostCamera = CQFMapProjectionContext.Focused == hit;
        if (ev.type == EventType.ScrollWheel)
        {
            CQFMapProjectionContext.Focus(host, hit);
            hit.ZoomView(Mathf.Pow(1.15f, ev.delta.y), hit.ToSource(position));
            BlocksHostCamera = true;
            ev.Use();
        }
        else if (ev.type == EventType.MouseDown && ev.button == 2)
        {
            CQFMapProjectionContext.Focus(host, hit);
            dragged = hit;
            lastPointer = ev.mousePosition;
            BlocksHostCamera = true;
            ev.Use();
        }
        else if (CQFMapProjectionContext.Focused == hit && GUIUtility.keyboardControl == 0)
        {
            if (KeyBindingDefOf.MapZoom_In.KeyDownEvent || KeyBindingDefOf.MapZoom_Out.KeyDownEvent)
            {
                hit.ZoomView(KeyBindingDefOf.MapZoom_In.KeyDownEvent ? 0.85f : 1f / 0.85f, hit.ToSource(position));
                ev.Use();
            }
        }
    }

    public static void Update()
    {
        if (!WorldRendererUtility.DrawingMap || GameComponent_CQFProjectionUI.Instance?.Session != null || Find.WindowStack.AnyWindowAbsorbingAllInput
            || GUIUtility.keyboardControl != 0 || CQFMapProjectionContext.Focused is not { CanControl: true } projection
            || !projection.ScreenRect(Current.Camera).Contains(UI.MousePositionOnUIInverted)) return;
        Vector2 direction = Vector2.zero;
        if (KeyBindingDefOf.MapDolly_Left.IsDown) direction.x -= 1f;
        if (KeyBindingDefOf.MapDolly_Right.IsDown) direction.x += 1f;
        if (KeyBindingDefOf.MapDolly_Up.IsDown) direction.y += 1f;
        if (KeyBindingDefOf.MapDolly_Down.IsDown) direction.y -= 1f;
        if (direction != Vector2.zero) projection.PanView(direction * (projection.ViewRect.height * Time.deltaTime));
    }

    public static void Release(CQFMapProjection projection)
    {
        if (dragged == projection) CancelDrag();
    }

    public static void CancelDrag() => dragged = null;

    private static CQFMapProjection? dragged;
    private static Vector2 lastPointer;
}
#endif
