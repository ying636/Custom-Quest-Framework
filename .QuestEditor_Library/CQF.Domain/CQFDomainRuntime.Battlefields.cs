using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    public IntVec3 BattlefieldSize(DomainDef first, DomainDef second)
        => BattlefieldSize(new[] { first, second });

    public IntVec3 BattlefieldSize(IReadOnlyList<DomainDef> definitions, IntVec3 minimum = default)
    {
        if (definitions.Count < 2 || definitions.Count > MapComponent_Domain.MaximumDomains
            || definitions.Any(definition => definition == null || definition.size.x <= 0 || definition.size.z <= 0))
            throw new ArgumentException("[CQF] A battlefield requires between two and eight valid domain definitions.");
        double scale = 0.75d * Math.Sqrt(2d / definitions.Count);
        double width = Math.Max(Math.Ceiling(definitions.Sum(definition => (long)definition.size.x) * scale), minimum.x > 0 ? minimum.x + 1d : 0d);
        double height = Math.Max(Math.Ceiling(definitions.Sum(definition => (long)definition.size.z) * scale), minimum.z > 0 ? minimum.z + 1d : 0d);
        if (width > int.MaxValue || height > int.MaxValue || width * height > int.MaxValue)
            throw new InvalidOperationException("[CQF] Contest battlefield dimensions are invalid or exceed the map cell limit.");
        return new IntVec3(checked((int)width), 1, checked((int)height));
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
        List<Pawn> captured = targets.ToList();
        List<Pawn> entrants = new[] { pawn }.Concat(captured).Where(entrant => entrant.Spawned && !entrant.Dead && entrant.Map == pawn.Map).Distinct().ToList();
        return TransferDomainGroups(first.Battlefield, null, definition,
            field => field.EnterChallenger(definition, pawn, captured, template, fromSource), pawn, entrants);
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
            RelocateBattlefieldThing(thing, destination, cell, rotation);
            if (!thing.Spawned || thing.Map != destination)
                throw new InvalidOperationException("[CQF] Battlefield transfer could not spawn " + thing.ThingID);
        }
    }
}
