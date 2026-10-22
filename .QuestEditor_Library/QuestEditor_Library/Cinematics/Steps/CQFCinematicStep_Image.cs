using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Image : CQFCinematicStep
{
    public override IEnumerable<string> Channels => new[] { "Image" };

    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        OnEnter(context, state);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(imagePath)) yield return "CQFCinematicStep_Image requires an image path.";
        if ((float.IsNaN(rectNormalized.x) || float.IsInfinity(rectNormalized.x)) || (float.IsNaN(rectNormalized.y) || float.IsInfinity(rectNormalized.y)) || (float.IsNaN(rectNormalized.width) || float.IsInfinity(rectNormalized.width))
            || (float.IsNaN(rectNormalized.height) || float.IsInfinity(rectNormalized.height)) || rectNormalized.x < 0f || rectNormalized.y < 0f
            || rectNormalized.width <= 0f || rectNormalized.height <= 0f || rectNormalized.xMax > 1f || rectNormalized.yMax > 1f)
            yield return "CQFCinematicStep_Image requires a rectangle inside the normalized screen bounds.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref imagePath, "imagePath", "");
        Scribe_Values.Look(ref rectNormalized, "rectNormalized", new Rect(0f, 0f, 1f, 1f));
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (ContentFinder<Texture2D>.Get(imagePath, false) == null)
            throw new InvalidOperationException("CQF cinematic texture was not found: " + imagePath);
        context.Presentation.ImagePath = imagePath;
        context.Presentation.ImageRectNormalized = rectNormalized;
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        return state.elapsed >= duration;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        context.Presentation.ImagePath = "";
    }

    [NoTranslate]
    public string imagePath = "";
    public Rect rectNormalized = new(0f, 0f, 1f, 1f);
}
