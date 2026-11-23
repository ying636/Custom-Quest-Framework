using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositeMapWindow : Window
{
    internal CQFCompositeMapWindow(GameComponent_MapManager manager, CQFCompositeMapInstance instance)
    {
        this.manager = manager;
        this.instance = instance;
        portraits = new CQFCompositePortraitInteraction(manager, () => Close());
        tabs = new CQFCompositeMapTabs(instance);
        mapLayout = new CQFCompositeMapLayout(full: true);
        group = instance.GetGroup(Find.CurrentMap) ?? instance.RootGroup;
        selectRegion = SelectRegion;
        selectGroup = SelectGroup;
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
        forcePause = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(1040, UI.screenWidth - 64), Mathf.Min(740, UI.screenHeight - 64));
    internal CQFCompositeMapInstance Instance => instance;
    internal static float ColonistRowHeight => Find.ColonistBar.Size.y + ColonistPortraitTop
        + (Prefs.DisableTinyText || !Text.TinyFontSupported ? 16f : 12f);
    private static float ColonistPortraitTop => Mathf.Max(4f, -Find.ColonistBar.drawer.GetPawnTextureRect(Vector2.zero).yMin);

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new();
        if (!instance.Groups.Contains(group)) group = instance.RootGroup;
        float top = tabs.Draw(inRect.width - 32f, group, out CQFCompositeMapGroup? clicked) + 8f;
        if (clicked != null) SelectGroup(clicked);
        Rect layout = new(0, top, inRect.width, inRect.height - top);
        if (Event.current.type == EventType.Repaint)
        {
            Widgets.DrawBoxSolid(layout, CQFUIStyle.Canvas);
            CQFUIStyle.DrawBox(layout);
        }
        DrawLayout(manager, instance, group, mapLayout, layout.ContractedBy(18), selectRegion, selectGroup, portraits);
    }

    internal static void DrawLayout(GameComponent_MapManager manager, CQFCompositeMapInstance instance,
        CQFCompositeMapGroup group, CQFCompositeMapLayout layout, Rect available, Action<Map> select,
        Action<CQFCompositeMapGroup> selectGroup, CQFCompositePortraitInteraction? portraits = null)
    {
        bool full = portraits != null;
        bool repaint = Event.current.type == EventType.Repaint;
        IReadOnlyList<CQFCompositeMapRegion> regions = group.Regions;
        Dictionary<string, Rect> positions = layout.GetPositions(instance, group, available);
        Dictionary<CQFCompositeMapGroup, Rect> branches = layout.BranchPositions;

        if (repaint)
        {
            IReadOnlyList<CQFCompositeMapConnection> edges = group.Edges;
            for (int i = 0; i < edges.Count; i++)
            {
                CQFCompositeMapConnection link = edges[i];
                DrawConnection(positions[link.from].center, positions[link.to].center, full, false);
            }
            IReadOnlyList<CQFCompositeMapConnection> portals = group.Portals;
            for (int i = 0; i < portals.Count; i++)
            {
                CQFCompositeMapConnection link = portals[i];
                DrawConnection(positions[link.from].center, positions[link.to].center, full, true);
            }
            foreach ((CQFCompositeMapGroup branch, Rect rect) in branches)
                DrawConnection(positions[branch.ParentRegion!].center, rect.center, full, true, branchBackground);
        }

        using CQFUIScope scope = new();
        Text.Font = full ? GameFont.Small : GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.WordWrap = false;
        Map currentMap = Find.CurrentMap;
        Map? entryMap = instance.GetMap(group.EntryRegion);
        for (int i = 0; i < regions.Count; i++)
        {
            CQFCompositeMapRegion region = regions[i];
            Map? map = instance.GetMap(region.id);
            if (map == null || map.Disposed) continue;
            Rect rect = positions[region.id];
            if (rect.width <= 4 || rect.height <= 4) continue;
            bool current = map == currentMap;
            List<Pawn>? colonists = full && rect.height >= 53 && rect.width >= 40 && !manager.IsHibernating(map)
                ? map.mapPawns.FreeColonists : null;
            float nameHeight = Mathf.Min(21f, rect.height);
            Rect name = new(rect.x, rect.center.y - nameHeight * 0.5f, rect.width, nameHeight);
            if (repaint)
            {
                CQFUIStyle.DrawBackground(rect, current);
                if (GetRegionHighlight(map, manager, current) is Color outline)
                {
                    Color original = GUI.color;
                    try
                    {
                        GUI.color = outline;
                        Widgets.DrawBox(rect.ContractedBy(1), full ? 3 : 2);
                    }
                    finally { GUI.color = original; }
                }
                string label = GameComponent_MapManager.MapTitle(map);
                if (map == entryMap)
                    TooltipHandler.TipRegionByKey(name, "CQF_Composite_EntryMap", label);
                else
                    TooltipHandler.TipRegion(name, label);
                Widgets.Label(name.ContractedBy(3, 0), label.Truncate(name.width - 6));
            }
            if (colonists is { Count: > 0 } && portraits != null)
            {
                Vector2 scroll = Vector2.zero;
                DrawColonists(new Rect(rect.x + 5, name.yMax + 3, rect.width - 10, rect.yMax - name.yMax - 7),
                    colonists, ref scroll, portraits, fitAll: true);
            }
            if (Widgets.ButtonInvisible(rect) &&
                (full || !current))
                select(map);
        }
        foreach ((CQFCompositeMapGroup branch, Rect rect) in branches)
        {
            if (rect.width <= 4f || rect.height <= 4f) continue;
            if (repaint)
            {
                Widgets.DrawBoxSolidWithOutline(rect, Mouse.IsOver(rect) ? branchHover : branchBackground, branchBorder);
                string name = instance.GroupName(branch);
                Color previous = GUI.color;
                GUI.color = branchTextColor;
                Widgets.Label(rect.ContractedBy(4f, 0f), name.Truncate(rect.width - 8f));
                GUI.color = previous;
                TooltipHandler.TipRegion(rect, name);
            }
            if (Widgets.ButtonInvisible(rect))
            {
                selectGroup(branch);
                return;
            }
        }
    }

    internal static bool DrawParentButton(Rect rect, string destination)
    {
        using CQFUIScope scope = new();
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        if (Event.current.type == EventType.Repaint)
        {
            CQFUIStyle.DrawBackground(rect, false);
            Widgets.Label(rect, "↑");
            TooltipHandler.TipRegion(rect, destination);
        }
        return Widgets.ButtonInvisible(rect);
    }

    internal static void DrawColonists(Rect viewport, List<Pawn> colonists, ref Vector2 scroll,
        CQFCompositePortraitInteraction portraits, bool fitAll = false)
    {
        const float ScrollbarWidth = 16f;
        float availableWidth = fitAll ? viewport.width : viewport.width - ScrollbarWidth;
        if (availableWidth <= 0f || viewport.height <= 0f || colonists.Count == 0) return;

        ColonistBar bar = Find.ColonistBar;
        float rowHeight = ColonistRowHeight;
        float slotWidth = Mathf.Max(bar.Size.x + bar.SpaceBetweenColonistsHorizontal, 32f) + 8f;
        float fittingWidth = fitAll ? slotWidth * colonists.Count : slotWidth;
        float scale = Mathf.Min(1f, Mathf.Min(viewport.height / rowHeight, availableWidth / fittingWidth));
        Vector2 portraitSize = bar.Size * scale;
        slotWidth *= scale;
        rowHeight *= scale;
        float top = ColonistPortraitTop * scale;
        int columns = fitAll ? colonists.Count : Mathf.Max(1, Mathf.FloorToInt(viewport.width / slotWidth));
        bool scrollable = !fitAll && Mathf.CeilToInt((float)colonists.Count / columns) * rowHeight > viewport.height + 0.01f;
        float width = scrollable ? viewport.width - ScrollbarWidth : viewport.width;
        if (scrollable) columns = Mathf.Max(1, Mathf.FloorToInt(width / slotWidth));
        int rows = Mathf.CeilToInt((float)colonists.Count / columns);
        Vector2 origin = viewport.position;
        if (scrollable)
        {
            Widgets.BeginScrollView(viewport, ref scroll, new Rect(0, 0, width, rows * rowHeight));
            origin = Vector2.zero;
        }
        else
        {
            scroll = Vector2.zero;
        }

        try
        {
            for (int row = 0; row < rows; row++)
            {
                if (scrollable && ((row + 1) * rowHeight < scroll.y || row * rowHeight > scroll.y + viewport.height)) continue;
                int count = Mathf.Min(columns, colonists.Count - row * columns);
                float x = origin.x + (width - count * slotWidth + slotWidth - portraitSize.x) / 2;
                float y = origin.y + row * rowHeight + top;
                for (int i = 0; i < count; i++)
                {
                    Pawn pawn = colonists[row * columns + i];
                    Rect portrait = new(new Vector2(x + i * slotWidth, y), portraitSize);
                    portraits.Draw(portrait, pawn, scale);
                }
            }
        }
        finally
        {
            if (scrollable) Widgets.EndScrollView();
        }
    }

    private void SelectRegion(Map map)
    {
        manager.ViewCompositeRegion(map);
        Close();
    }

    internal void SelectGroup(CQFCompositeMapGroup target)
    {
        if (instance.Groups.Contains(target)) group = target;
    }

    private static Color? GetRegionHighlight(Map map, GameComponent_MapManager? manager, bool current)
    {
        if (current) return Color.white;
        if (manager == null) return null;
        (bool hasColonists, bool keptAwake) = manager.GetCompositeMapStatus(map);
        if (hasColonists) return colonistsHighlight;
        if (keptAwake) return keptAwakeHighlight;
        return null;
    }

    private static void DrawConnection(Vector2 from, Vector2 to, bool full, bool portal, Color? color = null)
    {
        if (!portal)
        {
            Widgets.DrawLine(from, to, CQFUIStyle.Link, full ? 5 : 2);
            return;
        }
        Vector2 difference = to - from;
        float lineWidth = full ? 3 : 2;
        float step = full ? 12 : 6;
        float distance = difference.magnitude;
        Vector2 direction = difference / distance;
        for (float offset = 0; offset < distance; offset += step)
            Widgets.DrawLine(from + direction * offset, from + direction * Mathf.Min(offset + step / 2, distance),
                color ?? CQFUIStyle.Accent, lineWidth);
    }

    private readonly GameComponent_MapManager manager;
    private readonly CQFCompositeMapInstance instance;
    private readonly CQFCompositePortraitInteraction portraits;
    private readonly CQFCompositeMapTabs tabs;
    private readonly CQFCompositeMapLayout mapLayout;
    private readonly Action<Map> selectRegion;
    private readonly Action<CQFCompositeMapGroup> selectGroup;
    private CQFCompositeMapGroup group;
    private static readonly Color colonistsHighlight = new(0.34f, 0.66f, 1f);
    private static readonly Color keptAwakeHighlight = new(1f, 0.37f, 0.40f);
    private static readonly Color branchBackground = new(0.82f, 0.75f, 0.62f);
    private static readonly Color branchHover = new(0.90f, 0.84f, 0.71f);
    private static readonly Color branchBorder = new(0.53f, 0.46f, 0.34f);
    private static readonly Color branchTextColor = new(0.18f, 0.14f, 0.10f);
}
