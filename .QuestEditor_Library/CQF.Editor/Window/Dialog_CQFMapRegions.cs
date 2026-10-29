using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Dialog_CQFMapRegions : Window
{
    public Dialog_CQFMapRegions(Map map, CQFMapRegion? selected = null)
    {
        this.map = map; this.selected = selected;
        doCloseX = true; draggable = true; resizeable = true; forcePause = true;
        Select(selected ?? Registry.All.FirstOrDefault());
    }

    public override Vector2 InitialSize => new Vector2(850f, 680f);
    private CQFMapRegionCollection Registry => MapComponent_CustomMapData.GetComp(map).Regions;

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
        Widgets.Label(new Rect(0f, 0f, inRect.width * 0.45f, 30f), "CQF_Region_Title".Translate());
        if (CQFUIStyle.ButtonText(new Rect(inRect.width * 0.48f, 0f, inRect.width * 0.46f, 30f), "CustomMapStep_Layout".Translate()))
        {
            Close(); Find.WindowStack.Add(new Dialog_CQFLayout(map, reference: selected));
        }
        float listWidth = Mathf.Min(230f, inRect.width * 0.33f);
        if (CQFUIStyle.ButtonText(new Rect(0f, 38f, listWidth, 32f), "Add".Translate()))
        {
            CQFMapRegion draft = new CQFMapRegion { key = Registry.NextScope("CQF_Region") };
            Close(); Find.DesignatorManager.Select(new Designator_CQFRegionPaint(map, draft, false, false));
        }
        Rect viewport = new Rect(0f, 78f, listWidth, inRect.height - 82f);
        Widgets.BeginScrollView(viewport, ref listScroll, new Rect(0f, 0f, listWidth - 20f, Registry.All.Count * 38f));
        float row = 0f;
        foreach (CQFMapRegion region in Registry.All.ToList())
        {
            if (CQFUIStyle.ButtonText(new Rect(0f, row, listWidth - 22f, 32f), region.Label)) Select(region);
            row += 38f;
        }
        Widgets.EndScrollView();
        if (selected == null || !Registry.All.Contains(selected)) return;
        Rect details = new Rect(listWidth + 14f, 38f, inRect.width - listWidth - 14f, inRect.height - 42f);
        Widgets.BeginScrollView(details, ref detailScroll, new Rect(0f, 0f, details.width - 20f, detailHeight));
        float width = details.width - 22f;
        float y = 0f;
        Field("CQF_Region_Key", ref key, ref y, width);
        Field("CQF_Region_Name", ref name, ref y, width);
        Field("CQF_Region_Tags", ref tags, ref y, width);
        if (CQFUIStyle.ButtonText(new Rect(0f, y, width, 32f), "Apply".Translate()))
        {
            try
            {
                Registry.Rename(selected, key.Trim()); selected.name = name;
                selected.tags = tags.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList();
                error = "";
            }
            catch (Exception exception) { error = exception.Message; Log.Error(exception.ToString()); }
        }
        y += 40f;
        Widgets.CheckboxLabeled(new Rect(0f, y, width, 28f), "CQF_Region_Protect".Translate(), ref selected.protect);
        y += 36f;
        foreach ((string label, bool erase, bool paint) mode in new[]
        {
            ("CQF_Region_Rectangle", false, false), ("CQF_Region_Paint", false, true), ("CQF_Region_Erase", true, true)
        })
        {
            if (CQFUIStyle.ButtonText(new Rect(0f, y, width, 32f), mode.label.Translate()))
            {
                Close(); Find.DesignatorManager.Select(new Designator_CQFRegionPaint(map, selected, mode.erase, mode.paint));
            }
            y += 38f;
        }
        if (CQFUIStyle.ButtonText(new Rect(0f, y, width, 32f), "CQF_Region_AddConnection".Translate())) AddConnection();
        y += 38f;
        foreach (CQFMapRegionConnection connection in selected.connections.ToList())
        {
            Widgets.Label(new Rect(0f, y, width * 0.4f, 30f), connection.cell.ToString());
            if (CQFUIStyle.ButtonText(new Rect(width * 0.4f, y, width * 0.25f, 30f), connection.direction.ToStringHuman()))
                connection.direction.Rotate(RotationDirection.Clockwise);
            if (CQFUIStyle.ButtonText(new Rect(width * 0.66f, y, width * 0.15f, 30f), connection.width.ToString()))
                connection.width = connection.width >= 9 ? 1 : connection.width + 1;
            if (CQFUIStyle.ButtonText(new Rect(width * 0.82f, y, width * 0.18f, 30f), "Delete".Translate())) selected.connections.Remove(connection);
            y += 38f;
        }
        if (CQFUIStyle.ButtonText(new Rect(0f, y, width, 32f), "Delete".Translate()))
        {
            CQFMapRegion deleting = selected;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDelete".Translate(deleting.Label), () => { Registry.Remove(deleting); Select(null); }));
        }
        y += 40f;
        if (!string.IsNullOrEmpty(error)) { Widgets.Label(new Rect(0f, y, width, 80f), error); y += 84f; }
        detailHeight = y;
        Widgets.EndScrollView();
    }

    private void Select(CQFMapRegion? region)
    {
        selected = region; key = region?.key ?? ""; name = region?.name ?? ""; tags = string.Join(",", region?.tags ?? new List<string>());
    }

    private void Field(string label, ref string value, ref float y, float width)
    {
        Widgets.Label(new Rect(0f, y, width, 24f), label.Translate()); y += 26f;
        value = Widgets.TextField(new Rect(0f, y, width, 30f), value); y += 38f;
    }

    private void AddConnection()
    {
        CQFMapRegion region = selected!;
        Close();
        Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false }, target =>
        {
            Rot4 direction = Enumerable.Range(0, 4).Select(i => new Rot4(i)).First(d => !region.cells.Contains(target.Cell + d.FacingCell));
            region.connections.Add(new CQFMapRegionConnection { cell = target.Cell, direction = direction });
            Find.WindowStack.Add(new Dialog_CQFMapRegions(map, region));
        }, target => GenDraw.DrawFieldEdges(region.cells), target => Find.CurrentMap == map && region.cells.Contains(target.Cell)
            && GenAdj.CardinalDirections.Any(d => !region.cells.Contains(target.Cell + d)));
    }

    private readonly Map map;
    private CQFMapRegion? selected;
    private string key = "";
    private string name = "";
    private string tags = "";
    private string error = "";
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private float detailHeight = 700f;
}
