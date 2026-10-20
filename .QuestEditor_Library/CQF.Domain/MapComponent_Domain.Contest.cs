using RimWorld;
using Verse.AI.Group;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public partial class MapComponent_Domain
{
    public MapComponent_Domain Battlefield => map.GetComponent<MapComponent_Domain>();
    public MapComponent_Domain? Challenger => challenger;
    public DomainContest? Contest => Battlefield.contest;
    public bool HasActiveDomain => Battlefield.active || Battlefield.challenger?.Active == true;
    public bool CanAcceptChallenger => !independentInstance && active && Ready && !ArrivalLocked && challenger == null && Worker?.SupportsContests == true;

    public MapComponent_Domain? FindDomainFor(Pawn? pawn)
    {
        MapComponent_Domain battlefield = Battlefield;
        if (pawn == null || battlefield.caster == pawn) return battlefield;
        return battlefield.challenger?.Caster == pawn ? battlefield.challenger : null;
    }

    public MapComponent_Domain? DomainAt(IntVec3 cell)
    {
        if (!cell.InBounds(map)) return null;
        MapComponent_Domain battlefield = Battlefield;
        return battlefield.contest?.OwnerAt(battlefield, cell)
            ?? (battlefield.active ? battlefield : battlefield.challenger?.Active == true ? battlefield.challenger : null);
    }

    public bool OwnsCell(IntVec3 cell) => ReferenceEquals(Battlefield.DomainAt(cell), this);

    public T? GetActiveWorker<T>() where T : DomainWorker
    {
        MapComponent_Domain battlefield = Battlefield;
        return battlefield.active && battlefield.worker is T ownWorker ? ownWorker
            : battlefield.challenger?.Active == true ? battlefield.challenger.GetWorker<T>() : null;
    }

    public bool OwnsParticipant(Pawn pawn) => caster == pawn || participants.Any(entry => entry.pawn == pawn);

    public bool HasOtherActiveDomain(MapComponent_Domain domain)
    {
        MapComponent_Domain battlefield = Battlefield;
        return battlefield.active && !ReferenceEquals(battlefield, domain)
            || battlefield.challenger?.Active == true && !ReferenceEquals(battlefield.challenger, domain);
    }

    public MapComponent_Domain EnterChallenger(DomainDef definition, Pawn pawn, IEnumerable<Pawn> targets, DomainMapTemplate template, bool fromSource = false)
    {
        Map? arrivalMap = fromSource ? sourceMap : map;
        if (!CanAcceptChallenger || !pawn.Spawned || pawn.Map != arrivalMap || pawn.Dead || pawn.Downed || pawn == caster)
            throw new InvalidOperationException("[CQF] Invalid domain counter-expansion state.");
        Worker!.PreparingContest();
        MapComponent_Domain side = new(map, true);
        side.Configure(definition);
        if (side.Worker?.SupportsContests != true) throw new InvalidOperationException("[CQF] Challenging domain worker does not support contests.");
        List<Pawn> entrants = new() { pawn };
        entrants.AddRange(targets.Where(target => target != pawn && target != caster && target.Spawned && !target.Dead && target.Map == arrivalMap).Distinct());
        DomainContest battle = new(this, template.Terrain);
        side.sourceMap = sourceMap;
        side.caster = pawn;
        side.active = true;
        side.health = side.MaximumHealth;
        foreach (Pawn entrant in entrants)
        {
            DomainParticipant? entry = participants.FirstOrDefault(record => record.pawn == entrant);
            if (entry != null) participants.Remove(entry);
            else entry = new DomainParticipant
            {
                pawn = entrant,
                originalPosition = fromSource ? entrant.Position : sourceOrigin + (entrant.Position - map.Center),
                originalLord = entrant.GetLord(),
                drafted = entrant.Drafted
            };
            side.participants.Add(entry);
        }
        side.sourceOrigin = side.participants[0].originalPosition;
        challenger = side;
        contest = battle;
        try
        {
            if (fromSource)
                foreach (DomainParticipant entry in side.participants)
                {
                    Pawn entrant = entry.pawn!;
                    IntVec3 cell = FindFreeCell(map, map.Center + (entry.originalPosition - sourceOrigin), true);
                    entry.originalLord?.RemovePawn(entrant);
                    entrant.DeSpawn();
                    GenSpawn.Spawn(entrant, cell, map);
                    if (entrant.drafter != null) entrant.drafter.Drafted = entry.drafted;
                }
            contest.Initialize(this);
            side.layout = new DomainLayout(side, template, false);
            contest.RefreshLayouts(this);
            side.worker!.Entering();
            side.InstallProtection();
            side.worker.Entered(entrants);
            side.CreateAnchor();
            worker!.ContestStarted();
            side.worker.ContestStarted();
            return side;
        }
        catch
        {
            side.RequestReturn("counter-expansion failed");
            throw;
        }
    }

    internal MapComponent_Domain AdoptDomain(MapComponent_Domain donor)
    {
        if (!CanAcceptChallenger || !donor.CanAcceptChallenger || donor == this || donor.map == map)
            throw new InvalidOperationException("[CQF] Domain maps cannot be merged in their current state.");
        DomainWorker replacement = Activator.CreateInstance(donor.Definition.workerClass) as DomainWorker
            ?? throw new InvalidOperationException("[CQF] Could not reset merged domain worker.");
        DomainContest battle = new(this, new DomainTerrainSnapshot(donor.map));
        MapComponent_Domain side = new(map, true)
        {
            definition = donor.definition,
            worker = donor.worker,
            active = donor.active,
            closing = donor.closing,
            collapsed = donor.collapsed,
            health = donor.health,
            caster = donor.caster,
            sourceMap = donor.sourceMap,
            sourceOrigin = donor.sourceOrigin,
            anchor = donor.anchor,
            participants = donor.participants,
            layout = donor.layout
        };
        challenger = side;
        contest = battle;
        side.worker!.Domain = side;
        side.layout?.MoveTo(side, donor.map.Size);
        side.anchor?.BindDomain(side);
        side.InstallProtection();
        donor.worker = replacement;
        donor.worker.Domain = donor;
        donor.active = false;
        donor.closing = false;
        donor.caster = null;
        donor.sourceMap = null;
        donor.sourceOrigin = IntVec3.Invalid;
        donor.anchor = null;
        donor.participants = new();
        donor.layout = null;
        donor.health = 0f;
        return side;
    }

    internal void FinishContest(Map destination)
    {
        worker?.Recycling(destination);
        challenger?.Worker?.Recycling(destination);
        contest?.Restore(this);
        challenger?.Worker?.Dispose();
        challenger = null;
        contest?.Dispose();
        contest = null;
    }

    private void InstallProtection()
    {
        if (caster == null) throw new InvalidOperationException("[CQF] Domain instance has no caster.");
        Comp_DomainProtection.Ensure(caster);
        if (Definition.protection == null) return;
        Hediff_DomainProtection? protection = caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>()
            .FirstOrDefault(candidate => candidate.def == Definition.protection);
        protection ??= caster.health.AddHediff(Definition.protection) as Hediff_DomainProtection;
        if (protection == null) throw new InvalidOperationException("[CQF] Invalid domain protection hediff.");
        protection.Initialize(this);
    }

    private void CreateAnchor()
    {
        if (Definition.anchor == null) return;
        if (sourceMap == null) throw new InvalidOperationException("[CQF] Domain anchor has no source map.");
        anchor = (Building_Domain)ThingMaker.MakeThing(Definition.anchor);
        anchor.Initialize(map);
        anchor.BindDomain(this);
        anchor.SetFaction(caster!.Faction);
        IntVec3 cell = FindFreeCell(sourceMap, sourceOrigin, false);
        GenSpawn.Spawn(anchor, cell, sourceMap);
    }

    private MapComponent_Domain? challenger;
    private DomainContest? contest;
}
