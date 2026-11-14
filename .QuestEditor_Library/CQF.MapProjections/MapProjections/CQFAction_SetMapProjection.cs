#if RIMWORLD_1_7
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFAction_SetMapProjection : CQFAction
{
    public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        Map[] hosts = GameTools.GetTargets(targets, quest, new List<string> { hostKey }).Values.Select(target => target.Map).Distinct().ToArray();
        Map[] sources = GameTools.GetTargets(targets, quest, new List<string> { sourceKey }).Values.Select(target => target.Map).Distinct().ToArray();
        if (hosts.Length != 1 || hosts[0] == null || sources.Length != 1 || sources[0] == null)
            throw new InvalidOperationException("[CQF] SetMapProjection requires exactly one host and one source map.");
        CQFMapProjection projection = new() { id = id, sourceMap = sources[0], sourceRect = sourceRect, destination = destination,
            mode = mode, background = background, enabled = enabled, resolution = resolution, refreshFrames = refreshFrames,
            allowZoom = allowZoom, matchCellSize = matchCellSize };
        hosts[0].GetComponent<MapComponent_CQFMapProjections>().Set(projection);
    }

    public override void Draw(ref float y, Rect inRect, float x)
    {
        base.Draw(ref y, inRect, x);
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MP_ID".Translate(), ref id, x); y += 34f;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MP_Host".Translate(), ref hostKey, x); y += 34f;
        CQFEditorTools.DrawLabelAndText_Line(y, "CQF_MP_Source".Translate(), ref sourceKey, x); y += 34f;
        if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 32f),
            (mode == CQFMapProjectionMode.Control ? "CQF_MP_Control" : "CQF_MP_Display").Translate()))
            mode = mode == CQFMapProjectionMode.Control ? CQFMapProjectionMode.Display : CQFMapProjectionMode.Control;
        y += 40f;
        float width = Mathf.Max(40f, inRect.width - x - 12f);
        Widgets.CheckboxLabeled(new Rect(x, y, width, 30f), "CQF_MP_Enabled".Translate(), ref enabled); y += 36f;
        Widgets.CheckboxLabeled(new Rect(x, y, width, 30f), "CQF_MP_Background".Translate(), ref background); y += 36f;
        Widgets.CheckboxLabeled(new Rect(x, y, width, 30f), "CQF_MP_AllowZoom".Translate(), ref allowZoom);
        TooltipHandler.TipRegion(new Rect(x, y, width, 30f), "CQF_MP_AllowZoomTip".Translate()); y += 36f;
        Widgets.CheckboxLabeled(new Rect(x, y, width, 30f), "CQF_MP_MatchCellSize".Translate(), ref matchCellSize);
        TooltipHandler.TipRegion(new Rect(x, y, width, 30f), "CQF_MP_MatchCellSizeTip".Translate()); y += 36f;
        bool whole = sourceRect.IsEmpty;
        bool previousWhole = whole;
        Widgets.CheckboxLabeled(new Rect(x, y, width, 30f), "CQF_MP_Whole".Translate(), ref whole); y += 36f;
        if (whole != previousWhole) sourceRect = whole ? CellRect.Empty : new CellRect(0, 0, 40, 40);
        if (!whole) DrawRect(ref y, x, width, "CQF_MP_SourceRect", ref sourceRect, 0);
        DrawRect(ref y, x, width, "CQF_MP_Destination", ref destination, 4);
        Widgets.Label(new Rect(x, y, width * 0.55f, 30f), "CQF_MP_Resolution".Translate());
        Widgets.TextFieldNumeric(new Rect(x + width * 0.55f, y, width * 0.45f, 30f), ref resolution, ref resolutionBuffer, 128f, 4096f); y += 36f;
        Widgets.Label(new Rect(x, y, width * 0.55f, 30f), "CQF_MP_Refresh".Translate());
        Widgets.TextFieldNumeric(new Rect(x + width * 0.55f, y, width * 0.45f, 30f), ref refreshFrames, ref refreshBuffer, 1f, 60f); y += 36f;
    }

    public override XElement SaveToXElement(string nodeName)
    {
        XElement result = base.SaveToXElement(nodeName);
        result.Add(new XElement("id", id), new XElement("hostKey", hostKey), new XElement("sourceKey", sourceKey),
            new XElement("sourceRect", sourceRect.ToString()), new XElement("destination", destination.ToString()), new XElement("mode", mode),
            new XElement("background", background), new XElement("enabled", enabled), new XElement("resolution", resolution),
            new XElement("refreshFrames", refreshFrames), new XElement("allowZoom", allowZoom), new XElement("matchCellSize", matchCellSize));
        return result;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref id, "id", "CQF_Projection");
        Scribe_Values.Look(ref hostKey, "hostKey", "Position");
        Scribe_Values.Look(ref sourceKey, "sourceKey", string.Empty);
        Scribe_Values.Look(ref sourceRect, "sourceRect", CellRect.Empty);
        Scribe_Values.Look(ref destination, "destination");
        Scribe_Values.Look(ref mode, "mode");
        Scribe_Values.Look(ref background, "background");
        Scribe_Values.Look(ref enabled, "enabled", true);
        Scribe_Values.Look(ref resolution, "resolution", 1024);
        Scribe_Values.Look(ref refreshFrames, "refreshFrames", 1);
        Scribe_Values.Look(ref allowZoom, "allowZoom", true);
        Scribe_Values.Look(ref matchCellSize, "matchCellSize");
    }

    private void DrawRect(ref float y, float x, float width, string key, ref CellRect rect, int offset)
    {
        Widgets.Label(new Rect(x, y, width, 26f), key.Translate()); y += 28f;
        int[] values = { rect.minX, rect.minZ, rect.Width, rect.Height };
        if (offset == 4 && matchCellSize && !sourceRect.IsEmpty) { values[2] = sourceRect.Width; values[3] = sourceRect.Height; }
        float fieldWidth = (width - 18f) / 4f;
        for (int i = 0; i < 4; i++)
        {
            Rect field = new(x + (fieldWidth + 6f) * i, y, fieldWidth, 30f);
            bool fieldEnabled = GUI.enabled;
            GUI.enabled &= offset != 4 || i < 2 || !matchCellSize;
            Widgets.TextFieldNumeric(field, ref values[i], ref rectBuffers[offset + i], i < 2 ? 0f : 1f);
            GUI.enabled = fieldEnabled;
            TooltipHandler.TipRegion(field, ("CQF_MP_" + new[] { "X", "Z", "Width", "Height" }[i]).Translate());
        }
        rect = new CellRect(values[0], values[1], values[2], values[3]);
        y += 36f;
    }

    public string id = "CQF_Projection";
    public string hostKey = "Position";
    public string sourceKey = string.Empty;
    public CellRect sourceRect = CellRect.Empty;
    public CellRect destination = new(0, 0, 40, 40);
    public CQFMapProjectionMode mode;
    public bool background;
    public bool enabled = true;
    public int resolution = 1024;
    public int refreshFrames = 1;
    public bool allowZoom = true;
    public bool matchCellSize;
    private readonly string[] rectBuffers = new string[8];
    private string resolutionBuffer = string.Empty;
    private string refreshBuffer = string.Empty;
}
#endif
