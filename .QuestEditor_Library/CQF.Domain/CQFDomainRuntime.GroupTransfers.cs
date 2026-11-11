using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    internal bool HasAvailableBattlefield(MapComponent_Domain first, DomainDef incoming, Pawn pawn)
        => HasSizedBattlefield(PlanBattlefield(first.Battlefield, null, incoming, pawn).size);

    internal void PrepareBattlefield(MapComponent_Domain first, DomainDef incoming, Pawn pawn)
        => PrepareSizedBattlefield(PlanBattlefield(first.Battlefield, null, incoming, pawn).size, first.Definition, first.SourceMap ?? first.map);

    private bool HasSizedBattlefield(IntVec3 size) => domainMaps.Any(map => !map.Disposed && !map.Parent.Destroyed && map.Size == size
        && map.GetComponent<MapComponent_Domain>().IsBattlefield && map.GetComponent<MapComponent_Domain>().Available
        && map.mapPawns.AllPawns.Count == 0);

    private Map PrepareSizedBattlefield(IntVec3 size, DomainDef definition, Map source)
    {
        Map? existing = domainMaps.FirstOrDefault(map => !map.Disposed && !map.Parent.Destroyed && map.Size == size
            && map.GetComponent<MapComponent_Domain>().IsBattlefield && map.GetComponent<MapComponent_Domain>().Available
            && map.mapPawns.AllPawns.Count == 0);
        if (existing != null) return existing;
        MapParent_Domain parent = (MapParent_Domain)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("CQF_DomainBattlefield"));
        parent.SetFaction(Faction.OfPlayer);
        parent.mapGenerator = DefDatabase<MapGeneratorDef>.GetNamed("CQF_DomainBattlefieldGenerator");
        Map map = GameTools.GenerateSubMap(size, parent, parent.mapGenerator, Array.Empty<GenStepWithParams>(), source);
        MapComponent_Domain field = map.GetComponent<MapComponent_Domain>();
        field.Configure(definition); field.MarkBattlefield();
        domainMaps.Add(map); parent.sourceMap = null; host.Hibernate(map);
        Log.Message("[CQF] Prepared contest battlefield " + map.uniqueID + " size=" + size + ".");
        return map;
    }

    private (IntVec3 size, IntVec3 origin, List<IntVec3> offsets) PlanBattlefield(MapComponent_Domain first,
        MapComponent_Domain? second, DomainDef? incoming, Pawn? pawn)
    {
        List<MapComponent_Domain> groups = new() { first.Battlefield };
        if (second != null) groups.Add(second.Battlefield);
        List<MapComponent_Domain> sides = groups.SelectMany(group => group.Instances).ToList();
        if (sides.Count + (incoming == null ? 0 : 1) > MapComponent_Domain.MaximumDomains)
            throw new InvalidOperationException("[CQF] A domain battle cannot contain more than eight instances.");
        List<DomainDef> definitions = sides.Where(side => side.Active).Select(side => side.Definition).ToList();
        if (incoming != null) definitions.Add(incoming);
        bool sharedSource = groups.All(group => group.SourceMap == first.SourceMap);
        List<IntVec3> origins = sides.Where(side => side.Active).Select(side => side.SourceOrigin).ToList();
        IntVec3? incomingOrigin = pawn == null ? null : pawn.Map == first.map
            ? sides.SelectMany(side => side.Participants).FirstOrDefault(record => record.pawn == pawn)?.originalPosition
                ?? first.ProjectionOrigin + pawn.Position - first.map.Center : pawn.Position;
        if (incomingOrigin.HasValue) origins.Add(incomingOrigin.Value);
        IntVec3 origin = sharedSource ? new IntVec3((int)Math.Round(origins.Average(cell => cell.x)), 0,
            (int)Math.Round(origins.Average(cell => cell.z))) : first.ProjectionOrigin;
        List<IntVec3> offsets = groups.Select(group => sharedSource ? group.ProjectionOrigin - origin : IntVec3.Zero).ToList();
        int minimumX = groups.Max(group => group.map.Size.x), minimumZ = groups.Max(group => group.map.Size.z);
        for (int index = 0; index < groups.Count; index++)
        {
            IntVec3 size = groups[index].map.Size, offset = offsets[index];
            minimumX = Math.Max(minimumX, 2 * Math.Max(Math.Abs(offset.x - size.x / 2), Math.Abs(offset.x + size.x - size.x / 2)) - 1);
            minimumZ = Math.Max(minimumZ, 2 * Math.Max(Math.Abs(offset.z - size.z / 2), Math.Abs(offset.z + size.z - size.z / 2)) - 1);
        }
        if (incoming != null && incomingOrigin.HasValue)
        {
            IntVec3 offset = incomingOrigin.Value - origin;
            minimumX = Math.Max(minimumX, 2 * (Math.Abs(offset.x) + (incoming.size.x + 1) / 2) - 1);
            minimumZ = Math.Max(minimumZ, 2 * (Math.Abs(offset.z) + (incoming.size.z + 1) / 2) - 1);
        }
        return (BattlefieldSize(definitions, new IntVec3(minimumX, 1, minimumZ)), origin, offsets);
    }

    private MapComponent_Domain TransferDomainGroups(MapComponent_Domain first, MapComponent_Domain? second,
        DomainDef? incoming, Func<MapComponent_Domain, MapComponent_Domain>? enter, Pawn? pawn = null,
        IReadOnlyList<Pawn>? entrants = null)
    {
        first = first.Battlefield; second = second?.Battlefield;
        var plan = PlanBattlefield(first, second, incoming, pawn);
        List<MapComponent_Domain> groups = new() { first };
        if (second != null) groups.Add(second);
        List<List<MapComponent_Domain>> originals = groups.Select(group => group.Instances.ToList()).ToList();
        List<DomainContest?> oldContests = groups.Select(group => group.Contest).ToList();
        Map destination = PrepareSizedBattlefield(plan.size, first.Definition, first.SourceMap ?? first.map);
        if (host.IsHibernating(destination) || host.IsHibernateRequested(destination)) host.Activate(destination);
        MapComponent_Domain primary = destination.GetComponent<MapComponent_Domain>();
        primary.Configure(first.Definition); primary.SetProjectionOrigin(plan.origin);
        ((MapParent_Domain)destination.Parent).sourceMap = first.SourceMap;
        List<(Thing thing, Map source, IntVec3 cell, Rot4 rotation, Lord? lord)> moved = new();
        List<(MapComponent_Domain source, MapComponent_Domain target)> copied = new();
        List<(DomainOpening opening, Map source, IntVec3 cell)> pendingOpenings = new();
        List<DomainTerrainSnapshot> terrains = new();
        List<IntVec3> centers = new();
        MapComponent_Domain? result = null;
        bool viewing = groups.Any(group => Find.CurrentMap == group.map);
        try
        {
            foreach (MapComponent_Domain side in originals.SelectMany(group => group))
            { if (side.Active) side.Worker!.PreparingContest(); side.Layout?.ParkAll(); }
            foreach (MapComponent_Domain group in groups)
                foreach (DomainOpening opening in group.map.listerThings.AllThings.OfType<DomainOpening>().ToList())
                    opening.ReleaseForFusion();
            int total = 0;
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                MapComponent_Domain group = groups[groupIndex];
                IntVec3 offset = plan.offsets[groupIndex];
                DomainContest? old = oldContests[groupIndex];
                for (int sideIndex = 0; sideIndex < originals[groupIndex].Count; sideIndex++)
                {
                    MapComponent_Domain source = originals[groupIndex][sideIndex];
                    terrains.Add(old == null ? new DomainTerrainSnapshot(source.map, offset)
                        : new DomainTerrainSnapshot(source.map, cell => old.TerrainAt(source, cell), offset));
                    IntVec3 center = old == null ? source.Caster!.Position : old.Partition.OpeningCenters[sideIndex];
                    centers.Add(destination.Center + center - source.map.Center + offset);
                    MapComponent_Domain target = total++ == 0 ? primary : new MapComponent_Domain(destination, true);
                    copied.Add((source, target));
                    source.CopyToBattlefield(target, offset);
                    if (target != primary) primary.AttachInstance(target);
                    if (second != null && source == second) result = target;
                }
                MoveBattlefieldThings(group, destination, offset, moved);
                foreach (DomainOpening opening in group.map.listerThings.AllThings.OfType<DomainOpening>().ToList())
                {
                    IntVec3 cell = opening.Position;
                    pendingOpenings.Add((opening, group.map, cell));
                    opening.MoveWithBattlefield(destination, destination.Center + cell - group.map.Center + offset);
                }
            }
            terrains[0].Restore(destination);
            primary.InitializeTransferredScene(terrains, centers);
            for (int index = 0; index < groups.Count; index++)
            {
                DomainContest? old = oldContests[index];
                if (old == null) continue;
                int start = originals.Take(index).Sum(group => group.Count);
                primary.Contest!.Inherit(primary, old, Enumerable.Range(start, originals[index].Count).ToList(),
                    destination.Center - groups[index].map.Center + plan.offsets[index]);
            }
            if (enter != null)
            {
                foreach (Pawn entrant in entrants ?? Array.Empty<Pawn>())
                    if (entrant.Spawned && entrant.Map != destination && !moved.Any(item => item.thing == entrant))
                        moved.Add((entrant, entrant.Map, entrant.Position, entrant.Rotation, entrant.GetLord()));
                result = enter(primary);
            }
            foreach (MapComponent_Domain side in primary.Instances.Where(side => side.Active)) side.Worker!.ContestStarted();
            foreach (var pair in copied) pair.source.CommitBattlefieldTransfer();
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Domain group transfer failed; restoring source scenes: " + error);
            List<Exception> failures = new();
            foreach (MapComponent_Domain side in primary.Instances.Where(side => copied.All(pair => pair.target != side)).ToList())
                try { side.AbortNewBattlefieldSide(); } catch (Exception rollback) { failures.Add(rollback); }
            foreach (var pair in copied)
                try { pair.target.Layout?.ParkAll(); } catch (Exception rollback) { failures.Add(rollback); }
            foreach (var item in moved.AsEnumerable().Reverse())
                try
                {
                    if (item.thing.Destroyed) throw new InvalidOperationException("[CQF] Transferred thing was destroyed: " + item.thing.ThingID);
                    if (item.thing.Spawned) RelocateBattlefieldThing(item.thing, item.source, item.cell, item.rotation);
                    else GenSpawn.Spawn(item.thing, item.cell, item.source, item.rotation, respawningAfterLoad: true);
                    if (item.thing is Pawn restored && item.lord != null) item.lord.AddPawn(restored);
                }
                catch (Exception rollback) { failures.Add(rollback); }
            foreach (var pair in copied.AsEnumerable().Reverse())
                try { pair.source.RestoreBattlefieldTransfer(pair.target); } catch (Exception rollback) { failures.Add(rollback); }
            foreach (var item in pendingOpenings.AsEnumerable().Reverse())
                try { item.opening.MoveWithBattlefield(item.source, item.cell); } catch (Exception rollback) { failures.Add(rollback); }
            try { primary.DetachTransferredScene(); } catch (Exception rollback) { failures.Add(rollback); }
            foreach (MapComponent_Domain group in groups)
                try
                {
                    if (group.Contest != null) group.Contest.RefreshLayouts(group);
                    else group.Layout?.RevealOwned();
                }
                catch (Exception rollback) { failures.Add(rollback); }
            if (failures.Count > 0) throw new AggregateException("[CQF] Domain group rollback was incomplete.", new[] { error }.Concat(failures));
            ((MapParent_Domain)destination.Parent).sourceMap = null;
            host.Hibernate(destination);
            throw;
        }
        foreach (MapComponent_Domain group in groups)
        {
            try
            {
                group.DetachTransferredScene();
                foreach (Lord lord in group.map.lordManager.lords.ToList()) group.map.lordManager.RemoveLord(lord);
                ((MapParent_Domain)group.map.Parent).sourceMap = null;
                host.Hibernate(group.map);
            }
            catch (Exception error) { Log.Error("[CQF] Domain group transfer committed, but an empty source scene could not be retired: " + error); }
        }
        result ??= primary.Instances.Last(side => side.Active);
        NotifyDomainEntered(destination);
        if (viewing && transition == null)
        {
            Current.Game.CurrentMap = destination;
            Find.Selector.ClearSelection();
            if (result.Caster is { Spawned: true, Destroyed: false } caster && caster.Map == destination)
            { Find.CameraDriver.JumpToCurrentMapLoc(caster.Position); Find.Selector.Select(caster); }
        }
        return result;
    }
}
