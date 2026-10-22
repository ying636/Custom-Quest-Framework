using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_PawnJob : CQFCinematicStep
{
    public CQFCinematicStep_PawnJob()
    {
        clock = CQFCinematicClock.GameTime;
    }

    public override bool PreviewSafe => false;
    public override IEnumerable<string> Channels => new[] { "Pawn:" + pawnKey };

    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (!state.flag || state.Child(0).flag) return;
        Pawn pawn = state.pawn ?? throw new InvalidOperationException("CQF cinematic job pawn was lost: " + pawnKey);
        if (pawn.CurJob?.loadID != state.intValue) throw new InvalidOperationException("CQF cinematic job was lost while loading: " + pawnKey);
        state.runtime = new CQFCinematicJobBinding(pawn.jobs.curDriver, state.Child(0));
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(pawnKey)) yield return "CQFCinematicStep_PawnJob requires a pawn target key.";
        if (jobDef == null || jobDef.driverClass == null) yield return "CQFCinematicStep_PawnJob requires a job definition with a driver.";
        if (float.IsNaN(timeout) || float.IsInfinity(timeout) || timeout <= 0f)
            yield return "CQFCinematicStep_PawnJob requires a positive finite timeout.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref pawnKey, "pawnKey", "Trigger");
        Scribe_Defs.Look(ref jobDef, "jobDef");
        Scribe_Values.Look(ref targetKey, "targetKey", "");
        Scribe_Values.Look(ref timeout, "timeout", 60f);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.pawn = context.ResolveTarget(pawnKey).Thing as Pawn;
        if (state.pawn == null) throw new InvalidOperationException("CQF cinematic pawn is missing: " + pawnKey);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        CQFCinematicStepState result = state.Child(0);
        if (result.flag)
        {
            if ((JobCondition)result.intValue != JobCondition.Succeeded)
                throw new InvalidOperationException("CQF cinematic pawn job ended with " + (JobCondition)result.intValue + ": " + pawnKey);
            return true;
        }
        Pawn pawn = state.pawn ?? throw new InvalidOperationException("CQF cinematic job pawn was lost: " + pawnKey);
        if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Map != context.Map)
            throw new InvalidOperationException("CQF cinematic pawn cannot perform the job: " + pawnKey);
        if (!state.flag)
        {
            if (jobDef == null) throw new InvalidOperationException("CQF cinematic job definition is missing.");
            Job job;
            if (string.IsNullOrWhiteSpace(targetKey)) job = JobMaker.MakeJob(jobDef);
            else
            {
                TargetInfo target = context.ResolveTarget(targetKey);
                if (!target.IsValid || target.Map != context.Map) throw new InvalidOperationException("CQF cinematic job target is missing or on another map: " + targetKey);
                job = JobMaker.MakeJob(jobDef, target.HasThing ? new LocalTargetInfo(target.Thing) : new LocalTargetInfo(target.Cell));
            }
            job.playerForced = true;
            state.intValue = job.loadID;
            state.flag = true;
            CQFCinematicJobBinding binding = new(job, result);
            state.runtime = binding;
            try
            {
                pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, tag: JobTag.Misc);
            }
            finally
            {
                binding.ReleasePending();
            }
            if (result.flag)
            {
                if ((JobCondition)result.intValue != JobCondition.Succeeded)
                    throw new InvalidOperationException("CQF cinematic pawn job ended with " + (JobCondition)result.intValue + ": " + pawnKey);
                return true;
            }
        }
        if (pawn.CurJob?.loadID != state.intValue) throw new InvalidOperationException("CQF cinematic pawn job was interrupted: " + pawnKey);
        if (state.elapsed >= timeout) throw new TimeoutException("CQF cinematic pawn job timed out: " + pawnKey);
        return false;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        if (state.runtime is CQFCinematicJobBinding binding) binding.Dispose();
        state.runtime = null;
        if (context.Instance.IsCurrentGame && cancelled && state.flag && state.pawn?.CurJob?.loadID == state.intValue)
            state.pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    public string pawnKey = "Trigger";
    public JobDef jobDef = null!;
    public string targetKey = "";
    public float timeout = 60f;
}
