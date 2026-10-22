using QuestEditor_Library;

public sealed class CinematicProbeStep : CQFCinematicStep
{
    protected override void OnEnter(CQFCinematicContext context, CQFCinematicStepState state) { Entered++; }

    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
        => state.elapsed >= duration;

    protected override void OnExit(CQFCinematicContext context, CQFCinematicStepState state, bool cancelled)
    {
        Exited++;
        if (cancelled) Cancelled++;
    }

    public static int Entered;
    public static int Exited;
    public static int Cancelled;
}
