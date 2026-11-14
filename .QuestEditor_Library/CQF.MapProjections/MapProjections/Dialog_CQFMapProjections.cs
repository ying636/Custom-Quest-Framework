#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Dialog_CQFMapProjections : Window
{
    public Dialog_CQFMapProjections(Map host)
    {
        this.host = host;
        doCloseX = true;
        forcePause = true;
        draggable = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
    }

    public override Vector2 InitialSize => new(950f, 650f);

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width);
        if (closed) return;
        if (host.Disposed) { Close(); return; }
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width - 220f, 36f), "CQF_MP_Title".Translate());
        Text.Font = GameFont.Small;
        if (CQFUIStyle.ButtonText(new Rect(inRect.width - 210f, 0f, 180f, 34f), "CQF_MP_Add".Translate()))
        {
            Find.WindowStack.Add(new Dialog_CQFProjectionSourcePicker(host, source =>
            {
                GameComponent_CQFProjectionUI.Instance!.BeginEdit(host, source: source);
                Close();
            }));
        }
        CQFMapProjection[] projections = host.GetComponent<MapComponent_CQFMapProjections>().Projections.ToArray();
        if (projections.Length == 0)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = CQFUIStyle.Muted;
            Widgets.Label(new Rect(0f, 80f, inRect.width, inRect.height - 120f), "CQF_MP_Empty".Translate());
            return;
        }
        Rect list = new(0f, 52f, inRect.width, inRect.height - 52f);
        Widgets.BeginScrollView(list, ref scroll, new Rect(0f, 0f, list.width - 16f, projections.Length * 150f));
        for (int index = 0; index < projections.Length && !closed; index++)
        {
            if (index * 150f + 140f < scroll.y || index * 150f > scroll.y + list.height) continue;
            DrawRow(new Rect(0f, index * 150f, list.width - 20f, 140f), projections[index]);
        }
        Widgets.EndScrollView();
    }

    public override void PostClose()
    {
        closed = true;
        foreach (CQFMapProjectionPreview preview in previews.Values) preview.Dispose();
        previews.Clear();
        base.PostClose();
    }

    private void DrawRow(Rect row, CQFMapProjection projection)
    {
        Widgets.DrawBoxSolid(row, CQFUIStyle.Card);
        CQFUIStyle.DrawBox(row);
        if (projection.sourceMap is { Disposed: false } source)
        {
            if (!previews.TryGetValue(projection, out CQFMapProjectionPreview? preview))
                previews[projection] = preview = new CQFMapProjectionPreview(host, source, area: projection.sourceRect);
            Rect frame = new(row.x + 10f, row.y + 10f, 176f, row.height - 20f);
            Widgets.DrawBoxSolid(frame, CQFUIStyle.Canvas);
            preview.Draw(preview.Fit(frame));
        }
        float x = row.x + 200f;
        float width = row.width - 214f;
        if (renaming == projection)
        {
            renameBuffer = Widgets.TextField(new Rect(x, row.y + 10f, width - 88f, 30f), renameBuffer);
            if (CQFUIStyle.ButtonText(new Rect(x + width - 80f, row.y + 10f, 80f, 30f), "CQF_MP_Finish".Translate()))
            {
                projection.name = renameBuffer.Trim();
                renaming = null;
            }
        }
        else
        {
            Widgets.Label(new Rect(x, row.y + 10f, width - 94f, 30f), projection.DisplayName);
            if (CQFUIStyle.ButtonText(new Rect(x + width - 88f, row.y + 10f, 88f, 28f), "CQF_MP_Rename".Translate()))
            {
                renaming = projection;
                renameBuffer = projection.DisplayName;
            }
        }
        GUI.color = CQFUIStyle.Muted;
        Widgets.Label(new Rect(x, row.y + 46f, width, 25f), projection.SourceName);
        Widgets.Label(new Rect(x, row.y + 73f, width, 25f), "CQF_MP_RowInfo".Translate(
            (projection.mode == CQFMapProjectionMode.Control ? "CQF_MP_Play" : "CQF_MP_Display").Translate(),
            projection.DestinationBounds.Width, projection.DestinationBounds.Height));
        GUI.color = CQFUIStyle.TextColor;
        float buttonWidth = (width - 18f) / 4f;
        float y = row.y + 102f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, buttonWidth, 28f), (projection.enabled ? "CQF_MP_On" : "CQF_MP_Off").Translate()))
        {
            if (!projection.enabled) GameComponent_MapManager.Instance!.Activate(projection.sourceMap!);
            projection.enabled = !projection.enabled;
            projection.Dispose();
        }
        if (CQFUIStyle.ButtonText(new Rect(x + buttonWidth + 6f, y, buttonWidth, 28f), "CQF_MP_Edit".Translate()))
        {
            GameComponent_CQFProjectionUI.Instance!.BeginEdit(host, projection);
            Close();
        }
        if (CQFUIStyle.ButtonText(new Rect(x + (buttonWidth + 6f) * 2f, y, buttonWidth, 28f), "CQF_MP_Locate".Translate()))
        {
            CameraJumper.TryJump(projection.DestinationBounds.CenterCell, host, CameraJumper.MovementMode.Cut);
            Close();
        }
        if (CQFUIStyle.ButtonText(new Rect(x + (buttonWidth + 6f) * 3f, y, buttonWidth, 28f), "CQF_MP_Delete".Translate()))
        {
            host.GetComponent<MapComponent_CQFMapProjections>().Remove(projection.id);
            if (previews.Remove(projection, out CQFMapProjectionPreview? preview)) preview.Dispose();
        }
        TooltipHandler.TipRegion(row, "CQF_MP_ID".Translate() + ": " + projection.id);
    }

    private readonly Map host;
    private readonly Dictionary<CQFMapProjection, CQFMapProjectionPreview> previews = new();
    private CQFMapProjection? renaming;
    private string renameBuffer = string.Empty;
    private Vector2 scroll;
    private bool closed;
}
#endif
