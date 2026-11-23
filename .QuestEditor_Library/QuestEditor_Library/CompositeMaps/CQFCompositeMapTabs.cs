using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositeMapTabs
{
    internal CQFCompositeMapTabs(CQFCompositeMapInstance instance)
    {
        this.instance = instance;
    }

    internal float Draw(float width, CQFCompositeMapGroup selected, out CQFCompositeMapGroup? clicked)
    {
        using CQFUIScope scope = new();
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;
        if (layoutVersion != instance.RootGroup.LayoutVersion) Rebuild();

        bool scrollable = contentWidth > width;
        if (selected != previousSelection)
        {
            foreach (Tab tab in tabs)
            {
                if (tab.Group != selected) continue;
                scroll.x = Mathf.Clamp(scroll.x, tab.Rect.xMax - width, tab.Rect.xMin);
                break;
            }
            previousSelection = selected;
        }
        scroll.x = Mathf.Clamp(scroll.x, 0f, Mathf.Max(0f, contentWidth - width));
        bool repaint = Event.current.type == EventType.Repaint;
        if (repaint) Widgets.DrawLineHorizontal(0, Height - 1f, width, divider);
        if (scrollable)
            Widgets.BeginScrollView(new Rect(0, 0, width, Height + 16f), ref scroll, new Rect(0, 0, contentWidth, Height));

        clicked = null;
        try
        {
            foreach (Tab tab in tabs)
            {
                if (repaint)
                {
                    bool current = tab.Group == selected;
                    Color previous = GUI.color;
                    GUI.color = current || Mouse.IsOver(tab.Rect) ? activeText : inactiveText;
                    Widgets.Label(new Rect(tab.Rect.x + 5f, tab.Rect.y, tab.Rect.width - 10f, Height - 3f), tab.Name);
                    GUI.color = previous;
                    if (current)
                        Widgets.DrawBoxSolid(new Rect(tab.Rect.x + 5f, tab.Rect.yMax - 2f, tab.Rect.width - 10f, 2f), underline);
                    TooltipHandler.TipRegion(tab.Rect, tab.Tooltip);
                }
                if (Widgets.ButtonInvisible(tab.Rect)) clicked = tab.Group;
            }
        }
        finally
        {
            if (scrollable) Widgets.EndScrollView();
        }
        return Height + (scrollable ? 16f : 0f);
    }

    private void Rebuild()
    {
        tabs.Clear();
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (CQFCompositeMapGroup group in instance.Groups)
        {
            string name = instance.GroupName(group);
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }
        float x = 0f;
        foreach (CQFCompositeMapGroup group in instance.Groups)
        {
            string name = instance.GroupName(group);
            string tooltip = name;
            if (group != instance.RootGroup && instance.GetMap(group.EntryRegion) is { } entry)
            {
                string mapName = GameComponent_MapManager.MapTitle(entry);
                if (name != mapName)
                {
                    tooltip = $"{name} · {mapName}";
                    if (counts[name] > 1) name = mapName;
                }
            }
            float textWidth = Text.CalcSize(name).x;
            float width = Mathf.Clamp(Mathf.Ceil(textWidth) + 12f, 54f, 220f);
            tabs.Add(new Tab(group, textWidth > width - 10f ? name.Truncate(width - 10f) : name, tooltip,
                new Rect(x, 0, width, Height)));
            x += width + Gap;
        }
        contentWidth = x - Gap;
        layoutVersion = instance.RootGroup.LayoutVersion;
    }

    private const float Height = 32f;
    private const float Gap = 18f;
    private static readonly Color divider = new(0.31f, 0.31f, 0.31f);
    private static readonly Color underline = new(0.82f, 0.82f, 0.82f);
    private static readonly Color activeText = new(0.95f, 0.95f, 0.95f);
    private static readonly Color inactiveText = new(0.68f, 0.68f, 0.68f);
    private readonly CQFCompositeMapInstance instance;
    private readonly List<Tab> tabs = new();
    private int layoutVersion = -1;
    private float contentWidth;
    private Vector2 scroll;
    private CQFCompositeMapGroup? previousSelection;
    private readonly record struct Tab(CQFCompositeMapGroup Group, string Name, string Tooltip, Rect Rect);
}
