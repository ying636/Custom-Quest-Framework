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
        mapLayout = new CQFCompositeMapLayout(instance.Definition, full: true);
        selectRegion = SelectRegion;
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
        forcePause = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(1040, UI.screenWidth - 64), Mathf.Min(740, UI.screenHeight - 64));

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new();
        Rect layout = new(0, 43, inRect.width, inRect.height - 43);
        if (Event.current.type == EventType.Repaint)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width - 45, 32), instance.DisplayName);
            Widgets.DrawBoxSolid(layout, CQFUIStyle.Canvas);
            CQFUIStyle.DrawBox(layout);
        }
        DrawLayout(manager, instance, mapLayout, layout.ContractedBy(18), selectRegion, portraitScroll, portraits);
    }

    internal static void DrawLayout(GameComponent_MapManager manager, CQFCompositeMapInstance instance,
        CQFCompositeMapLayout layout, Rect available, Action<Map> select,
        Dictionary<Map, Vector2>? portraitScroll = null, CQFCompositePortraitInteraction? portraits = null)
    {
        bool full = portraitScroll != null;
        bool repaint = Event.current.type == EventType.Repaint;
        List<CQFCompositeMapRegion> regions = instance.Definition.regions;
        Dictionary<string, Rect> positions = layout.GetPositions(available);

        if (repaint)
        {
            foreach (CQFCompositeMapConnection link in instance.Definition.edges)
                DrawConnection(positions[link.from].center, positions[link.to].center, full, false);
            foreach (CQFCompositeMapConnection link in instance.Definition.portals)
                DrawConnection(positions[link.from].center, positions[link.to].center, full, true);
        }

        using CQFUIScope scope = new();
        Text.Font = full ? GameFont.Small : GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.WordWrap = false;
        Map currentMap = Find.CurrentMap;
        Map? entryMap = instance.EntryMap;
        foreach (CQFCompositeMapRegion region in regions)
        {
            Map? map = instance.GetMap(region.id);
            if (map == null || map.Disposed) continue;
            Rect rect = positions[region.id];
            if (rect.width <= 4 || rect.height <= 4) continue;
            bool current = map == currentMap;
            List<Pawn>? colonists = full && rect.height >= 53 && rect.width >= 40 && !manager.IsHibernating(map)
                ? map.mapPawns.FreeColonists : null;
            bool showColonists = colonists is { Count: > 0 };
            Rect name = showColonists ? new Rect(rect.x, rect.y + 2, rect.width, 21) : rect;
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
                string label = map.Parent is CQFCompositeMapParent parent ? parent.MapName : region.id;
                if (map == entryMap)
                    TooltipHandler.TipRegionByKey(name, "CQF_Composite_EntryMap", label);
                else
                    TooltipHandler.TipRegion(name, label);
                Widgets.Label(name.ContractedBy(3, 0), label.Truncate(name.width - 6));
            }
            if (showColonists && portraitScroll != null && portraits != null)
            {
                Vector2 scroll = portraitScroll.GetValueOrDefault(map);
                DrawColonists(new Rect(rect.x + 5, name.yMax + 3, rect.width - 10, rect.yMax - name.yMax - 7),
                    colonists!, 28, ref scroll, portraits);
                portraitScroll[map] = scroll;
            }
            if (Widgets.ButtonInvisible(rect) &&
                (full || !current))
                select(map);
        }
    }

    internal static void DrawColonists(Rect viewport, List<Pawn> colonists, float size, ref Vector2 scroll,
        CQFCompositePortraitInteraction portraits)
    {
        if (viewport.width < size || viewport.height < size) return;
        const float Gap = 2;
        float stride = size + Gap;
        int columns = Mathf.Max(1, Mathf.FloorToInt((viewport.width + Gap) / stride));
        bool scrollable = Mathf.CeilToInt((float)colonists.Count / columns) * stride - Gap > viewport.height;
        float width = scrollable ? viewport.width - 16 : viewport.width;
        columns = Mathf.Max(1, Mathf.FloorToInt((width + Gap) / stride));
        int rows = Mathf.CeilToInt((float)colonists.Count / columns);
        Vector2 origin = viewport.position;
        if (scrollable)
        {
            Widgets.BeginScrollView(viewport, ref scroll, new Rect(0, 0, width, rows * stride - Gap));
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
                if (scrollable && (row * stride + size < scroll.y || row * stride > scroll.y + viewport.height)) continue;
                int count = Mathf.Min(columns, colonists.Count - row * columns);
                float x = origin.x + (width - count * stride + Gap) / 2;
                float y = origin.y + row * stride;
                for (int i = 0; i < count; i++)
                {
                    Pawn pawn = colonists[row * columns + i];
                    Rect portrait = new(x + i * stride, y, size, size);
                    portraits.Draw(portrait, pawn);
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

    private static Color? GetRegionHighlight(Map map, GameComponent_MapManager? manager, bool current)
    {
        if (current) return Color.white;
        if (manager == null) return null;
        (bool hasColonists, bool keptAwake) = manager.GetCompositeMapStatus(map);
        if (hasColonists) return colonistsHighlight;
        if (keptAwake) return keptAwakeHighlight;
        return null;
    }

    private static void DrawConnection(Vector2 from, Vector2 to, bool full, bool portal)
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
                CQFUIStyle.Accent, lineWidth);
    }

    private readonly GameComponent_MapManager manager;
    private readonly CQFCompositeMapInstance instance;
    private readonly CQFCompositePortraitInteraction portraits;
    private readonly CQFCompositeMapLayout mapLayout;
    private readonly Action<Map> selectRegion;
    private readonly Dictionary<Map, Vector2> portraitScroll = new();
    private static readonly Color colonistsHighlight = new(0.34f, 0.66f, 1f);
    private static readonly Color keptAwakeHighlight = new(1f, 0.37f, 0.40f);
}
