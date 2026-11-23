using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFCompositeMinimap : Window
{
    internal CQFCompositeMinimap(GameComponent_MapManager manager, CQFCompositeMapInstance instance)
    {
        this.manager = manager;
        Instance = instance;
        portraitInteraction = new CQFCompositePortraitInteraction(manager);
        mapLayout = new CQFCompositeMapLayout(full: false);
        selectRegion = manager.ViewCompositeRegion;
        selectGroup = OpenOverview;
        layer = WindowLayer.GameUI;
        doWindowBackground = false;
        drawShadow = false;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = false;
        preventCameraMotion = false;
        focusWhenOpened = false;
    }

    internal CQFCompositeMapInstance Instance { get; }
    public override Vector2 InitialSize => new(Mathf.Min(315, UI.screenWidth * 0.40f), MapPanelHeight + PortraitPanelHeight);
    private float MapPanelHeight => Instance.MinimapExpanded ? 196 : 34;
    private float PortraitPanelHeight => colonists is { Count: > 0 } ? CQFCompositeMapWindow.ColonistRowHeight + 12f : 0;
    protected override float Margin => 0;

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new();
        bool expanded = Instance.MinimapExpanded;
        bool repaint = Event.current.type == EventType.Repaint;
        CQFCompositeMapGroup group = Instance.GetGroup(Find.CurrentMap) ?? Instance.RootGroup;
        Map? parent = group.ParentRegion is { } region ? Instance.GetMap(region) : null;
        Rect mapPanel = new(0, 0, inRect.width, MapPanelHeight);
        Rect fullMap = new(4, 3, 22, 28);
        Rect header = new(30, 3, inRect.width - 70, 28);
        Rect back = new(30, 3, 22, 28);
        if (parent != null) header.xMin = back.xMax + 4f;
        Rect toggle = new(inRect.width - 36, 3, 32, 28);
        bool fullMapOpen = Find.WindowStack.IsOpen(typeof(CQFCompositeMapWindow));
        if (repaint)
        {
            Widgets.DrawBoxSolid(mapPanel, CQFUIStyle.Panel);
            CQFUIStyle.DrawBox(mapPanel);
            CQFUIStyle.DrawBackground(fullMap, fullMapOpen);
            for (int y = -5; y <= 5; y += 5)
                Widgets.DrawLineHorizontal(fullMap.center.x - 6, fullMap.center.y + y, 12, CQFUIStyle.TextColor);
            CQFUIStyle.DrawBackground(toggle, expanded);
            GUI.DrawTexture(toggle.ContractedBy(7f), expanded ? TexButton.Collapse : TexButton.Reveal, ScaleMode.ScaleToFit);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(header, Instance.GroupName(group).Truncate(header.width));
            TooltipHandler.TipRegionByKey(fullMap, "CQF_Composite_FullMap");
        }

        if (Widgets.ButtonInvisible(fullMap) && !fullMapOpen)
            OpenOverview(group);
        if (parent != null && CQFCompositeMapWindow.DrawParentButton(back, GameComponent_MapManager.MapTitle(parent)))
        {
            selectRegion(parent);
            return;
        }
        HandleDrag(header);
        if (Widgets.ButtonInvisible(toggle))
            Instance.MinimapExpanded = !expanded;

        if (expanded)
            CQFCompositeMapWindow.DrawLayout(manager, Instance, group, mapLayout,
                new Rect(9, 38, inRect.width - 18, mapPanel.height - 46), selectRegion, selectGroup);

        if (colonists is { Count: > 0 })
        {
            Rect portraits = new(0, mapPanel.yMax + 4, inRect.width, PortraitPanelHeight - 4);
            if (repaint)
            {
                Widgets.DrawBoxSolid(portraits, CQFUIStyle.Panel);
                CQFUIStyle.DrawBox(portraits);
            }
            CQFCompositeMapWindow.DrawColonists(portraits.ContractedBy(4), colonists, ref portraitScroll, portraitInteraction);
        }
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 size = InitialSize;
        Vector2 position = Instance.MinimapPosition;
        if (position.x < 0 || position.y < 0)
            position = new Vector2(UI.screenWidth - size.x - 12, 54);
        windowRect = new Rect(position, size);
        SyncGeometry();
    }

    internal void SyncGeometry()
    {
        Map map = Find.CurrentMap;
        if (portraitMap != map) portraitScroll = Vector2.zero;
        portraitMap = map;
        colonists = manager.GetCompositeMap(map) == Instance ? map.mapPawns.FreeColonists : null;
        Vector2 size = InitialSize;
        Vector2 requested = windowRect.position + pendingMove;
        pendingMove = Vector2.zero;
        Vector2 position = new(Mathf.Clamp(requested.x, 0, Mathf.Max(0, UI.screenWidth - size.x)),
            Mathf.Clamp(requested.y, 0, Mathf.Max(0, UI.screenHeight - size.y)));
        windowRect = new Rect(position, size);
        Instance.MinimapPosition = position;
    }

    public override void PostClose()
    {
        if (dragControl != 0 && GUIUtility.hotControl == dragControl)
            GUIUtility.hotControl = 0;
        base.PostClose();
    }

    private void HandleDrag(Rect header)
    {
        Event current = Event.current;
        int control = GUIUtility.GetControlID(FocusType.Passive);
        if (current.type == EventType.MouseDown && current.button == 0 && header.Contains(current.mousePosition))
        {
            dragControl = control;
            pointerTravel = Vector2.zero;
            dragging = false;
            GUIUtility.hotControl = control;
            current.Use();
        }
        else if (dragControl != 0 && GUIUtility.hotControl == dragControl)
        {
            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                if (dragging)
                    pendingMove += current.delta;
                else
                {
                    pointerTravel += current.delta;
                    if (pointerTravel.sqrMagnitude >= 16f)
                    {
                        dragging = true;
                        pendingMove += pointerTravel;
                    }
                }
                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0)
            {
                GUIUtility.hotControl = 0;
                dragControl = 0;
                current.Use();
            }
        }
    }

    private void OpenOverview(CQFCompositeMapGroup group)
    {
        CQFCompositeMapWindow? window = Find.WindowStack.WindowOfType<CQFCompositeMapWindow>();
        if (window?.Instance != Instance)
        {
            if (window != null) Find.WindowStack.TryRemove(window, false);
            window = new CQFCompositeMapWindow(manager, Instance);
            Find.WindowStack.Add(window);
        }
        window.SelectGroup(group);
    }

    private readonly GameComponent_MapManager manager;
    private readonly CQFCompositePortraitInteraction portraitInteraction;
    private readonly CQFCompositeMapLayout mapLayout;
    private readonly Action<Map> selectRegion;
    private readonly Action<CQFCompositeMapGroup> selectGroup;
    private Vector2 pendingMove;
    private int dragControl;
    private Vector2 pointerTravel;
    private bool dragging;
    private Map? portraitMap;
    private List<Pawn>? colonists;
    private Vector2 portraitScroll;
}
