using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    internal void NotifyDomainEntered(Map map)
    {
        failedContestPairs.RemoveWhere(pair => pair.first == map || pair.second == map);
        contestCheckRequested = true;
        fusionCheckRequested = true;
    }

    public MapComponent_Domain EnterOrContest(DomainDef definition, Pawn caster, IEnumerable<Pawn> targets)
    {
        MapComponent_Domain current = caster.Map.GetComponent<MapComponent_Domain>();
        if (current.HasActiveDomain)
        {
            if (!current.CanAcceptChallenger) throw new InvalidOperationException("[CQF] This domain cannot accept another challenger.");
            DomainMapTemplate template = GetMapTemplate(definition, caster.Map);
            return current.EnterChallenger(definition, caster, targets, template);
        }
        Map realm = Acquire(definition, caster);
        MapComponent_Domain domain = realm.GetComponent<MapComponent_Domain>();
        domain.Enter(caster, targets);
        return domain;
    }

    public MapComponent_Domain MergeDomains(MapComponent_Domain first, MapComponent_Domain second)
    {
        if (!first.CanAcceptChallenger || !second.CanAcceptChallenger || first.map == second.map
            || !domainMaps.Contains(first.map) || !domainMaps.Contains(second.map))
            throw new InvalidOperationException("[CQF] Only two registered, ready, contest-capable domains can merge.");
        FinishTransitionFor(first.map);
        FinishTransitionFor(second.map);
        first.Worker!.PreparingContest();
        second.Worker!.PreparingContest();
        bool viewingSecond = Find.CurrentMap == second.map;
        List<(Thing thing, IntVec3 origin, Rot4 rotation)> moved = new();
        try
        {
            first.Layout?.ParkAll();
            second.Layout?.ParkAll();
            foreach (Thing thing in second.map.listerThings.AllThings.ToList())
            {
                IntVec3 origin = thing.Position;
                Rot4 rotation = thing.Rotation;
                IntVec3 target = new(Math.Min(first.map.Size.x - 1, origin.x * first.map.Size.x / second.map.Size.x), 0,
                    Math.Min(first.map.Size.z - 1, origin.z * first.map.Size.z / second.map.Size.z));
                IntVec3 cell = first.FindFreeCell(first.map, target, thing is Pawn);
                if (thing is Building || thing.def.category == ThingCategory.Plant)
                {
                    IntVec3? safeCell = null;
                    foreach (IntVec3 candidate in GenRadial.RadialCellsAround(target, 12f, true).Concat(first.map.AllCells))
                    {
                        CellRect occupied = GenAdj.OccupiedRect(candidate, rotation, thing.def.size);
                        if (occupied.Any(part => !part.InBounds(first.map) || !part.Standable(first.map)
                            || part.GetThingList(first.map).Count > 0)) continue;
                        safeCell = candidate;
                        break;
                    }
                    cell = safeCell ?? throw new InvalidOperationException("[CQF] No non-destructive landing footprint exists for " + thing.ThingID);
                }
                moved.Add((thing, origin, rotation));
                if (thing is Pawn pawn) pawn.GetLord()?.RemovePawn(pawn);
                thing.DeSpawn();
                GenSpawn.Spawn(thing, cell, first.map, rotation);
                if (!thing.Spawned || thing.Map != first.map)
                    throw new InvalidOperationException("[CQF] Domain merge could not spawn transferred thing: " + thing.ThingID);
            }
            MapComponent_Domain side = first.AdoptDomain(second);
            first.Contest!.Initialize(first);
            first.Worker!.ContestStarted();
            side.Worker!.ContestStarted();
            if (viewingSecond)
            {
                Current.Game.CurrentMap = first.map;
                Find.Selector.ClearSelection();
                if (side.Caster?.Spawned == true)
                {
                    Find.CameraDriver.JumpToCurrentMapLoc(side.Caster.Position);
                    Find.Selector.Select(side.Caster);
                }
            }
            foreach (Thing thing in second.map.listerThings.AllThings.ToList()) thing.Destroy();
            foreach (Verse.AI.Group.Lord lord in second.map.lordManager.lords.ToList()) second.map.lordManager.RemoveLord(lord);
            host.Hibernate(second.map);
            return side;
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Domain merge failed: " + error);
            if (second.Active)
            {
                foreach ((Thing thing, IntVec3 origin, Rot4 rotation) in moved)
                {
                    if (thing.Destroyed) continue;
                    if (thing.Spawned) thing.DeSpawn();
                    GenSpawn.Spawn(thing, origin, second.map, rotation);
                    if (!thing.Spawned || thing.Map != second.map)
                        throw new InvalidOperationException("[CQF] Domain merge could not restore transferred thing: " + thing.ThingID);
                }
                first.Layout?.RevealOwned();
                second.Layout?.RevealOwned();
            }
            else if (second.map.mapPawns.AllPawns.Count == 0) host.Hibernate(second.map);
            throw;
        }
    }

    private void ProcessDomainContests()
    {
        if (!contestCheckRequested || transition != null) return;
        contestCheckRequested = false;
        if (domainMaps.Count < 2) return;
        for (int firstIndex = 0; firstIndex < domainMaps.Count; firstIndex++)
        {
            MapComponent_Domain first = domainMaps[firstIndex].GetComponent<MapComponent_Domain>();
            if (!first.CanAcceptChallenger || first.Anchor?.Spawned != true) continue;
            for (int secondIndex = firstIndex + 1; secondIndex < domainMaps.Count; secondIndex++)
            {
                MapComponent_Domain second = domainMaps[secondIndex].GetComponent<MapComponent_Domain>();
                if (failedContestPairs.Contains((first.map, second.map))) continue;
                if (!second.CanAcceptChallenger || second.Anchor?.Spawned != true || first.SourceMap != second.SourceMap) continue;
                float radius = first.Definition.OpeningRadius + second.Definition.OpeningRadius;
                if (first.Anchor.Position.DistanceToSquared(second.Anchor.Position) > radius * radius) continue;
                try { MergeDomains(first, second); }
                catch (Exception error)
                {
                    Log.Error("[CQF] Could not merge overlapping domains; automatic retry is disabled for this pair: " + error);
                    failedContestPairs.Add((first.map, second.map));
                }
                contestCheckRequested = true;
                return;
            }
        }
    }

    private readonly HashSet<(Map first, Map second)> failedContestPairs = new();
    private bool contestCheckRequested;
}
