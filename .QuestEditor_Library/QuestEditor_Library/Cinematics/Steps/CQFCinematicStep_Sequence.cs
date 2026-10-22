using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Sequence : CQFCinematicStep
{
    public override IEnumerable<string> Channels => (steps ?? new()).Where(step => step != null).SelectMany(step => step.Channels).Distinct();

    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (state.cursor < steps.Count && state.Child(state.cursor).entered && !state.Child(state.cursor).completed)
            steps[state.cursor].Restore(context, state.Child(state.cursor));
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (steps == null) { yield return "CQFCinematicStep_Sequence requires steps."; yield break; }
        foreach (CQFCinematicStep step in steps)
        {
            if (step == null) { yield return "CQFCinematicStep_Sequence contains a null step."; continue; }
            foreach (string error in step.ConfigErrors()) yield return error;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref steps, "steps", LookMode.Deep);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        if (state.cursor < steps.Count) steps[state.cursor].Prepare(context, state.Child(state.cursor));
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        while (state.cursor < steps.Count)
        {
            if (!steps[state.cursor].Advance(context, state.Child(state.cursor), realSeconds, gameTicks)) return false;
            state.cursor++;
            realSeconds = 0f;
            gameTicks = 0;
        }
        return true;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        if (cancelled && state.cursor < steps.Count) steps[state.cursor].Cancel(context, state.Child(state.cursor));
    }

    public List<CQFCinematicStep> steps = new();
}
