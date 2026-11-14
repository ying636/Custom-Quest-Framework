#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapProjectionCanvas
{
    public bool IsDragging { get; private set; }
    public Rect ImageRect { get; private set; }
    public Rect ScreenRect { get; private set; }
    public CellRect PendingArea { get; private set; }

    public void Draw(CQFMapProjectionPreview preview, Rect frame, CellRect area, bool allowSelection, Action<CellRect> apply)
    {
        Widgets.DrawBoxSolid(frame, CQFUIStyle.Canvas);
        ImageRect = preview.Fit(frame.ContractedBy(3f));
        ScreenRect = new Rect(GUIUtility.GUIToScreenPoint(Vector2.zero) + ImageRect.position, ImageRect.size);
        preview.Draw(ImageRect);
        Event ev = Event.current;
        if (GUI.enabled && allowSelection)
        {
            if (ev.type == EventType.MouseDown && ev.button == 0 && ImageRect.Contains(ev.mousePosition))
            {
                start = CellAt(ev.mousePosition, preview.Source);
                PendingArea = new CellRect(start.x, start.z, 1, 1);
                IsDragging = true;
                ev.Use();
            }
            else if (IsDragging && ev.type is EventType.MouseDrag or EventType.MouseUp)
            {
                PendingArea = CellRect.FromLimits(start, CellAt(ev.mousePosition, preview.Source));
                bool finish = ev.type == EventType.MouseUp;
                ev.Use();
                if (finish) { IsDragging = false; apply(PendingArea); }
            }
            else if (IsDragging && ev.type == EventType.MouseDown && ev.button == 1) { Cancel(); ev.Use(); }
        }
        CellRect selected = IsDragging ? PendingArea : area;
        if (!selected.IsEmpty)
        {
            Rect selectedRect = AreaRect(selected, preview.Source);
            Color previous = GUI.color;
            GUI.color = CQFUIStyle.Accent;
            Widgets.DrawBox(selectedRect, 2);
            GUI.color = previous;
            TooltipHandler.TipRegion(selectedRect, "CQF_MP_AreaSize".Translate(selected.Width, selected.Height));
        }
        CQFUIStyle.DrawBox(frame);
    }

    public void Cancel() => IsDragging = false;

    public IntVec3 CellAt(Vector2 pointer, Map source)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((pointer.x - ImageRect.x) / ImageRect.width * source.Size.x), 0, source.Size.x - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt((1f - (pointer.y - ImageRect.y) / ImageRect.height) * source.Size.z), 0, source.Size.z - 1);
        return new IntVec3(x, 0, z);
    }

    public Rect AreaRect(CellRect area, Map source) => new(ImageRect.x + area.minX * ImageRect.width / source.Size.x,
        ImageRect.y + (source.Size.z - area.maxZ - 1) * ImageRect.height / source.Size.z,
        area.Width * ImageRect.width / source.Size.x, area.Height * ImageRect.height / source.Size.z);

    private IntVec3 start;
}
#endif
