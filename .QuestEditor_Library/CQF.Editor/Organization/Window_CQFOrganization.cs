using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Window_CQFOrganization : Window
{
    public Window_CQFOrganization(Map? map)
    {
        this.root = map == null ? null : LevelPather.GetRoot(map);
        this.organization = this.Organizations().FirstOrDefault();
        this.doCloseX = true;
        this.forcePause = true;
        this.absorbInputAroundWindow = true;
        this.draggable = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(960f, UI.screenWidth - 28f), Mathf.Min(680f, UI.screenHeight - 28f));

    public override void DoWindowContents(Rect rect)
    {
        using var scope = new CQFUIScope(rect.width, rect.height);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0, 0, rect.width - 30, 34), "CQF_Organization_Title".Translate().Colorize(CQFUIStyle.Accent));
        Text.Font = GameFont.Small;
        if (this.root == null)
        {
            Rect authoring = new(0f, 40f, rect.width, rect.height - 40f);
            CQFUIStyle.DrawMenuSection(authoring);
            this.DrawRules(authoring.ContractedBy(10f));
            return;
        }
        if (rect.width < 590f)
        {
            float tabWidth = (rect.width - 8f) / 2f;
            Rect runtimeTab = new(0f, 38f, tabWidth, 28f);
            Rect rulesTab = new(tabWidth + 8f, 38f, tabWidth, 28f);
            CQFUIStyle.DrawBackground(this.showRules ? rulesTab : runtimeTab, true);
            if (CQFUIStyle.ButtonText(runtimeTab, "CQF_Organization_OverviewTab".Translate(), false, overrideTextAnchor: TextAnchor.MiddleCenter))
                this.showRules = false;
            if (CQFUIStyle.ButtonText(rulesTab, "CQF_Organization_RulesTab".Translate(), false, overrideTextAnchor: TextAnchor.MiddleCenter))
                this.showRules = true;
            Rect panel = new(0f, 72f, rect.width, rect.height - 72f);
            CQFUIStyle.DrawMenuSection(panel);
            if (this.showRules) this.DrawRules(panel.ContractedBy(8f));
            else this.DrawRuntime(panel.ContractedBy(8f));
            return;
        }
        float leftWidth = Mathf.Clamp(rect.width * 0.34f, 175f, 320f);
        Rect runtimePanel = new(0, 40, leftWidth, rect.height - 40);
        Rect authoringPanel = new(runtimePanel.xMax + 10, 40, rect.width - leftWidth - 10, rect.height - 40);
        CQFUIStyle.DrawMenuSection(runtimePanel);
        CQFUIStyle.DrawMenuSection(authoringPanel);
        this.DrawRuntime(runtimePanel.ContractedBy(10));
        this.DrawRules(authoringPanel.ContractedBy(10));
    }

    private void DrawRuntime(Rect rect)
    {
        Rect select = new(rect.x, rect.y, rect.width, 30);
        if (CQFUIStyle.ButtonText(select, this.organization?.name ?? "CQF_Organization_Select".Translate().ToString()))
            CQFEditorTools.DrawFloatMenu(this.Organizations(), owner => this.organization = owner, owner => owner.name);

        Rect view = new(rect.x, rect.y + 38, rect.width, rect.height - 38);
        if (this.organization is not { Disposed: false } owner)
        {
            Widgets.Label(view, "CQF_Organization_NoRuntime".Translate());
            return;
        }

        var levels = owner.Levels.Where(level => level.level is { Disposed: false }).ToArray();
        var moves = owner.Moves.ToArray();
        int rows = 3 + levels.Length + levels.Sum(level => level.Lords.Count) + moves.Length;
        Rect content = new(0, 0, view.width - 18, Mathf.Max(view.height, rows * 26));
        Widgets.BeginScrollView(view, ref this.runtimeScroll, content);
        Widgets.Label(new Rect(0, 0, content.width, 26), "CQF_Organization_RulesField".Translate(
            owner.eventDef?.LabelCap.ToString() ?? "CQF_MetaLord_Unassigned".Translate().ToString()));
        float y = 29;
        foreach (var level in levels)
        {
            Widgets.Label(new Rect(0, y, content.width, 26), level.level.Parent.LabelCap.ToString().Colorize(CQFUIStyle.Accent));
            y += 26;
            foreach (var lord in level.Lords)
            {
                if (lord?.LordJob is not MultLevelLordJobBase job) continue;
                Rect row = new(8, y, content.width - 8, 26);
                string label = "CQF_Organization_Roster".Translate(job.groupName, lord.ownedPawns.Count);
                if (Mouse.IsOver(row) && Text.CalcSize(label).x > row.width - 12f)
                    TooltipHandler.TipRegion(row, label);
                if (CQFUIStyle.ButtonText(row, label, false, overrideTextAnchor: TextAnchor.MiddleLeft))
                {
                    string members = string.Join("\n", lord.ownedPawns.Select(pawn => pawn.LabelShortCap.ToString()));
                    Find.WindowStack.Add(new Dialog_MessageBox(members.NullOrEmpty() ? "CQF_Organization_EmptySquad".Translate() : members,
                        title: job.groupName));
                }
                y += 26;
            }
        }
        Widgets.Label(new Rect(0, y, content.width, 26), "CQF_Organization_Moves".Translate(moves.Length).Colorize(CQFUIStyle.Accent));
        y += 26;
        foreach (var move in moves)
        {
            string label = "CQF_Organization_MoveRow".Translate(move.pawn?.LabelShortCap.ToString() ?? string.Empty,
                move.targetGroup ?? move.targetMap?.Parent.LabelCap.ToString() ?? string.Empty);
            Rect row = new(0, y, content.width, 26);
            Widgets.Label(row, label.Truncate(row.width));
            TooltipHandler.TipRegion(row, label);
            y += 26;
        }
        Widgets.EndScrollView();
    }

    private void DrawRules(Rect rect)
    {
        float buttonWidth = (rect.width - 16) / 3;
        Rect toolbar = new(rect.x, rect.y, buttonWidth, 30);
        if (CQFUIStyle.ButtonText(toolbar, "CQF_Organization_NewRules".Translate()))
            this.ChangeDraft(() => new MetaLordEventDef { defName = "CQF_", tickInterval = 2500 });
        toolbar.x += buttonWidth + 8;
        if (CQFUIStyle.ButtonText(toolbar, "CQF_Organization_LoadRules".Translate()))
            CQFEditorTools.DrawFloatMenu(DefDatabase<MetaLordEventDef>.AllDefsListForReading,
                def => this.ChangeDraft(() => CQFOrganizationRuleBook.Copy(def)), def => def.LabelCap);
        toolbar.x += buttonWidth + 8;
        if (this.draft != null && CQFUIStyle.ButtonText(toolbar, "Save".Translate()))
            this.Save(false);
        bool canApply = this.draft != null && this.organization is { Disposed: false };
        bool dockApply = canApply && rect.height >= 174f;
        Rect view = new(rect.x, rect.y + 38f, rect.width, Mathf.Max(0f, rect.height - 38f - (dockApply ? 38f : 0f)));
        if (this.draft is not { } definition)
        {
            if (view.height >= 52f) Widgets.Label(view, "CQF_Organization_RulesHint".Translate());
            else TooltipHandler.TipRegion(new Rect(rect.x, rect.y, rect.width, 30f), "CQF_Organization_RulesHint".Translate());
            return;
        }

        float formHeight = 144f + definition.events.Count * 38f;
        float inlineApplyHeight = canApply && !dockApply ? 40f : 0f;
        Rect content = new(0f, 0f, view.width - 18f, Mathf.Max(view.height, formHeight + inlineApplyHeight));
        Widgets.BeginScrollView(view, ref this.rulesScroll, content);
        this.DrawRuleBody(content.width, definition);
        if (inlineApplyHeight > 0f && CQFUIStyle.ButtonText(
            new Rect(0f, formHeight + 4f, content.width, 30f), "CQF_Organization_ApplyRuntime".Translate()))
            this.Save(true);
        Widgets.EndScrollView();
        if (dockApply && CQFUIStyle.ButtonText(
            new Rect(rect.x, rect.yMax - 32, rect.width, 30), "CQF_Organization_ApplyRuntime".Translate()))
            this.Save(true);
    }

    private void DrawRuleBody(float width, MetaLordEventDef definition)
    {
        using var scope = new CQFUIScope(width);
        float labelWidth = Mathf.Min(160f, (width - 12f) * 0.45f);
        float y = 4f;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_DefName".Translate(), ref definition.defName, 0f, width - labelWidth);
        y += 32;
        CQFEditorTools.DrawLabelAndText_Line(y, "Name".Translate(), ref definition.label, 0f, width - labelWidth);
        y += 32;
        Widgets.Label(new Rect(0f, y, labelWidth, 25f), "CQF_Organization_TickInterval".Translate());
        Widgets.TextFieldNumeric(new Rect(labelWidth + 8f, y, 90f, 25f), ref definition.tickInterval, ref this.tickBuffer, 60);
        y += 37;
        Widgets.Label(new Rect(0f, y, width - 110, 28), "CQF_MetaLordEvents".Translate().Colorize(CQFUIStyle.Accent));
        if (CQFUIStyle.ButtonText(new Rect(width - 105, y, 105, 28), "CQF_Organization_AddRule".Translate()))
        {
            string key = "CQF_Event_" + (definition.events.Count + 1);
            while (definition.events.Any(entry => entry.key == key)) key += "_1";
            var entry = new MetaLordEvent { key = key, trigger = new MetaLordEventTrigger { type = LordEventSignalType.External }, once = true };
            definition.events.Add(entry);
            Find.WindowStack.Add(new Dialog_EditOrganizationRule(entry, this.organization));
        }
        y += 35;
        foreach (var entry in definition.events.ToArray())
        {
            string title = entry.key + " · " + ("CQF_Organization_Trigger_" + entry.trigger.type).Translate();
            if (CQFUIStyle.ButtonText(new Rect(0, y, width - 36, 30), title, overrideTextAnchor: TextAnchor.MiddleLeft))
                Find.WindowStack.Add(new Dialog_EditOrganizationRule(entry, this.organization));
            if (CQFUIStyle.ButtonImage(new Rect(width - 29, y + 2, 24, 24), TexButton.Delete, tooltip: "Delete".Translate()))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_Organization_DeleteRule".Translate(entry.key), () => definition.events.Remove(entry)));
            y += 38;
        }
    }

    private List<MetaLord> Organizations() => Current.Game?.GetComponent<GameComponent_MetaLord>().Manager.Lords
        .Where(owner => !owner.Disposed && owner.rootMap == this.root).ToList() ?? new List<MetaLord>();

    private void ChangeDraft(Func<MetaLordEventDef> create)
    {
        void Change()
        {
            try
            {
                this.draft = create();
                this.tickBuffer = null;
                this.rulesScroll = Vector2.zero;
            }
            catch (Exception error)
            {
                Log.Error("[CQF] Could not open organization rules: " + error);
                Find.WindowStack.Add(new Dialog_MessageBox(error.Message));
            }
        }
        if (this.draft == null) Change();
        else Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("CQF_Organization_ReplaceDraft".Translate(), Change));
    }

    private void Save(bool apply)
    {
        try
        {
            MetaLordEventDef saved = CQFOrganizationRuleBook.Save(this.draft!);
            if (apply) this.organization?.SetEventDefinition(saved, isRuntimeOverride: true);
            Messages.Message((apply ? "CQF_Organization_Applied" : "CQF_Organization_Saved").Translate(saved.defName), MessageTypeDefOf.PositiveEvent);
        }
        catch (Exception error)
        {
            Log.Warning("[CQF] Could not save organization rules: " + error);
            Find.WindowStack.Add(new Dialog_MessageBox(error.Message));
        }
    }

    private readonly Map? root;
    private MetaLord? organization;
    private MetaLordEventDef? draft;
    private Vector2 runtimeScroll;
    private Vector2 rulesScroll;
    private string? tickBuffer;
    private bool showRules;
}
