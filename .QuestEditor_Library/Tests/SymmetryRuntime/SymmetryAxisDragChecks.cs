using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.SymmetryChecks;

internal sealed class SymmetryAxisDragChecks
{
    public SymmetryAxisDragChecks(Map map, Action<bool, string> check)
    {
        this.map = map;
        this.settings = map.GetComponent<MapComponent_CQFSymmetrySettings>();
        this.drag = CQFEditorSymmetry.For(map).AxisDrag;
        this.check = check;
    }

    public bool Complete { get; private set; }

    public void RunOnGUI()
    {
        if (this.Complete || Event.current.type != EventType.Repaint) return;
        this.Complete = true;
        Event original = Event.current;
        try
        {
            GUIUtility.hotControl = 0;
            settings.enabled = true;
            settings.showAxes = true;
            settings.mirrorX = true;
            settings.mirrorZ = true;
            settings.axisX = 79;
            settings.axisZ = 80;
            Find.DesignatorManager.Select(new Designator_SpawnThing());
            Designator selected = Find.DesignatorManager.SelectedDesignator;
            check(selected != null, "drag test uses a real selected placement tool");
            int objectCount = map.listerThings.AllThings.Count;
            float scale = UI.CurUICellSize();
            Vector2 start = drag.HandleRect(1).center;
            check(drag.HandleRect(1).width > 0f && drag.HandleRect(2).width > 0f, "both drag handles are inside the rendered map viewport");
            check(Fire(EventType.MouseDown, start), "a handle captures mouse down before the selected placement tool; blocking="
                + Find.WindowStack.AnyWindowAbsorbingAllInput + ", obscured=" + Find.WindowStack.MouseObscuredNow
                + ", targeter=" + Find.Targeter.IsTargeting + ", handle=" + start);
            check(drag.IsDragging && GUIUtility.hotControl != 0, "axis dragging owns the native GUI mouse control");
            check(Fire(EventType.MouseDrag, start + Vector2.right * scale * 3.2f), "axis dragging consumes mouse movement");
            check(settings.axisX == 85 && settings.axisZ == 80, "dragging X moves and snaps the axis without moving Z");
            check(Fire(EventType.MouseUp, start + Vector2.right * scale * 3.2f), "axis dragging consumes mouse release");
            check(!drag.IsDragging && GUIUtility.hotControl == 0, "release commits the axis and releases mouse ownership");
            check(ReferenceEquals(selected, Find.DesignatorManager.SelectedDesignator) && map.listerThings.AllThings.Count == objectCount,
                "dragging preserves the selected tool and never places or deletes an object");
            start = drag.HandleRect(2).center;
            check(Fire(EventType.MouseDown, start) && drag.IsDragging, "the horizontal handle captures the mouse while a placement tool is selected");
            Fire(EventType.MouseDrag, start + Vector2.up * scale * 2.7f);
            Fire(EventType.MouseUp, start + Vector2.up * scale * 2.7f);
            check(settings.axisZ == 75, "dragging the horizontal handle moves Z with half-cell snapping");
            int oldX = settings.axisX;
            int oldZ = settings.axisZ;
            start = drag.HandleRect(1).center;
            Fire(EventType.MouseDown, start);
            Fire(EventType.MouseDrag, start + Vector2.left * scale * 4f);
            check(Fire(EventType.KeyDown, start, key: KeyCode.Escape) && settings.axisX == oldX && !drag.IsDragging,
                "Escape cancels and restores the starting axis");
            start = drag.HandleRect(2).center;
            Fire(EventType.MouseDown, start);
            Fire(EventType.MouseDrag, start + Vector2.up * scale * 3f);
            check(Fire(EventType.MouseDown, start, button: 1) && settings.axisZ == oldZ && !drag.IsDragging,
                "right click cancels without deselecting the active editing tool");
            check(ReferenceEquals(selected, Find.DesignatorManager.SelectedDesignator), "Escape and right click cancel only the axis drag");
            Vector2 linePoint = new(drag.HandleRect(1).center.x, drag.HandleRect(1).yMax + 60f);
            check(!Fire(EventType.MouseDown, linePoint, runManager: false) && !drag.IsDragging,
                "a selected editing tool keeps ordinary clicks on the axis line");
            Find.DesignatorManager.Deselect();
            check(Fire(EventType.MouseDown, linePoint) && drag.IsDragging, "with no editing tool selected the axis line itself is draggable");
            Fire(EventType.KeyDown, linePoint, key: KeyCode.Escape);
            Vector2 cross = new Vector3((settings.AxisX + 1) * 0.5f, AltitudeLayer.MetaOverlays.AltitudeFor(), (settings.AxisZ + 1) * 0.5f).MapToUIPosition();
            Fire(EventType.MouseDown, cross);
            Fire(EventType.MouseDrag, cross + new Vector2(scale, -scale * 2f));
            Fire(EventType.MouseUp, cross + new Vector2(scale, -scale * 2f));
            check(settings.axisX == oldX + 2 && settings.axisZ == oldZ + 4, "dragging the intersection moves both axes");
            start = drag.HandleRect(1).center;
            Fire(EventType.MouseDown, start);
            Fire(EventType.MouseDrag, start + Vector2.right * 10000f);
            Fire(EventType.MouseUp, start + Vector2.right * 10000f);
            check(settings.axisX == (map.Size.x - 1) * 2, "dragging beyond the map clamps the axis at its last cell");
            settings.axisX = -1;
            start = drag.HandleRect(1).center;
            Fire(EventType.MouseDown, start);
            Fire(EventType.MouseDrag, start + Vector2.left * scale * 3f);
            Fire(EventType.MouseDown, start, button: 1);
            check(settings.axisX == -1, "cancel restores the original automatic-center setting exactly");
            start = drag.HandleRect(1).center;
            Fire(EventType.MouseDown, start);
            Fire(EventType.MouseDrag, start + Vector2.right * scale * 2f);
            settings.showAxes = false;
            Fire(EventType.Repaint, start, runManager: false);
            check(!drag.IsDragging && settings.axisX == -1 && GUIUtility.hotControl == 0, "hiding the axes cancels a drag and releases input");
            settings.showAxes = true;
            start = drag.HandleRect(1).center;
            Fire(EventType.MouseDown, start);
            Fire(EventType.MouseDrag, start + Vector2.right * scale * 2f);
            DebugSettings.godMode = false;
            Fire(EventType.Repaint, start, runManager: false);
            check(!drag.IsDragging && settings.axisX == -1, "leaving god mode cancels a pending drag");
            DebugSettings.godMode = true;
            start = drag.HandleRect(1).center;
            Fire(EventType.MouseDown, start);
            Fire(EventType.MouseDrag, start + Vector2.right * scale * 2f);
            CQFSymmetryAxisDrag.ValidateActiveDrag(Find.Maps.First(value => value != map));
            check(!drag.IsDragging && settings.axisX == -1 && GUIUtility.hotControl == 0, "switching maps cancels the old map's drag");
            Dialog_CQFSymmetry dialog = new(map);
            Find.WindowStack.Add(dialog);
            check(!Fire(EventType.MouseDown, start, runManager: false) && !drag.IsDragging, "a settings dialog blocks dragging the map behind it");
            dialog.Close(false);
            Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations = true }, _ => { });
            check(!Fire(EventType.MouseDown, start, runManager: false) && !drag.IsDragging, "target selection has priority over symmetry handles");
            Find.Targeter.StopTargeting();
            check(map.listerThings.AllThings.Count == objectCount, "all axis interactions leave map objects unchanged");
            settings.axisX = 79;
            settings.axisZ = 80;
        }
        finally
        {
            Event.current = original;
            DebugSettings.godMode = true;
            settings.showAxes = true;
        }
    }

    private bool Fire(EventType type, Vector2 position, int button = 0, KeyCode key = KeyCode.None, bool runManager = true)
    {
        Event ev = new() { type = type, mousePosition = position, button = button, keyCode = key };
        Event.current = ev;
        CQFEditorBridge.SymmetryOnGUI!(map);
        bool consumed = ev.type == EventType.Used;
        if (runManager && type != EventType.Repaint) Find.DesignatorManager.ProcessInputEvents();
        return consumed;
    }

    private readonly Map map;
    private readonly MapComponent_CQFSymmetrySettings settings;
    private readonly CQFSymmetryAxisDrag drag;
    private readonly Action<bool, string> check;
}
