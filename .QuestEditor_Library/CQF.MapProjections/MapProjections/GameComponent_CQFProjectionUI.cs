#if RIMWORLD_1_7
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class GameComponent_CQFProjectionUI : GameComponent, IDisposable
{
    public GameComponent_CQFProjectionUI(Game game) { }

    public static GameComponent_CQFProjectionUI? Instance => Current.Game?.GetComponent<GameComponent_CQFProjectionUI>();
    public CQFMapProjectionEditSession? Session { get; private set; }
    public bool IsDisposed => disposed;

    public void OpenManager(Map host)
    {
        CQFMapProjectionContext.ReturnToHost();
        if (Session != null) return;
        Dialog_CQFMapProjections? existing = Find.WindowStack.WindowOfType<Dialog_CQFMapProjections>();
        if (existing != null) Find.WindowStack.Notify_ManuallySetFocus(existing);
        else Find.WindowStack.Add(new Dialog_CQFMapProjections(host));
    }

    public void BeginEdit(Map host, CQFMapProjection? original = null, Map? source = null)
    {
        Session?.Cancel();
        CQFMapProjectionContext.ReturnToHost();
        Session = new CQFMapProjectionEditSession(this, host, original, source);
    }

    public void EndSession(CQFMapProjectionEditSession session)
    {
        if (ReferenceEquals(Session, session)) Session = null;
    }

    public bool IsSourceHeld(Map map) => sourceUsers.ContainsKey(map);

    public void AcquireSource(Map source)
    {
        if (sourceUsers.TryGetValue(source, out int count)) sourceUsers[source] = count + 1;
        else
        {
            sourceWasSleeping[source] = GameComponent_MapManager.Instance!.IsHibernating(source);
            sourceUsers[source] = 1;
        }
        try { GameComponent_MapManager.Instance!.Activate(source); }
        catch { ReleaseSource(source); throw; }
    }

    public void ReleaseSource(Map source)
    {
        if (!sourceUsers.TryGetValue(source, out int count)) return;
        if (count > 1) { sourceUsers[source] = count - 1; return; }
        bool restore = sourceWasSleeping[source];
        sourceUsers.Remove(source);
        sourceWasSleeping.Remove(source);
        if (!disposed && restore && !source.Disposed && source != CQFMapProjectionContext.HostMap
            && !MapComponent_CQFMapProjections.KeepsActive(source)) GameComponent_MapManager.Instance?.Hibernate(source);
    }

    public override void GameComponentUpdate()
    {
        if (disposed || Current.ProgramState != ProgramState.Playing) return;
        if (Session != null)
        {
            if (CQFMapProjectionContext.HostMap != Session.Host || Session.Host.Disposed || Session.Draft.sourceMap?.Disposed == true) Session.Cancel();
            else Find.TickManager.Pause();
        }
        foreach (Map source in sourceUsers.Keys.ToArray())
            if (!source.Disposed) GameComponent_MapManager.Instance?.Activate(source);
        CQFMapProjectionNavigation.Update();
    }

    public override void GameComponentOnGUI()
    {
        if (disposed || Current.ProgramState != ProgramState.Playing || !WorldRendererUtility.DrawingMap
            || CQFMapProjectionContext.HostMap is not { } host || Find.ScreenshotModeHandler.Active) return;
        using CQFUIScope scope = new(UI.screenWidth, UI.screenHeight);
        if (Session != null) { Session.OnGUI(); return; }
        if (Find.WindowStack.AnyWindowAbsorbingAllInput) return;
        DrawViewportFrames(host);
        if (CQFMapProjectionContext.Focused is { } focused)
        {
            Rect status = new(UI.screenWidth - 342f, 94f, 330f, 28f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(status, "CQF_MP_Playing".Translate(focused.DisplayName));
            TooltipHandler.TipRegion(status, (focused.CanZoom ? "CQF_MP_NavigateTip" : "CQF_MP_NavigateLockedTip").Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            if (CQFUIStyle.ButtonText(new Rect(UI.screenWidth - 162f, 126f, 150f, 32f), "CQF_MP_ReturnHost".Translate()))
                CQFMapProjectionContext.ReturnToHost();
            if (CQFUIStyle.ButtonText(new Rect(UI.screenWidth - 320f, 126f, 150f, 32f), "CQF_MP_ResetView".Translate()))
            {
                if (focused.CanZoom) focused.ResetView();
                else focused.CenterView(focused.SourceBounds.CenterCell);
            }
        }
    }

    public void DrawSession(Map host)
    {
        if (Session?.Host == host) Session.DrawMap();
    }

    public void NotifyMapRemoved(Map map)
    {
        if (Session?.Host == map || Session?.Draft.sourceMap == map) Session.Cancel();
    }

    public void PrepareForSave()
    {
        Session?.Cancel();
        CloseWindows();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Session?.Cancel();
        CloseWindows();
        sourceUsers.Clear();
        sourceWasSleeping.Clear();
        CQFMapProjectionContext.ReturnToHost();
    }

    private void DrawViewportFrames(Map host)
    {
        Camera camera = CQFMapProjectionContext.Current?.HostCamera ?? Current.Camera;
        foreach (CQFMapProjection projection in host.GetComponent<MapComponent_CQFMapProjections>().Projections)
        {
            if (!projection.CanControl) continue;
            Rect rect = projection.ScreenRect(camera);
            if (CQFMapProjectionContext.Focused != projection && !Mouse.IsOver(rect)) continue;
            Color previous = GUI.color;
            GUI.color = CQFUIStyle.Accent;
            Widgets.DrawBox(rect, 1);
            GUI.color = previous;
            TooltipHandler.TipRegion(rect, (projection.CanZoom ? "CQF_MP_NavigateTip" : "CQF_MP_NavigateLockedTip").Translate());
        }
    }

    private void CloseWindows()
    {
        Find.WindowStack.WindowOfType<Dialog_CQFMapProjections>()?.Close(false);
        Find.WindowStack.WindowOfType<Dialog_CQFProjectionSourcePicker>()?.Close(false);
    }

    private readonly Dictionary<Map, int> sourceUsers = new();
    private readonly Dictionary<Map, bool> sourceWasSleeping = new();
    private bool disposed;
}
#endif
