#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapProjectionPlacement
{
    public CQFMapProjectionPlacement(CQFMapProjectionEditSession session) => this.session = session;

    public bool IsDragging => operation != 0;
    public bool PlaceMode { get; set; }
    public Rect ScreenRect { get; private set; }
    public IReadOnlyList<Rect> Handles => handles;

    public void OnGUI(Rect panel)
    {
        CellRect area = session.Draft.DestinationBounds;
        Vector2 first = new Vector3(area.minX, 0f, area.minZ).MapToUIPosition();
        Vector2 second = new Vector3(area.maxX + 1f, 0f, area.maxZ + 1f).MapToUIPosition();
        ScreenRect = Rect.MinMaxRect(Mathf.Min(first.x, second.x), Mathf.Min(first.y, second.y), Mathf.Max(first.x, second.x), Mathf.Max(first.y, second.y));
        Vector2[] corners = { ScreenRect.min, new(ScreenRect.xMax, ScreenRect.yMin), ScreenRect.max, new(ScreenRect.xMin, ScreenRect.yMax) };
        for (int i = 0; i < 4; i++) handles[i] = new Rect(corners[i].x - 6f, corners[i].y - 6f, 12f, 12f);
        if (session.Placed || IsDragging)
        {
            Color old = GUI.color;
            GUI.color = CQFUIStyle.Accent;
            Widgets.DrawBox(ScreenRect, 2);
            if (!session.Draft.matchCellSize)
                foreach (Rect handle in handles) Widgets.DrawBoxSolid(handle, CQFUIStyle.Accent);
            GUI.color = old;
            Rect label = new(ScreenRect.x, ScreenRect.y - 28f, ScreenRect.width, 24f);
            Widgets.Label(label, "CQF_MP_AreaSize".Translate(area.Width, area.Height));
        }
        if (Find.WindowStack.AnyWindowAbsorbingAllInput) return;
        Event ev = Event.current;
        if (ev.type == EventType.MouseDown && ev.button == 1 && IsDragging) { CancelGesture(); ev.Use(); return; }
        if (ev.type == EventType.MouseDown && ev.button == 0 && !panel.Contains(ev.mousePosition)
            && Find.WindowStack.GetWindowAt(UI.MousePositionOnUIInverted) == null && Find.ColonistBar.ColonistOrCorpseAt(UI.MousePositionOnUIInverted) == null)
        {
            IntVec3 cell = UI.MouseCell();
            if (!cell.InBounds(session.Host)) return;
            int corner = Array.FindIndex(handles, rect => rect.Contains(ev.mousePosition));
            if (PlaceMode || !session.Placed) BeginPlace(cell);
            else if (corner >= 0 && !session.Draft.matchCellSize) BeginResize(corner, cell);
            else if (ScreenRect.Contains(ev.mousePosition)) BeginMove(cell);
            else return;
            ev.Use();
        }
        else if (IsDragging && ev.type is EventType.MouseDrag or EventType.MouseUp)
        {
            UpdatePointer(UI.MouseCell());
            bool done = ev.type == EventType.MouseUp;
            ev.Use();
            if (done) EndGesture();
        }
    }

    public void BeginPlace(IntVec3 cell)
    {
        CellRect source = session.Draft.SourceBounds;
        if (session.Draft.matchCellSize && (source.Width > session.Host.Size.x || source.Height > session.Host.Size.z))
        {
            Messages.Message("CQF_MP_CellSizeTooLarge".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }
        Begin(1, cell);
        session.Draft.destination = session.Draft.matchCellSize
            ? new CellRect(Mathf.Clamp(cell.x, 0, session.Host.Size.x - source.Width), Mathf.Clamp(cell.z, 0, session.Host.Size.z - source.Height), source.Width, source.Height)
            : new CellRect(cell.x, cell.z, 1, 1);
    }

    public void BeginMove(IntVec3 cell) => Begin(2, cell);

    public void BeginResize(int corner, IntVec3 cell)
    {
        if (session.Draft.matchCellSize) return;
        Begin(3 + corner, cell);
        int x = corner is 0 or 3 ? before.maxX : before.minX;
        int z = corner is 0 or 1 ? before.minZ : before.maxZ;
        anchor = new IntVec3(x, 0, z);
    }

    public void UpdatePointer(IntVec3 pointer)
    {
        if (!IsDragging) return;
        session.NotifyPlacementChanged();
        pointer.x = Mathf.Clamp(pointer.x, 0, session.Host.Size.x - 1);
        pointer.z = Mathf.Clamp(pointer.z, 0, session.Host.Size.z - 1);
        if (operation == 2)
        {
            int x = Mathf.Clamp(before.minX + pointer.x - start.x, 0, session.Host.Size.x - before.Width);
            int z = Mathf.Clamp(before.minZ + pointer.z - start.z, 0, session.Host.Size.z - before.Height);
            session.Draft.destination = new CellRect(x, z, before.Width, before.Height);
            return;
        }
        if (session.Draft.matchCellSize)
        {
            CellRect source = session.Draft.SourceBounds;
            session.Draft.destination = new CellRect(Mathf.Clamp(pointer.x, 0, session.Host.Size.x - source.Width),
                Mathf.Clamp(pointer.z, 0, session.Host.Size.z - source.Height), source.Width, source.Height);
            return;
        }
        IntVec3 origin = operation == 1 ? start : anchor;
        CellRect rect = CellRect.FromLimits(origin, pointer);
        if (session.LockAspect)
        {
            CellRect source = session.Draft.SourceBounds;
            float ratio = source.Width / (float)source.Height;
            int width = rect.Width, height = rect.Height;
            if (width / (float)height > ratio) height = Mathf.Max(1, Mathf.RoundToInt(width / ratio));
            else width = Mathf.Max(1, Mathf.RoundToInt(height * ratio));
            int directionX = pointer.x >= origin.x ? 1 : -1;
            int directionZ = pointer.z >= origin.z ? 1 : -1;
            int maxWidth = directionX > 0 ? session.Host.Size.x - origin.x : origin.x + 1;
            int maxHeight = directionZ > 0 ? session.Host.Size.z - origin.z : origin.z + 1;
            float scale = Mathf.Min(1f, Mathf.Min(maxWidth / (float)width, maxHeight / (float)height));
            width = Mathf.Max(1, Mathf.FloorToInt(width * scale));
            height = Mathf.Max(1, Mathf.FloorToInt(height * scale));
            rect = new CellRect(directionX > 0 ? origin.x : origin.x - width + 1,
                directionZ > 0 ? origin.z : origin.z - height + 1, width, height);
        }
        session.Draft.destination = rect;
    }

    public void EndGesture()
    {
        if (!IsDragging) return;
        operation = 0;
        PlaceMode = false;
        session.Placed = true;
    }

    public void CancelGesture()
    {
        if (!IsDragging) return;
        session.Draft.destination = before;
        session.Placed = wasPlaced;
        operation = 0;
    }

    private void Begin(int action, IntVec3 cell)
    {
        before = session.Draft.DestinationBounds;
        wasPlaced = session.Placed;
        start = cell;
        operation = action;
    }

    private readonly CQFMapProjectionEditSession session;
    private readonly Rect[] handles = new Rect[4];
    private CellRect before;
    private IntVec3 start;
    private IntVec3 anchor;
    private int operation;
    private bool wasPlaced;
}
#endif
