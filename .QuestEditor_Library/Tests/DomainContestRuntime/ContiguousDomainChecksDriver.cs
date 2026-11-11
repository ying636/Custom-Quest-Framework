using System.Diagnostics;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class ContiguousDomainChecksDriver : MonoBehaviour
{
    private string DirectoryPath => Environment.GetEnvironmentVariable("CQF_COUNTER_EVIDENCE")!;
    private string ResultPath => Path.Combine(DirectoryPath, "ContiguousLab.txt");

    public void Resume(int casterId)
    {
        field = CQFDomainRuntime.Instance.DomainMaps.Select(map => map.GetComponent<MapComponent_Domain>()).Single(side => side.Caster?.thingIDNumber == casterId);
        source = field.SourceMap;
        actors = field.Instances.Select(side => side.Caster!).ToList();
        settledPositions = actors.Select(pawn => pawn.Position).ToArray();
        Find.TickManager.Pause();
        Find.Camera.farClipPlane = 1000f;
        Find.CameraDriver.SetRootPosAndSize(field.map.Center.ToVector3Shifted(), field.map.Size.z * 0.52f);
        int[] expected = File.ReadAllLines(Path.Combine(DirectoryPath, "Owners.txt")).Select(int.Parse).ToArray();
        Check(field.map.AllCells.Select(field.Contest!.Partition.OwnerIndex).SequenceEqual(expected), "complete game reload retains the exact connected partition");
        DomainContiguousRuntimeChecks.Validate(field, Check);
        stage = 4; frame = Time.frameCount;
    }

    private void Update()
    {
        if (finished || stage == 99 || LongEventHandler.AnyEventNowOrWaiting || Find.CurrentMap == null) return;
        try
        {
            if (Time.realtimeSinceStartup > 240f) throw new TimeoutException("Contiguous laboratory stage=" + stage);
            CQFDomainRuntime runtime = CQFDomainRuntime.Instance;
            if (stage == 0)
            {
                if (DomainNames.Any(name => !runtime.HasAvailable(DefDatabase<DomainDef>.GetNamed(name)))) return;
                using Process process = Process.GetCurrentProcess();
                File.AppendAllText(ResultPath, "PROCESS " + process.Id + " " + process.MainModule!.FileName + " " + Environment.CommandLine + "\n");
                Find.TickManager.Pause(); source = Find.CurrentMap;
                for (int index = 0; index < DomainNames.Length; index++)
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, mustBeCapableOfViolence: true));
                    GenSpawn.Spawn(pawn, source.GetComponent<MapComponent_Domain>().FindFreeCell(source, source.Center + new IntVec3(index * 2, 0, 0), true), source);
                    pawn.drafter.Drafted = true; pawn.drafter.FireAtWill = false; actors.Add(pawn);
                }
                File.WriteAllLines(Path.Combine(DirectoryPath, "Origins.txt"), actors.Select(pawn => pawn.Position.x + "|" + pawn.Position.z));
                field = runtime.EnterOrContest(DefDatabase<DomainDef>.GetNamed(DomainNames[0]), actors[0], actors.Skip(1));
                for (int index = 1; index < DomainNames.Length; index++)
                    field = runtime.EnterOrContest(DefDatabase<DomainDef>.GetNamed(DomainNames[index]), actors[index], Array.Empty<Pawn>()).Battlefield;
                Find.TickManager.DebugSetTicksGame(Math.Max(GenTicks.TicksGame, 1801));
                foreach (MapComponent_Domain side in field.Instances)
                    HarmonyLib.AccessTools.Field(side.Worker!.GetType(), "epoch").SetValue(side.Worker, GenTicks.TicksGame - 1800);
                settledPositions = actors.Select(pawn => pawn.Position).ToArray();
                float[] health = { 76391f, 76691f, 40510f, 88275f };
                foreach (MapComponent_Domain side in field.Instances) side.DrainHealth(side.Health - health[side.InstanceIndex]);
                field.Contest!.ResolveSurvivor(field);
                Check(field.InstanceCount == 4, "the exact four domain types from the reported scene share one battlefield");
                DomainContiguousRuntimeChecks.Validate(field, Check);
                CheckPositions();
                Current.Game.CurrentMap = field.map;
                Find.Camera.farClipPlane = 1000f;
                Find.CameraDriver.SetRootPosAndSize(field.map.Center.ToVector3Shifted(), field.map.Size.z * 0.52f);
                stage = 1; frame = Time.frameCount;
            }
            else if (stage == 1)
            {
                if (Time.frameCount <= frame + 3) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "FourDomains_ReportedHealth.png"));
                stage = 10; frame = Time.frameCount;
            }
            else if (stage == 10)
            {
                if (Time.frameCount <= frame + 2) return;
                foreach (MapComponent_Domain side in field!.Instances) side.RestoreHealth(side.MaximumHealth);
                MapComponent_Domain weak = field.Instances.ElementAt(2); weak.DrainHealth(weak.Health - 1000f);
                field.Contest!.ResolveSurvivor(field);
                DomainContiguousRuntimeChecks.Validate(field, Check);
                Check(field.map.AllCells.Where(weak.OwnsCell).All(cell => cell.x > 0 && cell.z > 0 && cell.x < field.map.Size.x - 1 && cell.z < field.map.Size.z - 1),
                    "the weak ossuary domain becomes a single enclosed island");
                CheckPositions(); stage = 2; frame = Time.frameCount;
            }
            else if (stage == 2)
            {
                if (Time.frameCount <= frame + 3) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "FourDomains_WeakIsland.png"));
                stage = 20; frame = Time.frameCount;
            }
            else if (stage == 20)
            {
                if (Time.frameCount <= frame + 2) return;
                foreach (MapComponent_Domain side in field!.Instances) side.RestoreHealth(side.MaximumHealth);
                field.Contest!.ResolveSurvivor(field);
                DomainContiguousRuntimeChecks.Validate(field, Check);
                CheckPositions(); stage = 3; frame = Time.frameCount;
            }
            else if (stage == 3)
            {
                if (Time.frameCount <= frame + 3) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "FourDomains_Recovered.png"));
                stage = 30; frame = Time.frameCount;
            }
            else if (stage == 30)
            {
                if (Time.frameCount <= frame + 2) return;
                File.WriteAllLines(Path.Combine(DirectoryPath, "Owners.txt"), field!.map.AllCells.Select(cell => field.Contest!.Partition.OwnerIndex(cell).ToString()));
                Current.Game.GetComponent<GameComponent_ContestChecks>().Record(actors[0].thingIDNumber, actors[3].thingIDNumber);
                stage = 99; GameDataSaveLoader.SaveGame("CQFContiguousDomains"); GameDataSaveLoader.LoadGame("CQFContiguousDomains");
            }
            else if (stage == 4)
            {
                if (Time.frameCount <= frame + 3) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(DirectoryPath, "FourDomains_Reloaded.png"));
                stage = 40; frame = Time.frameCount;
            }
            else if (stage == 40)
            {
                if (Time.frameCount <= frame + 2) return;
                foreach (MapComponent_Domain side in field!.Instances.ToList()) side.RequestReturn("contiguous checks cleanup");
                stage = 5;
            }
            else if (stage == 5)
            {
                if (runtime.Transition != null || actors.Any(pawn => pawn.Map != source)) return;
                IntVec3[] origins = File.ReadAllLines(Path.Combine(DirectoryPath, "Origins.txt")).Select(line => line.Split('|')).Select(parts => new IntVec3(int.Parse(parts[0]), 0, int.Parse(parts[1]))).ToArray();
                Check(actors.Select((pawn, index) => pawn.Position == origins[index]).All(value => value), "all four casters return to their original source cells");
                foreach (Pawn pawn in actors) pawn.Destroy();
                File.AppendAllText(ResultPath, "COMPLETE\n"); finished = true; Application.Quit(0);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n"); Log.Error("[CQF] Contiguous laboratory checks failed: " + error);
            finished = true; Application.Quit(1);
        }
    }

    private void CheckPositions() => Check(actors.Select((pawn, index) => pawn.Position == settledPositions![index]).All(value => value), "health-driven boundary updates do not move any pawn");

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        File.AppendAllText(ResultPath, "PASS " + message + "\n");
    }

    public static readonly string[] DomainNames = { "CQFDL_GoldenSanctum", "CQFDL_Starfall", "CQFDL_Ossuary", "CQFDL_MirrorCorridor" };
    private Map? source;
    private MapComponent_Domain? field;
    private List<Pawn> actors = new();
    private IntVec3[]? settledPositions;
    private int stage;
    private int frame;
    private bool finished;
}
