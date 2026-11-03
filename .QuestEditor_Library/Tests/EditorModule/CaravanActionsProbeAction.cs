using QuestEditor_Library;
using RimWorld.Planet;

internal sealed class CaravanActionsProbeAction : WorldAction
{
    public override void Work(WorldTarget target)
    {
        Targets.Add(target.Caravan);
    }

    public readonly List<Caravan> Targets = new();
}
