using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_PawnWait : CQFCinematicStep
{
    public CQFCinematicStep_PawnWait()
    {
        clock = CQFCinematicClock.GameTime;
    }

    public override bool PreviewSafe => false;
    public override IEnumerable<string> Channels => new[] { "Pawn:" + pawnKey };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(pawnKey)) yield return "CQFCinematicStep_PawnWait requires a pawn target key.";
        if (clock != CQFCinematicClock.GameTime) yield return "CQFCinematicStep_PawnWait requires the game-time clock.";
        if (duration > (int.MaxValue - 60) / 60f) yield return "CQFCinematicStep_PawnWait duration exceeds the supported tick range.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref pawnKey, "pawnKey", "Trigger");
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.pawn = context.ResolveTarget(pawnKey).Thing as Pawn;
        if (state.pawn == null) throw new InvalidOperationException("CQF cinematic pawn is missing: " + pawnKey);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        Pawn pawn = state.pawn ?? throw new InvalidOperationException("CQF cinematic waiting pawn was lost: " + pawnKey);
        if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Map != context.Map)
            throw new InvalidOperationException("CQF cinematic pawn cannot wait: " + pawnKey);
        if (state.elapsed >= duration) return true;
        if (!state.flag)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Wait, Mathf.CeilToInt(duration * 60f) + 60);
            job.playerForced = true;
            job.overrideFacing = pawn.Rotation;
            state.intValue = job.loadID;
            state.flag = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, tag: JobTag.Misc);
        }
        if (pawn.CurJob?.loadID != state.intValue) throw new InvalidOperationException("CQF cinematic pawn wait was interrupted: " + pawnKey);
        return false;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        if (context.Instance.IsCurrentGame && state.flag && state.pawn?.CurJob?.loadID == state.intValue)
            state.pawn.jobs.EndCurrentJob(cancelled ? JobCondition.InterruptForced : JobCondition.Succeeded);
    }

    public string pawnKey = "Trigger";
}
