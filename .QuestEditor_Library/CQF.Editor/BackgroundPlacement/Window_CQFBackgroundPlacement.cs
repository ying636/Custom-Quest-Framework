#if RIMWORLD_1_7
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class Window_CQFBackgroundPlacement : Window
{
    public Window_CQFBackgroundPlacement(CQFBackgroundPlacementSession session, Designator_CQFBackgroundPlacement designator)
    {
        this.session = session;
        this.designator = designator;
        this.designator.ElementPicked += this.ShowPickedElement;
        this.structureDefs = DefDatabase<CQFBackgroundStructureDef>.AllDefsListForReading
            .OrderBy(def => def.LabelCap.ToString()).ToArray();
        this.categories = this.structureDefs.Select(def => def.category).OfType<CQFBackgroundStructureCategoryDef>()
            .Distinct().OrderBy(def => def.order).ThenBy(def => def.LabelCap.ToString()).ToArray();
        this.hasUncategorized = this.structureDefs.Any(def => def.category == null);
        this.layer = WindowLayer.GameUI;
        this.doCloseX = true;
        this.closeOnAccept = false;
        this.closeOnClickedOutside = false;
        this.closeOnCancel = false;
        this.preventCameraMotion = false;
        this.absorbInputAroundWindow = false;
        this.draggable = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(920f, UI.screenWidth - 24f), Mathf.Min(284f, UI.screenHeight - 72f));
    protected override float Margin => 11f;
    private bool PlacementActive => Find.DesignatorManager?.SelectedDesignator == this.designator;

    public void ActivatePlacement() => this.designator.SwitchMode();
    public void ActivateSelection() => this.designator.StartPicking();

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (this.session.Finished || this.session.Map.Disposed)
        {
            this.Close(false);
            return;
        }
        if (!this.session.CanEditMap && this.PlacementActive) Find.DesignatorManager.Deselect();
        Vector2 size = this.InitialSize;
        if (this.windowRect.size == size) return;
        this.windowRect.size = size;
        this.windowRect.x = Mathf.Clamp(this.windowRect.x, 0f, Mathf.Max(0f, UI.screenWidth - size.x));
        this.windowRect.y = Mathf.Clamp(this.windowRect.y, 0f, Mathf.Max(0f, UI.screenHeight - size.y));
    }

    public override void PostClose()
    {
        this.session.Finish(false);
        if (this.PlacementActive) Find.DesignatorManager.Deselect();
        base.PostClose();
    }

    public override void DoWindowContents(Rect inRect)
    {
        using var scope = new CQFUIScope(inRect.width, inRect.height);
        Text.Font = GameFont.Small;
        this.DrawToolbar(inRect.width);
        if (!this.session.CanEditMap)
        {
            Rect returnRect = new Rect(0f, 40f, Mathf.Min(inRect.width, 260f), 30f);
            if (Button(returnRect, "CQF_BackgroundPlacementReturnToMap", Find.Maps.Contains(this.session.Map)))
                CameraJumper.TryJump(this.session.Map.Center, this.session.Map);
            TooltipHandler.TipRegion(returnRect, "CQF_BackgroundPlacementReturnToMapTip".Translate());
            return;
        }

        float contentTop = this.DrawTabs(inRect.width);
        Rect body = new Rect(0f, contentTop, inRect.width, inRect.height - contentTop);
        if (this.page == SceneryPage.Settings)
        {
            this.DrawSettings(body);
            return;
        }

        float inspectorWidth = Mathf.Min(320f, body.width * .48f);
        Rect palette = new Rect(body.x, body.y, body.width - inspectorWidth - 8f, body.height);
        Rect inspector = new Rect(palette.xMax + 8f, body.y, inspectorWidth, body.height);
        CQFUIStyle.DrawMenuSection(palette);
        CQFUIStyle.DrawMenuSection(inspector);
        this.DrawElements(palette.ContractedBy(8f));
        this.DrawInspector(inspector.ContractedBy(8f));
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 size = this.InitialSize;
        this.windowRect = new Rect((UI.screenWidth - size.x) / 2f, UI.screenHeight - size.y - 45f, size.x, size.y).Rounded();
    }

    private void DrawToolbar(float width)
    {
        float buttonWidth = Mathf.Min(98f, (width - 145f) / 5f);
        float x = width - 31f - buttonWidth;
        if (Button(new Rect(x, 0f, buttonWidth, 27f), "CQF_BackgroundPlacementApply", this.session.CanEditMap && this.session.CanUndo))
            this.session.Apply();
        x -= buttonWidth + 6f;
        if (Button(new Rect(x, 0f, buttonWidth, 27f), "CQF_BackgroundPlacementUndo", this.session.CanEditMap && this.session.CanUndo))
        {
            this.session.Undo();
            this.ResetSettingInputs();
            if (this.PlacementActive) this.ActivateSelection();
        }
        x -= buttonWidth + 6f;
        if (Button(new Rect(x, 0f, buttonWidth, 27f), this.windowPinned ? "CQF_UnpinWindow" : "CQF_PinWindow"))
        {
            this.windowPinned = !this.windowPinned;
            this.draggable = !this.windowPinned;
        }

        bool placing = this.PlacementActive;
        bool picking = placing && this.designator.Picking;
        x -= buttonWidth + 6f;
        Rect picker = new Rect(x, 0f, buttonWidth, 27f);
        if (Button(picker, picking ? "CQF_SceneryStopPicking" : "CQF_SceneryPick", this.session.CanEditMap))
        {
            if (picking) Find.DesignatorManager.Deselect();
            else this.ActivateSelection();
        }
        TooltipHandler.TipRegion(picker, "CQF_SceneryPickTip".Translate());
        bool placementReady = this.page switch
        {
            SceneryPage.Cliffs => this.session.Cliffs,
            SceneryPage.Structures => this.session.Pending != null,
            SceneryPage.Placed => this.session.CliffSelection != null || this.session.Pending != null || (this.session.Selection != null && this.session.CanPlace),
            _ => false
        };
        if (this.session.CanEditMap && !picking && (placing || placementReady))
        {
            x -= buttonWidth + 6f;
            if (Button(new Rect(x, 0f, buttonWidth, 27f), placing ? "CQF_BackgroundPlacementPause" : "CQF_BackgroundPlacementResume"))
            {
                if (placing) Find.DesignatorManager.Deselect();
                else this.ActivatePlacement();
            }
        }

        Text.Font = GameFont.Medium;
        Widgets.LabelFit(new Rect(0f, 0f, x - 8f, 28f), "CQF_BackgroundPlacementStart".Translate());
        Text.Font = GameFont.Small;
        TooltipHandler.TipRegion(new Rect(width - 28f, 0f, 28f, 24f), "CQF_SceneryCloseTip".Translate());
    }

    private void ShowPickedElement()
    {
        this.page = SceneryPage.Placed;
        this.category = null;
        this.inspectorScroll = Vector2.zero;
    }

    private float DrawTabs(float width)
    {
        const float tabWidth = 106f;
        const float spacing = 5f;
        int count = 3 + this.categories.Length + (this.hasUncategorized ? 1 : 0);
        float contentWidth = count * (tabWidth + spacing) - spacing;
        bool scroll = contentWidth > width;
        if (scroll) Widgets.BeginScrollView(new Rect(0f, 35f, width, 45f), ref this.tabScroll, new Rect(0f, 0f, contentWidth, 29f));
        float x = 0f;
        float y = scroll ? 0f : 35f;
        void DrawTab(string label, SceneryPage target, CQFBackgroundStructureCategoryDef? group = null)
        {
            Rect rect = new Rect(x, y, tabWidth, 29f);
            bool current = this.page == target && (target != SceneryPage.Structures || this.category == group);
            CQFUIStyle.DrawBackground(rect, current);
            if (current) Widgets.DrawBoxSolid(new Rect(rect.x + 1f, rect.yMax - 3f, rect.width - 2f, 2f), CQFUIStyle.Accent);
            if (CQFUIStyle.ButtonText(rect, label, drawBackground: false, overrideTextAnchor: TextAnchor.MiddleCenter) && !current)
            {
                if (this.PlacementActive) Find.DesignatorManager.Deselect();
                this.page = target;
                this.category = group;
                if (target == SceneryPage.Structures)
                    this.visibleStructureDefs = this.structureDefs.Where(def => def.category == group).ToArray();
                this.paletteScroll = Vector2.zero;
                this.inspectorScroll = Vector2.zero;
            }
            x += tabWidth + spacing;
        }
        DrawTab("CQF_ScenerySettings".Translate(), SceneryPage.Settings);
        DrawTab("CQF_BackgroundPlacementCliff".Translate(), SceneryPage.Cliffs);
        foreach (CQFBackgroundStructureCategoryDef group in this.categories)
            DrawTab(group.LabelCap, SceneryPage.Structures, group);
        if (this.hasUncategorized) DrawTab("CQF_SceneryOtherStructures".Translate(), SceneryPage.Structures);
        DrawTab("CQF_BackgroundPlacementPlaced".Translate(), SceneryPage.Placed);
        if (scroll) Widgets.EndScrollView();
        return scroll ? 85f : 70f;
    }

    private static bool Button(Rect rect, string key, bool active = true) => CQFUIStyle.ButtonText(rect, key.Translate(), active: active);

    private static bool Toggle(Rect rect, string key, bool value)
    {
        const float icon = 24f;
        Widgets.LabelFit(new Rect(rect.x, rect.y, rect.width - icon - 4f, rect.height), key.Translate());
        Widgets.CheckboxDraw(rect.xMax - icon, rect.y + (rect.height - icon) / 2f, value, false, icon);
        return Widgets.ButtonInvisible(rect);
    }

    private enum SceneryPage
    {
        Settings,
        Cliffs,
        Structures,
        Placed
    }

    private readonly CQFBackgroundPlacementSession session;
    private readonly Designator_CQFBackgroundPlacement designator;
    private readonly CQFBackgroundStructureDef[] structureDefs;
    private readonly CQFBackgroundStructureCategoryDef[] categories;
    private readonly bool hasUncategorized;
    private CQFBackgroundStructureDef[] visibleStructureDefs = Array.Empty<CQFBackgroundStructureDef>();
    private CQFBackgroundStructureCategoryDef? category;
    private SceneryPage page = SceneryPage.Cliffs;
    private bool windowPinned;
    private Vector2 tabScroll;
    private Vector2 paletteScroll;
    private Vector2 inspectorScroll;
}
#endif
