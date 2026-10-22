using QuestEditor_Library;
using RimWorld;
using Verse;

namespace CQF.CinematicsRuntimeChecks;

public sealed class CinematicsCheckAction : CQFAction
{
    public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        => Current.Game.GetComponent<GameComponent_CinematicsChecks>().SkipActions++;

    public override void ExposeData() { }
}
