#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class Window_CQFBackgroundPlacement
{
    private void DrawSettings(Rect rect)
    {
        CQFUIStyle.DrawMenuSection(rect);
        Rect scroll = rect.ContractedBy(8f);
        var view = new Rect(0f, 0f, scroll.width - 16f, Mathf.Max(164f, scroll.height));
        Widgets.BeginScrollView(scroll, ref this.settingsScroll, view);
        using var scope = new CQFUIScope(view.width);
        CustomMapBackgroundData data = this.session.Draft;
        float column = (view.width - 22f) / 2f;
        this.DrawBackgroundOptions(data, column);
        this.DrawBackgroundAppearance(data, column + 22f, column);
        Widgets.EndScrollView();
    }

    private void DrawBackgroundOptions(CustomMapBackgroundData data, float width)
    {
        bool panoramic = data.DrawOnCameraVisibleArea;
        Rect panoramicRect = new Rect(0f, 0f, width, 27f);
        TooltipHandler.TipRegion(panoramicRect, "CQF_SceneryPanoramaTip".Translate());
        if (Toggle(panoramicRect, "CQF_BackgroundPlacementPanorama", panoramic))
            this.session.Change(settings => settings.drawScope = panoramic
                ? CustomMapBackgroundDrawScope.Map : CustomMapBackgroundDrawScope.CameraVisible);

        bool terrainEdges = data.enableTerrainEdges;
        Rect backgroundRect = new Rect(0f, 31f, width, 27f);
        TooltipHandler.TipRegion(backgroundRect, "CQF_MapBackgroundIsBackgroundMapTip".Translate());
        if (Toggle(backgroundRect, "CQF_SceneryBackgroundMap", terrainEdges))
            this.session.Change(settings => settings.enableTerrainEdges = !terrainEdges);

        bool hasTexture = !data.texPath.NullOrEmpty() && data.texPath != EmptyTexture;
        float actionWidth = Mathf.Min(94f, width * .32f);
        Rect texture = new Rect(0f, 65f, hasTexture ? width - actionWidth - 6f : width, 27f);
        string label = hasTexture ? data.texPath.Substring(data.texPath.LastIndexOf('/') + 1)
            : "CQF_SceneryChooseTexture".Translate();
        if (CQFUIStyle.ButtonText(texture, label))
            Find.WindowStack.Add(new Dialog_SelectMapBackgroundImage(path => this.SetTexture(path), data.texPath));
        if (hasTexture)
        {
            TooltipHandler.TipRegion(texture, data.texPath);
            if (Button(new Rect(width - actionWidth, 65f, actionWidth, 27f), "CQF_SceneryClearTexture"))
                this.SetTexture(EmptyTexture);
        }

        this.textureInput ??= hasTexture ? data.texPath : string.Empty;
        float pathLabel = Mathf.Min(84f, width * .27f);
        Widgets.LabelFit(new Rect(0f, 98f, pathLabel - 5f, 27f), "CQF_SceneryTexturePath".Translate());
        Rect input = new Rect(pathLabel, 98f, width - actionWidth - pathLabel - 6f, 27f);
        this.textureInput = Widgets.TextField(input, this.textureInput);
        TooltipHandler.TipRegion(input, "CQF_SceneryTexturePathTip".Translate());
        if (Button(new Rect(width - actionWidth, 98f, actionWidth, 27f), "CQF_ScenerySetTexture",
                !string.IsNullOrWhiteSpace(this.textureInput) && this.textureInput != data.texPath))
        {
            string path = this.textureInput.Trim();
            if (ContentFinder<Texture2D>.Get(path, false) == null)
                Messages.Message("CQF_SceneryMissingTexture".Translate(path), MessageTypeDefOf.RejectInput, false);
            else this.SetTexture(path);
        }

        if (data.DrawOnCameraVisibleArea)
        {
            float labelWidth = width * .48f;
            Widgets.LabelFit(new Rect(0f, 131f, labelWidth - 6f, 27f), "CQF_SceneryFit".Translate());
            if (CQFUIStyle.ButtonText(new Rect(labelWidth, 131f, width - labelWidth, 27f), data.FitModeLabel))
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("CQF_MapBackgroundFitMode_Tile".Translate(),
                        () => this.SetFit(CustomMapBackgroundFitMode.Tile)),
                    new FloatMenuOption("CQF_MapBackgroundFitMode_Stretch".Translate(),
                        () => this.SetFit(CustomMapBackgroundFitMode.Stretch)),
                    new FloatMenuOption("CQF_MapBackgroundFitMode_Cover".Translate(),
                        () => this.SetFit(CustomMapBackgroundFitMode.Cover))
                }));
        }
    }

    private void DrawBackgroundAppearance(CustomMapBackgroundData data, float x, float width)
    {
        float labelWidth = width * .47f;
        float alpha = data.alpha;
        Widgets.LabelFit(new Rect(x, 0f, labelWidth - 5f, 27f), "CQF_SceneryOpacity".Translate());
        Widgets.TextFieldPercent(new Rect(x + labelWidth, 0f, Mathf.Min(67f, width - labelWidth - 17f), 27f),
            ref alpha, ref this.alphaInput);
        alpha = Mathf.Clamp01(alpha);
        if (alpha != data.alpha) this.session.Change(settings => settings.alpha = alpha);

        Rect second = new Rect(x, 33f, width, 27f);
        if (!data.DrawOnCameraVisibleArea)
        {
            Vector2 size = ReadVector(second, "CQF_ScenerySize", data.drawSize, ref this.sizeXInput, ref this.sizeYInput, 0f);
            if (size != data.drawSize) this.session.Change(settings => settings.drawSize = size);
            TooltipHandler.TipRegion(second, "CQF_ScenerySizeTip".Translate());
        }
        else if (data.fitMode == CustomMapBackgroundFitMode.Tile)
        {
            float scale = data.scale;
            Widgets.LabelFit(new Rect(x, 33f, labelWidth - 5f, 27f), "CQF_SceneryTextureScale".Translate());
            Widgets.TextFieldNumeric(new Rect(x + labelWidth, 33f, width - labelWidth, 27f), ref scale, ref this.scaleInput, .01f);
            if (scale != data.scale) this.session.Change(settings => settings.scale = scale);
        }

        Vector2 offset = ReadVector(new Rect(x, 66f, width, 27f), "CQF_SceneryOffset", data.offset,
            ref this.offsetXInput, ref this.offsetYInput);
        if (offset != data.offset) this.session.Change(settings => settings.offset = offset);
        float colorY = 99f;
        CQFEditorTools.DrawSelectColorButtons(ref colorY, "CQF_SceneryTint".Translate(), data.color,
            tint => { if (this.session.Draft.color != tint) this.session.Change(settings => settings.color = tint); }, x);
    }

    private void SetTexture(string path)
    {
        if (!this.session.CanEditMap || this.session.Draft.texPath == path) return;
        this.session.Change(settings => settings.texPath = path);
        this.textureInput = path == EmptyTexture ? string.Empty : path;
    }

    private void SetFit(CustomMapBackgroundFitMode mode)
    {
        if (this.session.Draft.fitMode != mode) this.session.Change(settings => settings.fitMode = mode);
    }

    private void ResetSettingInputs()
    {
        this.textureInput = null;
        this.alphaInput = this.scaleInput = null!;
        this.sizeXInput = this.sizeYInput = this.offsetXInput = this.offsetYInput = null!;
    }

    private static Vector2 ReadVector(Rect row, string key, Vector2 value, ref string xBuffer, ref string yBuffer, float min = float.MinValue)
    {
        float labelWidth = row.width * .47f;
        float fieldWidth = (row.width - labelWidth - 5f) / 2f;
        Widgets.LabelFit(new Rect(row.x, row.y, labelWidth - 5f, row.height), key.Translate());
        Widgets.TextFieldNumeric(new Rect(row.x + labelWidth, row.y, fieldWidth, row.height), ref value.x, ref xBuffer, min);
        Widgets.TextFieldNumeric(new Rect(row.x + labelWidth + fieldWidth + 5f, row.y, fieldWidth, row.height), ref value.y, ref yBuffer, min);
        return value;
    }

    private const string EmptyTexture = "UI/Null";
    private Vector2 settingsScroll;
    private string? textureInput;
    private string alphaInput = null!;
    private string scaleInput = null!;
    private string sizeXInput = null!;
    private string sizeYInput = null!;
    private string offsetXInput = null!;
    private string offsetYInput = null!;
}
#endif
