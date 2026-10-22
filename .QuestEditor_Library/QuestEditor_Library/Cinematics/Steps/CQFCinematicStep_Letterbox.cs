using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Letterbox : CQFCinematicStep
{
    public override IEnumerable<string> Channels => new[] { "Letterbox" };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if ((float.IsNaN(height) || float.IsInfinity(height)) || height < 0f || height > 0.45f) yield return "CQFCinematicStep_Letterbox height must be between 0 and 0.45.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref height, "height", 0.12f);
        Scribe_Values.Look(ref fromCurrent, "fromCurrent", true);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.number = fromCurrent ? context.Presentation.LetterboxHeight : 0f;
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        context.Presentation.LetterboxHeight = Mathf.Lerp(state.number, height, duration <= 0f ? 1f : state.elapsed / duration);
        return state.elapsed >= duration;
    }

    public float height = 0.12f;
    public bool fromCurrent = true;
}
