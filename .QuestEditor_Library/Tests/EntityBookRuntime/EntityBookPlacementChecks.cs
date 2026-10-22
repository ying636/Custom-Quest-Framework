using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.EntityBookRuntimeChecks;

public static class EntityBookPlacementChecks
{
    public static Spawner PreviewMarker => markers[0];

    public static void RunBefore(Action<bool, string> check)
    {
        map = Find.CurrentMap;
        check(map != null, "entity placement checks use a live map");
        Pawn actor = map!.mapPawns.FreeColonistsSpawned.First();
        initialMapPawns = map.mapPawns.AllPawnsSpawned.Count;
        initialRecords = Current.Game.GetComponent<GameComponent_EntityBook>().Records.Count;
        initialUniquePawns = GameComponent_Editor.Instance.pawns.Count;
        EntityBookPlacementAction.Calls = 0;
        startedFrame = Time.frameCount;
        cells = map.AllCells.Where(cell => CQFEntityMapPlacement.CanPlace(cell, map).Accepted
            && cell.GetThingList(map).Count == 0).Take(4).ToArray();
        check(cells.Length == 4, "the placement fixture finds four safe empty cells without replacing map objects");
        check(!CQFEntityMapPlacement.CanPlace(new IntVec3(-1, 0, -1), map).Accepted, "entity placement rejects cells outside the map");
        PawnSpawnData buildingRecipe = new() { spawnType = SpawnType.BuildingDamaged };
        check(!CQFEntityMapPlacement.CanPlace(cells[0], map, buildingRecipe).Accepted,
            "a building damage trigger refuses an empty cell instead of exporting an unbound event");
        walls.Add(GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel), cells[2], map));
        walls.Add(GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel), cells[3], map));
        check(!GenSpawn.SpawningWipes(QEDefOf.QE_Spawner_Editor, ThingDefOf.Wall),
            "the native attachment spawn-point definition does not wipe its associated building");
        check(!CQFEntityMapPlacement.CanPlace(cells[2], map).Accepted, "ordinary entity placement rejects a wall instead of erasing it");
        SpawnType[] timings = { SpawnType.MapGeneration, SpawnType.BuildingTick, SpawnType.BuildingDamaged, SpawnType.BuildingDestroyed };
        for (int index = 0; index < timings.Length; index++)
        {
            PawnSpawnData recipe = new()
            {
                dataName = "CQF Entity Placement " + index,
                kind = actor.kindDef,
                faction = Faction.OfPlayer.def.defName,
                spawnType = timings[index],
                timeToSpawn = 1,
                count = new IntRange(2, 3),
                generationChance = 0.65f,
                way = new ArrivingWay_DropPod(),
                actions = new List<CQFAction> { new EntityBookPlacementAction() }
            };
            Spawner marker = CQFEntityMapPlacement.Place(recipe, cells[index], map);
            markers.Add(marker);
            PawnSpawnData placed = marker.pawns.Single();
            check(marker.Spawned && placed.EntityBookSourceId == recipe.EntityBookSourceId,
                "map placement persists the original entity source identity for " + timings[index]);
            check(!ReferenceEquals(placed, recipe) && !ReferenceEquals(placed.actions, recipe.actions)
                && placed.count == recipe.count && placed.generationChance == recipe.generationChance
                && placed.spawnType == recipe.spawnType && (recipe.spawnType != SpawnType.BuildingTick || placed.timeToSpawn == recipe.timeToSpawn),
                "map placement creates an independent complete spawn configuration for " + timings[index]);
            check(placed.way is ArrivingWay_DropPod && !ReferenceEquals(placed.way, recipe.way),
                "map placement preserves the derived arrival method through value copying for " + timings[index]);
            check(!CQFEntityMapPlacement.CanPlace(cells[index], map).Accepted,
                "entity placement refuses an existing spawn point for " + timings[index]);
            if (index >= 2)
                check(walls[index - 2].Spawned && !walls[index - 2].Destroyed
                    && ReferenceEquals(cells[index].GetFirstBuilding(map), walls[index - 2]),
                    "placing an event spawn point preserves the associated building for " + timings[index]);
        }
        CustomMapDataDef exported = new();
        MethodInfo saveThings = typeof(CustomMapDataDef).GetMethod("SaveThings", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(typeof(CustomMapDataDef).FullName, "SaveThings");
        saveThings.Invoke(exported, new object[] { IntVec3.Zero, markers.Cast<Thing>().Concat(walls).ToList(), cells.ToList() });
        for (int index = 2; index < timings.Length; index++)
            check(exported.specialSpawnPawns[cells[index]].Single().EntityBookSourceId == markers[index].pawns.Single().EntityBookSourceId
                && exported.thingDatas.Any(thing => thing.def == ThingDefOf.Wall
                    && (thing.allRect.Count > 0 ? thing.allRect.Any(rect => rect.Contains(cells[index])) : thing.position == cells[index])),
                "map object collection preserves both event recipe identity and its building at one cell for " + timings[index]);
        VerifyNoGeneration(check, "immediately after placing spawn points");
    }

    public static void VerifyAndCleanup(Action<bool, string> check)
    {
        check(Time.frameCount - startedFrame >= 3, "placed spawn configurations remain on the native map for at least three frames");
        VerifyNoGeneration(check, "after native frames have elapsed");
        MethodInfo tick = typeof(ThingWithComps).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(typeof(ThingWithComps).FullName, "Tick");
        foreach (Spawner marker in markers)
            for (int index = 0; index < 65; index++) tick.Invoke(marker, null);
        VerifyNoGeneration(check, "after manually invoking each marker's tick 65 times without advancing the map");
        foreach (Spawner marker in markers) marker.Destroy();
        markers.Clear();
        foreach (Thing wall in walls) wall.Destroy();
        walls.Clear();
    }

    private static void VerifyNoGeneration(Action<bool, string> check, string stage)
    {
        check(map!.mapPawns.AllPawnsSpawned.Count == initialMapPawns
            && Current.Game.GetComponent<GameComponent_EntityBook>().Records.Count == initialRecords
            && GameComponent_Editor.Instance.pawns.Count == initialUniquePawns
            && EntityBookPlacementAction.Calls == 0,
            "map spawn-point authoring does not generate pawns, register actors, cache unique pawns or execute spawn events " + stage);
    }

    private static Map? map;
    private static readonly List<Thing> walls = new();
    private static IntVec3[] cells = Array.Empty<IntVec3>();
    private static readonly List<Spawner> markers = new();
    private static int initialMapPawns;
    private static int initialRecords;
    private static int initialUniquePawns;
    private static int startedFrame;
}
