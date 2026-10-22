using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace CQF.DomainContestRuntimeChecks;

public sealed class EntryChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");
    private bool ChallengesAnchor => caseIndex >= 2;
    private DomainDef Incoming => DefDatabase<DomainDef>.GetNamed(caseIndex % 2 == 0 ? "CQF_TestDomain" : "RSH_BloodSeaDomain");
    private DomainDef Existing => DefDatabase<DomainDef>.GetNamed(caseIndex < 4 || caseIndex == 9 ? "CQF_TestDomain" : "RSH_BloodSeaDomain");

    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup - heartbeat > 3f)
            {
                heartbeat = Time.realtimeSinceStartup;
                File.AppendAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "EntryTrace.txt"),
                    "case=" + caseIndex + " stage=" + stage + " time=" + Time.realtimeSinceStartup + " tick=" + GenTicks.TicksGame
                    + " long=" + LongEventHandler.AnyEventNowOrWaiting + " map=" + Find.CurrentMap + "\n");
            }
            if (stage != 0 && Time.realtimeSinceStartup - startedAt > 70f)
                throw new TimeoutException("Entry checks case " + caseIndex + ", stage " + stage);
            if (LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                if (!runtime.HasAvailable(Incoming) || !runtime.HasAvailable(Existing) || runtime.Transition != null) return;
                Find.TickManager.Pause();
                if (caseIndex >= 8)
                {
                    DefDatabase<DomainDef>.GetNamed("CQF_TestDomain").visuals!.radius = caseIndex % 2 == 0 ? 6f : 32f;
                    DefDatabase<DomainDef>.GetNamed("RSH_BloodSeaDomain").openingRadius = caseIndex % 2 == 0 ? 32f : 6f;
                }
                source = Find.CurrentMap;
                preparedMaps = runtime.DomainMaps.ToArray();
                IntVec3 center = source.Center;
                if (ChallengesAnchor)
                {
                    Pawn owner = MakePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, center + new IntVec3(-18, 0, 0));
                    existing = runtime.EnterOrContest(Existing, owner, Array.Empty<Pawn>());
                    Current.Game.CurrentMap = source;
                }
                caster = MakePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, center);
                guests.Add(MakePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, center + new IntVec3(-2, 0, 0)));
                guests.Add(MakePawn(PawnKindDefOf.Colonist, null, center + new IntVec3(0, 0, -2)));
                guests.Add(MakePawn(DefDatabase<PawnKindDef>.GetNamed("Muffalo"), null, center + new IntVec3(2, 0, 0)));
                guests.Add(MakePawn(DefDatabase<PawnKindDef>.GetNamed("Mech_Scyther"), Faction.OfMechanoids, center + new IntVec3(0, 0, 3)));
                downed = MakePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, center + new IntVec3(3, 0, 3));
                HealthUtility.DamageUntilDowned(downed, false);
                Check(downed.Downed && !downed.Dead, "fixture has a living downed pawn");
                guests.Add(downed);
                outsider = MakePawn(DefDatabase<PawnKindDef>.GetNamed("Muffalo"), null, center + new IntVec3((int)Incoming.OpeningRadius + 8, 0, 0));
                observer = MakePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, center + new IntVec3(-1, 0, -1));
                guests.Add(observer);
                escapee = MakePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, center + new IntVec3((int)Incoming.OpeningRadius - 2, 0, -2));
                foreach (Pawn pawn in created.Where(pawn => pawn != caster && pawn != observer && pawn != escapee && pawn.Map == source))
                    pawn.stances.stunner.StunFor(2400, null, false, false, true);
                AbilityDef ability = DefDatabase<AbilityDef>.GetNamed(caseIndex % 2 == 0 ? "CQF_TestDomainExpansion" : "RSH_BoundlessBloodSea");
                caster.abilities.GainAbility(ability);
                caster.abilities.GetAbility(ability).QueueCastingJob(caster, caster);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                startedAt = Time.realtimeSinceStartup;
                stage = 1;
            }
            else if (stage == 1)
            {
                if (Find.CurrentMap == source) Find.CameraDriver.SetRootPosAndSize((source!.Center + new IntVec3(-6, 0, 0)).ToVector3Shifted(), 25f);
                DomainOpening? opening = source!.listerThings.AllThings.OfType<DomainOpening>().FirstOrDefault(value => value.Caster == caster && value is not DomainOpening_Fusion);
                if (caseIndex == 1 && opening is { IsClosing: false } && opening.Progress > 0.45f && !phaseShot)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "BloodSea_Expansion_FreePawns.png"));
                    phaseShot = true;
                }
                if (opening is { IsClosing: false } && opening.Progress > 0.1f && !freeChecked)
                {
                    Check(observer!.Spawned && observer.stances.stunner.StunTicksLeft == 0 && escapee!.Spawned && escapee.stances.stunner.StunTicksLeft == 0,
                        "expansion leaves surrounding creatures spawned and able to act");
                    escapee.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Goto, source.Center + new IntVec3((int)Incoming.OpeningRadius + 6, 0, -2)));
                    escapee.pather.StopDead();
                    escapee.Position = source.GetComponent<MapComponent_Domain>().FindFreeCell(source,
                        source.Center + new IntVec3((int)Incoming.OpeningRadius + 6, 0, -2), true);
                    freeChecked = true;
                }
                if (opening?.IsClosing == true && !closeChecked)
                {
                    Check(observer!.MapHeld == source && observer.stances.stunner.StunTicksLeft > 0,
                        "closure locks creatures only after the reaction window");
                    Check(escapee!.Spawned && escapee.Position.DistanceToSquared(opening.Position) > opening.Radius * opening.Radius,
                        "a creature that leaves before closure is excluded from capture");
                    closeChecked = true;
                    Pawn duringClosure = MakePawn(DefDatabase<PawnKindDef>.GetNamed("Muffalo"), null, source.Center + new IntVec3(1, 0, 1));
                    guests.Add(duringClosure);
                }
                if (opening?.Progress > 0.35f && !lateGuest)
                {
                    guests.Add(MakePawn(DefDatabase<PawnKindDef>.GetNamed("Muffalo"), null, source.Center + new IntVec3(4, 0, 0)));
                    guests.Last().stances.stunner.StunFor(2400, null, false, false, true);
                    lateGuest = true;
                }
                if (caseIndex >= 8 && opening is { IsClosing: true } && opening.Progress > 0.93f && !closedShot)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Anchor_Closed_" + caseIndex + ".png"));
                    closedShot = true;
                }
                DomainOpening_Fusion? fusion = opening?.Fusion;
                if (ChallengesAnchor && fusion?.Active == true)
                {
                    if (!paired)
                    {
                        Check(fusion.ExistingAnchor == existing!.Anchor && existing.Anchor!.OpeningFusion == fusion
                            && existing.Caster!.Map == existing.map && caster!.MapHeld == source,
                            "new opening pairs with the existing anchor before moving its caster");
                        paired = true;
                    }
                    if (caseIndex == 6 && fusion.Progress > 0.45f)
                    {
                        opening!.Destroy();
                        stage = 4;
                        return;
                    }
                    if (caseIndex == 7 && fusion.Progress > 0.45f && !anchorLost)
                    {
                        existing!.Anchor!.Destroy();
                        anchorLost = true;
                        return;
                    }
                    if (fusion.ContactAge > 0.15f && !contactShot)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Anchor_Contact_" + caseIndex + ".png"));
                        contactShot = true;
                    }
                    if (fusion.ContactAge > 0.85f && !interactionShot)
                    {
                        Check(fusion.VisualBlend == 1f && !existing!.Anchor!.DrawFusion().Equals(default(DomainFusionState)),
                            "existing dome and growing dome share a live erosion field");
                        ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Anchor_Interaction_" + caseIndex + ".png"));
                        interactionShot = true;
                    }
                }
                if (runtime.Transition != null)
                {
                    transitions.Add(runtime.Transition);
                    if (runtime.Transition.Progress > 0.35f && !coverShot)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(GenFilePaths.SaveDataFolderPath, "Entry_Cover_" + caseIndex + ".png"));
                        coverShot = true;
                    }
                }
                if (caster!.Map == source || !caster.Spawned) return;
                entered = caster.Map.GetComponent<MapComponent_Domain>().FindDomainFor(caster)!;
                Check(runtime.Transition != null && runtime.Transition.Progress >= 0.42f,
                    "entry transfers only beneath the full-screen cover");
                if (ChallengesAnchor && !anchorLost)
                    Check(paired && contactShot && interactionShot && runtime.Transition is DomainFusionTransition
                        && entered.Battlefield == existing && existing!.Challenger == entered,
                        "anchor challenge directly joins the existing map after its interaction animation");
                if (anchorLost)
                    Check(entered.Battlefield.Challenger == null && runtime.Transition is not DomainFusionTransition,
                        "removing the challenged anchor lets the incoming domain finish its original opening");
                stage = 2;
            }
            else if (stage == 2)
            {
                if (runtime.Transition != null) return;
                Find.TickManager.Pause();
                Check(lateGuest && guests.All(pawn => pawn.Spawned && !pawn.Dead && pawn.Map == entered!.map)
                    && outsider!.Map == source && downed!.Downed && escapee!.Map == source && freeChecked && closeChecked,
                    "all friendly, neutral, wild, mechanoid, downed and late-arriving creatures enter; outside creatures stay: "
                        + string.Join("; ", guests.Select(pawn => pawn.ThingID + " map=" + pawn.Map + " held=" + pawn.MapHeld + " dead=" + pawn.Dead + " pos=" + pawn.PositionHeld))
                        + "; outside=" + outsider!.Map + "; escape=" + escapee!.Map + "; downed=" + downed!.Downed
                        + "; free=" + freeChecked + "; closed=" + closeChecked);
                Check(guests.Where(pawn => !pawn.HostileTo(caster)).All(pawn => pawn.GetLord()?.LordJob is not LordJob_AssaultColony),
                    "captured non-hostiles do not receive assault AI");
                MapComponent_Domain field = entered!.Battlefield;
                Check(guests.All(pawn => field.Participants.Count(record => record.pawn == pawn)
                    + (field.Challenger?.Participants.Count(record => record.pawn == pawn) ?? 0) == 1),
                    "each captured creature has exactly one return record");
                Check(runtime.DomainMaps.Count == preparedMaps.Length && runtime.DomainMaps.All(preparedMaps.Contains)
                    && transitions.Count == 1 && !entered.ArrivalLocked && !field.ArrivalLocked,
                    "entry reuses prepared maps and releases a single transition without loading another map");
                Current.Game.CurrentMap = source;
                field.RequestReturn("entry checks");
                field.Challenger?.RequestReturn("entry checks");
                stage = 3;
            }
            else if (stage == 3)
            {
                if (entered!.Battlefield.HasActiveDomain || runtime.Transition != null || created.Any(pawn => pawn.Map != source)) return;
                Check(guests.All(pawn => pawn.Position == origins[pawn]), "every captured creature returns to its source position");
                Check(!source!.listerThings.AllThings.OfType<DomainOpening>().Any(), "entry leaves no opening or anchor challenge coordinator behind");
                foreach (Pawn pawn in created) pawn.Destroy();
                created.Clear(); guests.Clear(); origins.Clear(); transitions.Clear();
                existing = entered = null;
                paired = lateGuest = contactShot = interactionShot = coverShot = anchorLost = freeChecked = closeChecked = closedShot = false;
                if (++caseIndex == (int.TryParse(Environment.GetEnvironmentVariable("CQF_ENTRY_END_CASE"), out int endCase) ? endCase : 10)) { Finish(true); return; }
                stage = 0;
            }
            else if (stage == 4)
            {
                if (source!.listerThings.AllThings.OfType<DomainOpening>().Any()) return;
                Find.TickManager.Pause();
                Check(existing!.CanAcceptChallenger && existing.Anchor!.OpeningFusion == null && existing.Caster!.Map == existing.map
                    && caster!.Map == source && guests.All(pawn => pawn.Map == source),
                    "cancelling the incoming opening releases the anchor reservation without trapping or moving anyone");
                entered = existing;
                existing.RequestReturn("cancelled entry checks");
                stage = 3;
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Entry runtime checks failed: " + error);
            Finish(false);
        }
    }

    private Pawn MakePawn(PawnKindDef kind, Faction? faction, IntVec3 near)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
        GenSpawn.Spawn(pawn, source!.GetComponent<MapComponent_Domain>().FindFreeCell(source, near, true), source);
        if (pawn.RaceProps.Humanlike) pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        if (pawn.drafter != null)
        {
            pawn.drafter.Drafted = true;
            pawn.drafter.FireAtWill = false;
        }
        created.Add(pawn);
        origins.Add(pawn, pawn.Position);
        return pawn;
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Entry case " + caseIndex + ": " + message);
        File.AppendAllText(ResultPath, "PASS Entry case " + caseIndex + ": " + message + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private readonly List<Pawn> created = new();
    private readonly List<Pawn> guests = new();
    private readonly Dictionary<Pawn, IntVec3> origins = new();
    private readonly HashSet<DomainTransition> transitions = new();
    private Map[] preparedMaps = Array.Empty<Map>();
    private Map? source;
    private MapComponent_Domain? existing;
    private MapComponent_Domain? entered;
    private Pawn? caster;
    private Pawn? outsider;
    private Pawn? downed;
    private Pawn? observer;
    private Pawn? escapee;
    private int caseIndex = int.TryParse(Environment.GetEnvironmentVariable("CQF_ENTRY_START_CASE"), out int initialCase) ? initialCase : 0;
    private int stage;
    private float startedAt;
    private float heartbeat;
    private bool paired;
    private bool lateGuest;
    private bool contactShot;
    private bool interactionShot;
    private bool coverShot;
    private bool anchorLost;
    private bool freeChecked;
    private bool closeChecked;
    private bool closedShot;
    private bool phaseShot;
    private bool finished;
}
