using System.Reflection;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class ReviewFixesChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.Game?.CurrentMap == null) return;
        try
        {
            if (Time.realtimeSinceStartup > 180f) throw new TimeoutException("Review fixes stage " + stage);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                if (Find.CurrentMap == null) return;
                DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                DomainDef blood = DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain");
                if (!runtime.HasAvailable(basic) || !runtime.HasAvailable(blood)) return;
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                firstPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                secondPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                IntVec3 origin = source.mapPawns.FreeColonistsSpawned.First().Position;
                GenSpawn.Spawn(firstPawn, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, origin, true), source);
                GenSpawn.Spawn(secondPawn, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, origin + new IntVec3(8, 0, 0), true), source);
                firstPawn.abilities ??= new Pawn_AbilityTracker(firstPawn);
                secondPawn.abilities ??= new Pawn_AbilityTracker(secondPawn);
                firstPawn.drafter.Drafted = secondPawn.drafter.Drafted = true;
                field = runtime.EnterOrContest(basic, firstPawn, Array.Empty<Pawn>());
                MapComponent_Domain donor = runtime.EnterOrContest(blood, secondPawn, Array.Empty<Pawn>());
                Current.Game.CurrentMap = source;
                CheckMerge(runtime, donor);
                Current.Game.CurrentMap = field.map;
                Find.CameraDriver.JumpToCurrentMapLoc(field.map.Center);
                stage = 1;
                frames = 0;
            }
            else if (stage == 1)
            {
                if (++frames < 15) return;
                field!.RequestReturn("review checks dissolve primary");
                field.CompleteReturn();
                stage = 7;
            }
            else if (stage == 7)
            {
                if (!field!.Trapped || runtime.Transition != null) return;
                Check(firstPawn!.Map == field.map && secondPawn!.Map == field.map,
                    "primary dissolution traps both casters until the blood sea survivor exits");
                Check(field.map.AllCells.All(cell => field.DomainAt(cell) == field.Challenger && field.Challenger!.DomainAt(cell) == field.Challenger),
                    "survivor ownership is identical through either instance");
                returning = field.Challenger!;
                BeginReturn();
            }
            else if (stage == 2)
            {
                if (secondPawn!.Map == returning!.map)
                {
                    if (!transition!.ReturnCovered)
                    {
                        if (Find.CurrentMap != returning.map || !returning.Active || !returning.ArrivalLocked)
                            throw new InvalidOperationException("Return transferred or unlocked before the cover frame.");
                        beforeCoverFrames++;
                    }
                    if (!coverShot && transition.Progress >= 0.3f)
                    {
                        coverShot = true;
                        ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "BloodReturnCover_" + returnCase + ".png"));
                    }
                    return;
                }
                Check(beforeCoverFrames > 1 && transition!.ReturnCovered,
                    "blood sea return retains its map and caster until a completed cover frame, case " + returnCase);
                Check(secondPawn.Map == source && firstPawn!.Map == source && Find.CurrentMap == source,
                    "covered return transfers all casters and switches to their source map, case " + returnCase);
                Check(ReferenceEquals(runtime.Transition, transition) && transition.Progress >= 0.46f,
                    "return continues the original effect into the destination reveal, case " + returnCase);
                stage = 3;
            }
            else if (stage == 3)
            {
                if (runtime.Transition != null || !GameComponent_MapManager.Instance!.IsHibernating(returning!.map)) return;
                Check(returning.Battlefield.Available && !returning.ArrivalLocked && secondPawn!.stances.stunner.StunTicksLeft == 0,
                    "completed return releases its locks and hibernates the battlefield, case " + returnCase);
                Check(secondPawn!.abilities.GetAbility(DefDatabase<AbilityDef>.GetNamed("RSH_BloodSeaTraverse")) == null,
                    "completed blood sea return removes temporary abilities, case " + returnCase);
                ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "BloodReturnComplete_" + returnCase + ".png"));
                frames = 0;
                stage = 4;
            }
            else if (stage == 4)
            {
                if (++frames < 8) return;
                returning = runtime.EnterOrContest(DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain"), secondPawn!, Array.Empty<Pawn>());
                if (++returnCase == 1)
                {
                    Current.Game.CurrentMap = returning.map;
                    Find.CameraDriver.JumpToCurrentMapLoc(returning.map.Center);
                    frames = 0;
                    stage = 5;
                }
                else
                {
                    Current.Game.CurrentMap = source;
                    returning.RequestReturn("review checks offscreen return");
                    returning.CompleteReturn();
                    Check(secondPawn!.Map == source && runtime.Transition == null,
                        "offscreen blood sea returns without waiting for an invisible cover");
                    stage = 6;
                }
            }
            else if (stage == 5)
            {
                if (++frames < 15) return;
                BeginReturn();
            }
            else if (stage == 6 && GameComponent_MapManager.Instance!.IsHibernating(returning!.map))
            {
                Check(returning.Battlefield.Available && !returning.ArrivalLocked,
                    "offscreen return also releases and recycles its domain");
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Review fix checks failed: " + error);
            Finish(false);
        }
    }

    private void CheckMerge(CQFDomainRuntime runtime, MapComponent_Domain donor)
    {
        IntVec3 origin = donor.map.Center + new IntVec3(5, 0, 0);
        Thing bed = ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
        bed.HitPoints = 137;
        GenSpawn.Spawn(bed, origin, donor.map, Rot4.East);
        CellRect footprint = bed.OccupiedRect();
        IntVec3 collision = GenAdj.OccupiedRect(origin, Rot4.North, bed.def.size).First(cell => !footprint.Contains(cell));
        Thing blocker = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
        blocker.HitPoints = 211;
        GenSpawn.Spawn(blocker, collision, field!.map);
        failThing = bed;
        failMap = field.map;
        Harmony harmony = new("CQF.Checks.MergeRollback");
        MethodInfo spawn = AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn),
            new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) });
        MethodInfo hook = AccessTools.Method(typeof(ReviewFixesChecksDriver), nameof(FailAfterSpawn));
        bool failedAsExpected = false;
        try
        {
            harmony.Patch(spawn, postfix: new HarmonyMethod(hook));
            try { runtime.MergeDomains(field, donor); }
            catch (InvalidOperationException error) when (error.Message == InjectedFailure) { failedAsExpected = true; }
        }
        finally
        {
            failThing = null; failMap = null;
            harmony.Unpatch(spawn, hook);
        }
        Check(failedAsExpected && field.Challenger == null && field.Active && donor.Active,
            "injected post-spawn failure keeps both original domain instances active");
        Check(bed.Spawned && bed.Map == donor.map && bed.Position == origin && bed.Rotation == Rot4.East
            && bed.HitPoints == 137 && bed.OccupiedRect() == footprint,
            "merge rollback restores the original building position, rotation, footprint and health");
        Check(blocker.Spawned && blocker.HitPoints == 211,
            "failed merge does not wipe the cell outside the validated rotated footprint");
        MapComponent_Domain second = runtime.MergeDomains(field, donor);
        Check(bed.Spawned && bed.Map == field.map && bed.Position == origin && bed.Rotation == Rot4.East
            && bed.OccupiedRect() == footprint && bed.HitPoints == 137 && blocker.Spawned && blocker.HitPoints == 211,
            "successful merge preserves rotated building occupancy without deleting its neighbour");
        Check(field.map.AllCells.All(cell => field.DomainAt(cell) == second.DomainAt(cell)
            && (field.DomainAt(cell) == firstPawn!.Map.GetComponent<MapComponent_Domain>()) == field.OwnsCell(cell)),
            "both live instances resolve every battlefield cell identically");
        Check(second.FindDomainFor(firstPawn) == field && second.FindDomainFor(secondPawn) == second
            && second.HasOtherActiveDomain(second) && second.HasActiveDomain,
            "challenger caster and activity queries resolve through the shared battlefield");
    }

    private void BeginReturn()
    {
        returning!.RequestReturn("review checks visible return");
        returning.CompleteReturn();
        transition = CQFDomainRuntime.Instance.Transition;
        Check(transition?.ReturningDomain == returning && !transition.ReturnCovered && returning.ArrivalLocked
            && secondPawn!.Map == returning.map && Find.CurrentMap == returning.map,
            "starting visible blood sea return waits on its own staged transition, case " + returnCase);
        beforeCoverFrames = 0;
        coverShot = false;
        stage = 2;
    }

    private static void FailAfterSpawn(Thing newThing, Map map)
    {
        if (!ReferenceEquals(newThing, failThing) || map != failMap) return;
        failMap = null;
        throw new InvalidOperationException(InjectedFailure);
    }

    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        File.AppendAllText(ResultPath, "PASS " + name + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private Map? source;
    private MapComponent_Domain? field;
    private MapComponent_Domain? returning;
    private Pawn? firstPawn;
    private Pawn? secondPawn;
    private DomainTransition? transition;
    private int stage;
    private int frames;
    private int beforeCoverFrames;
    private int returnCase;
    private bool coverShot;
    private bool finished;
    private static Thing? failThing;
    private static Map? failMap;
    private const string InjectedFailure = "CQF_EXPECTED_MERGE_ROLLBACK_CHECK";
}
