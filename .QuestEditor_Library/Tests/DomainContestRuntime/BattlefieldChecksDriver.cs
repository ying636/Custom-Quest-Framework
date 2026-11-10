using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class BattlefieldChecksDriver : MonoBehaviour
{
    private string DirectoryPath => Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE") ?? GenFilePaths.SaveDataFolderPath;
    private string ResultPath => Path.Combine(DirectoryPath, "BattlefieldChecks.txt");

    public void Resume(int firstId, int secondId, int savedCase)
    {
        caseIndex = savedCase;
        first = CQFDomainRuntime.Instance.DomainMaps.Select(map => map.GetComponent<MapComponent_Domain>())
            .Single(domain => domain.Active && domain.Caster?.thingIDNumber == firstId);
        host = first.Caster;
        guest = first.Challenger!.Caster;
        source = first.SourceMap;
        leasedId = first.map.uniqueID;
        bed = first.Layout!.Structures.Select(item => item.Thing).OfType<Building>().Single(item => item.thingIDNumber == Current.Game.GetComponent<GameComponent_ContestChecks>().StructureId);
        Find.TickManager.Pause();
        Check(guest!.thingIDNumber == secondId && first.IsBattlefield && first.map.Size == new IntVec3(120, 1, 90)
            && first.Contest!.Partition.UsesCurvedFront && first.Contest.Partition.Encirclement == 1f,
            "complete game reload restores the dedicated battlefield and enclosed front");
        Check(first.Contest.Partition.HasProtections && bed.HitPoints == Current.Game.GetComponent<GameComponent_ContestChecks>().StructureHitPoints,
            "complete reload restores protected territory and the original damaged building");
        startedAt = Time.realtimeSinceStartup;
        first.RequestReturn("battlefield saved checks");
        stage = 3;
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            if (stage != 0 && Time.realtimeSinceStartup - startedAt > 80f) throw new TimeoutException("Battlefield stage=" + stage + " case=" + caseIndex);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                if (runtime.Transition != null || !runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"))) return;
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                host = MakePawn(source, source.Center + new IntVec3(-5, 0, 0));
                guest = MakePawn(source, source.Center + new IntVec3(5, 0, 0));
                DomainDef small = Definition("CQF_Check_BattlefieldSmall", "CQF_TestDomain", new IntVec3(60, 1, 40));
                DomainDef large = Definition("CQF_Check_BattlefieldLarge", "RSH_BloodSeaDomain", new IntVec3(100, 1, 80));
                DomainDef opening = caseIndex == 1 ? large : small;
                first = runtime.EnterOrContest(opening, host, caseIndex == 0 ? new[] { guest } : Array.Empty<Pawn>());
                first.DrainHealth(7500f);
                bed = (Building)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
                bed.HitPoints = bed.MaxHitPoints - 17;
                GenSpawn.Spawn(bed, first.FindFreeCell(first.map, first.map.Center + new IntVec3(-8, 0, 5), false), first.map, Rot4.East);
                first.Layout!.Register(bed);
                Map firstMap = first.map;
                DomainDef incoming = caseIndex == 1 ? small : large;
                if (caseIndex == 2) incoming = Definition("CQF_Check_BattlefieldFailure", "CQF_TestDomain", new IntVec3(100, 1, 80));
                if (caseIndex == 0)
                {
                    MapComponent_Domain side = runtime.EnterOrContest(incoming, guest, Array.Empty<Pawn>());
                    first = side.Battlefield;
                }
                else
                {
                    Current.Game.CurrentMap = source;
                    second = runtime.EnterOrContest(incoming, guest, Array.Empty<Pawn>());
                    if (caseIndex == 2)
                    {
                        DomainWorker_BattlefieldFailure.FailNext = true;
                        bool rejected = false;
                        try { runtime.MergeDomains(first, second); }
                        catch (Exception error) { rejected = error.ToString().Contains("CQF_EXPECTED_BATTLEFIELD_ROLLBACK"); }
                        File.AppendAllText(ResultPath, "TRACE rollback rejected=" + rejected + ", host=" + host.Map?.uniqueID
                            + ", first=" + firstMap.uniqueID + ", guest=" + guest.Map?.uniqueID + ", second=" + second.map.uniqueID
                            + ", active=" + first.Active + "/" + second.Active + ", reserve=" + first.Health
                            + ", bed=" + bed.Map?.uniqueID + "/" + bed.HitPoints + "/" + bed.Rotation + "\n");
                        Check(rejected && host.Map == firstMap && guest.Map == second.map && first.Active && second.Active
                            && first.Health == 92500f && bed.HitPoints == bed.MaxHitPoints - 17 && bed.Map == firstMap && bed.Rotation == Rot4.East,
                            "injected transfer failure rolls both instances, participants and the same damaged building back to their source maps");
                    }
                    first = runtime.MergeDomains(first, second).Battlefield;
                }
                Current.Game.CurrentMap = first.map;
                Find.CameraDriver.SetRootPosAndSize(first.map.Center.ToVector3Shifted(), 42f);
                Check(first.IsBattlefield && first.map != firstMap && first.map.Size == new IntVec3(120, 1, 90)
                    && host.Map == first.map && guest.Map == first.map && first.Health == 92500f,
                    "mixed-size domains migrate to a new 120 by 90 battlefield while preserving the first reserve");
                Check(first.Layout!.Structures.Any(item => item.Thing == bed) && bed.HitPoints == bed.MaxHitPoints - 17,
                    "battlefield layout owns the original damaged building rather than a replacement");
                if (caseIndex != 0)
                    Check(guest.Position - host.Position == first.Challenger!.SourceOrigin - first.SourceOrigin,
                        "merging established maps preserves caster spacing from the shared source instead of stacking both at the map center");
                if (leasedId != 0) Check(first.map.uniqueID == leasedId, "a different first domain reuses the same sleeping map by size");
                leasedId = first.map.uniqueID;
                first.DrainHealth(first.Health - 10000f);
                startedAt = Time.realtimeSinceStartup;
                startedTick = GenTicks.TicksGame;
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                stage = 1;
            }
            else if (stage == 1)
            {
                if (GenTicks.TicksGame - startedTick < 130) return;
                Find.TickManager.Pause();
                DomainTerritoryPartition partition = first!.Contest!.Partition;
                Check(partition.Encirclement == 1f && partition.FirstFraction < 0.11f
                    && first.map.AllCells.Where(cell => partition.IsFirst(cell)).All(cell => cell.x > 0 && cell.z > 0 && cell.x < 119 && cell.z < 89),
                    "a heavily weakened domain becomes an interior island with no edge strip");
                IntVec3 protectedCell = first.map.AllCells.First(partition.IsFirst);
                first.ProtectTerritory(new[] { protectedCell }, 600);
                Shot("Island");
                stage = 2;
            }
            else if (stage == 2)
            {
                if (caseIndex == 0 && GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks"))
                {
                    GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                    saved.Record(host!.thingIDNumber, guest!.thingIDNumber);
                    saved.RecordStructure(bed!);
                    saved.CounterOpeningCase = caseIndex;
                    stage = 99;
                    GameDataSaveLoader.SaveGame("CQFBattlefieldChecks");
                    GameDataSaveLoader.LoadGame("CQFBattlefieldChecks");
                    return;
                }
                first!.RequestReturn("battlefield checks first cancellation");
                stage = 3;
            }
            else if (stage == 3)
            {
                if (first!.Active || CQFDomainRuntime.Instance.Transition != null) return;
                Check(first.Trapped && first.Challenger!.Active && host!.Map == first.map && guest!.Map == first.map,
                    "a cancelled domain stays trapped in the opposing domain on the dedicated map");
                first.Challenger.RequestReturn("battlefield checks final cancellation");
                stage = 4;
            }
            else if (stage == 4)
            {
                if (host!.Map != source || guest!.Map != source || runtime.Transition != null) return;
                Check(GameComponent_MapManager.Instance!.IsHibernating(first!.map) && first.Available && first.Challenger == null,
                    "final cancellation returns both casters and hibernates the cleared battlefield");
                host.Destroy(); guest.Destroy();
                caseIndex++;
                if (caseIndex == 5) { File.AppendAllText(ResultPath, "COMPLETE\n"); finished = true; Application.Quit(0); return; }
                stage = caseIndex == 3 ? 10 : caseIndex == 4 ? 20 : 0;
            }
            else if (stage == 10)
            {
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                host = MakePawn(source, source.Center + new IntVec3(-5, 0, 0));
                guest = MakePawn(source, source.Center + new IntVec3(5, 0, 0));
                first = runtime.EnterOrContest(DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain"), host, Array.Empty<Pawn>());
                previousMap = first.map;
                Current.Game.CurrentMap = source;
                guest.abilities ??= new Pawn_AbilityTracker(guest);
                AbilityDef ability = DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion");
                guest.abilities.GainAbility(ability);
                Find.CameraDriver.SetRootPosAndSize(guest.Position.ToVector3Shifted(), 18f);
                guest.abilities.GetAbility(ability).QueueCastingJob(guest, guest);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                startedAt = Time.realtimeSinceStartup;
                stage = 11;
            }
            else if (stage == 11)
            {
                if (guest!.Map == source) return;
                first = guest.Map.GetComponent<MapComponent_Domain>();
                Check(first.IsBattlefield && first.map != previousMap && first.map.Size == new IntVec3(90, 1, 90)
                    && host!.Map == first.map && first.Challenger != null
                    && !previousMap!.GetComponent<MapComponent_Domain>().HasActiveDomain,
                    "expanding beside an established external anchor also transfers both domains to a fresh dedicated battlefield");
                DomainTransition? active = runtime.Transition;
                Check(active is DomainFusionTransition && (int)typeof(DomainTransition).GetField("coveredFrame", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(active)! >= 0,
                    "external-anchor transfer retains the original fusion fullscreen barrier");
                Shot("EstablishedCover");
                stage = 12;
            }
            else if (stage == 12)
            {
                if (runtime.Transition != null) return;
                Find.TickManager.Pause();
                Shot("EstablishedBattle");
                first!.RequestReturn("established anchor checks");
                stage = 3;
            }
            else if (stage == 20)
            {
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                host = MakePawn(source, source.Center + new IntVec3(-5, 0, 0));
                guest = MakePawn(source, source.Center + new IntVec3(5, 0, 0));
                first = runtime.EnterOrContest(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"), host, new[] { guest });
                previousMap = first.map;
                Current.Game.CurrentMap = first.map;
                Find.CameraDriver.SetRootPosAndSize(guest.Position.ToVector3Shifted(), 18f);
                DomainDef plain = DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain");
                Check(plain.surface == null, "counter-opening fixture has no surface or screen shader");
                DomainOpening_Basic.Start(plain, guest);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                startedAt = Time.realtimeSinceStartup;
                stage = 21;
            }
            else if (stage == 21)
            {
                if (guest!.Map == previousMap) return;
                first = guest.Map.GetComponent<MapComponent_Domain>();
                DomainTransition? active = runtime.Transition;
                Check(first.IsBattlefield && first.map != previousMap && first.map.Size == new IntVec3(90, 1, 90)
                    && first.Challenger!.Definition.surface == null && host!.Map == first.map,
                    "a domain without shaders counter-expands into its own dedicated battlefield");
                Check(active is DomainCounterTransition && (int)typeof(DomainTransition).GetField("coveredFrame", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(active)! >= 0,
                    "shaderless counter-opening commits only after a fully covered fallback frame");
                Shot("PlainCounterCover");
                stage = 22;
            }
            else if (stage == 22)
            {
                if (runtime.Transition != null) return;
                Find.TickManager.Pause();
                Shot("PlainCounterBattle");
                first!.RequestReturn("plain counter checks");
                stage = 3;
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Battlefield checks failed: " + error);
            finished = true; Application.Quit(1);
        }
    }

    private static DomainDef Definition(string name, string preset, IntVec3 size)
    {
        DomainDef? found = DefDatabase<DomainDef>.GetNamedSilentFail(name);
        if (found != null) return found;
        DomainDef source = DefDatabase<DomainDef>.GetNamed(preset);
        DomainDef value = new() { defName = name, size = size, worldObject = source.worldObject, mapGenerator = source.mapGenerator,
            workerClass = source.workerClass, anchor = source.anchor, protection = source.protection, openingAbility = source.openingAbility,
            maximumHealth = 100000f, visuals = source.visuals, surface = source.surface, contestHealthLossPerSecond = 0f, preload = false };
        if (name.EndsWith("Failure")) value.workerClass = typeof(DomainWorker_BattlefieldFailure);
        DefDatabase<DomainDef>.Add(value);
        return value;
    }

    private static Pawn MakePawn(Map map, IntVec3 cell)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
        GenSpawn.Spawn(pawn, map.GetComponent<MapComponent_Domain>().FindFreeCell(map, cell, true), map);
        pawn.drafter.Drafted = true; pawn.drafter.FireAtWill = false;
        return pawn;
    }

    private void Shot(string stageName) => ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "Battlefield_" + caseIndex + "_" + stageName + ".png"));

    private void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS case " + caseIndex + " " + message + "\n");
    }

    private Map? source;
    private Map? previousMap;
    private Pawn? host;
    private Pawn? guest;
    private Building? bed;
    private MapComponent_Domain? first;
    private MapComponent_Domain? second;
    private int stage;
    private int caseIndex;
    private int leasedId;
    private int startedTick;
    private float startedAt;
    private bool finished;
}
