using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.CinematicsRuntimeChecks;

public sealed class CinematicsCheckCoverAction : CQFAction
{
    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
    {
        CQFCinematicPresentation presentation = GameComponent_Cinematics.Instance!.Active!.Presentation;
        if (presentation.CoverVersion <= 0 || presentation.FadeColor.a < 1f)
            throw new InvalidOperationException("Full-cover action ran before a real opaque repaint.");
        GameComponent_CinematicsChecks state = Current.Game.GetComponent<GameComponent_CinematicsChecks>();
        state.CoverActions++;
        state.ObservedCoverVersion = presentation.CoverVersion;
    }

    public override void ExposeData() { }
}
