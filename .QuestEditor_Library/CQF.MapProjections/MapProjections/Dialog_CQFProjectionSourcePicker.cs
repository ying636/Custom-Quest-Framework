#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Dialog_CQFProjectionSourcePicker : Window
{
    public Dialog_CQFProjectionSourcePicker(Map host, Action<Map> select)
    {
        this.host = host;
        this.select = select;
        sources = Current.Game.Maps.Concat(GameComponent_MapManager.Instance!.HibernatingMaps)
            .Where(map => map != host && !map.Disposed).Distinct().ToList();
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
    }

    public override Vector2 InitialSize => new(950f, 690f);

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width);
        if (closed) return;
        if (host.Disposed) { Close(); return; }
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 34f), "CQF_MP_ChooseSource".Translate());
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(0f, 44f, 94f, 30f), "CQF_MP_Search".Translate());
        search = Widgets.TextField(new Rect(100f, 44f, inRect.width - 100f, 30f), search);
        List<Map> visible = sources.Where(map => !map.Disposed && (map.Parent?.LabelCap is { Length: > 0 } label ? label : "CQF_MP_MapName".Translate(map.uniqueID).ToString())
            .IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        if (visible.Count == 0)
        {
            GUI.color = CQFUIStyle.Muted;
            Widgets.Label(new Rect(0f, 100f, inRect.width, 50f), "CQF_MP_NoMaps".Translate());
            return;
        }
        Rect list = new(0f, 88f, inRect.width, inRect.height - 88f);
        int columns = Mathf.Max(1, Mathf.FloorToInt((list.width - 16f) / 280f));
        float width = (list.width - 16f - (columns - 1) * 12f) / columns;
        Widgets.BeginScrollView(list, ref scroll, new Rect(0f, 0f, list.width - 16f, Mathf.CeilToInt(visible.Count / (float)columns) * 246f));
        for (int i = 0; i < visible.Count; i++)
        {
            Map source = visible[i];
            Rect card = new(i % columns * (width + 12f), i / columns * 246f, width, 234f);
            if (card.yMax < scroll.y || card.y > scroll.y + list.height) continue;
            if (!previews.TryGetValue(source, out CQFMapProjectionPreview? preview)) previews[source] = preview = new CQFMapProjectionPreview(host, source);
            CQFUIStyle.DrawBackground(card);
            Rect frame = new(card.x + 10f, card.y + 10f, width - 20f, 158f);
            Widgets.DrawBoxSolid(frame, CQFUIStyle.Canvas);
            preview.Draw(preview.Fit(frame));
            Widgets.Label(new Rect(card.x + 10f, card.y + 177f, width - 20f, 28f), source.Parent?.LabelCap is { Length: > 0 } label ? label : "CQF_MP_MapName".Translate(source.uniqueID).ToString());
            GUI.color = CQFUIStyle.Muted;
            Widgets.Label(new Rect(card.x + 10f, card.y + 205f, width - 20f, 24f), "CQF_MP_AreaSize".Translate(source.Size.x, source.Size.z));
            GUI.color = CQFUIStyle.TextColor;
            if (Widgets.ButtonInvisible(card))
            {
                select(source);
                Close();
                break;
            }
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

    private readonly Map host;
    private readonly Action<Map> select;
    private readonly List<Map> sources;
    private readonly Dictionary<Map, CQFMapProjectionPreview> previews = new();
    private string search = string.Empty;
    private Vector2 scroll;
    private bool closed;
}
#endif
