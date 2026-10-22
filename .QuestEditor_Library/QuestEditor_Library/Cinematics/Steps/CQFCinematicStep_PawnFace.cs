using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_PawnFace : CQFCinematicStep
{
    public override bool PreviewSafe => false;
    public override IEnumerable<string> Channels => new[] { "Pawn:" + pawnKey };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(pawnKey)) yield return "CQFCinematicStep_PawnFace requires a pawn target key.";
        if (string.IsNullOrWhiteSpace(targetKey) && !rotation.IsValid) yield return "CQFCinematicStep_PawnFace requires a target or valid rotation.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref pawnKey, "pawnKey", "Trigger");
        Scribe_Values.Look(ref targetKey, "targetKey", "");
        Scribe_Values.Look(ref rotation, "rotation", Rot4.South);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.pawn = context.ResolveTarget(pawnKey).Thing as Pawn;
        if (state.pawn == null) throw new InvalidOperationException("CQF cinematic pawn is missing: " + pawnKey);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        Pawn pawn = state.pawn ?? throw new InvalidOperationException("CQF cinematic pawn was lost: " + pawnKey);
        if (!pawn.Spawned || pawn.Dead || pawn.Map != context.Map) throw new InvalidOperationException("CQF cinematic pawn cannot face target: " + pawnKey);
        if (string.IsNullOrWhiteSpace(targetKey)) pawn.Rotation = rotation;
        else
        {
            TargetInfo target = context.ResolveTarget(targetKey);
            if (!target.IsValid || target.Map != pawn.Map) throw new InvalidOperationException("CQF cinematic facing target is missing or on another map: " + targetKey);
            pawn.rotationTracker.FaceTarget(target.HasThing ? new LocalTargetInfo(target.Thing) : new LocalTargetInfo(target.Cell));
        }
        return state.elapsed >= duration;
    }

    public string pawnKey = "Trigger";
    public string targetKey = "";
    public Rot4 rotation = Rot4.South;
}
