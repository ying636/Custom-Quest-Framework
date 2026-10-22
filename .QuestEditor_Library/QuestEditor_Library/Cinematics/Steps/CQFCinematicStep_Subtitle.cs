using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Subtitle : CQFCinematicStep
{
    public override IEnumerable<string> Channels => new[] { "Subtitle" };

    public override void Restore(CQFCinematicContext context, CQFCinematicStepState state)
    {
        context.Presentation.Subtitle = textKey.Translate();
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(textKey)) yield return "CQFCinematicStep_Subtitle requires a text key.";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref textKey, "textKey", "CQFCinematicStep_Subtitle");
    }

    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state)
    {
        context.Presentation.Subtitle = textKey.Translate();
    }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        return state.elapsed >= duration;
    }

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        context.Presentation.Subtitle = "";
    }

    [NoTranslate]
    public string textKey = "CQFCinematicStep_Subtitle";
}
