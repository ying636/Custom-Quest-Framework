#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFMapProjectionEditSession
{
    private void DrawPanel()
    {
        CQFUIStyle.DrawMenuSection(PanelRect);
        GUI.BeginGroup(PanelRect.ContractedBy(12f));
        float width = PanelRect.width - 24f;
        float height = PanelRect.height - 24f;
        bool enabledBefore = GUI.enabled;
        GUI.enabled &= !Find.WindowStack.AnyWindowAbsorbingAllInput;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, width, 32f), "CQF_MP_Layout".Translate());
        TooltipHandler.TipRegion(new Rect(0f, 0f, width, 32f), "CQF_MP_LayoutTip".Translate());
        Text.Font = GameFont.Small;
        Rect scrollRect = new(0f, 40f, width, height - 86f);
        Widgets.BeginScrollView(scrollRect, ref panelScroll, new Rect(0f, 0f, width - 16f, panelContentHeight));
        float inner = width - 16f;
        float y = 0f;
        Widgets.Label(new Rect(0f, y, inner, 24f), "CQF_MP_Name".Translate()); y += 26f;
        Draft.name = Widgets.TextField(new Rect(0f, y, inner, 30f), Draft.name); y += 40f;
        Widgets.Label(new Rect(0f, y, inner - 76f, 28f), Draft.SourceName);
        if (CQFUIStyle.ButtonText(new Rect(inner - 72f, y, 72f, 28f), "CQF_MP_Change".Translate()))
            Find.WindowStack.Add(new Dialog_CQFProjectionSourcePicker(Host, source => SetSource(source)));
        y += 36f;
        if (sourcePreview != null)
        {
            Rect canvas = new(0f, y, inner, 206f);
            Canvas.Draw(sourcePreview, canvas, Draft.SourceBounds, !whole, SetSourceArea);
            TooltipHandler.TipRegion(canvas, (whole ? "CQF_MP_PreviewTip" : "CQF_MP_CropTip").Translate());
            y += 214f;
        }
        float half = (inner - 6f) / 2f;
        if (Choice(new Rect(0f, y, half, 32f), "CQF_MP_FullMap", whole))
        {
            whole = true;
            Draft.sourceRect = CellRect.Empty;
            Draft.ResetView();
            Draft.Dispose();
            SyncCellSize();
        }
        if (Choice(new Rect(half + 6f, y, half, 32f), "CQF_MP_SelectArea", !whole)) whole = false;
        y += 38f;
        Widgets.Label(new Rect(0f, y, inner, 24f), "CQF_MP_AreaSize".Translate(Draft.SourceBounds.Width, Draft.SourceBounds.Height)); y += 32f;
        Widgets.Label(new Rect(0f, y, inner, 24f), "CQF_MP_Mode".Translate()); y += 26f;
        if (Choice(new Rect(0f, y, half, 32f), "CQF_MP_Display", Draft.mode == CQFMapProjectionMode.Display)) Draft.mode = CQFMapProjectionMode.Display;
        if (Choice(new Rect(half + 6f, y, half, 32f), "CQF_MP_Play", Draft.mode == CQFMapProjectionMode.Control)) Draft.mode = CQFMapProjectionMode.Control;
        TooltipHandler.TipRegion(new Rect(0f, y, inner, 32f), "CQF_MP_ModeTip".Translate()); y += 40f;
        Widgets.Label(new Rect(0f, y, inner, 24f), "CQF_MP_Layer".Translate()); y += 26f;
        if (Choice(new Rect(0f, y, half, 32f), "CQF_MP_Above", !Draft.background)) Draft.background = false;
        if (Choice(new Rect(half + 6f, y, half, 32f), "CQF_MP_Below", Draft.background)) Draft.background = true;
        TooltipHandler.TipRegion(new Rect(0f, y, inner, 32f), "CQF_MP_LayerTip".Translate()); y += 42f;
        Widgets.CheckboxLabeled(new Rect(0f, y, inner, 28f), "CQF_MP_Enabled".Translate(), ref Draft.enabled); y += 34f;
        Widgets.CheckboxLabeled(new Rect(0f, y, inner, 28f), "CQF_MP_AllowZoom".Translate(), ref Draft.allowZoom);
        TooltipHandler.TipRegion(new Rect(0f, y, inner, 28f), "CQF_MP_AllowZoomTip".Translate()); y += 34f;
        bool matchCells = Draft.matchCellSize;
        Widgets.CheckboxLabeled(new Rect(0f, y, inner, 28f), "CQF_MP_MatchCellSize".Translate(), ref matchCells);
        TooltipHandler.TipRegion(new Rect(0f, y, inner, 28f), "CQF_MP_MatchCellSizeTip".Translate()); y += 34f;
        if (matchCells != Draft.matchCellSize) SetMatchCellSize(matchCells);
        bool aspect = LockAspect;
        bool aspectEnabled = GUI.enabled;
        GUI.enabled &= !Draft.matchCellSize;
        Widgets.CheckboxLabeled(new Rect(0f, y, inner, 28f), "CQF_MP_KeepAspect".Translate(), ref aspect);
        GUI.enabled = aspectEnabled;
        LockAspect = aspect; y += 36f;
        string placeKey = !Placed || Placement.PlaceMode ? "CQF_MP_Placing" : "CQF_MP_PlaceAgain";
        if (CQFUIStyle.ButtonText(new Rect(0f, y, inner, 32f), placeKey.Translate())) Placement.PlaceMode = true;
        TooltipHandler.TipRegion(new Rect(0f, y, inner, 32f), "CQF_MP_PlacementTip".Translate()); y += 42f;
        if (CQFUIStyle.ButtonText(new Rect(0f, y, inner, 30f), (advanced ? "CQF_MP_HideAdvanced" : "CQF_MP_Advanced").Translate())) advanced = !advanced;
        y += 38f;
        if (advanced) DrawAdvanced(ref y, inner);
        panelContentHeight = y;
        Widgets.EndScrollView();
        bool valid = Placed && !Placement.IsDragging && !Canvas.IsDragging;
        GUI.enabled &= valid;
        if (CQFUIStyle.ButtonText(new Rect(0f, height - 34f, (width - 8f) / 2f, 34f), (Draft.mode == CQFMapProjectionMode.Control ? "CQF_MP_FinishAndPlay" : "CQF_MP_Finish").Translate())
            && (!preciseDirty || ApplyPrecise())) Commit();
        GUI.enabled = enabledBefore && !Find.WindowStack.AnyWindowAbsorbingAllInput;
        if (CQFUIStyle.ButtonText(new Rect((width + 8f) / 2f, height - 34f, (width - 8f) / 2f, 34f), "Cancel".Translate())) Cancel();
        GUI.enabled = enabledBefore;
        GUI.EndGroup();
        if (Event.current.type is EventType.MouseDown or EventType.MouseUp or EventType.ScrollWheel && PanelRect.Contains(Event.current.mousePosition)) Event.current.Use();
    }

    private void DrawAdvanced(ref float y, float width)
    {
        int resolution = Draft.resolution, refresh = Draft.refreshFrames;
        Widgets.Label(new Rect(0f, y, width, 24f), "CQF_MP_Resolution".Translate()); y += 26f;
        Widgets.TextFieldNumeric(new Rect(0f, y, width, 40f), ref Draft.resolution, ref resolutionBuffer, 128f, 4096f); y += 48f;
        Widgets.Label(new Rect(0f, y, width, 24f), "CQF_MP_Refresh".Translate()); y += 26f;
        Widgets.TextFieldNumeric(new Rect(0f, y, width, 40f), ref Draft.refreshFrames, ref refreshBuffer, 1f, 60f); y += 48f;
        if (resolution != Draft.resolution || refresh != Draft.refreshFrames) Draft.Dispose();
        if (!preciseDirty)
        {
            CellRect source = Draft.SourceBounds, destination = Draft.DestinationBounds;
            int[] values = { source.minX, source.minZ, source.Width, source.Height, destination.minX, destination.minZ, destination.Width, destination.Height };
            for (int i = 0; i < values.Length; i++) rectangleBuffers[i] = values[i].ToString();
        }
        if (!whole) DrawPreciseRect(ref y, width, "CQF_MP_SourceRect", 0);
        DrawPreciseRect(ref y, width, "CQF_MP_Destination", 4);
        if (CQFUIStyle.ButtonText(new Rect(0f, y, width, 30f), "CQF_MP_ApplyCoordinates".Translate())) ApplyPrecise();
        y += 38f;
    }

    private void DrawPreciseRect(ref float y, float width, string key, int offset)
    {
        Widgets.Label(new Rect(0f, y, width, 24f), key.Translate()); y += 26f;
        float fieldWidth = (width - 18f) / 4f;
        string[] labels = { "X", "Z", "Width", "Height" };
        for (int i = 0; i < 4; i++)
        {
            Rect field = new(i * (fieldWidth + 6f), y, fieldWidth, 40f);
            string previous = rectangleBuffers[offset + i];
            bool fieldEnabled = GUI.enabled;
            GUI.enabled &= offset != 4 || i < 2 || !Draft.matchCellSize;
            rectangleBuffers[offset + i] = Widgets.TextField(field, previous);
            GUI.enabled = fieldEnabled;
            preciseDirty |= previous != rectangleBuffers[offset + i];
            TooltipHandler.TipRegion(field, ("CQF_MP_" + labels[i]).Translate());
        }
        y += 48f;
    }

    private bool ApplyPrecise()
    {
        try
        {
            int[] values = rectangleBuffers.Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            CQFMapProjection candidate = Draft.Copy();
            candidate.sourceRect = whole ? CellRect.Empty : new CellRect(values[0], values[1], values[2], values[3]);
            candidate.destination = new CellRect(values[4], values[5], values[6], values[7]);
            candidate.Validate(Host);
            Draft.sourceRect = candidate.sourceRect;
            Draft.ResetView();
            Draft.destination = candidate.DestinationBounds;
            Draft.Dispose();
            Placed = true;
            preciseDirty = false;
            return true;
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Could not apply projection coordinates: " + error);
            Messages.Message("CQF_MP_Invalid".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }

    private bool Choice(Rect rect, string key, bool selected)
    {
        CQFUIStyle.DrawBackground(rect, selected);
        if (selected)
        {
            Color previous = GUI.color;
            GUI.color = CQFUIStyle.Accent;
            Widgets.DrawBox(rect, 2);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), CQFUIStyle.Accent);
            GUI.color = previous;
        }
        return CQFUIStyle.ButtonText(rect, key.Translate(), false, overrideTextAnchor: TextAnchor.MiddleCenter);
    }
}
#endif
