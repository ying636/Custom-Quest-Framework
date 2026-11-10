using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class CounterOpeningChecksDriver : MonoBehaviour
{
    private string EvidenceDirectory => Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE") ?? GenFilePaths.SaveDataFolderPath;
    private string ResultPath => Path.Combine(EvidenceDirectory, "DomainContestChecks.txt");
    private DomainDef Existing => DefDatabase<DomainDef>.GetNamed(caseIndex % 4 < 2 ? "CQF_TestDomain" : "RSH_BloodSeaDomain");
    private DomainDef Incoming => DefDatabase<DomainDef>.GetNamed(caseIndex % 2 == 0 ? "CQF_TestDomain" : "RSH_BloodSeaDomain");

    public void Resume(int firstId, int secondId, int savedCase)
    {
        Find.TickManager.Pause();
        caseIndex = savedCase;
        field = CQFDomainRuntime.Instance.DomainMaps.Select(map => map.GetComponent<MapComponent_Domain>())
            .Single(domain => domain.Caster?.thingIDNumber == firstId);
        source = field.SourceMap;
        host = field.Caster;
        caster = field.map.mapPawns.AllPawnsSpawned.Single(pawn => pawn.thingIDNumber == secondId);
        observer = field.map.mapPawns.AllPawnsSpawned.Single(pawn => pawn != host && pawn != caster);
        opening = field.map.listerThings.AllThings.OfType<DomainOpening>().Single(item => item.Caster == caster);
        preparedMaps = CQFDomainRuntime.Instance.DomainMaps.ToArray();
        hostCell = host!.Position; casterCell = caster.Position;
        reloaded = true;
        preparationEarly = true;
        preparationRadius = 0f;
        preparationSurface = null;
        startedAt = Time.realtimeSinceStartup;
        Check(opening.IsCounterOpening && !opening.IsClosing && !opening.SupportsFusion,
            "complete reload preserves the fullscreen counter-opening mode and unfinished preparation");
        stage = 1;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
    }

    private void Update()
    {
        if (finished || stage == 99) return;
        try
        {
            if (stage != 0 && Time.realtimeSinceStartup - startedAt > 70f)
                throw new TimeoutException("Counter opening case " + caseIndex + " stage " + stage);
            if (LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                if (!runtime.HasAvailable(Existing) || !runtime.HasTerrainTemplate(Incoming) || runtime.Transition != null) return;
                Find.TickManager.Pause();
                source = Find.CurrentMap;
                preparedMaps = runtime.DomainMaps.ToArray();
                host = MakePawn(source, source.Center + new IntVec3(-2, 0, 0));
                caster = MakePawn(source, source.Center + new IntVec3(2, 0, 0));
                observer = MakePawn(source, source.Center + new IntVec3(3, 0, 1));
                field = runtime.EnterOrContest(Existing, host, new[] { caster, observer });
                Current.Game.CurrentMap = field.map;
                hostCell = host.Position; casterCell = caster.Position;
                Find.CameraDriver.SetRootPosAndSize(caster.Position.ToVector3Shifted(), 15f);
                caster.abilities ??= new Pawn_AbilityTracker(caster);
                caster.abilities.GainAbility(Incoming.openingAbility);
                Check(!caster.abilities.GetAbility(Incoming.openingAbility).GizmoDisabled(out string disabledReason),
                    "the actual ability button allows a different caster to counter-expand: " + disabledReason);
                if (Existing.defName == "CQF_TestDomain") CheckBloodSeaButton(host, true, "RSH_BloodSeaAnotherDomainActive");
                caster.abilities.GetAbility(Incoming.openingAbility).QueueCastingJob(caster, caster);
                startedAt = Time.realtimeSinceStartup;
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                stage = 1;
            }
            else if (stage == 1)
            {
                opening ??= field!.map.listerThings.AllThings.OfType<DomainOpening>().FirstOrDefault(item => item.Caster == caster);
                if (opening == null) return;
                Check(opening.IsCounterOpening && !opening.SupportsFusion && opening.Fusion == null,
                    "an actual ability starts a dedicated counter-opening without a world fusion");
                stage = 2;
            }
            else if (stage == 2)
            {
                if (!preparationEarly && opening!.Progress >= 0.1f)
                {
                    Check(!opening.IsClosing && runtime.Transition == null && field!.Challenger == null
                        && CounterPreparationSurfaceChecks.TryGetDraw(opening, out preparationRadius),
                        "a real field surface is drawn early during preparation, before closure or any fullscreen transition");
                    Shot("PreparationEarly"); preparationEarly = true;
                }
                if (opening!.Progress < 0.3f) return;
                Check(!opening.IsClosing && field!.Challenger == null && runtime.Transition == null
                    && observer!.Spawned && observer.stances.stunner.StunTicksLeft == 0,
                    "preparation retains its duration while other creatures remain free and territory stays unchanged");
                CheckWorldVisuals();
                Check(CounterPreparationSurfaceChecks.TryGetDraw(opening, out float radius) && radius > preparationRadius,
                    "the field invasion radius grows while the opening is still preparing");
                preparationRadius = radius;
                preparationSurface = CounterPreparationSurfaceChecks.SurfaceFor(opening);
                if (caseIndex >= 4)
                {
                    if (caseIndex == 4) opening.Destroy();
                    else if (caseIndex == 5)
                    {
                        caster!.health.AddHediff(DefDatabase<HediffDef>.GetNamed("Anesthetic"));
                        expectedCasterStunEnd = GenTicks.TicksGame + caster!.stances.stunner.StunTicksLeft;
                        Check(caster.Downed && !caster.Dead, "cancellation fixture is downed by the native anesthetic health effect");
                    }
                    else
                    {
                        field!.RequestReturn("counter check host withdrawal");
                        CheckBloodSeaButton(observer!, true, "RSH_BloodSeaDomainOccupied");
                    }
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    stage = 6;
                    return;
                }
                if (caseIndex == 0 && !reloaded && GenCommandLine.CommandLineArgPassed("cqfcontestloadchecks"))
                {
                    Find.TickManager.Pause();
                    GameComponent_ContestChecks saved = Current.Game.GetComponent<GameComponent_ContestChecks>();
                    saved.Record(host!.thingIDNumber, caster!.thingIDNumber); saved.CounterOpeningCase = caseIndex;
                    GameDataSaveLoader.SaveGame("CQFCounterOpeningChecks");
                    stage = 99;
                    GameDataSaveLoader.LoadGame("CQFCounterOpeningChecks");
                    return;
                }
                Shot("Preparation");
                stage = 3;
            }
            else if (stage == 3)
            {
                if (!preparationLate && !opening!.IsClosing && opening.Progress >= 0.55f)
                {
                    Check(runtime.Transition == null && field!.Challenger == null
                        && CounterPreparationSurfaceChecks.TryGetDraw(opening, out float radius) && radius > preparationRadius,
                        "late preparation expands the ground invasion without creating a challenger or a fullscreen transition");
                    Shot("PreparationLate"); preparationLate = true;
                }
                if (runtime.Transition == null) return;
                Check(preparationEarly && preparationLate, "early and late field effects are both visible before the closing phase");
                transition = runtime.Transition;
                Check(transition.OpeningOwner == opening && field!.Challenger == null && opening!.IsClosing,
                    "fullscreen transfer starts at closure before the challenger exists");
                Check(transition is DomainCounterTransition,
                    "counter expansion continues its own field into fullscreen cover");
                Check(!transition.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Any(field => typeof(RenderTexture).IsAssignableFrom(field.FieldType)),
                    "counter expansion allocates no rectangular source snapshot");
                CheckWorldVisuals();
                Find.TickManager.Pause();
                frozenTick = GenTicks.TicksGame;
                stage = 4;
            }
            else if (stage == 4)
            {
                if (!startShot && transition!.Progress > 0.18f && field!.Challenger == null)
                {
                    Shot("IncursionStart"); startShot = true;
                }
                if (!coverShot && transition!.Progress > 0.33f && field!.Challenger == null)
                {
                    Shot("Cover"); coverShot = true;
                }
                if (!lateShot && transition!.Progress > 0.5f && field!.Challenger == null)
                {
                    Shot("IncursionLate"); lateShot = true;
                }
                if (caster!.Map == field!.map) return;
                Map previousMap = field.map;
                field = caster.Map.GetComponent<MapComponent_Domain>();
                int coveredFrame = (int)typeof(DomainTransition).GetField("coveredFrame", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(transition)!;
                Check(coveredFrame >= 0 && Time.frameCount > coveredFrame && transition!.Progress >= 0.58f,
                    "a fullscreen-covered frame is presented before either caster transfers");
                Check(field.IsBattlefield && field.Challenger != null && field.map.Size == runtime.BattlefieldSize(Existing, Incoming)
                    && !previousMap.GetComponent<MapComponent_Domain>().HasActiveDomain,
                    "counter expansion transfers both instances into a separate dynamically sized battlefield");
                IntVec3 offset = field.map.Center - previousMap.Center;
                Check(field.ArrivalLocked && field.Challenger!.ArrivalLocked && opening!.Destroyed
                    && host!.Position == hostCell + offset && caster.Position == casterCell + offset,
                    "cross-map transfer preserves relative positions and protects both sides under fullscreen cover");
                Check(preparedMaps!.ToHashSet().SetEquals(runtime.DomainMaps) && Find.CurrentMap == field.map,
                    "counter expansion reuses the prewarmed battlefield and domain templates");
                Shot("FullCover");
                stage = 5;
            }
            else if (stage == 5)
            {
                if (runtime.Transition != null)
                {
                    if (!startShot && transition!.Progress > 0.25f) { Shot("FrontStart"); startShot = true; }
                    else if (!coverShot && transition!.Progress > 0.5f) { Shot("FrontMiddle"); coverShot = true; }
                    else if (!lateShot && transition!.Progress > 0.78f) { Shot("FrontEnd"); lateShot = true; }
                }
                if (runtime.Transition != null) return;
                Check(GenTicks.TicksGame == frozenTick && !field!.ArrivalLocked && !field.Challenger!.ArrivalLocked
                    && field.LastTerrainRenderedFrame > 0 && field.Challenger.LastTerrainRenderedFrame > 0,
                    "paused fullscreen reveal finishes after both territories render and release both locks");
                CheckBloodSeaButton(observer!, true, "RSH_BloodSeaContestFull");
                Shot("Battle");
                shotAt = Time.realtimeSinceStartup;
                stage = 7;
            }
            else if (stage == 6)
            {
                if (caseIndex == 6)
                {
                    if (!GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                    Check(!field.map.listerThings.AllThings.OfType<DomainOpening>().Any(item => !item.Destroyed)
                        && caster!.Map == source && observer!.Map == source,
                        "host withdrawal cancels preparation and leaves no pending opening on the hibernated map");
                    Check(CounterPreparationSurfaceChecks.Released(opening!, preparationSurface),
                        "host withdrawal releases the preparation field material");
                    NextCase();
                    return;
                }
                if (!opening!.Destroyed) return;
                Check(CounterPreparationSurfaceChecks.Released(opening, preparationSurface),
                    "cancelled or downed preparation releases its field material");
                Find.TickManager.Pause();
                Check(field!.Challenger == null && runtime.Transition == null && host!.Spawned && observer!.Spawned
                    && observer.stances.stunner.StunTicksLeft == 0
                    && caster!.stances.stunner.StunTicksLeft <= Math.Max(0, expectedCasterStunEnd - GenTicks.TicksGame),
                    "cancelled or downed counter caster leaves no contest or leaked opening locks; challenger="
                        + (field.Challenger != null) + ", transition=" + runtime.Transition?.GetType().Name
                        + ", observer stun=" + observer.stances.stunner.StunTicksLeft + ", caster stun="
                        + caster!.stances.stunner.StunTicksLeft + ", allowed=" + Math.Max(0, expectedCasterStunEnd - GenTicks.TicksGame));
                field.RequestReturn("counter check cancelled cleanup");
                stage = 8;
            }
            else if (stage == 7)
            {
                if (Time.realtimeSinceStartup - shotAt < 0.3f || !File.Exists(ShotPath("Battle"))) return;
                field!.RequestReturn("counter check host cleanup"); field.Challenger!.RequestReturn("counter check challenger cleanup");
                stage = 8;
            }
            else if (stage == 8)
            {
                if (!GameComponent_MapManager.Instance!.IsHibernating(field!.map)) return;
                Check(field.Available && field.Challenger == null && host!.Map == source && caster!.Map == source && observer!.Map == source,
                    "contest cleanup returns all participants and hibernates the reused battlefield");
                NextCase();
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Counter opening checks failed: " + error);
            finished = true;
            Application.Quit(1);
        }
    }

    private void CheckWorldVisuals()
    {
        Type type = opening!.GetType();
        string[] resources = opening is DomainOpening_Basic ? new[] { "visuals" } : new[] { "frontCurtain", "backCurtain", "groundMaterial" };
        Check(resources.All(name => type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(opening) == null)
            && !field!.map.listerThings.AllThings.Any(thing => thing.GetType().Name == "PawnFlyer_BloodSeaOpening"),
            "counter opening allocates no dome or folded pawn holders");
    }

    private void CheckBloodSeaButton(Pawn pawn, bool disabled, string key)
    {
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        AbilityDef definition = DefDatabase<AbilityDef>.GetNamed("RSH_BoundlessBloodSea");
        pawn.abilities.GainAbility(definition);
        bool actual = pawn.abilities.GetAbility(definition).GizmoDisabled(out string reason);
        Check(actual == disabled && (!disabled || reason == key.Translate().ToString()),
            "blood sea button reports the actual domain state: " + key + ", reason=" + reason);
    }

    private static Pawn MakePawn(Map map, IntVec3 near)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            mustBeCapableOfViolence: true));
        GenSpawn.Spawn(pawn, map.GetComponent<MapComponent_Domain>().FindFreeCell(map, near, true), map);
        pawn.drafter.Drafted = true; pawn.drafter.FireAtWill = false;
        return pawn;
    }

    private string ShotPath(string label) => Path.Combine(EvidenceDirectory, "Counter_" + caseIndex + "_" + label + ".png");
    private void Shot(string label) => ScreenCapture.CaptureScreenshot(ShotPath(label));

    private void NextCase()
    {
        Current.Game.CurrentMap = source;
        foreach (Pawn pawn in new[] { host!, caster!, observer! }) if (!pawn.Destroyed) pawn.Destroy();
        if (++caseIndex == 7)
        {
            finished = true; File.AppendAllText(ResultPath, "COMPLETE\n"); Application.Quit(0); return;
        }
        opening = null; field = null; host = caster = observer = null; transition = null;
        startShot = coverShot = lateShot = false; reloaded = false; expectedCasterStunEnd = 0; stage = 0;
        preparationEarly = preparationLate = false; preparationRadius = 0f; preparationSurface = null;
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS case " + caseIndex + " " + message + "\n");
    }

    private Map? source;
    private MapComponent_Domain? field;
    private Pawn? host;
    private Pawn? caster;
    private Pawn? observer;
    private DomainOpening? opening;
    private DomainTransition? transition;
    private Map[]? preparedMaps;
    private IntVec3 hostCell;
    private IntVec3 casterCell;
    private int caseIndex = int.TryParse(Environment.GetEnvironmentVariable("CQF_COUNTER_START_CASE"), out int start) ? start : 0;
    private int stage;
    private int frozenTick;
    private int expectedCasterStunEnd;
    private float startedAt;
    private float shotAt;
    private bool reloaded;
    private bool coverShot;
    private bool startShot;
    private bool lateShot;
    private bool finished;
    private bool preparationEarly;
    private bool preparationLate;
    private float preparationRadius;
    private DomainSurfaceVisuals? preparationSurface;
}
