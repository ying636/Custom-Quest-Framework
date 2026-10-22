using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_WaitCondition : CQFCinematicStep
{
    public override bool PreviewSafe => false;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (conditions == null || conditions.Count == 0 || conditions.Any(condition => condition == null))
            yield return "CQFCinematicStep_WaitCondition requires non-null conditions.";
        if ((float.IsNaN(timeout) || float.IsInfinity(timeout)) || timeout <= 0f) yield return "CQFCinematicStep_WaitCondition requires a positive finite timeout.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref conditions, "conditions", LookMode.Deep);
        Scribe_Values.Look(ref timeout, "timeout", 60f);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        if (conditions.All(condition => condition.Satisfied(context.Targets, out _, context.Quest!))) return true;
        if (state.elapsed >= timeout) throw new TimeoutException("CQF cinematic condition wait timed out.");
        return false;
    }

    public List<DialogCondition> conditions = new();
    public float timeout = 60f;
}
