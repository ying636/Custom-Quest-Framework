using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Shake : CQFCinematicStep
{
    public override IEnumerable<string> Channels => new[] { "Shake" };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if ((float.IsNaN(magnitude) || float.IsInfinity(magnitude)) || magnitude < 0f) yield return "CQFCinematicStep_Shake requires a nonnegative finite magnitude.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref magnitude, "magnitude", 0.15f);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (context.Presentation.HasCamera) return;
        Vector3 position = Find.Camera.transform.position;
        position.y = 0f;
        context.Presentation.CameraPosition = position;
        context.Presentation.CameraSize = Find.CameraDriver.RootSize;
        context.Presentation.HasCamera = true;
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        float intensity = Mathf.Min(magnitude, 0.2f) * Prefs.ScreenShakeIntensity;
        context.Presentation.ShakeOffset = new Vector3(Mathf.Sin(state.elapsed * 24f) * intensity, 0f,
            Mathf.Sin(state.elapsed * 26.4f) * intensity);
        return state.elapsed >= duration;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        context.Presentation.ShakeOffset = Vector3.zero;
    }

    public float magnitude = 0.15f;
}
