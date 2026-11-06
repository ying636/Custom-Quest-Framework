using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFSymmetryAxisDrag
    {
        public CQFSymmetryAxisDrag(MapComponent_CQFSymmetrySettings settings)
        {
            this.settings = settings;
        }

        public bool IsDragging => this.draggedAxes != 0;
        public int HoveredAxes { get; private set; }

        public static void ValidateActiveDrag(Map map)
        {
            if (activeDrag != null && activeDrag.TryGetTarget(out var drag)
                && (drag.settings.map != map || !drag.settings.Active || !drag.settings.showAxes
                    || (drag.draggedAxes & drag.settings.Axes) != drag.draggedAxes || Time.frameCount - drag.lastFrame > 1))
                drag.Cancel();
        }

        public Rect HandleRect(int axis)
        {
            Rect line = this.LineRect(axis);
            if (line.width <= 0f || line.height <= 0f) return Rect.zero;
            return axis == 1
                ? new Rect(line.center.x - 20f, line.yMin + 12f, 40f, 24f)
                : new Rect(line.xMax - 36f, line.center.y - 20f, 24f, 40f);
        }

        public void OnGUI()
        {
            Event ev = Event.current;
            int id = GUIUtility.GetControlID(41394271, FocusType.Passive);
            this.lastFrame = Time.frameCount;
            bool canInteract = !Find.WindowStack.AnyWindowAbsorbingAllInput && !Find.WindowStack.MouseObscuredNow
                && !Find.Targeter.IsTargeting && ev.mousePosition.y >= 90f && ev.mousePosition.y < UI.screenHeight - 180f;
            this.HoveredAxes = canInteract ? this.HitTest(ev.mousePosition, Find.DesignatorManager.SelectedDesignator == null) : 0;
            if (this.IsDragging)
            {
                if (GUIUtility.hotControl != this.controlId || Find.WindowStack.AnyWindowAbsorbingAllInput
                    || ev.type == EventType.Ignore)
                {
                    this.Cancel();
                }
                else if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape
                    || ev.type == EventType.MouseDown && ev.button == 1)
                {
                    this.Cancel();
                    ev.Use();
                }
                else if ((ev.type == EventType.MouseDrag || ev.type == EventType.MouseUp) && ev.button == 0)
                {
                    Vector3 point = UI.UIToMapPosition(new Vector2(ev.mousePosition.x, UI.screenHeight - ev.mousePosition.y));
                    if ((this.draggedAxes & 1) != 0)
                        this.settings.axisX = Mathf.RoundToInt(Mathf.Clamp(this.initialX + (point.x - this.origin.x) * 2f, 0f, (this.settings.map.Size.x - 1) * 2f));
                    if ((this.draggedAxes & 2) != 0)
                        this.settings.axisZ = Mathf.RoundToInt(Mathf.Clamp(this.initialZ + (point.z - this.origin.z) * 2f, 0f, (this.settings.map.Size.z - 1) * 2f));
                    if (ev.type == EventType.MouseUp) this.Release();
                    ev.Use();
                }
                else if (ev.type == EventType.MouseDown || ev.type == EventType.ScrollWheel)
                {
                    ev.Use();
                }
            }
            else if (canInteract && this.HoveredAxes != 0 && ev.type == EventType.MouseDown && ev.button == 0 && GUIUtility.hotControl == 0)
            {
                using (new CQFSymmetryScope(CQFEditorSymmetry.For(this.settings.map))) { }
                this.savedX = this.settings.axisX;
                this.savedZ = this.settings.axisZ;
                this.initialX = this.settings.AxisX;
                this.initialZ = this.settings.AxisZ;
                this.origin = UI.UIToMapPosition(new Vector2(ev.mousePosition.x, UI.screenHeight - ev.mousePosition.y));
                this.draggedAxes = this.HoveredAxes;
                this.controlId = id;
                GUIUtility.hotControl = id;
                activeDrag = new System.WeakReference<CQFSymmetryAxisDrag>(this);
                ev.Use();
            }
            this.DrawAxis(1);
            this.DrawAxis(2);
        }

        private Rect LineRect(int axis, bool forInput = true)
        {
            if ((this.settings.Axes & axis) == 0) return Rect.zero;
            float height = AltitudeLayer.MetaOverlays.AltitudeFor();
            Vector2 topLeft = new Vector3(0f, height, this.settings.map.Size.z).MapToUIPosition();
            Vector2 bottomRight = new Vector3(this.settings.map.Size.x, height, 0f).MapToUIPosition();
            float left = Mathf.Max(forInput ? 32f : 0f, topLeft.x);
            float right = Mathf.Min(forInput ? UI.screenWidth - 240f : UI.screenWidth, bottomRight.x);
            float top = Mathf.Max(forInput ? 100f : 0f, topLeft.y);
            float bottom = Mathf.Min(forInput ? UI.screenHeight - 190f : UI.screenHeight, bottomRight.y);
            if (right - left < (forInput ? 44f : 1f) || bottom - top < (forInput ? 44f : 1f)) return Rect.zero;
            Vector2 center = new Vector3((this.settings.AxisX + 1) * 0.5f, height, (this.settings.AxisZ + 1) * 0.5f).MapToUIPosition();
            if (axis == 1) return center.x >= left && center.x <= right ? new Rect(center.x - 6f, top, 12f, bottom - top) : Rect.zero;
            return center.y >= top && center.y <= bottom ? new Rect(left, center.y - 6f, right - left, 12f) : Rect.zero;
        }

        private int HitTest(Vector2 point, bool includeLines)
        {
            int axes = 0;
            for (int axis = 1; axis <= 2; axis++)
            {
                Rect handle = this.HandleRect(axis);
                if (handle.width > 0f && handle.Contains(point)) return axis;
                if (includeLines && this.LineRect(axis).Contains(point)) axes |= axis;
            }
            return axes;
        }

        private void DrawAxis(int axis)
        {
            Rect line = this.LineRect(axis, false);
            if (line.width <= 0f || line.height <= 0f) return;
            bool highlighted = ((this.HoveredAxes | this.draggedAxes) & axis) != 0;
            Color color = highlighted ? CQFUIStyle.TextColor : CQFUIStyle.Accent;
            float outlineWidth = highlighted ? 7f : 5f;
            float lineWidth = highlighted ? 4f : 3f;
            Rect outline = axis == 1
                ? new Rect(line.center.x - outlineWidth * 0.5f, line.y, outlineWidth, line.height)
                : new Rect(line.x, line.center.y - outlineWidth * 0.5f, line.width, outlineWidth);
            Rect stroke = axis == 1
                ? new Rect(line.center.x - lineWidth * 0.5f, line.y, lineWidth, line.height)
                : new Rect(line.x, line.center.y - lineWidth * 0.5f, line.width, lineWidth);
            Widgets.DrawBoxSolid(outline, CQFUIStyle.Canvas);
            Widgets.DrawBoxSolid(stroke, color);
            Rect handle = this.HandleRect(axis);
            if (handle.width <= 0f) return;
            Widgets.DrawBoxSolid(handle, color);
            Widgets.DrawBoxSolid(handle.ContractedBy(1.5f), highlighted ? CQFUIStyle.Hover : CQFUIStyle.Button);
            Vector2 direction = axis == 1 ? Vector2.right : Vector2.up;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Vector2 start = handle.center - direction * 11f;
            Vector2 end = handle.center + direction * 11f;
            Widgets.DrawLine(start, end, color, 1f);
            Widgets.DrawLine(start, start + direction * 5f + normal * 4f, color, 1f);
            Widgets.DrawLine(start, start + direction * 5f - normal * 4f, color, 1f);
            Widgets.DrawLine(end, end - direction * 5f + normal * 4f, color, 1f);
            Widgets.DrawLine(end, end - direction * 5f - normal * 4f, color, 1f);
            if ((this.HoveredAxes & axis) != 0 && !this.IsDragging)
                TooltipHandler.TipRegion(handle.Contains(Event.current.mousePosition) ? handle : this.LineRect(axis),
                    (axis == 1 ? "CQF_SymmetryAxisX" : "CQF_SymmetryAxisZ").Translate() + "\n" + "CQF_SymmetryDragTip".Translate());
        }

        private void Cancel()
        {
            this.settings.axisX = this.savedX;
            this.settings.axisZ = this.savedZ;
            this.Release();
        }

        private void Release()
        {
            if (GUIUtility.hotControl == this.controlId) GUIUtility.hotControl = 0;
            this.draggedAxes = 0;
            activeDrag = null;
        }

        private readonly MapComponent_CQFSymmetrySettings settings;
        private static System.WeakReference<CQFSymmetryAxisDrag>? activeDrag;
        private int draggedAxes;
        private int controlId;
        private int initialX;
        private int initialZ;
        private int savedX;
        private int savedZ;
        private int lastFrame;
        private Vector3 origin;
    }
}
