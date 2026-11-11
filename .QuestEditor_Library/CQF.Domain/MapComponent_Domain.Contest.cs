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
    public bool HasActiveDomain => ActiveDomainCount > 0;
    public bool CanAcceptChallenger => !independentInstance && HasActiveDomain && InstanceCount < MaximumDomains
        && Instances.Where(side => side.Active).All(side => side.Ready && !side.ArrivalLocked && side.Worker?.SupportsContests == true);

    public MapComponent_Domain? FindDomainFor(Pawn? pawn)
    {
        MapComponent_Domain battlefield = Battlefield;
        if (pawn == null) return battlefield;
        MapComponent_Domain? inactive = null;
        for (int index = 0; index < battlefield.InstanceCount; index++)
        {
            MapComponent_Domain side = battlefield.InstanceAt(index);
            if (side.Caster != pawn) continue;
            if (side.Active) return side;
            inactive ??= side;
        }
        return inactive;
    }

    public MapComponent_Domain? DomainAt(IntVec3 cell)
    {
        if (!cell.InBounds(map)) return null;
        MapComponent_Domain battlefield = Battlefield;
        if (battlefield.contest != null) return battlefield.contest.OwnerAt(battlefield, cell);
        for (int index = 0; index < battlefield.InstanceCount; index++)
        {
            MapComponent_Domain side = battlefield.InstanceAt(index);
            if (side.Active) return side;
        }
        return null;
    }

    public bool OwnsCell(IntVec3 cell) => ReferenceEquals(Battlefield.DomainAt(cell), this);

    public T? GetActiveWorker<T>() where T : DomainWorker
    {
        MapComponent_Domain battlefield = Battlefield;
        for (int index = 0; index < battlefield.InstanceCount; index++)
        {
            MapComponent_Domain side = battlefield.InstanceAt(index);
            if (side.Active && side.GetWorker<T>() is { } worker) return worker;
        }
        return null;
    }

    public bool OwnsParticipant(Pawn pawn) => caster == pawn || participants.Any(entry => entry.pawn == pawn);

    public bool HasOtherActiveDomain(MapComponent_Domain domain)
    {
        MapComponent_Domain battlefield = Battlefield;
        for (int index = 0; index < battlefield.InstanceCount; index++)
        {
            MapComponent_Domain side = battlefield.InstanceAt(index);
            if (side.Active && !ReferenceEquals(side, domain)) return true;
        }
        return false;
    }

    public MapComponent_Domain EnterChallenger(DomainDef definition, Pawn pawn, IEnumerable<Pawn> targets, DomainMapTemplate template, bool fromSource = false)
    {
        Map? arrivalMap = fromSource ? sourceMap : map;
        if (!CanAcceptChallenger || !pawn.Spawned || pawn.Map != arrivalMap || pawn.Dead || pawn.Downed || Instances.Any(side => side.Active && side.Caster == pawn))
            throw new InvalidOperationException("[CQF] Invalid domain counter-expansion state.");
        foreach (MapComponent_Domain existing in Instances.Where(existing => existing.Active)) existing.Worker!.PreparingContest();
        MapComponent_Domain side = new(map, true);
        side.Configure(definition);
        if (side.Worker?.SupportsContests != true)
        {
            side.Worker?.Dispose();
            throw new InvalidOperationException("[CQF] Challenging domain worker does not support contests.");
        }
        List<Pawn> entrants = new() { pawn };
        entrants.AddRange(targets.Where(target => target != pawn && !Instances.Any(existing => existing.Active && existing.Caster == target) && target.Spawned && !target.Dead && target.Map == arrivalMap).Distinct());
        DomainContest battle = contest ?? new(this, template.Terrain);
        bool extending = contest != null;
        side.sourceMap = sourceMap;
        side.caster = pawn;
        side.active = true;
        side.health = side.MaximumHealth;
        foreach (Pawn entrant in entrants)
        {
            MapComponent_Domain? previous = Instances.FirstOrDefault(existing => existing.participants.Any(record => record.pawn == entrant));
            DomainParticipant? entry = previous?.participants.FirstOrDefault(record => record.pawn == entrant);
            if (entry != null) previous!.participants.Remove(entry);
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
        AttachInstance(side);
        contest = battle;
        try
        {
            if (fromSource)
                foreach (DomainParticipant entry in side.participants)
                {
                    Pawn entrant = entry.pawn!;
                    IntVec3 cell = FindFreeCell(map, map.Center + (entry.originalPosition - ProjectionOrigin), true);
                    entry.originalLord?.RemovePawn(entrant);
                    entrant.DeSpawn();
                    GenSpawn.Spawn(entrant, cell, map);
                    if (entrant.drafter != null) entrant.drafter.Drafted = entry.drafted;
                }
            if (extending) contest.AppendSide(this, template);
            else contest.Initialize(this);
            side.layout = new DomainLayout(side, template, false);
            contest.RefreshLayouts(this);
            side.worker!.Entering();
            side.InstallProtection();
            side.worker.Entered(entrants);
            side.CreateAnchor();
            if (!extending) worker!.ContestStarted();
            side.worker.ContestStarted();
            return side;
        }
        catch
        {
            side.RequestReturn("counter-expansion failed");
            throw;
        }
    }

    internal void FinishContest(Map destination)
    {
        worker?.Recycling(destination);
        foreach (MapComponent_Domain side in additionalDomains) side.Worker?.Recycling(destination);
        challenger?.Worker?.Recycling(destination);
        contest?.Restore(this);
        challenger?.Worker?.Dispose();
        challenger = null;
        foreach (MapComponent_Domain side in additionalDomains) side.Worker?.Dispose();
        additionalDomains.Clear();
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
