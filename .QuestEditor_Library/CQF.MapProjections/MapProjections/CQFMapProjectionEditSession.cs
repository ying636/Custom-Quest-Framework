#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFMapProjectionEditSession : IDisposable
{
    public CQFMapProjectionEditSession(GameComponent_CQFProjectionUI owner, Map host, CQFMapProjection? original, Map? source)
    {
        this.owner = owner;
        Host = host;
        Original = original;
        previousSpeed = Find.TickManager.CurTimeSpeed;
        Draft = original?.Copy() ?? new CQFMapProjection
        {
            id = "CQF_Projection_" + Guid.NewGuid().ToString("N"),
            name = "CQF_MP_NewName".Translate(host.GetComponent<MapComponent_CQFMapProjections>().Projections.Count + 1),
            destination = new CellRect(host.Center.x - 10, host.Center.z - 10, 20, 20), mode = CQFMapProjectionMode.Control
        };
        resolutionBuffer = Draft.resolution.ToString();
        refreshBuffer = Draft.refreshFrames.ToString();
        Placed = original != null;
        Placement = new CQFMapProjectionPlacement(this);
        if (Draft.sourceMap != null) SetSource(Draft.sourceMap, false);
        else if (source != null) SetSource(source);
        else throw new InvalidOperationException("[CQF] Projection editor requires a source map.");
        Find.MainTabsRoot.SetCurrentTab(null!, playSound: false);
        Find.TickManager.Pause();
    }

    public Map Host { get; }
    public CQFMapProjection? Original { get; }
    public CQFMapProjection Draft { get; }
    public CQFMapProjectionPlacement Placement { get; }
    public bool Placed { get; internal set; }
    public bool LockAspect { get; set; } = true;
    public bool IsFinished => finished;
    public Rect PanelRect { get; private set; }
    public CQFMapProjectionCanvas Canvas { get; } = new();

    public void SetSource(Map source, bool resetArea = true)
    {
        if (source == Host || source.Disposed) throw new InvalidOperationException("[CQF] Invalid projection editor source map.");
        if (leasedSource != source)
        {
            owner.AcquireSource(source);
            sourcePreview?.Dispose();
            if (leasedSource != null) owner.ReleaseSource(leasedSource);
            leasedSource = source;
            sourcePreview = new CQFMapProjectionPreview(Host, source, 768);
        }
        Draft.sourceMap = source;
        if (resetArea) { Draft.sourceRect = CellRect.Empty; Draft.ResetView(); }
        if (!Placed)
        {
            int width = Mathf.Min(40, Host.Size.x);
            int height = Mathf.Clamp(Mathf.RoundToInt(width * source.Size.z / (float)source.Size.x), 1, Host.Size.z);
            Draft.destination = new CellRect((Host.Size.x - width) / 2, (Host.Size.z - height) / 2, width, height);
        }
        Draft.Dispose();
        SyncCellSize();
        whole = Draft.sourceRect.IsEmpty;
        preciseDirty = false;
    }

    public void SetSourceArea(CellRect area)
    {
        if (Draft.sourceMap == null || area.IsEmpty || !area.Min.InBounds(Draft.sourceMap) || !area.Max.InBounds(Draft.sourceMap))
            throw new InvalidOperationException("[CQF] Invalid source selection in projection editor.");
        Draft.sourceRect = area;
        Draft.ResetView();
        Draft.Dispose();
        SyncCellSize();
        whole = false;
        preciseDirty = false;
    }

    internal void NotifyPlacementChanged() => preciseDirty = false;

    public void SetMatchCellSize(bool value)
    {
        Draft.matchCellSize = value;
        Draft.ResetView();
        SyncCellSize();
        preciseDirty = false;
    }

    public void OnGUI()
    {
        if (finished) return;
        PanelRect = new Rect(12f, 82f, Mathf.Min(330f, UI.screenWidth - 24f), Mathf.Clamp(UI.screenHeight - 116f, 280f, 820f));
        DrawPanel();
        if (finished) return;
        Placement.OnGUI(PanelRect);
        Event ev = Event.current;
        if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape && !Find.WindowStack.AnyWindowAbsorbingAllInput)
        {
            if (Canvas.IsDragging) Canvas.Cancel();
            else if (Placement.IsDragging) Placement.CancelGesture();
            else Cancel();
            ev.Use();
        }
    }

    public void DrawMap()
    {
        if (finished || !Placed && !Placement.IsDragging) return;
        try { Draft.Draw(Host); }
        catch (Exception error)
        {
            if (drawError == error.Message) return;
            drawError = error.Message;
            Log.Error("[CQF] Projection draft rendering failed: " + error);
        }
    }

    public bool Commit()
    {
        if (finished || !Placed || Draft.sourceMap == null) return false;
        try
        {
            Draft.name = Draft.name.Trim();
            Draft.Validate(Host);
            CQFMapProjection committed = Draft.Copy();
            Host.GetComponent<MapComponent_CQFMapProjections>().Set(committed);
            Dispose();
            if (committed.CanControl) CQFMapProjectionContext.Focus(Host, committed);
            return true;
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Could not commit projection draft: " + error);
            Messages.Message("CQF_MP_Invalid".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }

    public void Cancel() => Dispose();

    public void Dispose()
    {
        if (finished) return;
        finished = true;
        Placement.CancelGesture();
        Canvas.Cancel();
        Draft.Dispose();
        sourcePreview?.Dispose();
        if (leasedSource != null) owner.ReleaseSource(leasedSource);
        owner.EndSession(this);
        GUIUtility.keyboardControl = 0;
        GUIUtility.hotControl = 0;
        if (!owner.IsDisposed && Current.ProgramState == ProgramState.Playing) Find.TickManager.CurTimeSpeed = previousSpeed;
    }

    private void SyncCellSize()
    {
        if (!Draft.matchCellSize) return;
        CellRect area = Draft.DestinationBounds;
        Draft.destination = new CellRect(Mathf.Clamp(area.minX, 0, Mathf.Max(0, Host.Size.x - area.Width)),
            Mathf.Clamp(area.minZ, 0, Mathf.Max(0, Host.Size.z - area.Height)), area.Width, area.Height);
    }

    private readonly GameComponent_CQFProjectionUI owner;
    private readonly TimeSpeed previousSpeed;
    private CQFMapProjectionPreview? sourcePreview;
    private Map? leasedSource;
    private Vector2 panelScroll;
    private bool whole;
    private bool advanced;
    private float panelContentHeight = 700f;
    private string resolutionBuffer = string.Empty;
    private string refreshBuffer = string.Empty;
    private readonly string[] rectangleBuffers = new string[8];
    private bool preciseDirty;
    private string? drawError;
    private bool finished;
}
#endif
