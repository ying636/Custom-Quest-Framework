using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Window_CinematicRoles : Window
{
    public Window_CinematicRoles(QuestEditor_Cinematics owner, string? selectedKey = null)
    {
        this.owner = owner;
        this.selectedKey = selectedKey;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        layer = WindowLayer.GameUI;
        owner.Project.EnsureRoles();
    }

    public override Vector2 InitialSize => new(Mathf.Min(420f, UI.screenWidth - 16f), Mathf.Min(560f, UI.screenHeight * 0.65f));

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (!owner.IsOpen) Close(false);
    }

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        Widgets.Label(new Rect(0f, 0f, inRect.width - 28f, 28f), "CQF_Cinematic_ActorBindings".Translate().Colorize(CQFUIStyle.Accent));
        TooltipHandler.TipRegion(new Rect(0f, 0f, inRect.width - 28f, 28f), "CQF_Cinematic_ActorBindings_Tip".Translate());
        owner.Project.EnsureRoles();
        List<CQFCinematicRole> roles = owner.Project.Definition.roles;
        Rect viewport = new(0f, 34f, inRect.width, Mathf.Max(30f, inRect.height - 34f));
        Rect content = new(0f, 0f, viewport.width - 18f, Mathf.Max(viewport.height, contentHeight));
        Widgets.BeginScrollView(viewport, ref scroll, content);
        float y = 0f;
        if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 30f), "CQF_EntityBook_CinematicAddRole".Translate()))
            owner.BindEntityReference(token => selectedKey = owner.Project.BindEntityRole(token));
        y += 38f;
        if (roles.Count == 0)
            Widgets.Label(new Rect(0f, y, content.width, 60f), "CQF_Cinematic_RoleNoActors".Translate());
        else
        {
            CQFCinematicRole selected = roles.FirstOrDefault(role => role.key == selectedKey) ?? roles[0];
            selectedKey = selected.key;
            foreach (CQFCinematicRole role in roles)
            {
                Rect rect = new(0f, y, content.width, 30f);
                if (CQFUIStyle.ButtonText(rect, owner.Project.RoleLabel(role.key), overrideTextAnchor: TextAnchor.MiddleLeft)) selectedKey = role.key;
                y += 36f;
            }
            y += 8f;
            selected = roles.Single(role => role.key == selectedKey);
            selected.label = TextField(ref y, content.width, "CQF_Cinematic_RoleName", selected.label);
            Label(ref y, content.width, "CQF_Cinematic_RoleSource");
            Rect sourceRect = new(0f, y, content.width, 30f);
            if (CQFUIStyle.ButtonText(sourceRect, SourceLabel(selected.source)))
            {
                List<FloatMenuOption> options = Enum.GetValues(typeof(CQFCinematicRoleSource)).Cast<CQFCinematicRoleSource>()
                    .Select(source => new FloatMenuOption(SourceLabel(source), () =>
                    {
                        if (source == CQFCinematicRoleSource.EntityBook)
                        {
                            owner.BindEntityReference(token => owner.Project.SetEntityRole(selected.key, token));
                            return;
                        }
                        string key = source == CQFCinematicRoleSource.Context && string.IsNullOrWhiteSpace(selected.sourceKey) ? "Trigger" : selected.sourceKey;
                        owner.Project.SetRoleSource(selected.key, source, key);
                    })).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(sourceRect, (selected.source == CQFCinematicRoleSource.EntityBook
                ? "CQF_EntityBook_CinematicSource_Tip" : "CQF_Cinematic_Source_" + selected.source + "_Tip").Translate());
            y += 38f;
            if (selected.source == CQFCinematicRoleSource.EntityBook)
            {
                Label(ref y, content.width, "CQF_EntityBook_CinematicReference");
                if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 30f), CQFEntityBookBridge.DisplayReference(selected.sourceKey)))
                    owner.BindEntityReference(token => owner.Project.SetEntityRole(selected.key, token));
                y += 38f;
                if (CQFUIStyle.ButtonText(new Rect(0f, y, content.width, 30f), "CQF_EntityBook_CinematicAdvancedReference".Translate()))
                    showReference = !showReference;
                y += 38f;
                if (showReference)
                {
                    float height = Text.CalcHeight(selected.sourceKey, content.width);
                    Widgets.Label(new Rect(0f, y, content.width, height), selected.sourceKey);
                    y += height + 8f;
                }
            }
            else if (selected.source != CQFCinematicRoleSource.Recorded)
            {
                string sourceKey = TextField(ref y, content.width, "CQF_Cinematic_SourceKey", selected.sourceKey);
                if (sourceKey != selected.sourceKey) owner.Project.SetRoleSource(selected.key, selected.source, sourceKey);
            }
            Label(ref y, content.width, "CQF_Cinematic_RehearsalPawn");
            Rect pawnRect = new(0f, y, content.width, 30f);
            if (CQFUIStyle.ButtonText(pawnRect, owner.Project.RehearsalPawnLabel(selected.key)))
                owner.PickRehearsalPawn(selected.key);
            TooltipHandler.TipRegion(pawnRect, "CQF_Cinematic_ChooseRehearsalPawn_Tip".Translate());
            y += 38f;
        }
        contentHeight = y + 12f;
        Widgets.EndScrollView();
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 size = InitialSize;
        windowRect = new Rect(UI.screenWidth - size.x - 8f, Mathf.Max(8f, owner.windowRect.y - size.y - 8f), size.x, size.y);
    }

    private string TextField(ref float y, float width, string key, string value)
    {
        Label(ref y, width, key);
        string result = Widgets.TextField(new Rect(0f, y, width, 30f), value ?? "");
        y += 38f;
        return result;
    }

    private void Label(ref float y, float width, string key)
    {
        Rect rect = new(0f, y, width, 24f);
        Widgets.Label(rect, key.Translate());
        TooltipHandler.TipRegion(rect, (key + "_Tip").Translate());
        y += 26f;
    }

    private string SourceLabel(CQFCinematicRoleSource source)
        => (source == CQFCinematicRoleSource.EntityBook ? "CQF_EntityBook_CinematicSource" : "CQF_Cinematic_Source_" + source).Translate();

    private readonly QuestEditor_Cinematics owner;
    private string? selectedKey;
    private Vector2 scroll;
    private float contentHeight;
    private bool showReference;
}
