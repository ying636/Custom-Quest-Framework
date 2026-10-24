using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

internal static class DomainMapOwnershipChecks
{
    public static void Run()
    {
        Game previous = Current.Game;
        FieldInfo mainThread = typeof(UnityData).GetField("mainThreadId", BindingFlags.Static | BindingFlags.NonPublic)!;
        object previousThread = mainThread.GetValue(null)!;
        mainThread.SetValue(null, Environment.CurrentManagedThreadId);
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        World world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        world.pocketMaps = new List<PocketMapParent>();
        world.factionManager = new FactionManager();
        game.World = world;
        List<Map> maps = new();
        typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, maps);
        Current.Game = game;
        try
        {
            Map source = CreateMap(new MapParent());
            MapParent_Domain parent = new()
            {
                sourceMap = source,
                def = new WorldObjectDef { defName = "CQF_Check_DomainParent", canHaveFaction = true }
            };
            Map realm = CreateMap(parent);
            maps.Add(source);
            world.pocketMaps.Add(parent);
            Check(parent.Map == null && source.ChildPocketMaps.Single() == null,
                "detached pocket map with a stale source reproduces the invalid vanilla child map");
            bool reproduced = false;
            try { _ = source.mapPawns.AnyPawnBlockingMapRemoval; }
            catch (NullReferenceException) { reproduced = true; }
            Check(reproduced, "stale domain ownership reproduces the vanilla map-removal exception");
            maps.Add(realm);
            DomainDef definition = new() { defName = "CQF_Check_MapOwnership", maximumHealth = 1000f };
            MapComponent_Domain domain = new(realm);
            realm.components.Add(domain);
            domain.Configure(definition);
            DomainMapTemplate template = new();
            typeof(DomainMapTemplate).GetField("terrain", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(template, new DomainTerrainSnapshot());
            GameComponent_MapManager host = new(game);
            CQFDomainRuntime runtime = new(host);
            ((List<Map>)typeof(CQFDomainRuntime).GetField("domainMaps", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(runtime)!).Add(realm);
            ((Dictionary<DomainDef, DomainMapTemplate>)typeof(CQFDomainRuntime)
                .GetField("mapTemplates", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!)
                .Add(definition, template);
            runtime.Recycle(domain, source);
            Check(host.IsHibernateRequested(realm), "recycled domain is queued for hibernation");
            Check(parent.sourceMap == null, "recycling releases the source map before hibernation");
            maps.Remove(realm);
            Check(!source.ChildPocketMaps.Any() && !source.mapPawns.AnyPawnBlockingMapRemoval,
                "vanilla map-removal checks accept a source after its domain is detached");
            maps.Add(realm);
            Pawn caster = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
            typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(caster, (sbyte)0);
            Check(runtime.Acquire(definition, caster) == realm && parent.sourceMap == source,
                "reacquiring the pooled domain restores its active source association");
            ((List<Pawn>)realm.mapPawns.AllPawnsSpawned).Add(caster);
            bool occupiedRejected = false;
            try { runtime.Recycle(domain, source); }
            catch (InvalidOperationException) { occupiedRejected = true; }
            Check(occupiedRejected && parent.sourceMap == source && !host.IsHibernateRequested(realm),
                "rejected occupied-domain recycling preserves its return association");
        }
        finally
        {
            Current.Game = previous;
            mainThread.SetValue(null, previousThread);
        }
    }

    private static Map CreateMap(MapParent parent)
    {
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.info = new MapInfo { parent = parent, Size = IntVec3.Zero };
        map.components = new List<MapComponent>();
        map.events = new MapEvents(map);
        map.listerThings = new ListerThings(ListerThingsUse.Global);
        map.passingShipManager = new PassingShipManager(map);
        map.mapPawns = new MapPawns(map);
        map.lordManager = new LordManager(map);
        return map;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        Console.WriteLine("PASS " + description);
    }
}
