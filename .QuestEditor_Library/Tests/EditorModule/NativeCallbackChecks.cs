using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using RimWorld;
using Verse;
using Verse.AI.Group;

internal static class NativeCallbackChecks
{
    public static void Run()
    {
        Game previousGame = Current.Game;
        Game game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        typeof(Game).GetField("maps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, new List<Map> { map });
        game.components = new();
        Current.Game = game;
        try
        {
            map.events = new MapEvents(map);
            map.mapPawns = new MapPawns(map);
            map.lordManager = new LordManager(map);
            MapComponent_CustomMapData component = new(map);
            component.FinalizeInit();
            component.FinalizeInit();
            Delegate handler = (Delegate)typeof(MapEvents).GetField(nameof(MapEvents.ThingSpawned), BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(map.events)!;
            Check(handler.GetInvocationList().Length == 1, "repeated map finalization keeps a single native subscription");
            map.components = new List<MapComponent> { component };
            CQFEventArea area = new() { key = "CQF_Check_Events" };
            component.AddOrReplaceEventArea(area);
            Pawn pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
            pawn.def = new ThingDef { defName = "CQF_Check_Pawn", race = new RaceProperties() };
            FieldInfo mapIndex = typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!;
            mapIndex.SetValue(pawn, (sbyte)0);
            map.events.Notify_ThingSpawned(pawn);
            HashSet<Pawn> cached = (HashSet<Pawn>)typeof(CQFEventArea).GetField("pawns", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(area)!;
            Check(cached.Contains(pawn), "native spawn event caches a pawn before MapPawns registration");
            mapIndex.SetValue(pawn, (sbyte)-1);
            map.events.Notify_ThingDespawned(pawn);
            Check(!cached.Contains(pawn), "native despawn event removes an already-unspawned pawn");
            map.events.Notify_ThingSpawned(new Thing());
            Check(cached.Count == 0, "non-pawn map events leave the pawn cache unchanged");
            MapEvents oldEvents = map.events;
            map.events = new MapEvents(map);
            component.FinalizeInit();
            mapIndex.SetValue(pawn, (sbyte)0);
            oldEvents.Notify_ThingSpawned(pawn);
            Check(!cached.Contains(pawn), "map event replacement unsubscribes the old event source");
            map.events.Notify_ThingSpawned(pawn);
            Check(cached.Contains(pawn), "map event replacement subscribes the current source");
            map.events.Notify_ThingDespawned(pawn);
            component.MapRemoved();
            map.events.Notify_ThingSpawned(pawn);
            Check(!cached.Contains(pawn), "map removal releases event subscriptions");
            CheckClaims(map);
            Check(typeof(CustomMapDataDef).Assembly.GetType("QuestEditor_Library.Patch_PawnSpawnedForEventArea") == null
                && typeof(CustomMapDataDef).Assembly.GetType("QuestEditor_Library.Patch_DisableClaim") == null,
                "1.7 event and claim callbacks require no Harmony classes");
        }
        finally { Current.Game = previousGame; }
    }

    private static void CheckClaims(Map map)
    {
        Building building = new() { def = new ThingDef { defName = "CQF_Check_Building" } };
        typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(building, (sbyte)0);
        Lord lord = (Lord)RuntimeHelpers.GetUninitializedObject(typeof(Lord));
        lord.ownedBuildings = new List<Building> { building };
        lord.lordManager = map.lordManager;
        LordJob_Custom job = new() { lord = lord };
        typeof(Lord).GetField("curJob", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(lord, job);
        map.lordManager.lords.Add(lord);
        job.Notify_BuildingAdded(building);
        job.Notify_BuildingAdded(building);
        Comp_CustomLordBuilding restriction = building.AllComps.OfType<Comp_CustomLordBuilding>().Single();
        Check(restriction.CompPreventClaimingBy(null!), "custom lord building denies claiming through native comp callback");
        lord.ownedBuildings.Clear();
        Check(!restriction.CompPreventClaimingBy(null!), "leaving custom lord releases the claim restriction");
        lord.ownedBuildings.Add(building);
        typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(building, null);
        job.CreateGraph();
        Check(building.AllComps.OfType<Comp_CustomLordBuilding>().Single().CompPreventClaimingBy(null!),
            "lord graph reconstruction restores native claim component after loading");
        Building ordinary = new();
        Check(!ordinary.AllComps.OfType<Comp_CustomLordBuilding>().Any(), "unmanaged buildings receive no claim component");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
