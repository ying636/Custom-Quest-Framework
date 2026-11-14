using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private IEnumerable<object?> CheckEditorUX()
    {
        GameComponent_CQFProjectionUI ui = GameComponent_CQFProjectionUI.Instance!;
        MapComponent_CQFMapProjections component = host!.GetComponent<MapComponent_CQFMapProjections>();
        Dialog_CQFProjectionSourcePicker picker = new(host, map => ui.BeginEdit(host, source: map));
        Find.WindowStack.Add(picker);
        foreach (object? wait in Frames(12)) yield return wait;
        Check(!GameComponent_MapManager.Instance!.IsHibernating(source!), "source picker thumbnail wakes a sleeping source map");
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "06-SourcePicker.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        picker.Close();
        foreach (object? wait in Frames(3)) yield return wait;
        Check(GameComponent_MapManager.Instance.IsHibernating(source!), "closing the picker restores the source map hibernation state");
        picker = new Dialog_CQFProjectionSourcePicker(host, map => ui.BeginEdit(host, source: map));
        Find.WindowStack.Add(picker);
        foreach (object? wait in Frames(4)) yield return wait;
        guiAction = () =>
        {
            Vector2 card = new(picker.windowRect.x + 118f, picker.windowRect.y + 216f);
            foreach (EventType type in new[] { EventType.MouseDown, EventType.MouseUp })
            {
                SetPointer(card, type);
                float margin = (float)HarmonyLib.AccessTools.Property(typeof(Window), "Margin").GetValue(picker)!;
                Rect body = picker.windowRect.ContractedBy(margin);
                GUI.BeginGroup(body);
                try { picker.DoWindowContents(body.AtZero()); }
                finally { GUI.EndGroup(); }
            }
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        Check(ui.Session?.Draft.sourceMap == source && !picker.IsOpen, "native source card click opens a draft and transfers the preview lease safely");
        CQFMapProjectionEditSession session = ui.Session!;
        foreach (object? wait in Frames(8)) yield return wait;
        Check(component.Projections.Count == 0 && session.Canvas.ScreenRect.width > 100f && Find.CurrentMap == host,
            "new editor previews a separate draft without changing the host map or projection list");
        guiAction = () =>
        {
            session.LockAspect = false;
            Vector2 cropChoice = new(session.Canvas.ScreenRect.center.x + session.Canvas.ScreenRect.width / 4f, session.Canvas.ScreenRect.yMax + 24f);
            ClickEditor(session, cropChoice);
            Rect canvas = session.Canvas.ScreenRect;
            Vector2 first = PreviewPoint(canvas, new IntVec3(22, 0, 42));
            Vector2 last = PreviewPoint(canvas, new IntVec3(50, 0, 18));
            FireEditor(session, first, EventType.MouseDown);
            FireEditor(session, last, EventType.MouseDrag);
            FireEditor(session, last, EventType.MouseUp);
            File.AppendAllText(Path.Combine(Bootstrap.Output, "UXTrace.txt"), "scale=" + Prefs.UIScale + " panel=" + session.PanelRect + " canvas=" + canvas + " local=" + session.Canvas.ImageRect
                + " first=" + first + " last=" + last + " area=" + session.Draft.sourceRect + " whole=" + HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "whole").GetValue(session) + "\n");
            Check(session.Draft.sourceRect == new CellRect(22, 18, 29, 25), "native preview drag crops the correct source cells with north at the top");
            FireEditor(session, HostPoint(48, 26), EventType.MouseDown);
            FireEditor(session, HostPoint(64, 42), EventType.MouseDrag);
            FireEditor(session, HostPoint(64, 42), EventType.MouseUp);
            Check(session.Placed && session.Draft.destination == new CellRect(48, 26, 17, 17), "native map drag places the draft in the requested cells");
            CellRect placed = session.Draft.destination;
            FireEditor(session, HostPoint(56, 34), EventType.MouseDown);
            FireEditor(session, HostPoint(60, 38), EventType.MouseDrag);
            FireEditor(session, HostPoint(60, 38), EventType.MouseDown, 1);
            Check(session.Draft.destination == placed && !session.Placement.IsDragging, "right click cancels a move and restores the previous footprint");
            FireEditor(session, HostPoint(56, 34), EventType.MouseDown);
            FireEditor(session, HostPoint(60, 38), EventType.MouseDrag);
            FireEditor(session, HostPoint(60, 38), EventType.MouseUp);
            Check(session.Draft.destination == new CellRect(52, 30, 17, 17), "native drag moves the placed draft without changing its size");
            FireEditor(session, Vector2.zero, EventType.Repaint);
            Vector2 corner = session.Placement.Handles[0].center;
            FireEditor(session, corner, EventType.MouseDown);
            FireEditor(session, HostPoint(48, 50), EventType.MouseDrag);
            FireEditor(session, HostPoint(48, 50), EventType.MouseUp);
            Check(session.Draft.destination == new CellRect(48, 30, 21, 21), "native corner drag resizes around the opposite corner");
            CellRect resized = session.Draft.destination;
            session.Placement.BeginMove(resized.CenterCell);
            session.Placement.UpdatePointer(new IntVec3(10000, 0, 10000));
            Check(session.Draft.destination.Max.InBounds(host), "moving a draft clamps its entire footprint to the host map");
            Event.current = new Event { type = EventType.KeyDown, rawType = EventType.KeyDown, keyCode = KeyCode.Escape };
            session.OnGUI();
            Check(session.Draft.destination == resized && !session.Placement.IsDragging && ui.Session == session, "Escape cancels only the current drag before closing the editor");
            session.Draft.mode = CQFMapProjectionMode.Control;
            Check(CQFMapProjectionContext.BeginGUI() == null && component.Projections.Count == 0,
                "editing prevents control routing and keeps the draft uncommitted");
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        foreach (object? wait in Frames(8)) yield return wait;
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "07-DirectEditor.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        guiAction = () =>
        {
            HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "panelScroll").SetValue(session, new Vector2(0f, 68f));
            FireEditor(session, Vector2.zero, EventType.Repaint);
            float contentHeight = (float)HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "panelContentHeight").GetValue(session)!;
            Vector2 scroll = (Vector2)HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "panelScroll").GetValue(session)!;
            ClickEditor(session, new Vector2(session.PanelRect.center.x, session.PanelRect.y + contentHeight + 29f - scroll.y));
            Check((bool)HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "advanced").GetValue(session)!, "native advanced settings button expands the detailed controls");
            Check((string)HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "resolutionBuffer").GetValue(session)! == session.Draft.resolution.ToString()
                && (string)HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "refreshBuffer").GetValue(session)! == session.Draft.refreshFrames.ToString(),
                "advanced numeric fields start with the current resolution and refresh interval");
            SetPointer(session.PanelRect.center, EventType.ScrollWheel);
            Event.current.delta = new Vector2(0f, 24f);
            session.OnGUI();
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        foreach (object? wait in Frames(5)) yield return wait;
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "09-AdvancedEditor.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        guiAction = () =>
        {
            CellRect sourceArea = session.Draft.sourceRect, destination = session.Draft.destination;
            File.AppendAllText(Path.Combine(Bootstrap.Output, "UXTrace.txt"), "coordinates=" + string.Join(",", (string[])HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "rectangleBuffers").GetValue(session)!) + "\n");
            ClickEditor(session, new Vector2(session.PanelRect.center.x, session.PanelRect.yMax - 82f));
            Check(!(bool)HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "preciseDirty").GetValue(session)!
                && session.Draft.sourceRect == sourceArea && session.Draft.destination == destination,
                "native Apply coordinates accepts the displayed geometry without changing it");
            HarmonyLib.AccessTools.Field(typeof(CQFMapProjectionEditSession), "panelScroll").SetValue(session, Vector2.zero);
            FireEditor(session, Vector2.zero, EventType.Repaint);
            ClickEditor(session, new Vector2(session.PanelRect.x + 80f, session.PanelRect.y + 93f));
            Check(GUIUtility.keyboardControl != 0, "layout name field accepts native keyboard focus");
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        session.Draft.name = "CQF_MP_NewName".Translate(3);
        Check(session.Commit() && component.Projections.Count == 1 && ui.Session == null, "Finish commits one validated projection and leaves edit mode");
        Check(GUIUtility.keyboardControl == 0 && GUIUtility.hotControl == 0 && CQFMapProjectionContext.Focused == component.Projections.Single(),
            "Finish and play releases text editing focus and enters the new gameplay viewport");
        CQFMapProjection saved = component.Projections.Single();
        CellRect savedArea = saved.destination;
        ui.BeginEdit(host, saved);
        ui.Session!.Draft.name = "CQF_MP_NewName".Translate(4);
        ui.Session.Draft.enabled = false;
        ui.Session.Placement.BeginMove(savedArea.CenterCell);
        ui.Session.Placement.UpdatePointer(savedArea.CenterCell + new IntVec3(5, 0, 5));
        ui.Session.Placement.EndGesture();
        ui.Session.Cancel();
        Check(component.Projections.Single() == saved && saved.enabled && saved.name == "CQF_MP_NewName".Translate(3) && saved.destination == savedArea,
            "Cancel preserves the original name, enabled state, object and geometry");
        ui.BeginEdit(host, saved);
        ui.Session!.Draft.name = "CQF_MP_NewName".Translate(4);
        Check(ui.Session.Commit() && component.Projections.Single() != saved && component.Projections.Single().name == "CQF_MP_NewName".Translate(4),
            "editing an existing projection replaces it atomically at the same list position");
        component.Remove(saved.id);
        GameComponent_MapManager.Instance.Hibernate(source!);
        foreach (object? wait in Frames(3)) yield return wait;
        Check(GameComponent_MapManager.Instance.IsHibernating(source!), "editor preview resources and source leases are released after completion");
        guiAction = () =>
        {
            Event original = Event.current;
            try
            {
                Vector2 point = new(UI.screenWidth - 90f, 70f);
                SetPointer(point, EventType.MouseDown);
                ui.GameComponentOnGUI();
                SetPointer(point, EventType.MouseUp);
                ui.GameComponentOnGUI();
            }
            finally { Bootstrap.MousePosition = null; Event.current = original; }
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        Check(Find.WindowStack.WindowOfType<Dialog_CQFMapProjections>() == null, "normal map UI no longer opens projection management directly");
        DebugActions_MapProjections.Menu().action();
        Check(Find.WindowStack.WindowOfType<Dialog_CQFMapProjections>() != null, "native CQF developer action opens projection management");
        foreach (object? wait in Frames(4)) yield return wait;
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "08-EmptyManager.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        Find.WindowStack.WindowOfType<Dialog_CQFMapProjections>()!.Close();
    }

    private Vector2 PreviewPoint(Rect image, IntVec3 cell) => new(image.x + (cell.x + 0.5f) / source!.Size.x * image.width,
        image.y + (source.Size.z - cell.z - 0.5f) / source.Size.z * image.height);

    private Vector2 HostPoint(int x, int z) => new Vector3(x + 0.5f, 0f, z + 0.5f).MapToUIPosition();

    private void ClickEditor(CQFMapProjectionEditSession session, Vector2 point)
    {
        FireEditor(session, point, EventType.MouseDown);
        FireEditor(session, point, EventType.MouseUp);
    }

    private void FireEditor(CQFMapProjectionEditSession session, Vector2 point, EventType type, int button = 0)
    {
        Event original = Event.current;
        try
        {
            SetPointer(point, type, button);
            session.OnGUI();
            Find.MapUI.HandleMapClicks();
            Find.MapUI.HandleLowPriorityInput();
        }
        finally { Bootstrap.MousePosition = null; Event.current = original; }
    }

    private void SetPointer(Vector2 point, EventType type, int button = 0)
    {
        Bootstrap.MousePosition = new Vector3(point.x * Prefs.UIScale, (UI.screenHeight - point.y) * Prefs.UIScale, 0f);
        object backend = UnityStubs.Gui.GuiBackend.Current!;
        System.Reflection.FieldInfo mouse = HarmonyLib.AccessTools.Field(backend.GetType(), "physMouse");
        mouse.SetValue(backend, Activator.CreateInstance(mouse.FieldType, point.x * Prefs.UIScale, point.y * Prefs.UIScale));
        HarmonyLib.AccessTools.Field(backend.GetType(), "physMouseOverWindow").SetValue(backend, false);
        Event.current = new Event { type = type, rawType = type, button = button, clickCount = 1, mousePosition = point };
    }

    private void RunSimulatedGUI(Action action)
    {
        object backend = UnityStubs.Gui.GuiBackend.Current!;
        System.Reflection.FieldInfo mouse = HarmonyLib.AccessTools.Field(backend.GetType(), "physMouse");
        System.Reflection.FieldInfo over = HarmonyLib.AccessTools.Field(backend.GetType(), "physMouseOverWindow");
        object? oldMouse = mouse.GetValue(backend), oldOver = over.GetValue(backend);
        Matrix4x4 matrix = GUI.matrix;
        try { UI.ApplyUIScale(); action(); }
        finally { mouse.SetValue(backend, oldMouse); over.SetValue(backend, oldOver); GUI.matrix = matrix; }
    }

    private Action? guiAction;
}
