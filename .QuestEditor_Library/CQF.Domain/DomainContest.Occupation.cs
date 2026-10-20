using Verse;

namespace QuestEditor_Library;

public sealed partial class DomainContest
{
    public int OccupationTicks(MapComponent_Domain side)
        => (ReferenceEquals(side, side.Battlefield) ? firstOccupation : secondOccupation)!.Ticks;

    private void TickOccupation(MapComponent_Domain first)
    {
        MapComponent_Domain second = first.Challenger!;
        bool ready = first.Ready && second.Ready && !first.ArrivalLocked && !second.ArrivalLocked
            && first.Worker?.SuspendLifecycle != true && second.Worker?.SuspendLifecycle != true;
        int tick = GenTicks.TicksGame;
        bool captureFirst = firstOccupation!.Advance(tick, ready && CanOccupy(first));
        bool captureSecond = secondOccupation!.Advance(tick, ready && CanOccupy(second));
        if (!captureFirst && !captureSecond) return;
        (int firstGained, int secondGained) = Partition.Capture(captureFirst ? first.Caster!.Position : null,
            captureSecond ? second.Caster!.Position : null);
        if (firstGained > 0) first.RestoreHealth(300f);
        if (secondGained > 0) second.RestoreHealth(300f);
    }

    private bool CanOccupy(MapComponent_Domain domain)
        => domain.Caster is { Spawned: true, Dead: false, Downed: false } pawn
            && pawn.Map == domain.map && pawn.Position.InBounds(domain.map) && !domain.OwnsCell(pawn.Position);

    private DomainOccupation? firstOccupation = new();
    private DomainOccupation? secondOccupation = new();
}
