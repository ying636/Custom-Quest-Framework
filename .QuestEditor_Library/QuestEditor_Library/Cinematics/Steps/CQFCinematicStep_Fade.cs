using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Fade : CQFCinematicStep
{
    public override IEnumerable<string> Channels => new[] { "Fade" };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if ((float.IsNaN(color.r) || float.IsInfinity(color.r)) || (float.IsNaN(color.g) || float.IsInfinity(color.g)) || (float.IsNaN(color.b) || float.IsInfinity(color.b)) || (float.IsNaN(color.a) || float.IsInfinity(color.a)))
            yield return "CQFCinematicStep_Fade requires a finite color.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref color, "color", Color.black);
        Scribe_Values.Look(ref fromCurrent, "fromCurrent", true);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.color = fromCurrent ? context.Presentation.FadeColor : new Color(color.r, color.g, color.b, 0f);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        context.Presentation.FadeColor = Color.Lerp(state.color, color, duration <= 0f ? 1f : state.elapsed / duration);
        if (state.elapsed < duration) return false;
        if (color.a < 1f) return true;
        if (!state.flag)
        {
            state.flag = true;
            state.intValue = context.Presentation.CoverVersion;
        }
        return context.Presentation.CoverVersion != state.intValue;
    }

    public Color color = Color.black;
    public bool fromCurrent = true;
}
