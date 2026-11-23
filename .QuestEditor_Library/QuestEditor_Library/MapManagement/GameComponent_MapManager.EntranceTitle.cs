using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    public void ShowMapEntranceTitle(Map map)
    {
        ValidateMap(map);
        pendingTitleMap = map;
        pendingTitleText = null;
    }

    public void ShowMapEntranceText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        pendingTitleText = text;
        pendingTitleMap = null;
    }

    private void DrawMapEntranceTitle()
    {
        if (disposed || Event.current.type != EventType.Repaint || Current.ProgramState != ProgramState.Playing ||
            LongEventHandler.AnyEventNowOrWaiting || !WorldRendererUtility.DrawingMap || Find.UIRoot.screenshotMode.Active ||
            game.CurrentMap is not Map view)
            return;

        float now = Time.realtimeSinceStartup;
        if (titleView != view || now - titleStartedAt >= TitleDuration)
            activeTitle = null;

        if (pendingTitleText is string customText)
        {
            pendingTitleText = null;
            StartMapTitle(customText, view, now);
        }
        else if (pendingTitleMap is Map requested)
        {
            pendingTitleMap = null;
            if (!requested.Disposed)
            {
                StartMapTitle(MapTitle(requested), view, now);
                if (requested == view && ShowsAutomaticEntranceTitle(view)) shownEntranceTitles.Add(view.uniqueID);
            }
        }

        if (activeTitle == null && ShowsAutomaticEntranceTitle(view) && shownEntranceTitles.Add(view.uniqueID))
            StartMapTitle(MapTitle(view), view, now);
        if (activeTitle == null) return;

        float elapsed = now - titleStartedAt;
        float fade = Mathf.Min(Mathf.Clamp01(elapsed / 0.35f), Mathf.Clamp01((TitleDuration - elapsed) / 0.55f));
        if (fade <= 0) return;

        using CQFUIScope scope = new();
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.WordWrap = false;
        Vector2 size = Text.CalcSize(activeTitle);
        size.x += 4f;
        size.y = Mathf.Max(size.y, Text.LineHeightOf(GameFont.Medium));
        float availableWidth = UI.screenWidth * 0.90f;
        float scale = Mathf.Min(TitleTextScale, Mathf.Min(availableWidth / size.x, UI.screenHeight * 0.27f / size.y));
        float rise = 5f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.35f)));
        Vector2 center = new(UI.screenWidth * 0.5f, UI.screenHeight * 0.32f + rise);
        Vector2 scaledSize = size * scale;
        Rect text = new(center - scaledSize * 0.5f, scaledSize);
        float halfRule = Mathf.Min(availableWidth * 0.47f, Mathf.Max(90f, scaledSize.x * 0.5f + 48f));
        DrawMapTitleRule(new Vector2(center.x, text.yMin - 13f), halfRule, fade * 0.86f, -1f);
        DrawMapTitleRule(new Vector2(center.x, text.yMax + 14f), halfRule * 0.8f, fade * 0.72f, 1f);

        Matrix4x4 matrix = GUI.matrix;
        Vector2 offset = (GUIUtility.GUIToScreenPoint(Vector2.zero) + center) * (1f - scale);
        GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(offset.x, offset.y, 0f),
            Quaternion.identity, new Vector3(scale, scale, 1f));
        try
        {
            Rect label = new(center - size * 0.5f, size);
            GUI.color = new Color(0f, 0f, 0f, fade * 0.45f);
            Widgets.Label(new Rect(label.x, label.y + 2f / scale, label.width, label.height), activeTitle);
            GUI.color = new Color(0f, 0f, 0f, fade * 0.8f);
            Widgets.Label(new Rect(label.x + 1f / scale, label.y + 1f / scale, label.width, label.height), activeTitle);
            Color foreground = CQFUIStyle.TextColor;
            foreground.a = fade;
            GUI.color = foreground;
            Widgets.Label(label, activeTitle);
        }
        finally
        {
            GUI.matrix = matrix;
        }
    }

    private static void DrawMapTitleRule(Vector2 center, float halfWidth, float opacity, float direction)
    {
        float size = Mathf.Clamp(halfWidth / 140f, 0.55f, 1.5f);
        Color silver = CQFUIStyle.Accent;
        silver.a = opacity;
        Color dim = silver;
        dim.a *= 0.48f;
        Color faint = silver;
        faint.a *= 0.23f;
        Vector2 tip = center + new Vector2(0f, 7f * size);
        Vector2 basePoint = center - new Vector2(0f, 7f * size);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * size;
            float y = direction * size;
            Vector2 vertex = center + new Vector2(10f * x, 0f);
            Vector2 inner = center + new Vector2(15f * x, 0f);
            Vector2 shoulder = center + new Vector2(side * (halfWidth - 27f * size), 0f);
            Vector2 end = center + new Vector2(side * halfWidth, 0f);
            Widgets.DrawLine(tip, vertex, silver, 1.2f);
            Widgets.DrawLine(vertex, basePoint, silver, 1.2f);
            Widgets.DrawLine(inner, shoulder, dim, 1f);
            Widgets.DrawLine(shoulder, end, faint, 1f);
            DrawMapTitleCurve(inner, center + new Vector2(35f * x, 20f * y),
                center + new Vector2(53f * x, -4f * y), center + new Vector2(65f * x, 0f), silver);
            DrawMapTitleCurve(inner, center + new Vector2(28f * x, -12f * y),
                center + new Vector2(36f * x, 1f * y), center + new Vector2(52f * x, 0f), dim);
            DrawMapTitleCurve(shoulder - new Vector2(22f * x, 0f), shoulder + new Vector2(-7f * x, 0f),
                shoulder + new Vector2(2f * x, 9f * y), shoulder + new Vector2(10f * x, 5f * y), dim);
        }
        Widgets.DrawBoxSolid(new Rect(center.x - size, center.y - size, 2f * size, 2f * size), silver);
    }

    private static void DrawMapTitleCurve(Vector2 start, Vector2 first, Vector2 second, Vector2 end, Color color)
    {
        Vector2 previous = start;
        for (int segment = 1; segment <= 8; segment++)
        {
            float t = segment / 8f;
            float remaining = 1f - t;
            Vector2 point = start * (remaining * remaining * remaining) + first * (3f * remaining * remaining * t) +
                second * (3f * remaining * t * t) + end * (t * t * t);
            Widgets.DrawLine(previous, point, color, 1f);
            previous = point;
        }
    }

    private void StartMapTitle(string title, Map view, float now)
    {
        activeTitle = title;
        titleView = view;
        titleStartedAt = now;
    }

    private bool ShowsAutomaticEntranceTitle(Map map) => map.Parent switch
    {
        CQFCompositeMapParent parent => parent.mapDataDef?.showEntranceTitle == true ||
            GetCompositeMap(map)?.Definition?.showEntranceTitle == true,
        MapParent_Custom parent => parent.mapDataDef?.showEntranceTitle == true,
        MainSite site => site.mainMapDef?.showEntranceTitle == true || site.mapDef?.showEntranceTitle == true,
        CustomSite site => site.mapDef?.showEntranceTitle == true,
        _ => false
    };

    internal static string MapTitle(Map map)
    {
        string? title = map.Parent switch
        {
            MapParent_Custom parent => parent.MapName,
            MainSite { mainMapDef: { } main } => main.LabelCap.ToString(),
            CustomSite { mapDef: { } data } => data.LabelCap.ToString(),
            _ => null
        };
        return !string.IsNullOrWhiteSpace(title) ? title : map.Parent?.LabelCap.ToString() ?? map.GetUniqueLoadID();
    }

    private const float TitleDuration = 2.8f;
    private const float TitleTextScale = 4.8f;
    private HashSet<int> shownEntranceTitles = new();
    private Map? pendingTitleMap;
    private string? pendingTitleText;
    private Map? titleView;
    private string? activeTitle;
    private float titleStartedAt;
}
