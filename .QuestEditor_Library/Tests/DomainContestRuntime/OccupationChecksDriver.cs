using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class OccupationChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private MapComponent_Domain Second => field!.Challenger!;
    private DomainTerritoryPartition Partition => field!.Contest!.Partition;

    public void Resume(int first, int second, int phase)
    {
        Find.TickManager.Pause();
        field = CQFDomainRuntime.Instance.DomainMaps.Select(map => map.GetComponent<MapComponent_Domain>())
            .Single(domain => domain.Caster?.thingIDNumber == first && domain.Challenger?.Caster?.thingIDNumber == second);
        source = field.SourceMap;
        int probeId = Current.Game.GetComponent<GameComponent_ContestChecks>().StructureId;
        firstProbe = field.Layout!.Structures.Single(item => item.Thing?.thingIDNumber == probeId);
        secondProbe = Second.Layout!.Structures.Single(item => item.Position == firstProbe.Position);
        if (phase == 0)
        {
            Check(field.Contest!.OccupationTicks(field) == 179 && Partition.FirstClaimedCells == 0,
                "complete game reload preserves 179 ticks of unfinished enemy occupation");
            stage = 2;
        }
        else
        {
            Check(Partition.FirstClaimedCells == GenRadial.NumCellsInRadius(2f)
                && Footprint().All(field.OwnsCell) && firstProbe.Thing!.Spawned && !secondProbe.Thing!.Spawned,
                "complete game reload preserves captured territory, actual terrain layouts and original buildings");
            stage = phase == 1 ? 5 : 50;
        }
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 250f) throw new TimeoutException("Occupation stage " + stage);
            if (stage == 0)
            {
                if (Find.CurrentMap == null) return;
                DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                DomainDef blood = DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain");
                CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
                if (!runtime.HasAvailable(basic) || !runtime.HasAvailable(blood)
                    || !runtime.HasTerrainTemplate(basic) || !runtime.HasTerrainTemplate(blood)) return;
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                IntVec3 near = source.mapPawns.FreeColonistsSpawned.First().Position;
                Pawn a = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                Pawn b = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                GenSpawn.Spawn(a, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, near, true), source);
                GenSpawn.Spawn(b, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, near + new IntVec3(8, 0, 0), true), source);
                a.abilities ??= new Pawn_AbilityTracker(a); b.abilities ??= new Pawn_AbilityTracker(b);
                field = runtime.EnterOrContest(basic, a, new[] { b });
                runtime.EnterOrContest(blood, b, Array.Empty<Pawn>());
                Current.Game.CurrentMap = field.map;
                stage = 1;
            }
            else if (stage == 1)
            {
                if (field!.ArrivalLocked || Second.ArrivalLocked) return;
                Move(field.Caster!, new IntVec3(10, 0, 10)); Move(Second.Caster!, new IntVec3(50, 0, 45));
                field.DrainHealth(10000f);
                for (int tick = 0; tick < 180; tick++) field.Contest!.Tick(field);
                Move(field.Caster!, center);
                RunTicks(90);
                Check(field.Contest!.OccupationTicks(field) == 90 && Partition.FirstClaimedCells == 0,
                    "90 actual game ticks in opposing territory do not trigger conversion");
                Move(field.Caster!, new IntVec3(10, 0, 10)); RunTicks(1);
                Check(field.Contest.OccupationTicks(field) == 0, "one native tick at home resets incomplete occupation");
                Move(field.Caster!, center + new IntVec3(1, 0, 0)); RunTicks(90);
                Move(field.Caster!, center); RunTicks(89);
                Check(field.Contest.OccupationTicks(field) == 179 && Partition.FirstClaimedCells == 0,
                    "moving within opposing territory preserves the continuous timer without triggering before three seconds");
                IntVec3 probeCell = center + new IntVec3(1, 0, 0);
                Building defender = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                GenSpawn.Spawn(defender, probeCell, field.map); defender.HitPoints = 137;
                secondProbe = Second.RegisterStructure(defender);
                Building invader = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                invader.HitPoints = 211;
                firstProbe = new DomainStructure(invader, probeCell, Rot4.North);
                ((List<DomainStructure>)field.Layout!.Structures).Add(firstProbe);
                Save(0, 2);
            }
            else if (stage == 2)
            {
                float health = field!.Health;
                RunTicks(1);
                Check(Partition.FirstClaimedCells == GenRadial.NumCellsInRadius(2f) && Footprint().All(field.OwnsCell),
                    "the 180th native tick converts the complete radius-two footprint at the caster's current position");
                Check(Math.Abs(field.Health - health - 300f + Loss(field, 1)) < 0.1f,
                    "successful occupation restores 300 domain health while retaining normal contest consumption; before="
                        + health + ", after=" + field.Health + ", loss=" + Loss(field, 1));
                Check(Footprint().All(cell => cell.GetTerrain(field.map) == field.Contest!.TerrainAt(field, cell)),
                    "captured cells switch real terrain in the same tick as gameplay ownership");
                Check(firstProbe!.Thing!.Spawned && firstProbe.Thing.HitPoints == 211
                    && !secondProbe!.Thing!.Spawned && secondProbe.Thing.HitPoints == 137,
                    "capture parks and restores original buildings without destroying or healing them");
                float strength = (float)Second.Worker!.GetType().GetMethod("SkillStrengthAt")!
                    .Invoke(Second.Worker, new object[] { center })!;
                Check(Math.Abs(strength - 0.7f) < 0.001f, "blood sea skills recognize the newly captured opposing territory");
                health = field.Health;
                RunTicks(180);
                Check(field.Contest!.OccupationTicks(field) == 0 && Partition.FirstClaimedCells == GenRadial.NumCellsInRadius(2f)
                    && Math.Abs(field.Health - health + Loss(field, 180)) < 1f,
                    "remaining on the captured foothold does not generate repeated captures or healing");
                Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 10f);
                frames = 0;
                stage = 3;
            }
            else if (stage == 3)
            {
                if (++frames < 45) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainOccupation.png"));
                capturedAt = Time.realtimeSinceStartup;
                stage = 4;
            }
            else if (stage == 4)
            {
                if (Time.realtimeSinceStartup - capturedAt < 0.3f || !File.Exists(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainOccupation.png"))) return;
                Check(field!.Contest!.OccupationTicks(field) == 0, "paused rendering does not advance occupation timers");
                Save(1, 5);
            }
            else if (stage == 5)
            {
                Move(field!.Caster!, new IntVec3(10, 0, 10));
                RunTicks(419);
                Check(Partition.FirstClaimedCells == GenRadial.NumCellsInRadius(2f) && Footprint().All(field.OwnsCell),
                    "enemy-surrounded occupation survives exactly 599 active ticks before erosion");
                Save(2, 50);
            }
            else if (stage == 50)
            {
                float before = field!.Health;
                RunTicks(1);
                Check(Partition.FirstClaimedCells == 0 && Footprint().All(Second.OwnsCell),
                    "the 600th native tick after save and reload releases the occupied footprint");
                Check(Footprint().All(cell => cell.GetTerrain(field.map) == field.Contest!.TerrainAt(Second, cell))
                    && secondProbe!.Thing!.Spawned && secondProbe.Thing.HitPoints == 137
                    && !firstProbe!.Thing!.Spawned && firstProbe.Thing.HitPoints == 211,
                    "erosion switches actual terrain and restores original enemy buildings with damage preserved");
                float strength = (float)Second.Worker!.GetType().GetMethod("SkillStrengthAt")!.Invoke(Second.Worker, new object[] { center })!;
                Check(Math.Abs(strength - 1f) < 0.001f && Math.Abs(field.Health - before + Loss(field, 1)) < 0.1f,
                    "eroded land immediately restores enemy skill ownership without awarding occupation healing");
                frames = 0;
                stage = 51;
            }
            else if (stage == 51)
            {
                if (++frames < 45) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainOccupation_Eroded.png"));
                capturedAt = Time.realtimeSinceStartup;
                stage = 52;
            }
            else if (stage == 52)
            {
                if (Time.realtimeSinceStartup - capturedAt < 0.3f
                    || !File.Exists(Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainOccupation_Eroded.png"))) return;
                Move(field!.Caster!, center); RunTicks(180);
                Check(Footprint().All(field.OwnsCell), "lost territory can be deliberately occupied again");
                Move(Second.Caster!, center + new IntVec3(-1, 0, 0));
                float health = Second.Health;
                RunTicks(180);
                Check(Second.OwnsCell(Second.Caster!.Position) && !field!.OwnsCell(center)
                    && Math.Abs(Second.Health - health - 300f + Loss(Second, 180)) < 1f,
                    "the opposing caster can recapture a foothold after three seconds and receive the same 300 healing");
                Check(secondProbe!.Thing!.Spawned && secondProbe.Thing.HitPoints == 137
                    && !firstProbe!.Thing!.Spawned && firstProbe.Thing.HitPoints == 211,
                    "countercapture restores the original defender building with both damage states preserved; defender spawned="
                        + secondProbe.Thing.Spawned + ", hp=" + secondProbe.Thing.HitPoints + ", invader spawned="
                        + firstProbe.Thing.Spawned + ", hp=" + firstProbe.Thing.HitPoints + ", cell owner=" + Second.OwnsCell(firstProbe.Position));
                Move(field!.Caster!, new IntVec3(10, 0, 10)); Move(Second.Caster!, new IntVec3(50, 0, 45));
                IntVec3 protectedCenter = new(45, 0, 25);
                Second.ProtectTerritory(GenRadial.RadialCellsAround(protectedCenter, 2f, true), 10000);
                Move(field.Caster!, protectedCenter);
                int claims = Partition.FirstClaimedCells;
                health = field.Health; RunTicks(180);
                Check(!field.OwnsCell(protectedCenter) && Partition.FirstClaimedCells == claims
                    && Math.Abs(field.Health - health + Loss(field, 180)) < 1f,
                    "a fully protected enemy footprint blocks conversion and awards no occupation healing");
                field.RequestReturn("occupation checks first return");
                stage = 6;
            }
            else if (stage == 6)
            {
                if (!field!.Trapped) return;
                Check(Partition.FirstClaimedCells == 0 && field.Contest!.OccupationTicks(field) == 0
                    && field.map.AllCells.All(Second.OwnsCell), "dissolution clears defeated claims and timers before complete survivor takeover");
                Second.RequestReturn("occupation checks final return");
                stage = 7;
            }
            else if (stage == 7)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                Current.Game.CurrentMap = source;
                Pawn a = source!.mapPawns.FreeColonistsSpawned.First();
                Pawn b = source.mapPawns.FreeColonistsSpawned.First(pawn => pawn != a);
                DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                field = CQFDomainRuntime.Instance.EnterOrContest(basic, a, new[] { b });
                CQFDomainRuntime.Instance.EnterOrContest(basic, b, Array.Empty<Pawn>());
                Current.Game.CurrentMap = field.map;
                stage = 8;
            }
            else if (stage == 8)
            {
                if (field!.ArrivalLocked || Second.ArrivalLocked) return;
                Move(field.Caster!, EnemyCell(field)); Move(Second.Caster!, EnemyCell(Second));
                field.DrainHealth(10000f); Second.DrainHealth(10000f);
                float firstHealth = field.Health, secondHealth = Second.Health;
                RunTicks(180);
                Check(Partition.FirstClaimedCells > 0 && Partition.SecondClaimedCells > 0
                    && Math.Abs(field.Health - firstHealth - 300f + Loss(field, 180)) < 1f
                    && Math.Abs(Second.Health - secondHealth - 300f + Loss(Second, 180)) < 1f,
                    "both casters can capture and heal on the same native tick without a processing-order advantage");
                field.RequestReturn("occupation checks simultaneous cleanup"); Second.RequestReturn("occupation checks simultaneous cleanup");
                stage = 9;
            }
            else if (stage == 9)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                Check(field.Available && field.Challenger == null, "captured territory is cleaned before battlefield hibernation and reuse");
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Occupation runtime checks failed: " + error);
            Finish(false);
        }
    }

    private void Save(int phase, int nextStage)
    {
        if (!GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks")) { stage = nextStage; return; }
        GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
        saved.Record(field!.Caster!.thingIDNumber, Second.Caster!.thingIDNumber);
        saved.RecordStructure(firstProbe!.Thing!); saved.OccupationPhase = phase;
        string name = "CQFOccupationChecks" + phase;
        GameDataSaveLoader.SaveGame(name); stage = 99; GameDataSaveLoader.LoadGame(name);
    }

    private IEnumerable<IntVec3> Footprint() => GenRadial.RadialCellsAround(center, 2f, true);
    private static float Loss(MapComponent_Domain side, int ticks) => side.MaximumHealth * side.Definition.contestHealthLossPerSecond * ticks / 60f;
    private void RunTicks(int count)
    {
        object worker = Second.Worker!;
        var automaticTide = worker.GetType().GetField("automaticTideTicks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (worker.GetType().Name == "DomainWorker_BloodSea" && automaticTide == null)
            throw new InvalidOperationException("Blood sea automatic tide timer was not found for isolation of ownership checks.");
        for (int tick = 0; tick < count; tick++)
        {
            automaticTide?.SetValue(worker, 0);
            Find.TickManager.DoSingleTick();
        }
    }
    private static void Move(Pawn pawn, IntVec3 cell) { pawn.Position = cell; pawn.Notify_Teleported(); pawn.drafter.Drafted = true; }
    private static IntVec3 EnemyCell(MapComponent_Domain side) => side.map.AllCells.First(cell => !side.OwnsCell(cell)
        && cell.x > 8 && cell.z > 8 && cell.x < 51 && cell.z < 51 && cell.Standable(side.map)
        && !cell.GetThingList(side.map).Any(thing => thing is not Filth));

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private MapComponent_Domain? field;
    private Map? source;
    private DomainStructure? firstProbe;
    private DomainStructure? secondProbe;
    private readonly IntVec3 center = new(31, 0, 30);
    private int stage;
    private int frames;
    private bool finished;
    private float capturedAt;
}
