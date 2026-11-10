using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    public IntVec3 BattlefieldSize(DomainDef first, DomainDef second)
    {
        long width = ((long)first.size.x + second.size.x) * 3L + 3L;
        long height = ((long)first.size.z + second.size.z) * 3L + 3L;
        if (first.size.x <= 0 || first.size.z <= 0 || second.size.x <= 0 || second.size.z <= 0
            || (double)(width / 4) * (height / 4) > int.MaxValue)
            throw new InvalidOperationException("[CQF] Contest battlefield dimensions are invalid or exceed the map cell limit.");
        return new IntVec3(checked((int)(width / 4)), 1, checked((int)(height / 4)));
    }

    public bool HasAvailableBattlefield(DomainDef first, DomainDef second)
    {
        IntVec3 size = BattlefieldSize(first, second);
        return domainMaps.Any(map => !map.Disposed && !map.Parent.Destroyed && map.Size == size
            && map.GetComponent<MapComponent_Domain>().IsBattlefield && map.GetComponent<MapComponent_Domain>().Available
            && map.mapPawns.AllPawns.Count == 0);
    }

    internal void PrepareBattlefield(DomainDef first, DomainDef second, Map source)
    {
        if (HasAvailableBattlefield(first, second)) return;
        MapParent_Domain parent = (MapParent_Domain)WorldObjectMaker.MakeWorldObject(
            DefDatabase<WorldObjectDef>.GetNamed("CQF_DomainBattlefield"));
        parent.SetFaction(Faction.OfPlayer);
        parent.mapGenerator = DefDatabase<MapGeneratorDef>.GetNamed("CQF_DomainBattlefieldGenerator");
        Map map = GameTools.GenerateSubMap(BattlefieldSize(first, second), parent, parent.mapGenerator,
            Array.Empty<GenStepWithParams>(), source);
        MapComponent_Domain domain = map.GetComponent<MapComponent_Domain>();
        domain.Configure(first);
        domain.MarkBattlefield();
        domainMaps.Add(map);
        parent.sourceMap = null;
        host.Hibernate(map);
        Log.Message("[CQF] Prepared contest battlefield " + map.uniqueID + " size=" + map.Size + ".");
    }

    private Map AcquireBattlefield(DomainDef first, DomainDef second, Map source)
    {
        PrepareBattlefield(first, second, source);
        IntVec3 size = BattlefieldSize(first, second);
        Map map = domainMaps.First(candidate => !candidate.Disposed && !candidate.Parent.Destroyed && candidate.Size == size
            && candidate.GetComponent<MapComponent_Domain>().IsBattlefield && candidate.GetComponent<MapComponent_Domain>().Available
            && candidate.mapPawns.AllPawns.Count == 0);
        if (host.IsHibernating(map) || host.IsHibernateRequested(map)) host.Activate(map);
        map.GetComponent<MapComponent_Domain>().Configure(first);
        ((MapParent_Domain)map.Parent).sourceMap = source;
        return map;
    }

    private MapComponent_Domain EnterBattlefield(MapComponent_Domain first, DomainDef definition, Pawn pawn,
        IEnumerable<Pawn> targets, bool fromSource)
    {
        DomainMapTemplate template = GetMapTemplate(definition, pawn.Map);
        return TransferBattlefield(first, null, definition, field => field.EnterChallenger(definition, pawn, targets, template, fromSource));
    }

    private MapComponent_Domain TransferBattlefield(MapComponent_Domain first, MapComponent_Domain? second,
        DomainDef secondDefinition, Func<MapComponent_Domain, MapComponent_Domain> enter)
    {
        Map destination = AcquireBattlefield(first.Definition, secondDefinition, first.SourceMap ?? first.map);
        MapComponent_Domain primary = destination.GetComponent<MapComponent_Domain>();
        IntVec3 firstOffset = default, secondOffset = default;
        if (second != null && first.SourceMap != null && first.SourceMap == second.SourceMap)
        {
            IntVec3 center = new(first.SourceOrigin.x + (second.SourceOrigin.x - first.SourceOrigin.x) / 2, 0,
                first.SourceOrigin.z + (second.SourceOrigin.z - first.SourceOrigin.z) / 2);
            firstOffset = first.SourceOrigin - center;
            secondOffset = second.SourceOrigin - center;
        }
        DomainTerrainSnapshot firstTerrain = new(first.map, firstOffset);
        bool viewing = Find.CurrentMap == first.map || second != null && Find.CurrentMap == second.map;
        List<(Thing thing, Map source, IntVec3 cell, Rot4 rotation, Lord? lord)> moved = new();
        MapComponent_Domain? side = null;
        bool copiedFirst = false;
        bool copiedSecond = false;
        try
        {
            first.Worker!.PreparingContest();
            second?.Worker?.PreparingContest();
            first.Layout?.ParkAll();
            second?.Layout?.ParkAll();
            firstTerrain.Restore(destination);
            copiedFirst = true;
            first.CopyToBattlefield(primary, firstOffset);
            MoveBattlefieldThings(first, destination, firstOffset, moved);
            if (second != null)
            {
                side = new MapComponent_Domain(destination, true);
                copiedSecond = true;
                second.CopyToBattlefield(side, secondOffset);
                MoveBattlefieldThings(second, destination, secondOffset, moved);
                primary.AttachTransferredContest(side, new DomainTerrainSnapshot(second.map, secondOffset));
            }
            else side = enter(primary);
            first.CommitBattlefieldTransfer();
            second?.CommitBattlefieldTransfer();
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Contest battlefield transfer failed; restoring source domains: " + error);
            List<Exception> rollbackErrors = new();
            try { primary.AbortBattlefieldContest(second != null); }
            catch (Exception rollback) { rollbackErrors.Add(rollback); }
            foreach (var item in moved.AsEnumerable().Reverse())
            {
                try
                {
                    if (item.thing.Destroyed) throw new InvalidOperationException("[CQF] Transferred thing was destroyed: " + item.thing.ThingID);
                    if (item.thing.Spawned) item.thing.DeSpawn();
                    GenSpawn.Spawn(item.thing, item.cell, item.source, item.rotation);
                    if (item.thing is Pawn restored && item.lord != null) item.lord.AddPawn(restored);
                }
                catch (Exception rollback) { rollbackErrors.Add(rollback); }
            }
            if (copiedSecond && side != null)
                try { second!.RestoreBattlefieldTransfer(side); } catch (Exception rollback) { rollbackErrors.Add(rollback); }
            if (copiedFirst)
                try { first.RestoreBattlefieldTransfer(primary); } catch (Exception rollback) { rollbackErrors.Add(rollback); }
            first.Layout?.RevealOwned();
            second?.Layout?.RevealOwned();
            if (rollbackErrors.Count > 0) throw new AggregateException("[CQF] Contest battlefield rollback was incomplete.", new[] { error }.Concat(rollbackErrors));
            host.Hibernate(destination);
            throw;
        }
        foreach (Map source in second == null ? new[] { first.map } : new[] { first.map, second.map })
        {
            try
            {
                foreach (Lord lord in source.lordManager.lords.ToList()) source.lordManager.RemoveLord(lord);
                host.Hibernate(source);
            }
            catch (Exception error) { Log.Error("[CQF] Battlefield transfer committed, but its empty source map could not be retired: " + error); }
        }
        if (viewing && transition == null)
        {
            Current.Game.CurrentMap = destination;
            Find.Selector.ClearSelection();
            if (side!.Caster is { Spawned: true, Destroyed: false } caster && caster.Map == destination)
            {
                Find.CameraDriver.JumpToCurrentMapLoc(caster.Position);
                Find.Selector.Select(caster);
            }
        }
        return side!;
    }

    private void MoveBattlefieldThings(MapComponent_Domain domain, Map destination, IntVec3 offset,
        List<(Thing thing, Map source, IntVec3 cell, Rot4 rotation, Lord? lord)> moved)
    {
        foreach (Thing thing in domain.map.listerThings.AllThings.Where(thing => thing is not DomainOpening)
            .OrderBy(thing => thing is Building ? 0 : 1).ToList())
        {
            IntVec3 origin = thing.Position;
            Rot4 rotation = thing.Rotation;
            IntVec3 target = destination.Center + (origin - domain.map.Center) + offset;
            IntVec3 cell = domain.FindFreeCell(destination, target, thing is Pawn);
            if (thing is Building || thing.def.category == ThingCategory.Plant)
            {
                IntVec3? safe = null;
                foreach (IntVec3 candidate in GenRadial.RadialCellsAround(target, 12f, true).Concat(destination.AllCells))
                    if (GenAdj.OccupiedRect(candidate, rotation, thing.def.size).All(part => part.InBounds(destination)
                        && part.Standable(destination) && part.GetThingList(destination).Count == 0)) { safe = candidate; break; }
                cell = safe ?? throw new InvalidOperationException("[CQF] No safe battlefield footprint for " + thing.ThingID);
            }
            Lord? lord = thing is Pawn pawn ? pawn.GetLord() : null;
            moved.Add((thing, domain.map, origin, rotation, lord));
            if (thing is Pawn moving) lord?.RemovePawn(moving);
            thing.DeSpawn();
            GenSpawn.Spawn(thing, cell, destination, rotation);
            if (!thing.Spawned || thing.Map != destination)
                throw new InvalidOperationException("[CQF] Battlefield transfer could not spawn " + thing.ThingID);
        }
    }
}
