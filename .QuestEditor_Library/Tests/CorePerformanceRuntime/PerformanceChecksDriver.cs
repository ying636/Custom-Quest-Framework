using System.Security.Cryptography;
using System.Text.Json;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.CorePerformanceChecks;

public sealed partial class PerformanceChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        try
        {
            routine ??= Run().GetEnumerator();
            if (routine.MoveNext()) return;
            routine.Dispose();
            routine = null;
            finished = true;
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "COMPLETE\n");
            Application.Quit(0);
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "FAIL " + error + "\n");
            Log.Error("[CQF.CorePerformanceChecks] " + error);
            finished = true;
            routine?.Dispose();
            Application.Quit(1);
        }
    }

    private IEnumerable<object?> Run()
    {
        Find.TickManager.Pause();
        string save = Path.Combine(GenFilePaths.SaveDataFolderPath, "Saves", "CQF_CorePerformance.rws");
        if (!File.Exists(save)) GameDataSaveLoader.SaveGame("CQF_CorePerformance");
        yield return null;
        string sceneHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(save)));
        Game previous = Current.Game;
        GameDataSaveLoader.LoadGame("CQF_CorePerformance");
        yield return null;
        Check(Current.Game != previous, "native fixed scene reload completes");
        Find.TickManager.Pause();
        Map map = Find.CurrentMap;
        MapComponent_CustomMapData data = map.GetComponent<MapComponent_CustomMapData>();
        data.MapComponentTick();
        data.pawnSpawnDatas_Tick.Clear();
        CustomTrap[] traps = new CustomTrap[100];
        ThingDef trapDefinition = DefDatabase<ThingDef>.GetNamed("QE_StonePressurePlate");
        for (int i = 0; i < traps.Length; i++)
        {
            traps[i] = (CustomTrap)ThingMaker.MakeThing(trapDefinition, GenStuff.DefaultStuffFor(trapDefinition));
            GenSpawn.Spawn(traps[i], new IntVec3(10 + i % 20, 0, 10 + i / 20), map);
            traps[i].TrapComps.Clear();
        }
        Action<CustomTrap> tickTrap = AccessTools.Method(typeof(CustomTrap), "Tick").CreateDelegate<Action<CustomTrap>>();
        Check(!CQFModuleLoader.IsLoaded("MapProjections") && !CQFModuleLoader.DomainLoaded, "main library workloads require no optional module");
        CheckBehavior(data, traps[0], tickTrap, map);
        File.WriteAllText(Path.Combine(Bootstrap.Output, "Scene.json"), JsonSerializer.Serialize(new
        {
            Save = save, SHA256 = sceneHash, Map = map.uniqueID, MapSize = map.Size.ToString(), Traps = traps.Length,
            CoreSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(CustomTrap).Assembly.Location))),
            FixedWorkload = "1000 direct calls per sample frame, 30 warmup frames, 120 sampled frames; game simulation paused"
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (object? wait in Profile(data, traps, tickTrap)) yield return wait;
        foreach (CustomTrap trap in traps) trap.Destroy();
        data.pawnSpawnDatas_Tick.Clear();
    }

    private static void CheckBehavior(MapComponent_CustomMapData data, CustomTrap trap, Action<CustomTrap> tickTrap, Map map)
    {
        PerformanceSpawnData recipe = new() { timeToSpawn = 3, count = new IntRange(2, 2) };
        PawnDataWithPosAndTime schedule = new() { data = recipe, position = new IntVec3(16, 0, 16), time = 2 };
        data.pawnSpawnDatas_Tick.Add(schedule);
        data.MapComponentTick();
        Check(schedule.time == 3 && recipe.Calls == 0, "spawn scheduling preserves the strict greater-than threshold");
        data.MapComponentTick();
        Check(schedule.time == 0 && recipe.Calls == 2 && recipe.LastMap == map && recipe.LastPosition == schedule.position,
            "due spawn dispatch preserves count, reset and source coordinates");
        recipe.Callback = () => data.pawnSpawnDatas_Tick.Clear();
        schedule.time = 3;
        bool rejected = false;
        try { data.MapComponentTick(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "spawn callback mutation remains a visible collection error");
        data.pawnSpawnDatas_Tick.Clear();
        data.pawnSpawnDatas_Tick = null!;
        data.MapComponentTick();
        data.pawnSpawnDatas_Tick = new();
        Check(true, "missing optional spawn schedule remains supported");
        PerformanceAction first = new() { ExpectedTrap = trap }, second = new() { ExpectedTrap = trap };
        TrapComp periodic = new() { mode = ActionTriggerMode.Tick, tick = 1, actions = new() { first, second } };
        trap.TrapComps.Add(periodic);
        tickTrap(trap);
        Check(first.Calls == 1 && second.Calls == 1 && first.IsolatedTargets && second.IsolatedTargets && first.CorrectTarget && second.CorrectTarget,
            "periodic trap actions preserve execution count and separate target dictionaries");
        periodic.tick = 0;
        tickTrap(trap);
        periodic.mode = ActionTriggerMode.Signal;
        periodic.tick = 1;
        tickTrap(trap);
        Check(first.Calls == 1 && second.Calls == 1, "zero interval and non-tick trap modes remain inactive during tick polling");
        periodic.mode = ActionTriggerMode.Tick;
        periodic.actions = new() { first };
        first.Callback = () => trap.TrapComps.Add(new TrapComp());
        rejected = false;
        try { tickTrap(trap); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "trap component mutation remains a visible collection error");
        trap.TrapComps.Clear();
        periodic.actions = new() { first };
        first.Callback = () => periodic.actions.Add(second);
        trap.TrapComps.Add(periodic);
        rejected = false;
        try { tickTrap(trap); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "trap action mutation remains a visible collection error");
        trap.TrapComps.Clear();
    }

    private static void Check(bool value, string text)
    {
        if (!value) throw new InvalidOperationException(text);
        File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "PASS " + text + "\n");
    }

    private IEnumerator<object?>? routine;
    private bool finished;
}
