using UnityEngine;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.ProjectionQualityChecks;

public sealed partial class QualityChecksDriver
{
    private IEnumerable<object?> Profile()
    {
        ProfilerPrefsData prefs = ProfilerPrefs.Current;
        List<string> oldTargets = prefs.instrumentationTargets;
        bool oldInstrumentation = prefs.instrumentation, oldAlloc = prefs.allocTracking, oldMute = prefs.autoMuteTrivial, oldMain = prefs.mainThreadOnly;
        int oldHistory = prefs.historyCapacity;
        prefs.instrumentationTargets = new() { "CQF.MapProjections!QuestEditor_Library.MapComponent_CQFMapProjections::MapComponentDraw" };
        prefs.instrumentation = true;
        prefs.allocTracking = true;
        prefs.autoMuteTrivial = false;
        prefs.mainThreadOnly = true;
        prefs.historyCapacity = 2000;
        FrameProfiler.SetEnabled(true);
        ProfilerPrefs.Apply();
        FrameProfiler.ClearMutedLabels();
        Check(ProfilerInstrumentation.IsSupported && ProfilerInstrumentation.IsEnabled, "native frame profiler instrumentation is active");
        ProfilerSession session = new();
        Bootstrap.Profiling = true;
        try
        {
            foreach ((string name, bool enabled, float size, bool focused, int refresh) in Scenarios().Where(s => s.Item1 is "CloseDisplay" or "ClosePlayableEvery3" or "FullDisplay"))
            {
                Configure(enabled, size, focused, refresh);
                session.SetPaused(false);
                foreach (object? wait in Frames(35)) yield return wait;
                session.ClearFrames();
                Bootstrap.RenderCalls = 0;
                Bootstrap.Sampling = true;
                foreach (object? wait in Frames(120)) yield return wait;
                session.SetPaused(true);
                Bootstrap.Sampling = false;
                int renders = Bootstrap.RenderCalls;
                foreach (object? wait in Frames(12)) yield return wait;
                session.JumpToNewest();
                session.Poll();
                List<FrameProfiler.FrameSnapshot> frames = new();
                session.CollectFrames(120, frames);
                Check(frames.Count == 120 && frames.All(f => f.overflowedScopes == 0 && f.unbalancedEnds == 0 && f.droppedEvents == 0),
                    "native profiler exports complete samples: " + name);
                Dictionary<int, (double Total, double Self, long Calls, long Alloc)> totals = new();
                foreach (var frame in frames)
                    for (int i = 0; i < frame.pathEntryCount; i++)
                    {
                        var entry = frame.pathEntries[i];
                        totals.TryGetValue(entry.pathId, out var previous);
                        totals[entry.pathId] = (previous.Total + entry.totalMicroseconds, previous.Self + entry.selfMicroseconds,
                            previous.Calls + entry.calls, previous.Alloc + entry.allocBytes);
                    }
                Write("Profiler-" + name + ".json", new
                {
                    Scenario = name, Frames = frames.Count, ObservedRenders = renders,
                    Scopes = totals.Select(p => new { Path = ProfilePath(p.Key), TotalUs = p.Value.Total, SelfUs = p.Value.Self,
                        p.Value.Calls, AllocBytes = p.Value.Alloc }),
                    NativeFrames = frames.Select(f => new { f.frameIndex, f.totalMs, f.tickDelta })
                });
            }
            Find.WindowStack.Add(new Window_FrameProfiler());
            foreach (object? wait in Frames(4)) yield return wait;
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "NativeProfiler.png"));
            foreach (object? wait in Frames(4)) yield return wait;
            Find.WindowStack.WindowOfType<Window_FrameProfiler>()!.Close();
        }
        finally
        {
            Bootstrap.Profiling = false;
            Bootstrap.Sampling = false;
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

    private static string ProfilePath(int id)
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
