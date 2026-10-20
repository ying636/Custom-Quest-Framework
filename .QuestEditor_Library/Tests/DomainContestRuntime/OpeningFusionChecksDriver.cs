using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class OpeningFusionChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private string CaseName => CaseNames[caseIndex];

    private void Update()
    {
        if (finished) return;
        try
        {
            if (stage != 0 && Time.realtimeSinceStartup - caseStarted > 90f)
                throw new TimeoutException(CaseName + " stage " + stage + ", first=" + firstPawn?.MapHeld + ", second=" + secondPawn?.MapHeld
                    + ", current=" + Current.Game?.CurrentMap + ", longEvent=" + LongEventHandler.AnyEventNowOrWaiting);
            if (LongEventHandler.AnyEventNowOrWaiting || Current.Game?.CurrentMap == null) return;
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                if (Current.Game?.CurrentMap == null || runtime.Transition != null) return;
                if (!runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"))
                    || !runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain"))) return;
                if (GenCommandLine.CommandLineArgPassed("cqflayoutchecks") && !runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain"))) return;
                preparedMaps = runtime.DomainMaps.ToArray();
                source ??= Find.CurrentMap;
                firstPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                secondPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                IntVec3 origin = source.mapPawns.FreeColonistsSpawned.First().Position;
                GenSpawn.Spawn(firstPawn, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, origin, true), source);
                GenSpawn.Spawn(secondPawn, source.GetComponent<MapComponent_Domain>().FindFreeCell(source,
                    firstPawn.Position + new IntVec3(CaseName == "Distant" ? 36 : 8, 0, 0), true), source);
                firstPawn.abilities ??= new Pawn_AbilityTracker(firstPawn);
                secondPawn.abilities ??= new Pawn_AbilityTracker(secondPawn);
                firstPawn.drafter.Drafted = secondPawn.drafter.Drafted = true;
                firstDefinition = DefDatabase<DomainDef>.GetNamed(CaseName.StartsWith("Plain") ? "CQF_CheckPlainDomain"
                    : CaseName.StartsWith("Blood") ? "RSH_BloodSeaDomain" : "CQF_TestDomain");
                secondDefinition = DefDatabase<DomainDef>.GetNamed(CaseName.EndsWith("Plain") ? "CQF_CheckPlainDomain"
                    : CaseName.EndsWith("Blood") || CaseName == "Cancel" ? "RSH_BloodSeaDomain" : "CQF_TestDomain");
                firstAbility = DefDatabase<AbilityDef>.GetNamed(firstDefinition.defName == "RSH_BloodSeaDomain" ? "RSH_BoundlessBloodSea" : "CQF_TestDomainExpansion");
                secondAbility = DefDatabase<AbilityDef>.GetNamed(secondDefinition.defName == "RSH_BloodSeaDomain" ? "RSH_BoundlessBloodSea" : "CQF_TestDomainExpansion");
                firstPawn.abilities.GainAbility(firstAbility);
                secondPawn.abilities.GainAbility(secondAbility);
                StartOpening(firstPawn, firstDefinition, firstAbility);
                secondQueued = CaseName != "LateJoin";
                if (secondQueued) StartOpening(secondPawn, secondDefinition, secondAbility);
                Check((firstDefinition.surface == null || firstPawn.CurJob?.ability?.def == firstAbility)
                    && (!secondQueued || secondDefinition.surface == null || secondPawn.CurJob?.ability?.def == secondAbility),
                    "public opening entry points start both domains");
                Find.Selector.ClearSelection();
                Find.Selector.Select(firstPawn);
                sourceFocus = firstPawn.Position + new IntVec3(4, 0, 0);
                Find.CameraDriver.SetRootPosAndSize(sourceFocus.ToVector3Shifted(), 18f);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                caseStarted = Time.realtimeSinceStartup;
                stage = 1;
            }
            else if (stage == 1)
            {
                if (Find.CurrentMap == source) Find.CameraDriver.SetRootPosAndSize(sourceFocus.ToVector3Shifted(), 18f);
                if (!secondQueued && runtime.Transition?.OpeningOwner != null)
                {
                    lateOpening = runtime.Transition.OpeningOwner;
                    Check(runtime.CanBeginOpening(secondPawn!), "second caster may join an entry transition before its map switch");
                    secondPawn!.abilities.GetAbility(secondAbility!).QueueCastingJob(secondPawn, secondPawn);
                    secondQueued = true;
                }
                DomainOpening_Fusion? fusion = source!.listerThings.AllThings.OfType<DomainOpening_Fusion>().FirstOrDefault();
                if (fusion != null)
                {
                    if (!paired)
                    {
                        paired = true;
                        Check(fusion.First.Fusion == fusion && fusion.Second.Fusion == fusion && firstPawn!.MapHeld == source && secondPawn!.MapHeld == source,
                            "both live openings share one fusion before either caster leaves the source");
                        if (lateOpening != null) Check(!lateOpening.Destroyed, "joining detaches the old transition without destroying its opening");
                        if (CaseName == "Cancel")
                        {
                            DomainOpening cancelled = fusion.First.Caster == secondPawn ? fusion.First : fusion.Second;
                            cancelled.Destroy();
                            cancelledPair = true;
                        }
                    }
                    if (fusion.Active && fusion.Progress > 0.6f && !openingShot)
                    {
                        openingShot = true;
                        Shot("Opening");
                    }
                    if (fusion.Active && fusion.ContactAge > 0.05f && !contactShot)
                    {
                        contactShot = true;
                        Shot("Contact");
                    }
                    if (fusion.Active && fusion.ContactAge > 0.85f && !interactionShot)
                    {
                        Check(fusion.VisualBlend == 1f, "contact erosion reaches full strength before the screen transition");
                        interactionShot = true;
                        Shot("Interaction");
                    }
                    if (fusion.Active && fusion.Progress > 0.9f && !domeShot)
                    {
                        domeShot = true;
                        Shot("DomeInteraction");
                    }
                }
                if (runtime.Transition is DomainFusionTransition merged)
                {
                    transitions.Add(merged);
                    if (merged.Progress > 0.3f && merged.Progress < 0.42f && !coverShot)
                    {
                        coverShot = true;
                        Shot("Cover");
                    }
                }
                MapComponent_Domain? first = runtime.GetInstances(firstDefinition!)
                    .FirstOrDefault(domain => domain.Active && domain.Caster == firstPawn);
                if (first == null) return;
                if (CaseName == "Distant")
                {
                    MapComponent_Domain? second = runtime.GetInstances(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"))
                        .FirstOrDefault(domain => domain.Active && domain.Caster == secondPawn);
                    if (second == null || runtime.Transition != null) return;
                    Check(!paired && first.map != second.map, "non-overlapping openings remain separate domains");
                    battle = first.Battlefield;
                    separateSecond = second;
                }
                else if (CaseName == "Cancel")
                {
                    if (runtime.Transition != null) return;
                    Check(cancelledPair && first.Battlefield.Challenger == null && secondPawn!.Map == source,
                        "cancelling one opening releases the survivor to complete alone");
                    battle = first.Battlefield;
                }
                else
                {
                    battle = first.Battlefield;
                    Check(paired && battle.Challenger?.Active == true && firstPawn!.Map == battle.map && secondPawn!.Map == battle.map,
                        "one direct transfer starts a shared contest with both casters");
                    Check(transitions.Count == 1 && runtime.Transition is DomainFusionTransition && runtime.Transition.Progress >= 0.42f,
                        "only one fused screen transition transfers after full cover");
                    Check(battle.FindDomainFor(firstPawn!)!.SourceMap == source && battle.FindDomainFor(secondPawn!)!.SourceMap == source
                        && battle.FindDomainFor(firstPawn!)!.Anchor?.Spawned == true && battle.FindDomainFor(secondPawn!)!.Anchor?.Spawned == true,
                        "both instances retain their source map and external anchors");
                    Check(battle.ArrivalLocked && battle.Challenger!.ArrivalLocked,
                        "both sides stay protected while the merged scene reveals");
                }
                Check(runtime.DomainMaps.Count == preparedMaps.Length && runtime.DomainMaps.All(preparedMaps.Contains),
                    "opening and contest reuse pre-generated maps without creating another map");
                stage = 2;
                enteredAt = Time.realtimeSinceStartup;
                frames = 0;
            }
            else if (stage == 2)
            {
                if (Find.CurrentMap == battle!.map) Find.CameraDriver.SetRootPosAndSize(battle.map.Center.ToVector3Shifted(), 18f);
                if (runtime.Transition != null || ++frames < 3) return;
                if (!battleShot)
                {
                    Check(Time.realtimeSinceStartup - enteredAt < 4f, "scene becomes ready without the terrain timeout fallback");
                    Check(!battle!.ArrivalLocked && battle.Challenger?.ArrivalLocked != true, "shared reveal releases both arrival locks");
                    Shot("Battle");
                    battleShot = true;
                    return;
                }
                Current.Game.CurrentMap = source;
                Find.CameraDriver.SetRootPosAndSize(sourceFocus.ToVector3Shifted(), 18f);
                sourceViewedTick = GenTicks.TicksGame;
                Find.Selector.ClearSelection();
                frames = 0;
                stage = 4;
            }
            else if (stage == 4)
            {
                Find.CameraDriver.SetRootPosAndSize(sourceFocus.ToVector3Shifted(), 18f);
                if (GenTicks.TicksGame - sourceViewedTick < 30) return;
                Shot("SourceAnchors");
                stage = 5;
            }
            else if (stage == 5)
            {
                Find.CameraDriver.SetRootPosAndSize(sourceFocus.ToVector3Shifted(), 18f);
                if (GenTicks.TicksGame - sourceViewedTick < 90) return;
                Shot("SourceInteraction");
                battle.FindDomainFor(firstPawn!)!.RequestReturn("fusion checks");
                battle.FindDomainFor(secondPawn!)?.RequestReturn("fusion checks");
                separateSecond?.RequestReturn("fusion checks");
                stage = 3;
            }
            else if (stage == 3)
            {
                if (runtime.Transition != null || firstPawn!.Map != source || secondPawn!.Map != source || battle!.HasActiveDomain) return;
                Check(!source!.listerThings.AllThings.OfType<DomainOpening>().Any(), "return leaves no stale openings or fusion coordinator");
                firstPawn.Destroy();
                secondPawn.Destroy();
                caseIndex++;
                if (caseIndex == CaseNames.Length) { Finish(true); return; }
                paired = openingShot = coverShot = cancelledPair = battleShot = contactShot = interactionShot = domeShot = false;
                lateOpening = null;
                separateSecond = null;
                transitions.Clear();
                stage = 0;
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Opening fusion checks failed: " + error);
            Finish(false);
        }
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(CaseName + ": " + message);
        File.AppendAllText(ResultPath, "PASS " + CaseName + ": " + message + "\n");
    }

    private void StartOpening(Pawn pawn, DomainDef definition, AbilityDef ability)
    {
        if (definition.surface == null) DomainOpening_Basic.Start(definition, pawn, Array.Empty<Pawn>());
        else pawn.abilities.GetAbility(ability).QueueCastingJob(pawn, pawn);
    }

    private void Shot(string phase)
    {
        File.AppendAllText(ResultPath, "SHOT " + CaseName + ": " + phase + ", focus=" + sourceFocus + ", camera=" + Find.Camera.transform.position
            + ", view=" + Find.CameraDriver.CurrentViewRect + ", first=" + firstPawn!.PositionHeld + ", second=" + secondPawn!.PositionHeld + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, CaseName + "_" + phase + ".png"));
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private static string[] CaseNames => GenCommandLine.CommandLineArgPassed("cqflayoutchecks") ? LayoutCases : DefaultCases;
    private static readonly string[] DefaultCases = { "BasicBasic", "BasicBlood", "BloodBasic", "BloodBlood", "LateJoin", "Cancel", "Distant" };
    private static readonly string[] LayoutCases = { "PlainBasic", "BasicPlain", "PlainBlood", "BloodPlain", "PlainPlain" };
    private readonly HashSet<DomainTransition> transitions = new();
    private Map[] preparedMaps = Array.Empty<Map>();
    private Map? source;
    private IntVec3 sourceFocus;
    private MapComponent_Domain? battle;
    private MapComponent_Domain? separateSecond;
    private Pawn? firstPawn;
    private Pawn? secondPawn;
    private AbilityDef? firstAbility;
    private AbilityDef? secondAbility;
    private DomainDef? firstDefinition;
    private DomainDef? secondDefinition;
    private DomainOpening? lateOpening;
    private int caseIndex;
    private int stage;
    private int frames;
    private int sourceViewedTick;
    private float caseStarted;
    private float enteredAt;
    private bool secondQueued;
    private bool paired;
    private bool openingShot;
    private bool contactShot;
    private bool interactionShot;
    private bool domeShot;
    private bool coverShot;
    private bool battleShot;
    private bool cancelledPair;
    private bool finished;
}
