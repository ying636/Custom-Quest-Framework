using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicPresentation : IExposable
{
    public void ExposeData()
    {
        Scribe_Values.Look(ref FadeColor, "fadeColor");
        Scribe_Values.Look(ref LetterboxHeight, "letterboxHeight");
        Scribe_Values.Look(ref Subtitle, "subtitle", "");
        Scribe_Values.Look(ref ImagePath, "imagePath", "");
        Scribe_Values.Look(ref ImageRectNormalized, "imageRect", new Rect(0f, 0f, 1f, 1f));
        Scribe_Values.Look(ref HasCamera, "hasCamera");
        Scribe_Values.Look(ref CameraPosition, "cameraPosition");
        Scribe_Values.Look(ref CameraSize, "cameraSize", 24f);
        Scribe_Values.Look(ref CoverVersion, "coverVersion");
    }

    public void Draw(Rect rect)
    {
        using CQFUIScope scope = new(rect.width, rect.height);
        if (!string.IsNullOrWhiteSpace(ImagePath))
        {
            Texture2D texture = ContentFinder<Texture2D>.Get(ImagePath, true);
            if (texture == null) throw new InvalidOperationException("[CQF] Cinematic image missing: " + ImagePath);
            Rect imageRect = new(rect.x + rect.width * ImageRectNormalized.x, rect.y + rect.height * ImageRectNormalized.y,
                rect.width * ImageRectNormalized.width, rect.height * ImageRectNormalized.height);
            Widgets.DrawTextureFitted(imageRect, texture, 1f);
        }
        if (FadeColor.a > 0f)
        {
            Widgets.DrawBoxSolid(rect, FadeColor);
            if (FadeColor.a >= 1f && Event.current.type == EventType.Repaint) CoverVersion = unchecked(CoverVersion + 1);
        }
        float height = Mathf.Clamp(LetterboxHeight, 0f, 0.45f) * rect.height;
        if (height > 0f)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, height), Color.black);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - height, rect.width, height), Color.black);
        }
        if (!string.IsNullOrEmpty(Subtitle))
        {
            string text = Subtitle.CanTranslate() ? Subtitle.Translate().ToString() : Subtitle;
            float width = Mathf.Max(100f, rect.width * 0.78f);
            float textHeight = Mathf.Min(rect.height * 0.4f, Text.CalcHeight(text, width) + 24f);
            Rect subtitle = new(rect.center.x - width / 2f - 12f, rect.yMax - Mathf.Max(height, 36f) - textHeight - 12f, width + 24f, textHeight);
            Widgets.DrawBoxSolid(subtitle, new Color(0f, 0f, 0f, 0.8f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(subtitle.ContractedBy(12f), text);
        }
    }

    public Color FadeColor = Color.clear;
    public int CoverVersion;
    public float LetterboxHeight;
    public string Subtitle = "";
    public string ImagePath = "";
    public Rect ImageRectNormalized = new(0f, 0f, 1f, 1f);
    public bool HasCamera;
    public Vector3 CameraPosition;
    public float CameraSize = 24f;
    [Unsaved]
    public Vector3 ShakeOffset;
}
