using System.Text.Json;
using QuestEditor_Library;
using UnityEngine;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.CorePerformanceChecks;

public sealed partial class PerformanceChecksDriver
{
    private static IEnumerable<object?> Profile(MapComponent_CustomMapData data, CustomTrap[] traps, Action<CustomTrap> tickTrap)
    {
        ProfilerPrefsData prefs = ProfilerPrefs.Current;
        List<string> oldTargets = prefs.instrumentationTargets;
        bool oldInstrumentation = prefs.instrumentation, oldAlloc = prefs.allocTracking, oldMute = prefs.autoMuteTrivial, oldMain = prefs.mainThreadOnly;
        int oldHistory = prefs.historyCapacity;
        prefs.instrumentationTargets = new()
        {
            "QuestEditor_Library!QuestEditor_Library.CustomTrap::Tick",
            "QuestEditor_Library!QuestEditor_Library.MapComponent_CustomMapData::MapComponentTick"
        };
        prefs.instrumentation = true;
        prefs.allocTracking = true;
        prefs.autoMuteTrivial = false;
        prefs.mainThreadOnly = true;
        prefs.historyCapacity = 2000;
        FrameProfiler.SetEnabled(true);
        ProfilerPrefs.Apply();
        FrameProfiler.ClearMutedLabels();
        ProfilerSession session = new();
        List<object> results = new();
        try
        {
            Check(ProfilerInstrumentation.IsSupported && ProfilerInstrumentation.IsEnabled && FrameProfiler.AllocationTrackingSupported,
                "native profiler and allocation tracking are available");
            foreach (string scenario in new[] { "MapEmpty", "Map100Schedules", "100IdleTraps", "100PollingTraps", "100TriggeredTraps" })
            {
                data.pawnSpawnDatas_Tick.Clear();
                if (scenario == "Map100Schedules")
                    for (int i = 0; i < 100; i++) data.pawnSpawnDatas_Tick.Add(new PawnDataWithPosAndTime
                    { data = new PawnSpawnData { timeToSpawn = int.MaxValue }, position = new IntVec3(16, 0, 16) });
                foreach (CustomTrap trap in traps)
                {
                    trap.TrapComps.Clear();
                    if (scenario is "100PollingTraps" or "100TriggeredTraps") trap.TrapComps.Add(new TrapComp
                    {
                        mode = ActionTriggerMode.Tick, tick = scenario == "100TriggeredTraps" ? 1 : int.MaxValue,
                        actions = scenario == "100TriggeredTraps" ? new() { new PerformanceAction { ExpectedTrap = trap } } : new()
                    });
                }
                session.SetPaused(false);
                string label = "CQF_CoreWorkload_" + scenario;
                long allocated = 0, observedCalls = 0;
                for (int frame = 0; frame < 150; frame++)
                {
                    if (frame == 30) session.ClearFrames();
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    using (FrameProfiler.Scope(label))
                    {
                        if (scenario.StartsWith("Map", StringComparison.Ordinal))
                            for (int i = 0; i < 1000; i++) data.MapComponentTick();
                        else
                            for (int repeat = 0; repeat < 10; repeat++)
                                foreach (CustomTrap trap in traps) tickTrap(trap);
                    }
                    if (frame >= 30) { allocated += GC.GetAllocatedBytesForCurrentThread() - before; observedCalls += 1000; }
                    yield return null;
                }
                session.SetPaused(true);
                if (scenario == "Map100Schedules")
                    Check(data.pawnSpawnDatas_Tick.All(schedule => schedule.time == 150000), "all scheduled spawn timers advance once per measured call");
                if (scenario == "100TriggeredTraps")
                    Check(traps.All(trap => trap.TrapComps[0].actions[0] is PerformanceAction { Calls: 1500, IsolatedTargets: true, CorrectTarget: true }),
                        "all trap actions execute the fixed workload with valid independent targets");
                for (int i = 0; i < 12; i++) yield return null;
                session.JumpToNewest();
                session.Poll();
                List<FrameProfiler.FrameSnapshot> frames = new();
                session.CollectFrames(120, frames);
                Check(frames.Count == 120 && frames.All(frame => frame.overflowedScopes == 0 && frame.unbalancedEnds == 0 && frame.droppedEvents == 0),
                    "native profiler exports 120 complete frames: " + scenario);
                Dictionary<int, (double Total, long Calls, long Alloc)> totals = new();
                foreach (FrameProfiler.FrameSnapshot frame in frames)
                    for (int i = 0; i < frame.pathEntryCount; i++)
                    {
                        var entry = frame.pathEntries[i];
                        totals.TryGetValue(entry.pathId, out var previous);
                        totals[entry.pathId] = (previous.Total + entry.totalMicroseconds, previous.Calls + entry.calls, previous.Alloc + entry.allocBytes);
                    }
                var scopes = totals.Select(pair => new { Path = ProfilerPath(pair.Key), TotalUs = pair.Value.Total, pair.Value.Calls, AllocBytes = pair.Value.Alloc }).ToArray();
                Check(scopes.Any(scope => scope.Path.EndsWith("CQF_CoreWorkload_" + scenario, StringComparison.Ordinal) && scope.Calls > 0),
                    "native profiler captures the measured outer workload: " + scenario);
                Check(observedCalls == 120000, "fixed workload call count matches between runs: " + scenario);
                results.Add(new { Scenario = scenario, Frames = 120, ObservedCalls = observedCalls, DirectAllocatedBytes = allocated,
                    ProfilerInstrumentation.InstrumentedMethodCount, Scopes = scopes,
                    NativeFrames = frames.Select(frame => new { frame.frameIndex, frame.totalMs, frame.tickDelta }).ToArray() });
            }
            File.WriteAllText(Path.Combine(Bootstrap.Output, "ProfilerResults.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Find.WindowStack.Add(new Window_FrameProfiler());
            for (int i = 0; i < 4; i++) yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "01-NativeProfiler.png"));
            for (int i = 0; i < 4; i++) yield return null;
            Find.WindowStack.WindowOfType<Window_FrameProfiler>()!.Close();
        }
        finally
        {
            FrameProfiler.SetEnabled(false);
            prefs.instrumentationTargets = oldTargets;
            prefs.instrumentation = oldInstrumentation;
            prefs.allocTracking = oldAlloc;
            prefs.autoMuteTrivial = oldMute;
            prefs.mainThreadOnly = oldMain;
            prefs.historyCapacity = oldHistory;
            ProfilerPrefs.Apply();
        }
    }

    private static string ProfilerPath(int id)
    {
        List<string> labels = new();
        var entries = ProfilerPathTable.Entries;
        while (id > ProfilerPathTable.VirtualRoot)
        {
            labels.Add(FrameProfiler.GetLabel(entries[id].labelId));
            id = entries[id].parentPathId;
        }
        labels.Reverse();
        return string.Join(" / ", labels);
    }
}
