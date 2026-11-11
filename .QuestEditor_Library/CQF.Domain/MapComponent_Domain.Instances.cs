using Verse;

namespace QuestEditor_Library;

public sealed partial class MapComponent_Domain
{
    public IEnumerable<MapComponent_Domain> Instances
    {
        get
        {
            MapComponent_Domain field = Battlefield;
            yield return field;
            if (field.challenger != null) yield return field.challenger;
            if (field.additionalDomains != null) foreach (MapComponent_Domain side in field.additionalDomains) yield return side;
        }
    }

    public int InstanceCount => 1 + (Battlefield.challenger == null ? 0 : 1) + (Battlefield.additionalDomains?.Count ?? 0);
    public int ActiveDomainCount
    {
        get
        {
            MapComponent_Domain field = Battlefield;
            int count = field.active ? 1 : 0;
            if (field.challenger?.active == true) count++;
            if (field.additionalDomains != null)
                for (int index = 0; index < field.additionalDomains.Count; index++) if (field.additionalDomains[index].active) count++;
            return count;
        }
    }

    public int InstanceIndex
    {
        get
        {
            MapComponent_Domain field = Battlefield;
            if (ReferenceEquals(field, this)) return 0;
            if (ReferenceEquals(field.challenger, this)) return 1;
            int index = field.additionalDomains?.IndexOf(this) ?? -1;
            return index < 0 ? -1 : index + 2;
        }
    }

    internal MapComponent_Domain InstanceAt(int index)
    {
        MapComponent_Domain field = Battlefield;
        return index == 0 ? field : index == 1 ? field.challenger
            ?? throw new ArgumentOutOfRangeException(nameof(index)) : field.additionalDomains[index - 2];
    }

    internal void AttachInstance(MapComponent_Domain side)
    {
        if (independentInstance || side.map != map || !side.independentInstance || Instances.Contains(side) || InstanceCount >= MaximumDomains)
            throw new InvalidOperationException("[CQF] Invalid battlefield domain attachment.");
        if (challenger == null) challenger = side;
        else additionalDomains.Add(side);
    }

    internal bool CanContinueCounterOpening(Pawn? pawn) => HasActiveDomain && InstanceCount < MaximumDomains
        && Instances.Where(side => side.Active).All(side => side.Ready && side.Caster != pawn && side.Worker?.SupportsContests == true);

    public const int MaximumDomains = 8;
    private List<MapComponent_Domain> additionalDomains = new();
}
