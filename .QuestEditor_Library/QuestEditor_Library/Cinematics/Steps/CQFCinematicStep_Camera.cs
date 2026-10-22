using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Camera : CQFCinematicStep
{
    public override IEnumerable<string> Channels => new[] { "Camera" };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if ((float.IsNaN(size) || float.IsInfinity(size)) || size <= 0f) yield return "CQFCinematicStep_Camera requires a positive finite size.";
        if ((float.IsNaN(offset.x) || float.IsInfinity(offset.x)) || (float.IsNaN(offset.y) || float.IsInfinity(offset.y)) || (float.IsNaN(offset.z) || float.IsInfinity(offset.z)))
            yield return "CQFCinematicStep_Camera requires a finite offset.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref targetKey, "targetKey", "Position");
        Scribe_Values.Look(ref size, "size", 24f);
        Scribe_Values.Look(ref follow, "follow");
        Scribe_Values.Look(ref offset, "offset");
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.vector = context.Presentation.HasCamera ? context.Presentation.CameraPosition : Find.Camera.transform.position;
        state.vector.y = 0f;
        state.number = context.Presentation.HasCamera ? context.Presentation.CameraSize : Find.CameraDriver.RootSize;
        TargetInfo target = context.ResolveTarget(targetKey);
        if (!target.IsValid || target.Map != context.Map) throw new InvalidOperationException("CQF cinematic camera target is missing or on another map: " + targetKey);
        CQFCinematicStepState destination = state.Child(0);
        destination.vector = (target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3Shifted()) + offset;
        destination.vector.y = 0f;
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        Vector3 destination = state.Child(0).vector;
        if (follow)
        {
            TargetInfo target = context.ResolveTarget(targetKey);
            if (!target.IsValid || target.Map != context.Map) throw new InvalidOperationException("CQF cinematic camera follow target is missing or on another map: " + targetKey);
            destination = (target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3Shifted()) + offset;
            destination.y = 0f;
        }
        float progress = duration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, state.elapsed / duration);
        float targetSize = Mathf.Clamp(size, Find.CameraDriver.config.sizeRange.min, Find.CameraDriver.config.sizeRange.max);
        context.Presentation.CameraPosition = Vector3.Lerp(state.vector, destination, progress);
        context.Presentation.CameraSize = Mathf.Lerp(state.number, targetSize, progress);
        context.Presentation.HasCamera = true;
        return state.elapsed >= duration;
    }

    public string targetKey = "Position";
    public float size = 24f;
    public bool follow;
    public Vector3 offset;
}
