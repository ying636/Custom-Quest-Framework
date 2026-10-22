using RimWorld;
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_PawnMove : CQFCinematicStep
{
    public CQFCinematicStep_PawnMove()
    {
        clock = CQFCinematicClock.GameTime;
    }

    public override bool PreviewSafe => false;
    public override IEnumerable<string> Channels => new[] { "Pawn:" + pawnKey };

    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (!state.flag || state.Child(0).flag) return;
        Pawn pawn = state.pawn ?? throw new InvalidOperationException("CQF cinematic movement pawn was lost: " + pawnKey);
        if (pawn.CurJob?.loadID != state.intValue) throw new InvalidOperationException("CQF cinematic movement job was lost while loading: " + pawnKey);
        state.runtime = new CQFCinematicJobBinding(pawn.jobs.curDriver, state.Child(0));
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(pawnKey) || string.IsNullOrWhiteSpace(targetKey))
            yield return "CQFCinematicStep_PawnMove requires pawn and destination target keys.";
        if ((float.IsNaN(timeout) || float.IsInfinity(timeout)) || timeout <= 0f) yield return "CQFCinematicStep_PawnMove requires a positive finite timeout.";
        if (!Enum.IsDefined(typeof(LocomotionUrgency), urgency) || urgency == LocomotionUrgency.None)
            yield return "CQFCinematicStep_PawnMove requires a valid locomotion urgency.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref pawnKey, "pawnKey", "Trigger");
        Scribe_Values.Look(ref targetKey, "targetKey", "Position");
        Scribe_Values.Look(ref timeout, "timeout", 60f);
        Scribe_Values.Look(ref urgency, "urgency", LocomotionUrgency.Walk);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.pawn = context.ResolveTarget(pawnKey).Thing as Pawn;
        Pawn pawn = state.pawn ?? throw new InvalidOperationException("CQF cinematic pawn is missing: " + pawnKey);
        TargetInfo destination = context.ResolveTarget(targetKey);
        if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Map != context.Map || destination.Map != context.Map || !destination.Cell.InBounds(context.Map))
            throw new InvalidOperationException("CQF cinematic pawn cannot move to the requested destination: " + pawnKey);
        IntVec3 cell = destination.Cell;
        if (!cell.Standable(context.Map) || !context.Map.reachability.CanReach(pawn.Position, cell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)))
            throw new InvalidOperationException("CQF cinematic pawn destination is unreachable: " + targetKey);
        state.vector = cell.ToVector3();
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        CQFCinematicStepState result = state.Child(0);
        if (result.flag)
        {
            if ((JobCondition)result.intValue != JobCondition.Succeeded)
                throw new InvalidOperationException("CQF cinematic pawn movement ended with " + (JobCondition)result.intValue + ": " + pawnKey);
            return true;
        }
        Pawn pawn = state.pawn ?? throw new InvalidOperationException("CQF cinematic pawn was lost: " + pawnKey);
        if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Map != context.Map)
            throw new InvalidOperationException("CQF cinematic pawn can no longer move: " + pawnKey);
        if (!state.flag && pawn.Position == state.vector.ToIntVec3()) return true;
        if (!state.flag)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Goto, state.vector.ToIntVec3());
            job.locomotionUrgency = urgency;
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
                    throw new InvalidOperationException("CQF cinematic pawn movement ended with " + (JobCondition)result.intValue + ": " + pawnKey);
                return true;
            }
        }
        if (pawn.CurJob?.loadID != state.intValue) throw new InvalidOperationException("CQF cinematic pawn movement was interrupted: " + pawnKey);
        if (state.elapsed >= timeout) throw new TimeoutException("CQF cinematic pawn movement timed out: " + pawnKey);
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
    public string targetKey = "Position";
    public float timeout = 60f;
    public LocomotionUrgency urgency = LocomotionUrgency.Walk;
}
