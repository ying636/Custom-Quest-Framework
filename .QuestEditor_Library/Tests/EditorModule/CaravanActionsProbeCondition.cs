using QuestEditor_Library;
using RimWorld.Planet;

internal sealed class CaravanActionsProbeCondition : WorldCondition
{
    public override bool Satisfied(WorldTarget target)
    {
        Target = target.Caravan;
        return Allowed;
    }

    public bool Allowed;
    public Caravan? Target;
}
