using System.Diagnostics;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class MultipleDomainChecksDriver : MonoBehaviour
{
    private string DirectoryPath => Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE")!;
    private string ResultPath => Path.Combine(DirectoryPath, "MultipleDomains.txt");

    public void Resume(int firstId, int secondId, int phase)
    {
        CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            foreach (Map map in runtime.DomainMaps)
                foreach (MapComponent_Domain side in map.GetComponent<MapComponent_Domain>().Instances.Where(side => side.Active))
                    if (side.Worker?.GetType().FullName == "RSH.DomainWorker_BloodSea")
                        HarmonyLib.AccessTools.Field(side.Worker.GetType(), "automaticTideTicks").SetValue(side.Worker, 0);
        field = runtime.DomainMaps.Select(map => map.GetComponent<MapComponent_Domain>())
            .Single(candidate => candidate.Caster?.thingIDNumber == firstId && candidate.InstanceCount == 8);
        source = field.SourceMap;
        actors = field.Instances.Select(side => side.Caster!).ToList();
        actors.Add(field.map.mapPawns.AllPawnsSpawned.Single(pawn => pawn.thingIDNumber == Current.Game.GetComponent<GameComponent_ContestChecks>().ExtraPawnId));
        Find.TickManager.Pause();
        Check(actors[7].thingIDNumber == secondId && field.ActiveDomainCount == (phase == 1 ? 8 : 7)
            && field.Contest!.Partition.DomainCount == 8 && field.IsBattlefield,
            "full game reload restores all eight instances, indexed territory and active count");
        if (phase == 1)
        {
            GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
            bed = field.Instances.SelectMany(side => side.Layout?.Structures ?? Array.Empty<DomainStructure>()).Select(item => item.Thing)
                .OfType<Building>().Single(thing => thing.thingIDNumber == saved.StructureId);
            Check(bed.HitPoints == saved.StructureHitPoints && field.Contest.Partition.Protections.Any(item => item.Source == bed && item.OwnerIndex == 0),
                "full reload preserves the original damaged building and its indexed protection");
        }
        else Check(field.Trapped && actors[0].Map == field.map, "a dissolved primary remains trapped while seven opponents survive after reload");
        stage = phase == 1 ? 4 : 6;
        stageStarted = Time.realtimeSinceStartup;
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            if (stage != 0 && Time.realtimeSinceStartup - stageStarted > 100f) throw new TimeoutException("Multiple domain stage=" + stage);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            foreach (Map map in runtime.DomainMaps)
                foreach (MapComponent_Domain side in map.GetComponent<MapComponent_Domain>().Instances.Where(side => side.Active))
                    if (side.Worker?.GetType().FullName == "RSH.DomainWorker_BloodSea")
                        HarmonyLib.AccessTools.Field(side.Worker.GetType(), "automaticTideTicks").SetValue(side.Worker, 0);
            if (stage == 0)
            {
                if (!runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"))
                    || !runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain"))
                    || !runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain"))) return;
                using Process process = Process.GetCurrentProcess();
                File.AppendAllText(ResultPath, "PROCESS " + process.Id + " " + process.MainModule!.FileName + " " + Environment.CommandLine + "\n");
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                actors = Enumerable.Range(0, 9).Select(index => MakePawn(source.Center + new IntVec3(
                    (int)(Math.Cos(index * Math.PI * 2d / 9d) * 6d), 0, (int)(Math.Sin(index * Math.PI * 2d / 9d) * 6d)))).ToList();
                File.WriteAllLines(Path.Combine(DirectoryPath, "Origins.txt"), actors.Select(pawn => pawn.thingIDNumber + "|" + pawn.Position.x + "|" + pawn.Position.z));
                field = runtime.EnterOrContest(Definition(0), actors[0], actors.Skip(2));
                Current.Game.CurrentMap = source;
                Queue(1);
                joining = 2; stage = 1; stageStarted = Time.realtimeSinceStartup;
            }
            else if (stage == 1)
            {
                if (actors[joining - 1].Map == null || actors[joining - 1].Map == source || runtime.Transition != null) return;
                field = actors[joining - 1].Map.GetComponent<MapComponent_Domain>();
                if (field.InstanceCount < joining || field.Instances.Any(side => side.Active && side.ArrivalLocked)) return;
                Find.TickManager.Pause();
                Check(field.InstanceCount == joining && field.ActiveDomainCount == joining && field.IsBattlefield
                    && field.Instances.Select(side => side.Caster).Distinct().Count() == joining,
                    "join " + joining + " retains every existing caster on one dedicated battlefield");
                int[] sizes = { 90, 111, 128, 143, 156, 169, 180 };
                Check(field.map.Size.x >= sizes[joining - 2] && field.map.Size.z >= sizes[joining - 2]
                    && (previousSize == IntVec3.Zero || field.map.Size.x > previousSize.x && field.map.Size.z > previousSize.z),
                    "every new domain grows the battlefield while the size multiplier declines");
                previousSize = field.map.Size;
                if (bed != null) Check(field.Instances.First().Layout!.Structures.Any(item => item.Thing == bed)
                    && bed.HitPoints == bed.MaxHitPoints - 17 && field.Contest!.Partition.Protections.Any(item => item.Source == bed),
                    "growth preserves the same damaged building, layout and protection; destroyed=" + bed.Destroyed + "; HP=" + bed.HitPoints + "/" + bed.MaxHitPoints + "; registered=" + field.Instances.First().Layout!.Structures.Any(item => item.Thing == bed) + "; protections=" + field.Contest!.Partition.Protections.Count + "; matching=" + field.Contest.Partition.Protections.Count(item => item.Source == bed));
                foreach (MapComponent_Domain side in field.Instances) side.RestoreHealth(side.MaximumHealth);
                field.Contest!.ResolveSurvivor(field);
                Check(field.Instances.Where(side => side.Active).All(side => Math.Abs(field.Contest.Partition.CellsFor(side.InstanceIndex) - field.map.cellIndices.NumGridCells / (double)joining) < field.map.cellIndices.NumGridCells * 0.02d), "equal health yields proportional territory even with clustered casters");
                if (joining == 3) CreateProtectedBed();
                Current.Game.CurrentMap = field.map;
                Find.CameraDriver.SetRootPosAndSize(field.map.Center.ToVector3Shifted(), 24f);
                shotFrame = Time.frameCount; stage = 2;
            }
            else if (stage == 2)
            {
                if (Time.frameCount <= shotFrame + 2) return;
                Dump(); Shot("Domains_" + joining);
                if (joining == 8)
                {
                    Ability button = actors[8].abilities.GetAbility(DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion"));
                    Check(button.GizmoDisabled(out string reason) && reason == "CQF_DomainContestFull".Translate().ToString(),
                        "a ninth caster receives the exact full-battle reason from the real ability button");
                    GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                    saved.Record(actors[0].thingIDNumber, actors[7].thingIDNumber); saved.ExtraPawnId = actors[8].thingIDNumber;
                    saved.RecordStructure(bed!); saved.MultiplePhase = 1;
                    stage = 99; GameDataSaveLoader.SaveGame("CQFMultipleDomains"); GameDataSaveLoader.LoadGame("CQFMultipleDomains");
                    return;
                }
                if (joining == 6)
                {
                    Queue(6); Queue(7);
                    joining = 8;
                    stage = 3;
                }
                else { Queue(joining); joining++; stage = 1; }
                stageStarted = Time.realtimeSinceStartup;
            }
            else if (stage == 3)
            {
                if (runtime.Transition != null) return;
                if (actors[6].Map == null) return;
                field = actors[6].Map.GetComponent<MapComponent_Domain>();
                if (field.InstanceCount < 8) return;
                Check(actors[6].Map == actors[7].Map && field.InstanceCount == 8,
                    "two concurrent counter-openings both finish instead of losing the cast left on an old map");
                stage = 1;
            }
            else if (stage == 4)
            {
                CheckRate(8);
                field!.RequestReturn("multiple checks primary withdrawal");
                stage = 5; stageStarted = Time.realtimeSinceStartup;
            }
            else if (stage == 5)
            {
                if (field!.Active || runtime.Transition != null) return;
                Check(field.Trapped && field.ActiveDomainCount == 7 && actors.All(pawn => pawn.Map == field.map),
                    "primary withdrawal leaves all creatures trapped with seven active domains");
                GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                saved.MultiplePhase = 2;
                stage = 99; GameDataSaveLoader.SaveGame("CQFMultipleDomains"); GameDataSaveLoader.LoadGame("CQFMultipleDomains");
            }
            else if (stage == 6)
            {
                CheckRate(7);
                Check(field!.map.Size == previousSize || previousSize == IntVec3.Zero, "withdrawal does not shrink the physical battlefield");
                foreach (MapComponent_Domain side in field.Instances.Where(side => side.Active)) side.RestoreHealth(side.MaximumHealth);
                MapComponent_Domain weak = field.Instances.ElementAt(1);
                weak.DrainHealth(weak.Health - 1000f);
                field.Contest!.ResolveSurvivor(field);
                Check(field.map.AllCells.Where(cell => weak.OwnsCell(cell)).Any()
                    && field.map.AllCells.Where(cell => weak.OwnsCell(cell)).All(cell => cell.x > 0 && cell.z > 0 && cell.x < field.map.Size.x - 1 && cell.z < field.map.Size.z - 1),
                    "a severely weakened side remains an irregular interior island among seven opponents");
                shotFrame = Time.frameCount; stage = 7;
            }
            else if (stage == 7)
            {
                if (Time.frameCount <= shotFrame + 2) return;
                Shot("SevenDomains_Island");
                foreach (MapComponent_Domain side in field!.Instances.Where(side => side.Active)) side.RequestReturn("multiple checks final cleanup");
                stage = 8; stageStarted = Time.realtimeSinceStartup;
            }
            else if (stage == 8)
            {
                if (runtime.Transition != null || actors.Any(pawn => pawn.Map != source)) return;
                Check(GameComponent_MapManager.Instance!.IsHibernating(field!.map) && field.Available && field.InstanceCount == 1,
                    "the final withdrawal returns all nine creatures and hibernates the cleared battlefield");
                Dictionary<int, IntVec3> origins = File.ReadAllLines(Path.Combine(DirectoryPath, "Origins.txt")).Select(line => line.Split('|'))
                    .ToDictionary(parts => int.Parse(parts[0]), parts => new IntVec3(int.Parse(parts[1]), 0, int.Parse(parts[2])));
                Check(actors.All(pawn => pawn.Position == origins[pawn.thingIDNumber]), "every caster and captured creature returns to its original source cell");
                foreach (Pawn pawn in actors) pawn.Destroy();
                File.AppendAllText(ResultPath, "COMPLETE\n"); finished = true; Application.Quit(0);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL stage=" + stage + " joining=" + joining + " maps=" + string.Join(",", actors.Select(pawn => pawn.Map?.uniqueID.ToString() ?? "null")) + " " + error + "\n"); Log.Error("[CQF] Multiple domain checks failed: " + error);
            finished = true; Application.Quit(1);
        }
    }

    private void Dump()
    {
        string path = Path.Combine(DirectoryPath, "Terrain_" + joining + ".txt");
        File.WriteAllLines(path, field!.map.AllCells.GroupBy(cell => field.DomainAt(cell)?.InstanceIndex + "/" + field.map.terrainGrid.TerrainAt(cell).defName + "/" + field.Contest!.CoversBoundaryCell(cell)).Select(group => group.Key + "=" + group.Count()));
        foreach (IntVec3 cell in new[] { field.map.Center, new IntVec3(10, 0, 10), new IntVec3(30, 0, 30) })
        {
            Section section = field.map.mapDrawer.SectionAt(cell);
            foreach (Type type in new[] { typeof(SectionLayer_BasicDomainTerrain), typeof(SectionLayer_DomainContest), typeof(SectionLayer_Terrain), HarmonyLib.AccessTools.TypeByName("RSH.SectionLayer_BloodSeaDomain") })
            {
                SectionLayer layer = section.GetLayer(type);
                File.AppendAllText(path, cell + " " + type.Name + " visible=" + layer.Visible + " meshes=" + string.Join(",", layer.subMeshes.Select(mesh => mesh.mesh.vertexCount + "/" + mesh.disabled + "/" + mesh.material.shader.name)) + "\n");
            }
        }
    }
    private DomainDef Definition(int index) => DefDatabase<DomainDef>.GetNamed(index % 3 == 1 ? "RSH_BloodSeaDomain" : index % 3 == 2 ? "CQF_CheckPlainDomain" : "CQF_TestDomain");

    private Pawn MakePawn(IntVec3 cell)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
        GenSpawn.Spawn(pawn, source!.GetComponent<MapComponent_Domain>().FindFreeCell(source, cell, true), source);
        pawn.drafter.Drafted = true; pawn.drafter.FireAtWill = false;
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        pawn.abilities.GainAbility(DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion"));
        return pawn;
    }

    private void Queue(int index)
    {
        Pawn pawn = actors[index]; DomainDef definition = Definition(index);
        if (index % 3 == 2) DomainOpening_Basic.Start(definition, pawn);
        else
        {
            pawn.abilities.GainAbility(definition.openingAbility);
            Ability ability = pawn.abilities.GetAbility(definition.openingAbility);
            Check(!ability.GizmoDisabled(out string reason), "caster " + index + " can join via its actual ability: " + reason);
            ability.QueueCastingJob(pawn, pawn);
        }
        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
    }

    private void CreateProtectedBed()
    {
        MapComponent_Domain side = field!.Instances.First();
        IntVec3 cell = field.map.AllCells.First(cell => GenAdj.OccupiedRect(cell, Rot4.East, ThingDefOf.Bed.size)
            .All(part => part.InBounds(field.map) && side.OwnsCell(part) && part.Standable(field.map) && part.GetThingList(field.map).Count == 0));
        bed = (Building)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog); bed.HitPoints = bed.MaxHitPoints - 17;
        GenSpawn.Spawn(bed, cell, field.map, Rot4.East); side.RegisterStructure(bed);
        side.ProtectTerritory(bed.OccupiedRect(), 20000, bed);
    }

    private void CheckRate(int count)
    {
        Find.TickManager.Pause();
        List<MapComponent_Domain> active = field!.Instances.Where(side => side.Active).ToList();
        foreach (MapComponent_Domain side in active) side.RestoreHealth(side.MaximumHealth);
        for (int tick = 0; tick < 60; tick++) Find.TickManager.DoSingleTick();
        Check(active.Count == count && active.All(side => Math.Abs(side.Health - (side.MaximumHealth - side.MaximumHealth * side.Definition.contestHealthLossPerSecond * (count - 1))) < 1f),
            count + " active domains lose the configured maximum-health rate exactly once per tick");
    }

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private void Shot(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, name + ".png"));

    private Map? source;
    private MapComponent_Domain? field;
    private List<Pawn> actors = new();
    private Building? bed;
    private int joining;
    private int stage;
    private int shotFrame;
    private IntVec3 previousSize;
    private float stageStarted;
    private bool finished;
}
