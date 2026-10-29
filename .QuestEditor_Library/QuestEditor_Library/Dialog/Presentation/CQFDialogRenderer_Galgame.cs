using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFDialogRenderer_Galgame : CQFDialogRenderer
{
    public CQFDialogRenderer_Galgame(CQFDialogPresentation_Galgame settings) { this.settings = settings; }

    public override Vector2 GetInitialSize(Vector2 screenSize) => screenSize;

    public override void Draw(Rect rect, CQFDialogFrame frame)
    {
        using CQFUIScope scope = new(rect.width, rect.height);
        GUI.color = Color.white;
        if (!ReferenceEquals(lastNode, frame.Node))
        {
            lastNode = frame.Node;
            fallback = (CQFDialogNodePresentation_Galgame)settings.CreateNodePresentation(frame.Node);
            textScroll = Vector2.zero;
            optionScroll = Vector2.zero;
        }
        CQFDialogNodePresentation_Galgame node = frame.Node.presentations.OfType<CQFDialogNodePresentation_Galgame>().SingleOrDefault() ?? fallback!;
        CQFDialogPortrait left = node.overrideLeft ? node.left : settings.left;
        CQFDialogPortrait right = node.overrideRight ? node.right : settings.right;
        GUI.BeginGroup(rect);
        try
        {
            Rect bounds = new(0f, 0f, rect.width, rect.height);
            Color shade = CQFUIStyle.Panel; shade.a = 0.24f;
            Widgets.DrawBoxSolid(bounds, shade);
            foreach (CQFDialogSceneImage image in node.images.Where(image => image.placement == CQFDialogImagePlacement.Background))
                DrawSceneImage(bounds, image, true);
            float padding = Mathf.Clamp(rect.width * 0.025f, 12f, 40f);
            float dialogHeight = Mathf.Clamp(rect.height * 0.3f, 180f, 300f);
            dialogHeight = Mathf.Min(dialogHeight, rect.height * 0.55f);
            Rect dialog = new(padding, rect.height - dialogHeight - padding, rect.width - padding * 2f, dialogHeight);
            Rect stage = new(padding, padding, rect.width - padding * 2f, Mathf.Max(40f, dialog.y - padding));
            Rect leftRect = new(stage.x, stage.y, stage.width * 0.29f, stage.height + 22f);
            Rect rightRect = new(stage.xMax - leftRect.width, stage.y, leftRect.width, leftRect.height);
            Rect center = new(leftRect.xMax + 12f, stage.y + 30f, Mathf.Max(20f, rightRect.x - leftRect.xMax - 24f), Mathf.Max(20f, stage.height - 42f));
            DrawPortrait(leftRect, left, frame, node.speaker == CQFDialogSpeaker.None || node.speaker == CQFDialogSpeaker.Left);
            DrawPortrait(rightRect, right, frame, node.speaker == CQFDialogSpeaker.None || node.speaker == CQFDialogSpeaker.Right);
            CQFDialogSceneImage[] topics = node.images.Where(image => image.placement == CQFDialogImagePlacement.Center).ToArray();
            for (int i = 0; i < topics.Length; i++)
            {
                float height = center.height / topics.Length;
                DrawSceneImage(new Rect(center.x, center.y + i * height, center.width, Mathf.Max(1f, height - 8f)), topics[i], false);
            }
            Color panel = CQFUIStyle.Panel; panel.a = 0.94f;
            Widgets.DrawBoxSolid(dialog, panel);
            CQFUIStyle.DrawBox(dialog);
            string name = node.speaker == CQFDialogSpeaker.None ? string.Empty : PortraitName(node.speaker == CQFDialogSpeaker.Left ? left : right, frame);
            if (!string.IsNullOrEmpty(name))
            {
                Rect nameRect = new(dialog.x + 16f, dialog.y - 32f, Mathf.Min(dialog.width - 32f, Mathf.Max(160f, name.GetWidthCached() + 32f)), 32f);
                Widgets.DrawBoxSolid(nameRect, CQFUIStyle.Header);
                CQFUIStyle.DrawBox(nameRect);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(nameRect.ContractedBy(12f, 0f), name.Colorize(CQFUIStyle.Accent));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            if (!string.IsNullOrEmpty(frame.Title))
            {
                Rect title = new(padding, padding, rect.width - padding * 2f, 28f);
                Widgets.Label(title, frame.Title.Colorize(CQFUIStyle.TextColor));
            }
            Rect inner = dialog.ContractedBy(16f);
            bool wide = inner.width >= 680f;
            float optionWidth = wide ? Mathf.Clamp(inner.width * 0.31f, 220f, 390f) : inner.width;
            Rect textRect = wide ? new(inner.x, inner.y, inner.width - optionWidth - 20f, inner.height)
                : new(inner.x, inner.y, inner.width, inner.height * 0.48f);
            Rect optionsRect = wide ? new(inner.xMax - optionWidth, inner.y, optionWidth, inner.height)
                : new(inner.x, textRect.yMax + 8f, inner.width, inner.yMax - textRect.yMax - 8f);
            DrawText(textRect, frame.Elements);
            DrawOptions(optionsRect, frame);
        }
        finally { GUI.EndGroup(); }
    }

    private void DrawText(Rect rect, IReadOnlyList<IDialogElement> elements)
    {
        float width = Mathf.Max(1f, rect.width - 20f);
        string text = string.Join("\n\n", elements.Where(element => element is not DialogElement_Image).Select(element => element.GetText()));
        float height = Text.CalcHeight(text, width) + 8f;
        Widgets.BeginScrollView(rect, ref textScroll, new Rect(0f, 0f, width, Mathf.Max(rect.height, height)));
        try { Widgets.Label(new Rect(0f, 0f, width, height), text); }
        finally { Widgets.EndScrollView(); }
    }

    private void DrawOptions(Rect rect, CQFDialogFrame frame)
    {
        float width = Mathf.Max(1f, rect.width - 20f);
        float height = frame.Options.Sum(option => Mathf.Max(42f, Text.CalcHeight(option.text + (option.disabled ? $"({option.disableReason})" : null), width - 16f) + 12f) + 8f);
        Widgets.BeginScrollView(rect, ref optionScroll, new Rect(0f, 0f, width, Mathf.Max(rect.height, height)));
        try
        {
            float y = 0f;
            foreach (DialogElement_Option option in frame.Options) option.Draw(ref y, new Rect(0f, 0f, width, rect.height));
            if (frame.Options.Count == 0 && CQFUIStyle.ButtonText(new Rect(0f, 0f, width, 42f), "Close".Translate())) frame.Close();
        }
        finally { Widgets.EndScrollView(); }
    }

    private void DrawSceneImage(Rect rect, CQFDialogSceneImage image, bool background)
    {
        Texture2D? texture = GetTexture(image.imagePath);
        if (texture == null) { DrawMissing(rect, image.imagePath); return; }
        GUI.BeginGroup(rect);
        try
        {
            Rect local = new(0f, 0f, rect.width, rect.height);
            float ratio = background ? Mathf.Max(local.width / texture.width, local.height / texture.height)
                : Mathf.Min(local.width / texture.width, local.height / texture.height);
            float width = texture.width * ratio * image.scale, height = texture.height * ratio * image.scale;
            Rect target = new((local.width - width) * 0.5f + image.offsetX * local.width, (local.height - height) * 0.5f + image.offsetY * local.height, width, height);
            GUI.DrawTexture(target, texture);
        }
        finally { GUI.EndGroup(); }
    }

    private void DrawPortrait(Rect rect, CQFDialogPortrait portrait, CQFDialogFrame frame, bool speaking)
    {
        if (portrait.source == CQFDialogPortraitSource.Hidden) return;
        Color previous = GUI.color;
        GUI.color = speaking ? Color.white : new Color(0.52f, 0.52f, 0.52f, 1f);
        GUI.BeginGroup(rect);
        try
        {
            Texture? texture;
            if (portrait.source == CQFDialogPortraitSource.Image) texture = GetTexture(portrait.imagePath);
            else
            {
                Thing? participant = portrait.participant == CQFDialogParticipant.Interviewer ? frame.Interviewer : frame.Interviewee;
                if (participant is Pawn pawn)
                    texture = PortraitsCache.Get(pawn, new Vector2(384f, 512f), Rot4.South, cameraZoom: 1.1f);
                else
                {
                    if (participant != null)
                        Widgets.ThingIcon(new Rect(rect.width * (0.5f - portrait.scale * 0.5f + portrait.offsetX), rect.height * (1f - portrait.scale + portrait.offsetY), rect.width * portrait.scale, rect.height * portrait.scale), participant);
                    else
                    {
                        if (!frame.IsPreview && missingParticipants.Add(portrait.participant))
                            Log.Error($"CQF dialog {frame.Tree.defName}: missing portrait participant {portrait.participant}.");
                        DrawMissing(new Rect(0f, 0f, rect.width, rect.height), "CQF_DialogStyle_" + portrait.participant);
                    }
                    return;
                }
            }
            if (texture == null) { DrawMissing(new Rect(0f, 0f, rect.width, rect.height), portrait.imagePath); return; }
            float ratio = Mathf.Min(rect.width / texture.width, rect.height / texture.height) * portrait.scale;
            float width = texture.width * ratio, height = texture.height * ratio;
            Rect target = new((rect.width - width) * 0.5f + portrait.offsetX * rect.width, rect.height - height + portrait.offsetY * rect.height, width, height);
            GUI.DrawTextureWithTexCoords(target, texture, portrait.mirror ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f));
        }
        finally { GUI.EndGroup(); GUI.color = previous; }
    }

    private Texture2D? GetTexture(string path)
    {
        if (textures.TryGetValue(path ?? string.Empty, out Texture2D? texture)) return texture;
        texture = string.IsNullOrWhiteSpace(path) ? null : ContentFinder<Texture2D>.Get(path, true);
        if (string.IsNullOrWhiteSpace(path)) Log.Error("CQF Galgame image path is empty.");
        textures[path ?? string.Empty] = texture;
        return texture;
    }

    private void DrawMissing(Rect rect, string value)
    {
        Widgets.DrawBoxSolid(rect, CQFUIStyle.Card);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect.ContractedBy(8f), value.CanTranslate() ? value.Translate().ToString() : "CQF_DialogGraph_MissingImage".Translate(value).ToString());
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private string PortraitName(CQFDialogPortrait portrait, CQFDialogFrame frame)
    {
        if (!string.IsNullOrWhiteSpace(portrait.nameKey)) return portrait.nameKey.CanTranslate() ? portrait.nameKey.Translate().ToString() : portrait.nameKey;
        Thing? thing = portrait.participant == CQFDialogParticipant.Interviewer ? frame.Interviewer : frame.Interviewee;
        return thing is Pawn pawn ? pawn.Name?.ToStringShort ?? pawn.LabelShort : thing?.LabelShort ?? string.Empty;
    }

    private readonly CQFDialogPresentation_Galgame settings;
    private readonly Dictionary<string, Texture2D?> textures = new();
    private readonly HashSet<CQFDialogParticipant> missingParticipants = new();
    private DialogNode? lastNode;
    private CQFDialogNodePresentation_Galgame? fallback;
    private Vector2 textScroll;
    private Vector2 optionScroll;
}
