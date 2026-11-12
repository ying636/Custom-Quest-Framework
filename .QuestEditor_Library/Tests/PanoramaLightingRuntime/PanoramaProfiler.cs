using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.PanoramaLightingChecks;

public static class PanoramaProfiler
{
    public static IEnumerable<object> Run(Map map)
    {
        List<MapDrawLayer> layers = PanoramaChecksDriver.Layers(map);
        MapDrawLayer_CQFPanoramaLighting layer = layers.OfType<MapDrawLayer_CQFPanoramaLighting>().Single();
        int index = layers.IndexOf(layer);
        ProfilerPrefsData prefs = ProfilerPrefs.Current;
        var oldTargets = prefs.instrumentationTargets;
        bool oldInstrumentation = prefs.instrumentation;
        bool oldAlloc = prefs.allocTracking;
        bool oldMute = prefs.autoMuteTrivial;
        prefs.instrumentationTargets = new List<string>
        {
            "Assembly-CSharp!Verse.MapDrawer::DrawMapMesh",
            "QuestEditor_Library!QuestEditor_Library.MapDrawLayer_CQFPanoramaLighting::DrawLayer"
        };
        prefs.instrumentation = true;
        prefs.allocTracking = true;
        prefs.autoMuteTrivial = false;
        FrameProfiler.SetEnabled(true);
        ProfilerPrefs.Apply();
        FrameProfiler.SetHistoryCapacity(2000);
        FrameProfiler.SetMainThreadOnly(true);
        FrameProfiler.ClearMutedLabels();
        MethodTimingProbe.Install();
        double timingOverheadUs = MethodTimingProbe.Calibrate();
        Bootstrap.WriteJson("Profiler-Instrumentation.json", new
        {
            ProfilerInstrumentation.IsSupported, ProfilerInstrumentation.IsEnabled,
            ProfilerInstrumentation.MatchedMethodCount, ProfilerInstrumentation.InstrumentedMethodCount,
            FrameProfiler.AllocationTrackingSupported, FrameProfiler.AllocationTracking, FrameProfiler.ScopeOverheadNanoseconds
        });
        PanoramaChecksDriver.Check(ProfilerInstrumentation.IsSupported && ProfilerInstrumentation.IsEnabled,
            "native frame profiler instrumentation enabled");
        ProfilerSession session = new();
        try
        {
            foreach ((string name, Vector3 center, float size) in new[]
            {
                ("Inside", map.Center.ToVector3Shifted(), 20f),
                ("OneEdge", new Vector3(-200f, 0f, map.Size.z / 2f), 55f),
                ("Corner", new Vector3(-200f, 0f, -200f), 55f),
                ("Wide", map.Center.ToVector3Shifted(), 900f)
            })
            {
                PanoramaChecksDriver.Position(center, size);
                for (int repetition = 0; repetition < 3; repetition++)
                {
                    foreach (bool enabled in repetition % 2 == 0 ? new[] { false, true } : new[] { true, false })
                    {
                        if (enabled && !layers.Contains(layer)) layers.Insert(index, layer);
                        if (!enabled) layers.Remove(layer);
                        session.SetPaused(false);
                        foreach (object wait in PanoramaChecksDriver.Frames(25)) yield return wait;
                        session.ClearFrames();
                        MethodTimingProbe.Reset();
                        MethodTimingProbe.Enabled = true;
                        List<float> realFrameMs = new();
                        for (int i = 0; i < 180; i++)
                        {
                            yield return null;
                            realFrameMs.Add(Time.unscaledDeltaTime * 1000f);
                        }
                        session.SetPaused(true);
                        MethodTimingProbe.Enabled = false;
                        foreach (object wait in PanoramaChecksDriver.Frames(12)) yield return wait;
                        session.JumpToNewest();
                        session.Poll();
                        List<FrameProfiler.FrameSnapshot> frames = new();
                        session.CollectFrames(180, frames);
                        PanoramaChecksDriver.Check(frames.Count == 180, name + " native profiler 180 frames " + enabled + " repeat " + repetition);
                        PanoramaChecksDriver.Check(frames.All(f => f.overflowedScopes == 0 && f.unbalancedEnds == 0 && f.droppedEvents == 0),
                            name + " no lost profiler events " + enabled + " repeat " + repetition);
                        Dictionary<int, (double Total, double Self, long Calls, long Alloc)> totals = new();
                        foreach (var frame in frames)
                            for (int i = 0; i < frame.pathEntryCount; i++)
                            {
                                var entry = frame.pathEntries[i];
                                totals.TryGetValue(entry.pathId, out var previous);
                                totals[entry.pathId] = (previous.Total + entry.totalMicroseconds, previous.Self + entry.selfMicroseconds,
                                    previous.Calls + entry.calls, previous.Alloc + entry.allocBytes);
                            }
                        var scopes = totals.Select(pair => new
                        {
                            Path = PathLabel(pair.Key), Calls = pair.Value.Calls, TotalUs = pair.Value.Total,
                            SelfUs = pair.Value.Self, AllocBytes = pair.Value.Alloc
                        }).ToArray();
                        PanoramaChecksDriver.Check(scopes.Any(s => s.Path.Contains("DrawMapMesh") && s.Calls >= 180), name + " measured actual map render entry");
                        PanoramaChecksDriver.Check(MethodTimingProbe.Calls == (enabled ? 180 : 0), name + " independently counted supplemental render invocations");
                        Bootstrap.WriteJson("Profiler-" + name + "-" + enabled + "-" + repetition + ".json", new
                        {
                            Scenario = name, Enabled = enabled, Repetition = repetition, Frames = frames.Count,
                            Game = VersionControl.CurrentVersionStringWithRev, Scopes = scopes, RealFrameMs = realFrameMs,
                            NativeFrames = frames.Select(f => new { f.frameIndex, f.totalMs, f.tickDelta, f.overflowedScopes, f.unbalancedEnds, f.droppedEvents }),
                            LayerProbe = new { MethodTimingProbe.Calls, TotalUs = MethodTimingProbe.Ticks * 1000000d / System.Diagnostics.Stopwatch.Frequency,
                                AllocBytes = MethodTimingProbe.Allocated, TimingOverheadUs = timingOverheadUs },
                            Camera = new { X = Find.Camera.transform.position.x, Y = Find.Camera.transform.position.y,
                                Z = Find.Camera.transform.position.z, Find.Camera.orthographicSize, Find.Camera.nearClipPlane, Find.Camera.farClipPlane },
                            Method = "Actual map rendering; native method instrumentation; draw observer removed; baseline layer removed; alternating order; 25 warmup and 180 sample frames; paused game"
                        });
                    }
                }
            }
        }
        finally
        {
            if (!layers.Contains(layer)) layers.Insert(index, layer);
            FrameProfiler.SetEnabled(false);
            MethodTimingProbe.Remove();
            prefs.instrumentationTargets = oldTargets;
            prefs.instrumentation = oldInstrumentation;
            prefs.allocTracking = oldAlloc;
            prefs.autoMuteTrivial = oldMute;
            ProfilerPrefs.Apply();
        }
    }

    private static string PathLabel(int id)
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
