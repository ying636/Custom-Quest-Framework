using Verse;

namespace QuestEditor_Library;

public sealed class DomainTerritoryProtection : IExposable
{
    public DomainTerritoryProtection() { }

    public DomainTerritoryProtection(int id, bool first, IEnumerable<IntVec3> cells, int expiresTick, Thing? source)
        : this(id, first ? 0 : 1, cells, expiresTick, source) { }

    public DomainTerritoryProtection(int id, int ownerIndex, IEnumerable<IntVec3> cells, int expiresTick, Thing? source)
    {
        this.id = id;
        this.ownerIndex = ownerIndex;
        this.cells = cells.Distinct().ToList();
        this.expiresTick = expiresTick;
        this.source = source;
        requiresSource = source != null;
    }

    public int Id => id;
    public bool First => ownerIndex == 0;
    public int OwnerIndex => ownerIndex;
    public IReadOnlyList<IntVec3> Cells => cells;
    public int ExpiresTick => expiresTick;
    public Thing? Source => source;

    public bool Expired(int tick) => tick >= expiresTick || requiresSource && (source == null || source.Destroyed);

    public void ExposeData()
    {
        Scribe_Values.Look(ref id, "id");
        Scribe_Values.Look(ref ownerIndex, "ownerIndex");
        Scribe_Values.Look(ref expiresTick, "expiresTick");
        Scribe_Values.Look(ref requiresSource, "requiresSource");
        if (Scribe.mode == LoadSaveMode.Saving && source?.Destroyed == true) source = null;
        Scribe_References.Look(ref source, "source", saveDestroyedThings: true);
        Scribe_Collections.Look(ref cells, "cells", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (id <= 0 || cells == null || cells.Count == 0))
            throw new InvalidOperationException("[CQF] Invalid saved domain territory protection.");
    }

    private int id;
    private int ownerIndex;
    private int expiresTick;
    private bool requiresSource;
    private Thing? source;
    private List<IntVec3> cells = new();
}
