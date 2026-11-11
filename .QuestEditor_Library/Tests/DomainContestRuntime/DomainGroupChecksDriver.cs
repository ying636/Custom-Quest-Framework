using System.Diagnostics;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class DomainGroupChecksDriver : MonoBehaviour
{
    private string DirectoryPath => Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE")!;
    private string ResultPath => Path.Combine(DirectoryPath, "Groups.txt");

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            if (Time.realtimeSinceStartup > 200f) throw new TimeoutException("Domain group stage=" + stage);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                DomainDef basic = DefDatabase<DomainDef>.GetNamed("CQF_TestDomain");
                if (!runtime.HasAvailable(basic) || !runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain"))) return;
                using Process process = Process.GetCurrentProcess();
                File.AppendAllText(ResultPath, "PROCESS " + process.Id + " " + process.MainModule!.FileName + " " + Environment.CommandLine + "\n");
                Find.TickManager.Pause(); source = Find.CurrentMap;
                for (int index = 0; index < 8; index++)
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
                    IntVec3 cell = source.GetComponent<MapComponent_Domain>().FindFreeCell(source, source.Center + new IntVec3(index % 4 * 2, 0, index < 4 ? -4 : 4), true);
                    GenSpawn.Spawn(pawn, cell, source); pawn.drafter.Drafted = true;
                    pawns.Add(pawn); origins.Add(cell);
                }
                for (int group = 0; group < 2; group++)
                {
                    MapComponent_Domain field = runtime.EnterOrContest(basic, pawns[group * 4], pawns.Skip(group * 4 + 1).Take(3));
                    for (int index = 1; index < 4; index++)
                        field = runtime.EnterOrContest(index % 2 == 0 ? basic : DefDatabase<DomainDef>.GetNamed("CQF_CheckPlainDomain"), pawns[group * 4 + index], Array.Empty<Pawn>()).Battlefield;
                    groups.Add(field);
                    MapComponent_Domain side = field.Instances.ElementAt(1);
                    IntVec3 cell = field.map.AllCells.First(cell => GenAdj.OccupiedRect(cell, Rot4.East, ThingDefOf.Bed.size)
                        .All(part => part.InBounds(field.map) && side.OwnsCell(part) && part.Standable(field.map) && part.GetThingList(field.map).Count == 0));
                    Building bed = (Building)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.Steel);
                    bed.HitPoints -= 17; GenSpawn.Spawn(bed, cell, field.map, Rot4.East);
                    side.RegisterStructure(bed); side.ProtectTerritory(bed.OccupiedRect(), 10000, bed); beds.Add(bed);
                }
                AccessTools.Field(typeof(CQFDomainRuntime), "contestCheckRequested").SetValue(runtime, false);
                Type impactType = AccessTools.TypeByName("RSH.Thing_CrimsonTideImpact");
                impact = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RSH_CrimsonTideImpact"));
                AccessTools.Method(impactType, "Initialize").Invoke(impact, new object[] { pawns[0], Vector3.right, pawns[1] });
                GenSpawn.Spawn(impact, pawns[0].Position, pawns[0].Map);
                impactOrigin = pawns[0].Position;
                impactPath = ((List<IntVec3>)AccessTools.Field(impactType, "path").GetValue(impact)).ToList();
                Current.Game.CurrentMap = source;
                groups[0].RequestReturn("group checks primary withdraws"); stage = 1;
            }
            else if (stage == 1)
            {
                if (groups[0].Active || runtime.Transition != null) return;
                Check(groups[0].Trapped && groups[0].CanAcceptChallenger, "a group with an inactive primary accepts further opponents");
                DomainWorker original = groups[1].Worker!;
                DomainWorker_BattlefieldFailure probe = new();
                AccessTools.Property(typeof(DomainWorker), nameof(DomainWorker.Domain)).SetValue(probe, groups[1]);
                AccessTools.Field(typeof(MapComponent_Domain), "worker").SetValue(groups[1], probe);
                DomainWorker_BattlefieldFailure.FailNext = true;
                bool rejected = false;
                try { runtime.MergeDomains(groups[0], groups[1]); }
                catch (Exception error) { rejected = error.ToString().Contains("CQF_EXPECTED_BATTLEFIELD_ROLLBACK"); }
                Check(rejected && pawns.Select((pawn, index) => pawn.Map == groups[index / 4].map).All(value => value)
                    && groups.All(group => group.InstanceCount == 4 && group.CanAcceptChallenger)
                    && groups[0].Trapped && beds.All(bed => !bed.Destroyed && bed.HitPoints == bed.MaxHitPoints - 17)
                    && groups.All(group => group.Contest!.Partition.Protections.Count == 1),
                    "an injected eight-instance migration failure restores both groups, original buildings, captive state and protections");
                AccessTools.Field(typeof(MapComponent_Domain), "worker").SetValue(groups[1], original);
                probe.Dispose();
                AccessTools.Field(typeof(CQFDomainRuntime), "contestCheckRequested").SetValue(runtime, true);
                stage = 2;
            }
            else if (stage == 2)
            {
                if (runtime.Transition is DomainCounterTransition) sawCover = true;
                field = pawns[0].Map?.GetComponent<MapComponent_Domain>();
                if (field?.InstanceCount != 8 || runtime.Transition != null) return;
                Check(sawCover && field.Trapped && field.ActiveDomainCount == 7 && pawns.All(pawn => pawn.Map == field.map),
                    "two four-instance groups merge under a fullscreen transition with the inactive primary retained");
                Check(groups.All(group => GameComponent_MapManager.Instance!.IsHibernating(group.map)) && field.map.Size.x > groups.Max(group => group.map.Size.x),
                    "group merger grows the battlefield and hibernates both old maps");
                Check(beds.All(bed => !bed.Destroyed && bed.HitPoints == bed.MaxHitPoints - 17)
                    && field.Contest!.Partition.Protections.Count == 2
                    && field.Contest.Partition.Protections.Select(item => item.OwnerIndex).OrderBy(index => index).SequenceEqual(new[] { 1, 5 }),
                    "both original damaged buildings and their separately indexed protections survive a group merger");
                Check(field.Instances.Where(side => side.Active).All(side => side.Anchor?.Domain == side), "all surviving external anchors bind to the corresponding new instance");
                IntVec3 offset = pawns[0].Position - impactOrigin;
                Check(impact!.Map == field.map && (IntVec3)AccessTools.Field(impact.GetType(), "lastCell").GetValue(impact) == pawns[0].Position
                    && ((List<IntVec3>)AccessTools.Field(impact.GetType(), "path").GetValue(impact)).SequenceEqual(impactPath.Select(cell => cell + offset)),
                    "an ongoing blood-sea knockback retains its translated path and target position across expansion");
                Current.Game.CurrentMap = field.map;
                Find.CameraDriver.SetRootPosAndSize(field.map.Center.ToVector3Shifted(), 24f);
                shotFrame = Time.frameCount; stage = 3;
            }
            else if (stage == 3)
            {
                if (Time.frameCount <= shotFrame + 2) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "MergedGroups.png"));
                foreach (MapComponent_Domain side in field!.Instances.Where(side => side.Active).ToList()) side.RequestReturn("group checks cleanup");
                stage = 4;
            }
            else if (stage == 4)
            {
                if (runtime.Transition != null || pawns.Any(pawn => pawn.Map != source)) return;
                Check(pawns.Select((pawn, index) => pawn.Position == origins[index]).All(value => value), "group merger retains all eight original return cells");
                foreach (Pawn pawn in pawns) pawn.Destroy();
                File.AppendAllText(ResultPath, "COMPLETE\n"); finished = true; Application.Quit(0);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n"); Log.Error("[CQF] Domain group checks failed: " + error);
            finished = true; Application.Quit(1);
        }
    }

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    private readonly List<Pawn> pawns = new();
    private readonly List<IntVec3> origins = new();
    private readonly List<MapComponent_Domain> groups = new();
    private readonly List<Building> beds = new();
    private Map? source;
    private MapComponent_Domain? field;
    private int stage;
    private int shotFrame;
    private bool sawCover;
    private bool finished;
    private Thing? impact;
    private IntVec3 impactOrigin;
    private List<IntVec3> impactPath = new();
}
