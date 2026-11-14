using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class DomainChangesChecksDriver : MonoBehaviour
{
    private string DirectoryPath => Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE")!;
    private string ResultPath => Path.Combine(DirectoryPath, "DomainChanges.txt");

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            if (Time.realtimeSinceStartup > 180f) throw new TimeoutException("Domain changes stage=" + stage + " case=" + caseIndex);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                using System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                File.AppendAllText(ResultPath, "PROCESS " + process.Id + " " + process.MainModule!.FileName + " " + Environment.CommandLine + "\n");
                source = Find.CurrentMap;
                Find.TickManager.Pause();
                caster = MakePawn(source, source.Center);
                field = runtime.Acquire(DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain"), caster).GetComponent<MapComponent_Domain>();
                DomainMaterialTransition transition = new(field.Definition.visuals!, caster.Position, false);
                runtime.SetTransition(transition);
                field.Enter(caster, Array.Empty<Pawn>());
                transition.Bind(field);
                field.map.mapDrawer.RegenerateEverythingNow();
                SetTerrain(field.map, true, false);
                startedAt = Time.realtimeSinceStartup;
                stage = 1;
            }
            else if (stage == 1)
            {
                if (Find.CurrentMap != field!.map) return;
                Find.CameraDriver.SetRootPosAndSize(field.map.Center.ToVector3Shifted(), 28f);
                if (Time.realtimeSinceStartup - startedAt < 1.5f) return;
                Check(runtime.Transition != null && field.ArrivalLocked && field.LastTerrainRenderedFrame < 0,
                    "unsubmitted terrain keeps the entry covered and participants locked");
                SetTerrain(field.map, false, true);
                startedAt = Time.realtimeSinceStartup;
                stage = 2;
            }
            else if (stage == 2)
            {
                if (runtime.Transition != null) return;
                Check(!field!.ArrivalLocked && field.LastTerrainRenderedFrame < 0 && Time.realtimeSinceStartup - startedAt < 3f,
                    "entry detects submitted terrain outside the empty center without a worker report or timeout");
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "TerrainFallback.png"));
                SetTerrain(source!, false, true);
                field.RequestReturn("domain changes checks");
                stage = 3;
            }
            else if (stage == 3)
            {
                if (runtime.Transition != null || caster!.Map != source || field!.Active) return;
                Check(!source!.GetComponent<MapComponent_DomainTransitionLock>().Locked,
                    "visible return detects terrain outside the empty center and releases its lock");
                SetTerrain(source, false, false);
                caster.Destroy();
                stage = 4;
            }
            else if (stage == 4)
            {
                if (caseIndex == 8)
                {
                    if (host != null) { Current.Game.CurrentMap = source; host.RequestReturn("domain changes cleanup"); }
                    stage = 10;
                    return;
                }
                bool counter = caseIndex is 4 or 5;
                if (counter && host == null)
                {
                    Pawn owner = MakePawn(source!, source!.Center);
                    host = runtime.EnterOrContest(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"), owner, Array.Empty<Pawn>());
                }
                Map testMap = counter ? host!.map : source!;
                Current.Game.CurrentMap = testMap;
                IntVec3 center = counter ? testMap.Center : new IntVec3(25, 0, 25);
                caster = MakePawn(testMap, center);
                guest = MakePawn(testMap, center + new IntVec3(2, 0, 0));
                if (caseIndex is 2 or 3 or 5 or 7)
                {
                    opening = (DomainOpening)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RSH_BloodSeaDomainOpening"));
                    GenSpawn.Spawn(opening, caster.Position, testMap);
                    opening.GetType().GetMethod("Initialize")!.Invoke(opening, new object[] { caster });
                    Comp_DomainProtection.Ensure(caster);
                }
                else opening = DomainOpening_Basic.Start(DefDatabase<DomainDef>.GetNamed("CQF_TestDomain"), caster);
                typeof(DomainOpening).GetField("startTick", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(opening, GenTicks.TicksGame - opening.OpeningTicks / 2);
                if (caseIndex is 1 or 3 or 6 or 7) opening.BeginClosing();
                if (caseIndex >= 6) opening.CompleteOpening();
                Check(opening.IsCounterOpening == counter && caster.Spawned, "case " + caseIndex + " caster remains targetable during preparation or closure");
                Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 22f);
                frame = Time.frameCount;
                stage = 5;
            }
            else if (stage == 5)
            {
                if (Time.frameCount <= frame + 2) return;
                if (caseIndex >= 6 && runtime.Transition?.Progress < 0.12f) return;
                DamageInfo hit = new(DamageDefOf.Bullet, 100f);
                Comp_DomainProtection.Ensure(caster!).PostPreApplyDamage(ref hit, out bool absorbed);
                Check(!absorbed && hit.Amount == 50f, "case " + caseIndex + " opening takes exactly half damage");
                runtime.QueueOpening(opening!);
                caster!.TakeDamage(new DamageInfo(DamageDefOf.Bullet, 10000f, 100f, -1f, null, caster.health.hediffSet.GetBrain()));
                Check(caster.Dead, "case " + caseIndex + " a lethal attack kills the caster through opening protection");
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                cancelledAt = GenTicks.TicksGame;
                stage = 6;
            }
            else if (stage == 6)
            {
                if (GenTicks.TicksGame < cancelledAt + 12) return;
                Check(opening is { IsDissolving: true, Active: false, Destroyed: false } && opening.Opacity is > 0f and < 1f,
                    "case " + caseIndex + " death cancels gameplay while retaining a fading effect");
                Check(guest!.Spawned && guest.Map == opening!.Map && guest.stances.stunner.StunTicksLeft == 0,
                    "case " + caseIndex + " cancellation releases captured guests and their temporary stun");
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "CancelEarly_" + caseIndex + ".png"));
                previousOpacity = opening!.Opacity;
                stage = 7;
            }
            else if (stage == 7)
            {
                if (!middleShot && GenTicks.TicksGame >= cancelledAt + 30)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "CancelMiddle_" + caseIndex + ".png"));
                    middleShot = true;
                }
                if (GenTicks.TicksGame < cancelledAt + 42) return;
                Check(!opening!.Destroyed && opening.Opacity > 0f && opening.Opacity < previousOpacity,
                    "case " + caseIndex + " the effect opacity decreases continuously");
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "CancelLate_" + caseIndex + ".png"));
                stage = 8;
            }
            else if (stage == 8)
            {
                if (!opening!.Destroyed) return;
                Check(runtime.Transition == null && guest!.Spawned && caster!.Dead && (caseIndex is not (4 or 5) || host!.Active),
                    "case " + caseIndex + " fade completes without entering the cancelled domain or ending its host");
                guest!.Destroy();
                caseIndex++;
                middleShot = false;
                stage = 4;
            }
            else if (stage == 10)
            {
                if (runtime.Transition != null || host?.Active == true) return;
                File.AppendAllText(ResultPath, "COMPLETE\n");
                finished = true;
                Application.Quit(0);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Domain changes checks failed: " + error);
            finished = true;
            Application.Quit(1);
        }
    }

    private Pawn MakePawn(Map map, IntVec3 cell)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
        GenSpawn.Spawn(pawn, map.GetComponent<MapComponent_Domain>().FindFreeCell(map, cell, true), map);
        pawn.drafter.Drafted = true;
        pawn.drafter.FireAtWill = false;
        return pawn;
    }

    private void SetTerrain(Map map, bool disableAll, bool disableCenter)
    {
        Section center = map.mapDrawer.SectionAt(map.Center);
        for (int x = 0; x < map.Size.x; x += Section.Size)
            for (int z = 0; z < map.Size.z; z += Section.Size)
            {
                Section section = map.mapDrawer.SectionAt(new IntVec3(x, 0, z));
                foreach (LayerSubMesh mesh in section.GetLayer(typeof(SectionLayer_Terrain)).subMeshes)
                    mesh.disabled = disableAll || disableCenter && ReferenceEquals(section, center);
            }
    }

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private Map? source;
    private MapComponent_Domain? field;
    private MapComponent_Domain? host;
    private Pawn? caster;
    private Pawn? guest;
    private DomainOpening? opening;
    private int stage;
    private int caseIndex;
    private int frame;
    private int cancelledAt;
    private float startedAt;
    private float previousOpacity;
    private bool finished;
    private bool middleShot;
}
