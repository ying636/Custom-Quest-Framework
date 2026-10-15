using Verse;

namespace QuestEditor_Library;

public sealed partial class GameComponent_MapManager
{
    public DomainTransition? Transition => transition;

    public void QueueOpening(DomainOpening opening)
    {
        if (!openings.Contains(opening)) openings.Add(opening);
    }

    public void SetTransition(DomainTransition? value)
    {
        transition?.Dispose();
        transition = value;
    }

    public void FinishTransitionFor(Map map)
    {
        transition?.DisposeIfMap(map);
        if (transition?.Finished == true) SetTransition(null);
    }

    private void UpdateDomainTransition()
    {
        DomainTransition? current = transition;
        if (current == null) return;
#if RIMWORLD_1_7
        current.Update();
#endif
        if (ReferenceEquals(transition, current) && current.Finished) SetTransition(null);
    }

    private void ProcessDomainOpenings()
    {
        if (openings.Count == 0) return;
        DomainOpening[] pending = openings.ToArray();
        openings.Clear();
        foreach (DomainOpening opening in pending)
        {
            if (opening.Destroyed || !opening.Spawned) continue;
            if (HasAvailable(opening.Definition)) opening.CompleteOpening();
            else LongEventHandler.QueueLongEvent(opening.CompleteOpening, "GeneratingMap", false, null);
        }
    }

    private readonly List<DomainOpening> openings = new();
    private DomainTransition? transition;
}
