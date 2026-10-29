using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFDialogGalgameEditor : CQFDialogPresentationEditor
{
    public override string StyleId => "CQF_Galgame";

    public override void DrawTree(CQFDialogPresentation presentation, ref float y, float x, float width)
    {
        CQFDialogPresentation_Galgame settings = (CQFDialogPresentation_Galgame)presentation;
        PortraitButton(settings.left, "CQF_DialogStyle_Left", ref y, x, width);
        PortraitButton(settings.right, "CQF_DialogStyle_Right", ref y, x, width);
    }

    public override void DrawNode(CQFDialogPresentation presentation, CQFDialogNodePresentation node, ref float y, float x, float width)
    {
        CQFDialogNodePresentation_Galgame settings = (CQFDialogNodePresentation_Galgame)node;
        Choice(settings.speaker, value => settings.speaker = value, "CQF_DialogStyle_Speaker", ref y, x, width);
        Checkbox(ref settings.overrideLeft, "CQF_DialogStyle_OverrideLeft", ref y, x, width);
        if (settings.overrideLeft) PortraitButton(settings.left, "CQF_DialogStyle_Left", ref y, x, width);
        Checkbox(ref settings.overrideRight, "CQF_DialogStyle_OverrideRight", ref y, x, width);
        if (settings.overrideRight) PortraitButton(settings.right, "CQF_DialogStyle_Right", ref y, x, width);
        Label("DialogImages", ref y, x, width);
        foreach (CQFDialogSceneImage image in settings.images.ToArray())
        {
            Rect thumbnail = new(x, y, 64f, 64f);
            Texture2D? texture = string.IsNullOrEmpty(image.imagePath) ? null : ContentFinder<Texture2D>.Get(image.imagePath, false);
            Widgets.DrawBoxSolid(thumbnail, CQFUIStyle.Card);
            if (texture != null) Widgets.DrawTextureFitted(thumbnail, texture, 1f);
            if (Widgets.ButtonInvisible(thumbnail)) Find.WindowStack.Add(new Dialog_SelectDialogImage(path => image.imagePath = path, image.imagePath));
            TooltipHandler.TipRegion(thumbnail, "DialogImage_SelectTip".Translate());
            float labelWidth = width - 108f;
            string caption = ("CQF_DialogStyle_" + image.placement).Translate();
            float buttonHeight = Mathf.Max(34f, Text.CalcHeight(caption, labelWidth - 12f) + 8f);
            if (CQFUIStyle.ButtonText(new Rect(x + 72f, y, labelWidth, buttonHeight), caption))
                Find.WindowStack.Add(new Dialog_CQFDialogVisualSettings(w => DrawImage(image, w)));
            if (CQFUIStyle.ButtonImage(new Rect(x + width - 28f, y, 28f, 28f), TexButton.Delete, tooltip: "Delete".Translate())) settings.images.Remove(image);
            y += Mathf.Max(64f, buttonHeight) + 8f;
            if (!string.IsNullOrEmpty(image.imagePath))
            {
                Widgets.Label(new Rect(x, y, width, 24f), image.imagePath.Truncate(width));
                TooltipHandler.TipRegion(new Rect(x, y, width, 24f), image.imagePath);
                y += 30f;
            }
        }
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, 32f), "Add".Translate()))
            Find.WindowStack.Add(new Dialog_SelectDialogImage(path => settings.images.Add(new CQFDialogSceneImage { imagePath = path })));
        y += 44f;
    }

    private static void PortraitButton(CQFDialogPortrait portrait, string key, ref float y, float x, float width)
    {
        string caption = key.Translate() + ": " + ("CQF_DialogStyle_" + portrait.source).Translate();
        float height = Mathf.Max(36f, Text.CalcHeight(caption, width - 20f) + 12f);
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, height), caption))
            Find.WindowStack.Add(new Dialog_CQFDialogVisualSettings(w => DrawPortrait(portrait, w)));
        y += height + 10f;
    }

    private static float DrawPortrait(CQFDialogPortrait portrait, float width)
    {
        float y = 0f;
        Choice(portrait.source, value => portrait.source = value, "CQF_DialogStyle_Source", ref y, 0f, width);
        Choice(portrait.participant, value => portrait.participant = value, "CQF_DialogStyle_Participant", ref y, 0f, width);
        Label("CQF_DialogStyle_NameKey", ref y, 0f, width);
        portrait.nameKey = Widgets.TextField(new Rect(0f, y, width, 32f), portrait.nameKey); y += 42f;
        if (portrait.source == CQFDialogPortraitSource.Image) ImagePath(ref portrait.imagePath, path => portrait.imagePath = path, ref y, width);
        if (portrait.source != CQFDialogPortraitSource.Hidden)
        {
            Slider(ref portrait.scale, "DialogImage_Scale", 0.1f, 3f, ref y, width);
            Slider(ref portrait.offsetX, "CQF_DialogStyle_OffsetX", -1f, 1f, ref y, width);
            Slider(ref portrait.offsetY, "CQF_DialogStyle_OffsetY", -1f, 1f, ref y, width);
            Checkbox(ref portrait.mirror, "CQF_DialogStyle_Mirror", ref y, 0f, width);
        }
        return y;
    }

    private static float DrawImage(CQFDialogSceneImage image, float width)
    {
        float y = 0f;
        ImagePath(ref image.imagePath, path => image.imagePath = path, ref y, width);
        Choice(image.placement, value => image.placement = value, "CQF_DialogStyle_Placement", ref y, 0f, width);
        Slider(ref image.scale, "DialogImage_Scale", 0.1f, 3f, ref y, width);
        Slider(ref image.offsetX, "CQF_DialogStyle_OffsetX", -1f, 1f, ref y, width);
        Slider(ref image.offsetY, "CQF_DialogStyle_OffsetY", -1f, 1f, ref y, width);
        return y;
    }

    private static void ImagePath(ref string path, Action<string> selected, ref float y, float width)
    {
        string current = path;
        if (CQFUIStyle.ButtonText(new Rect(0f, y, width, 34f), "DialogImage_Select".Translate()))
            Find.WindowStack.Add(new Dialog_SelectDialogImage(selected, current));
        y += 42f;
        path = Widgets.TextField(new Rect(0f, y, width, 32f), path); y += 42f;
        Texture2D? texture = string.IsNullOrEmpty(path) ? null : ContentFinder<Texture2D>.Get(path, false);
        if (texture != null) Widgets.DrawTextureFitted(new Rect(0f, y, width, 140f), texture, 1f);
        else Widgets.Label(new Rect(0f, y, width, 140f), "CQF_DialogGraph_MissingImage".Translate(path));
        y += 150f;
    }

    private static void Choice<T>(T value, Action<T> selected, string key, ref float y, float x, float width) where T : struct, Enum
    {
        Label(key, ref y, x, width);
        string caption = ("CQF_DialogStyle_" + value).Translate();
        float height = Mathf.Max(34f, Text.CalcHeight(caption, width - 20f) + 12f);
        if (CQFUIStyle.ButtonText(new Rect(x, y, width, height), caption))
            Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(T)).Cast<T>().Select(item => new FloatMenuOption(("CQF_DialogStyle_" + item).Translate(), () => selected(item))).ToList()));
        y += height + 10f;
    }

    private static void Slider(ref float value, string key, float min, float max, ref float y, float width)
    {
        Label(key, ref y, 0f, width);
        value = Widgets.HorizontalSlider(new Rect(0f, y, width, 32f), value, min, max, label: value.ToString("0.00"));
        y += 42f;
    }

    private static void Checkbox(ref bool value, string key, ref float y, float x, float width)
    {
        string label = key.Translate();
        float height = Mathf.Max(32f, Text.CalcHeight(label, width - 36f) + 8f);
        Widgets.CheckboxLabeled(new Rect(x, y, width, height), label, ref value);
        y += height + 8f;
    }

    private static void Label(string key, ref float y, float x, float width)
    {
        string label = key.Translate();
        float height = Mathf.Max(24f, Text.CalcHeight(label, width));
        Widgets.Label(new Rect(x, y, width, height), label.Colorize(CQFUIStyle.Accent));
        y += height + 6f;
    }
}
