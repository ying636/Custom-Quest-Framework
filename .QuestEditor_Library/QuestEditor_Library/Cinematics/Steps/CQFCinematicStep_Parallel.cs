using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Parallel : CQFCinematicStep
{
    public override IEnumerable<string> Channels => (steps ?? new()).Where(step => step != null).SelectMany(step => step.Channels).Distinct();

    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        for (int i = 0; i < steps.Count; i++)
            if (state.Child(i).entered && !state.Child(i).completed) steps[i].Restore(context, state.Child(i));
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (steps == null) { yield return "CQFCinematicStep_Parallel requires steps."; yield break; }
        HashSet<string> claimedChannels = new(StringComparer.Ordinal);
        foreach (CQFCinematicStep step in steps)
        {
            if (step == null) { yield return "CQFCinematicStep_Parallel contains a null step."; continue; }
            foreach (string error in step.ConfigErrors()) yield return error;
            foreach (string channel in step.Channels)
                if (!claimedChannels.Add(channel)) yield return "CQFCinematicStep_Parallel has conflicting channel: " + channel;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref steps, "steps", LookMode.Deep);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        for (int i = 0; i < steps.Count; i++) steps[i].Prepare(context, state.Child(i));
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        bool completed = true;
        for (int i = 0; i < steps.Count; i++)
            completed &= steps[i].Advance(context, state.Child(i), realSeconds, gameTicks);
        return completed;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        if (cancelled)
            for (int i = 0; i < steps.Count; i++)
            {
                try { steps[i].Cancel(context, state.Child(i)); }
                catch (Exception error) { Log.Error("[CQF] Parallel cinematic step cleanup failed: " + steps[i].GetType().Name + "\n" + error); }
            }
    }

    public List<CQFCinematicStep> steps = new();
}
