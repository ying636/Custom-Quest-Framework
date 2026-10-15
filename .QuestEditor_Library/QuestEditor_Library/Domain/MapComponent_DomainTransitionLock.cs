using Verse;

namespace QuestEditor_Library;

public sealed class MapComponent_DomainTransitionLock : MapComponent
{
    public MapComponent_DomainTransitionLock(Map map) : base(map)
    {
    }

    public bool Locked => participants.Count > 0;

    public void Begin(IEnumerable<Pawn> pawns)
    {
        End();
        foreach (Pawn pawn in pawns.Distinct())
        {
            if (pawn.Dead || !pawn.Spawned || pawn.Map != map) continue;
            participants.Add(new DomainParticipant
                { pawn = pawn, transitionStunEnd = Find.TickManager.TicksGame + pawn.stances.stunner.StunTicksLeft });
            pawn.pather?.StopDead();
            pawn.stances.stunner.StunFor(90, null, false, false, true);
        }
    }

    public void End()
    {
        foreach (DomainParticipant entry in participants)
        {
            Pawn? pawn = entry.pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != map) continue;
            int remaining = Math.Max(0, entry.transitionStunEnd - Find.TickManager.TicksGame);
            if (pawn.stances.stunner.StunTicksLeft > Math.Max(remaining, 90)) continue;
            pawn.stances.stunner.StopStun();
            if (remaining > 0) pawn.stances.stunner.StunFor(remaining, null, false, false, true);
        }
        participants.Clear();
    }

    public override void MapComponentTick()
    {
        foreach (DomainParticipant entry in participants)
        {
            Pawn? pawn = entry.pawn;
            if (pawn != null && !pawn.Dead && pawn.Spawned && pawn.Map == map && pawn.stances.stunner.StunTicksLeft < 90)
                pawn.stances.stunner.StunFor(90, null, false, false, true);
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref participants, "domainReturnParticipants", LookMode.Deep);
    }

    private List<DomainParticipant> participants = new();
}
