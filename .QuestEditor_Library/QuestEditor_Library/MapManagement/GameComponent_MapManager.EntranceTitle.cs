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
        Text.WordWrap = true;
        float width = Mathf.Min(900, UI.screenWidth - 32);
        float height = Mathf.Min(Text.CalcHeight(activeTitle, width), UI.screenHeight * 0.25f);
        Rect text = new((UI.screenWidth - width) / 2, UI.screenHeight * 0.72f - height / 2, width, height);
        GUI.color = new Color(0, 0, 0, fade);
        Widgets.Label(new Rect(text.x + 1, text.y + 2, text.width, text.height), activeTitle);
        GUI.color = new Color(1, 1, 1, fade);
        Widgets.Label(text, activeTitle);
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
            GetCompositeMap(map)?.Definition.showEntranceTitle == true,
        MapParent_Custom parent => parent.mapDataDef?.showEntranceTitle == true,
        MainSite site => site.mainMapDef?.showEntranceTitle == true || site.mapDef?.showEntranceTitle == true,
        CustomSite site => site.mapDef?.showEntranceTitle == true,
        _ => false
    };

    private static string MapTitle(Map map)
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
    private HashSet<int> shownEntranceTitles = new();
    private Map? pendingTitleMap;
    private string? pendingTitleText;
    private Map? titleView;
    private string? activeTitle;
    private float titleStartedAt;
}
