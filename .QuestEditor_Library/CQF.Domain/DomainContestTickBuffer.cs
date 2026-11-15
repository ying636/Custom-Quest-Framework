using Verse;

namespace QuestEditor_Library;

internal sealed class DomainContestTickBuffer
{
    public List<MapComponent_Domain> Sides { get; } = new(MapComponent_Domain.MaximumDomains);
    public List<bool> Ready { get; } = new(MapComponent_Domain.MaximumDomains);
    public List<int> ErosionTicks { get; } = new(MapComponent_Domain.MaximumDomains);
    public List<IntVec3?> Captures { get; } = new(MapComponent_Domain.MaximumDomains);
    public List<float> Health { get; } = new(MapComponent_Domain.MaximumDomains);

    public void Clear()
    {
        Sides.Clear();
        Ready.Clear();
        ErosionTicks.Clear();
        Captures.Clear();
        Health.Clear();
    }
}
