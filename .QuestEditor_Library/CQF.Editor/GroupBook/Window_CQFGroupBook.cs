using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class Window_CQFGroupBook : Window
{
    internal Window_CQFGroupBook(Action<GroupDataDef>? selected = null, bool tagsOnly = false)
    {
        this.selected = selected;
        this.doCloseX = true;
        this.absorbInputAroundWindow = true;
        this.closeOnCancel = true;
        this.entries = DefDatabase<GroupDataDef>.AllDefsListForReading
            .Where(group => !tagsOnly || group.lord.groupTags.Any(tag => !tag.NullOrEmpty()))
            .OrderBy(group => group.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        this.filtered = this.entries;
    }

    public override Vector2 InitialSize => new Vector2(Mathf.Min(760f, UI.screenWidth - 48f),
        Mathf.Min(650f, UI.screenHeight - 48f));

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width - 42f, 32f), "CQF_GroupBook".Translate());
        Text.Font = GameFont.Small;
        float searchWidth = inRect.width - (this.selected == null ? 42f : 0f);
        Widgets.Label(new Rect(0f, 40f, 82f, 28f), "CQF_SignalBook_Search".Translate());
        string query = Widgets.TextField(new Rect(87f, 38f, searchWidth - 87f, 28f), this.search);
        if (query != this.search)
        {
            this.search = query;
            string text = query.Trim();
            this.filtered = text.Length == 0 ? this.entries : this.entries.Where(group =>
                group.defName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                group.DisplayName.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                group.lord.groupTags.Any(tag => tag.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            this.scroll = Vector2.zero;
        }
        if (this.selected == null && CQFUIStyle.ButtonImage(new Rect(inRect.width - 32f, 38f, 28f, 28f), TexButton.Plus))
        {
            this.Close();
            QuestEditor_GroupEditor.OpenDraft(new GroupDataDef());
            return;
        }

        Rect viewport = new Rect(0f, 76f, inRect.width, inRect.height - 76f);
        const float rowHeight = 42f;
        Rect content = new Rect(0f, 0f, viewport.width - 20f, this.filtered.Count * rowHeight);
        Widgets.BeginScrollView(viewport, ref this.scroll, content);
        try
        {
            int first = Mathf.Max(0, Mathf.FloorToInt(this.scroll.y / rowHeight));
            int last = Mathf.Min(this.filtered.Count, Mathf.CeilToInt((this.scroll.y + viewport.height) / rowHeight));
            for (int i = first; i < last; i++)
            {
                GroupDataDef group = this.filtered[i];
                Rect row = new Rect(0f, i * rowHeight, content.width, rowHeight - 4f);
                string tags = string.Join(" · ", group.lord.groupTags);
                float nameWidth = tags.NullOrEmpty() ? row.width : row.width * 0.58f;
                CQFUIStyle.DrawBackground(row, Mouse.IsOver(row));
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(row.x + 8f, row.y, nameWidth - 16f, row.height), group.DisplayName.Truncate(nameWidth - 16f));
                if (!tags.NullOrEmpty())
                    Widgets.Label(new Rect(row.x + nameWidth, row.y, row.width - nameWidth - 8f, row.height),
                        tags.Truncate(row.width - nameWidth - 8f).Colorize(CQFUIStyle.Muted));
                Text.Anchor = TextAnchor.UpperLeft;
                TooltipHandler.TipRegion(row, tags.NullOrEmpty() ? group.defName : group.defName + " · " + tags);
                if (!Widgets.ButtonInvisible(row)) continue;
                this.Close();
                if (this.selected != null) this.selected(group);
                else QuestEditor_GroupEditor.OpenDraft(CQFGroupBookCatalog.Copy(group));
                break;
            }
        }
        finally
        {
            Widgets.EndScrollView();
        }
    }

    private readonly Action<GroupDataDef>? selected;
    private readonly List<GroupDataDef> entries;
    private List<GroupDataDef> filtered;
    private string search = "";
    private Vector2 scroll;
}
