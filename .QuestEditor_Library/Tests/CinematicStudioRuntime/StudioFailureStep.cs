using QuestEditor_Library;

namespace CQF.CinematicStudioChecks;

public sealed class StudioFailureStep : CQFCinematicStep
{
    protected override bool OnUpdate(CQFCinematicContext context, CQFCinematicStepState state, float realSeconds, int gameTicks)
        => throw new InvalidOperationException("CQF expected nested failure");
}
