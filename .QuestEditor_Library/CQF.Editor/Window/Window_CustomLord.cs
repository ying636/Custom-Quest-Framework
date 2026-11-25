using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class Window_CustomLord : Window
{
    public Window_CustomLord(Map map)
    {
        this.map = map;
        this.doCloseX = true;
        this.absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new Vector2(Mathf.Min(920f, UI.screenWidth - 40f),
        Mathf.Min(640f, UI.screenHeight - 40f));

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        MapComponent_CustomMapData component = MapComponent_CustomMapData.GetComp(this.map);
        bool compact = inRect.width < 550f;
        float right = inRect.width - 36f;
        bool editCompact = compact && this.showDetails && this.selected != null && component.Lords.Contains(this.selected);
        Rect back = new(right - 128f, 2f, 128f, 30f);
        Rect fromBook = new(right - 152f, 2f, 152f, 30f);
        Rect create = new(fromBook.x - 119f, 2f, 111f, 30f);
        Text.Font = GameFont.Medium;
        float titleWidth = editCompact ? back.x - 8f : compact ? right : create.x - 12f;
        Widgets.Label(new Rect(0f, 0f, titleWidth, 34f), "CQF_MapGroups_Title".Translate().Colorize(CQFUIStyle.Accent));
        Text.Font = GameFont.Small;
        if (compact)
        {
            float panelTop = editCompact ? 45f : 78f;
            Rect panel = new(0f, panelTop, inRect.width, inRect.height - panelTop);
            CQFUIStyle.DrawMenuSection(panel);
            if (editCompact)
            {
                if (CQFUIStyle.ButtonText(back, "CQF_MapGroups_BackToList".Translate()))
                    this.showDetails = false;
                this.DrawSelected(component, panel.ContractedBy(8f));
                return;
            }
            float buttonWidth = (inRect.width - 8f) / 2f;
            this.DrawAddButtons(component, new Rect(0f, 38f, buttonWidth, 30f), new Rect(buttonWidth + 8f, 38f, buttonWidth, 30f));
            this.DrawGroups(component, panel.ContractedBy(8f), openDetails: true);
            return;
        }
        this.DrawAddButtons(component, create, fromBook);

        float listWidth = Mathf.Clamp(inRect.width * 0.29f, 148f, 260f);
        Rect list = new(0f, 45f, listWidth, inRect.height - 45f);
        Rect details = new(list.xMax + 10f, list.y, inRect.width - list.width - 10f, list.height);
        CQFUIStyle.DrawMenuSection(list);
        CQFUIStyle.DrawMenuSection(details);
        this.DrawGroups(component, list.ContractedBy(8f));
        this.DrawSelected(component, details.ContractedBy(11f));
    }

    internal static LordWithName AddTemplate(MapComponent_CustomMapData component, GroupDataDef template)
    {
        string seed = template.lord.name.NullOrEmpty() || template.lord.name == "default" ? template.defName : template.lord.name;
        return AddGroup(component, seed, template);
    }

    private void DrawAddButtons(MapComponent_CustomMapData component, Rect create, Rect fromBook)
    {
        if (CQFUIStyle.ButtonText(create, "CQF_MapGroups_New".Translate()))
            this.SelectNew(AddGroup(component, seed: "default", template: null));
        if (CQFUIStyle.ButtonText(fromBook, "CQF_GroupBook_FromBook".Translate()))
            CQFGroupBookCatalog.Open(template => this.SelectNew(AddTemplate(component, template)));
    }

    private void DrawGroups(MapComponent_CustomMapData component, Rect panel, bool openDetails = false)
    {
        Widgets.Label(new Rect(panel.x, panel.y + 2f, 50f, 26f), "CQF_SignalBook_Search".Translate());
        string query = Widgets.TextField(new Rect(panel.x + 50f, panel.y, panel.width - 50f, 27f), this.search);
        if (query != this.search)
        {
            this.search = query;
            this.listScroll = Vector2.zero;
        }
        string filter = this.search.Trim();
        var groups = filter.Length == 0 ? component.Lords : component.Lords.Where(group => Matches(group, filter)).ToList();
        if (!groups.Contains(this.selected))
            this.Select(groups.FirstOrDefault());
        Rect viewport = new(panel.x, panel.y + 35f, panel.width, panel.height - 35f);
        if (groups.Count == 0)
        {
            if (filter.Length > 0)
                Widgets.Label(viewport, "CQF_MapGroups_NoMatches".Translate().Colorize(CQFUIStyle.Muted));
            return;
        }

        const float rowHeight = 48f;
        Rect content = new(0f, 0f, viewport.width - 18f, groups.Count * rowHeight);
        Widgets.BeginScrollView(viewport, ref this.listScroll, content);
        try
        {
            int first = Mathf.Max(0, Mathf.FloorToInt(this.listScroll.y / rowHeight));
            int last = Mathf.Min(groups.Count, Mathf.CeilToInt((this.listScroll.y + viewport.height) / rowHeight));
            for (int i = first; i < last; i++)
            {
                LordWithName group = groups[i];
                Rect row = new(0f, i * rowHeight, content.width, rowHeight - 4f);
                CQFUIStyle.DrawBackground(row, group == this.selected || Mouse.IsOver(row));
                string name = group.data?.name ?? group.name;
                string source = GroupSource(group);
                Widgets.Label(new Rect(row.x + 8f, row.y + 4f, row.width - 16f, 21f), name.Truncate(row.width - 16f));
                Widgets.Label(new Rect(row.x + 8f, row.y + 23f, row.width - 16f, 19f),
                    source.Truncate(row.width - 16f).Colorize(CQFUIStyle.Muted));
                TooltipHandler.TipRegion(row, name + "\n" + source);
                if (Widgets.ButtonInvisible(row))
                {
                    this.Select(group);
                    if (openDetails) this.showDetails = true;
                }
            }
        }
        finally
        {
            Widgets.EndScrollView();
        }
    }

    private void DrawSelected(MapComponent_CustomMapData component, Rect panel)
    {
        if (this.selected is not { data: { } data } group)
        {
            string key = component.Lords.Count == 0 ? "CQF_MapGroups_Empty" : "CQF_MapGroups_Select";
            Widgets.Label(panel, key.Translate().Colorize(CQFUIStyle.Muted));
            return;
        }
        float removeWidth = Mathf.Min(75f, panel.width * 0.38f);
        Rect remove = new(panel.xMax - removeWidth, panel.y, removeWidth, 29f);
        Rect collect = new(panel.x, panel.y, Mathf.Min(151f, remove.x - panel.x - 8f), 29f);
        if (CQFUIStyle.ButtonText(collect, "CQF_GroupBook_Collect".Translate()))
            this.Collect(data);
        if (CQFUIStyle.ButtonText(remove, "Delete".Translate()))
        {
            component.Lords.Remove(group);
            this.Select(null);
            this.showDetails = false;
            return;
        }

        Rect viewport = new(panel.x, panel.y + 38f, panel.width, panel.height - 38f);
        Rect content = new(0f, 0f, viewport.width - 18f, Mathf.Max(viewport.height, this.formHeight));
        Widgets.BeginScrollView(viewport, ref this.formScroll, content);
        try
        {
            using CQFUIScope scope = new(content.width);
            float y = 2f;
            data.Draw(ref y, content, 4f);
            if (group.lord == null) group.name = data.name;
            this.formHeight = y + 8f;
        }
        finally
        {
            Widgets.EndScrollView();
        }
    }

    private void Collect(LordData data)
    {
        GroupDataDef draft = CQFGroupBookCatalog.Copy(new GroupDataDef
        {
            defName = "",
            label = data.name,
            lord = data.ResolveGroupTemplate(),
            pawns = this.map.listerThings.ThingsOfDef(QEDefOf.QE_Spawner_Editor).OfType<Spawner>()
                .SelectMany(spawner => spawner.pawns)
                .Where(recipe => recipe.lordDataName == data.name && recipe is not PawnSpawnData_Group).ToList()
        });
        foreach (PawnSpawnData recipe in draft.pawns)
            recipe.lordDataName = null;
        QuestEditor_GroupEditor.OpenDraft(draft);
    }

    private void Select(LordWithName? group)
    {
        if (this.selected == group) return;
        this.selected = group;
        this.formScroll = Vector2.zero;
        this.formHeight = 0f;
    }

    private void SelectNew(LordWithName group)
    {
        this.search = string.Empty;
        this.listScroll.y = this.map.GetComponent<MapComponent_CustomMapData>().Lords.Count * 48f;
        this.Select(group);
        this.showDetails = true;
    }

    private static bool Matches(LordWithName group, string search)
    {
        LordData? data = group.data;
        return (data?.name ?? group.name).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
            data?.groupTemplate?.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (data?.groupTemplate?.lord.groupTags ?? data?.groupTags)?.Any(tag => tag.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) == true;
    }

    private static string GroupSource(LordWithName group) => group.data?.groupTemplate is { } template
        ? "CQF_MapGroups_Template".Translate(template.DisplayName).ToString()
        : "CQF_MapGroups_Local".Translate().ToString();

    private static LordWithName AddGroup(MapComponent_CustomMapData component, string seed, GroupDataDef? template)
    {
        string name = seed;
        for (int suffix = 2; component.Lords.Any(group => group.name == name || group.data?.name == name); suffix++)
            name = seed + "_" + suffix;
        var entry = new LordWithName { name = name, data = new LordData { name = name, groupTemplate = template } };
        component.Lords.Add(entry);
        return entry;
    }

    private readonly Map map;
    private LordWithName? selected;
    private string search = "";
    private Vector2 listScroll;
    private Vector2 formScroll;
    private float formHeight;
    private bool showDetails;
}
