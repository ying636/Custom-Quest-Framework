using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_WaitSignal : CQFCinematicStep
{
    public override bool PreviewSafe => false;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(signal)) yield return "CQFCinematicStep_WaitSignal requires a signal.";
        if ((float.IsNaN(timeout) || float.IsInfinity(timeout)) || timeout <= 0f) yield return "CQFCinematicStep_WaitSignal requires a positive finite timeout.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref signal, "signal", "");
        Scribe_Values.Look(ref addQuestPrefix, "addQuestPrefix", true);
        Scribe_Values.Look(ref timeout, "timeout", 60f);
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        state.text = context.SignalName(signal, addQuestPrefix);
        state.intValue = context.SignalVersion(state.text);
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        if (context.SignalVersion(state.text) != state.intValue) return true;
        if (state.elapsed >= timeout) throw new TimeoutException("CQF cinematic signal wait timed out: " + state.text);
        return false;
    }

    public string signal = "";
    public bool addQuestPrefix = true;
    public float timeout = 60f;
}
