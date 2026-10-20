using Verse;

namespace QuestEditor_Library;

public partial class MapComponent_Domain
{
    public int ProtectTerritory(IEnumerable<IntVec3> cells, int durationTicks, Thing? source = null)
    {
        if (!Ready || Contest == null || durationTicks <= 0 || durationTicks > int.MaxValue - GenTicks.TicksGame
            || source != null && (source.Destroyed || source.MapHeld != map))
            throw new InvalidOperationException("[CQF] Territory protection requires an active contest, positive duration and a valid source.");
        return Contest.Partition.Protect(ReferenceEquals(this, Battlefield), cells, GenTicks.TicksGame + durationTicks, source);
    }

    public bool RemoveTerritoryProtection(int id)
    {
        DomainTerritoryProtection? protection = Contest?.Partition.Protections.FirstOrDefault(item => item.Id == id);
        if (protection == null) return false;
        if (protection.First != ReferenceEquals(this, Battlefield))
            throw new InvalidOperationException("[CQF] A domain cannot remove its opponent's territory protection.");
        return Contest!.Partition.RemoveProtection(id);
    }

    public DomainStructure RegisterStructure(Building building, bool parkOnTerritoryLoss = true)
        => (layout ?? throw new InvalidOperationException("[CQF] Domain has no active structure layout.")).Register(building, parkOnTerritoryLoss);
}
