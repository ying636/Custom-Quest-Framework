namespace QuestEditor_Library;

public sealed class CQFCinematicStep_Wait : CQFCinematicStep
{
    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
    {
        return state.elapsed >= duration;
    }
}
