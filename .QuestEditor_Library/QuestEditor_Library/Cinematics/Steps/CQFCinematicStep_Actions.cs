using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Actions : CQFCinematicStep
{
    public override bool PreviewSafe => false;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (actions == null || actions.Any(action => action == null)) yield return "CQFCinematicStep_Actions requires non-null actions.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref actions, "actions", LookMode.Deep);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        while (state.cursor < actions.Count)
        {
            actions[state.cursor].Work(context.Targets, context.Quest!);
            state.cursor++;
            if (context.Instance.Outcome != CQFCinematicOutcome.Playing) return false;
        }
        return true;
    }

    public List<CQFAction> actions = new();
}
