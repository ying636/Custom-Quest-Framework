using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private IEnumerable<object?> CheckProjectionProfiler()
    {
        Find.TickManager.Pause();
        CQFMapProjectionContext.ReturnToHost();
        Find.MainTabsRoot.SetCurrentTab(null, playSound: false);
        Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 32f);
        GameComponent_CQFProjectionUI.Instance!.AcquireSource(source!);
        MapComponent_CQFMapProjections component = host!.GetComponent<MapComponent_CQFMapProjections>();
        CQFMapProjection[] original = component.Projections.ToArray();
        foreach (CQFMapProjection projection in original) projection.enabled = false;
        List<CQFMapProjection> probes = new();
        for (int i = 0; i < 4; i++) probes.Add(component.Add(new CQFMapProjection
        {
            id = "CQF_Profiler_" + i, sourceMap = source, destination = new CellRect(10 + i * 18, 22, 16, 12), enabled = false
        }));
        ProfilerPrefsData prefs = ProfilerPrefs.Current;
        List<string> oldTargets = prefs.instrumentationTargets;
        bool oldInstrumentation = prefs.instrumentation, oldAlloc = prefs.allocTracking, oldMute = prefs.autoMuteTrivial, oldMain = prefs.mainThreadOnly;
        int oldHistory = prefs.historyCapacity;
        prefs.instrumentationTargets = new()
        {
            "CQF.MapProjections!QuestEditor_Library.MapComponent_CQFMapProjections::MapComponentDraw",
            "CQF.MapProjections!QuestEditor_Library.GameComponent_CQFProjectionUI::GameComponentOnGUI"
        };
        prefs.instrumentation = true;
        prefs.allocTracking = true;
        prefs.autoMuteTrivial = false;
        prefs.mainThreadOnly = true;
        prefs.historyCapacity = 2000;
        FrameProfiler.SetEnabled(true);
        ProfilerPrefs.Apply();
        FrameProfiler.ClearMutedLabels();
        Check(ProfilerInstrumentation.IsSupported && ProfilerInstrumentation.IsEnabled, "native frame profiler instrumentation is supported and enabled for projections");
        ProfilerSession session = new();
        string directory = Path.Combine(Bootstrap.Output, "Profiler");
        Directory.CreateDirectory(directory);
        try
        {
            for (int population = 0; population < 2; population++)
            {
                if (population == 1)
                    for (int i = 0; i < 48; i++)
                    {
                        Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                        int z = 14 + i / 12 * 2;
                        GenSpawn.Spawn(pawn, new IntVec3(22 + i % 12 * 2, 0, z == 20 ? 22 : z), source!);
                    }
                foreach ((string name, int count, int refresh, bool play) in new[]
                {
                    ("None", 0, 1, false), ("OneDisplay", 1, 1, false), ("OneDisplayEvery6", 1, 6, false),
                    ("OnePlayable", 1, 1, true), ("FourDisplays", 4, 1, false)
                })
                {
                    CQFMapProjectionContext.ReturnToHost();
                    for (int i = 0; i < probes.Count; i++)
                    {
                        probes[i].enabled = i < count;
                        probes[i].refreshFrames = refresh;
                        probes[i].mode = play && i == 0 ? CQFMapProjectionMode.Control : CQFMapProjectionMode.Display;
                    }
                    if (play) CQFMapProjectionContext.Focus(host, probes[0]);
                    session.SetPaused(false);
                    foreach (object? wait in Frames(35)) yield return wait;
                    session.ClearFrames();
                    Bootstrap.ProfilerRenderCalls = 0;
                    Bootstrap.ProfilerSampling = true;
                    foreach (object? wait in Frames(120)) yield return wait;
                    session.SetPaused(true);
                    Bootstrap.ProfilerSampling = false;
                    int observedRenders = Bootstrap.ProfilerRenderCalls;
                    foreach (object? wait in Frames(12)) yield return wait;
                    session.JumpToNewest();
                    session.Poll();
                    List<FrameProfiler.FrameSnapshot> frames = new();
                    session.CollectFrames(120, frames);
                    string scenario = population + "-" + name;
                    Check(frames.Count == 120 && frames.All(frame => frame.overflowedScopes == 0 && frame.unbalancedEnds == 0 && frame.droppedEvents == 0),
                        "native profiler exports 120 complete frames without dropped events: " + scenario);
                    Dictionary<int, (double Total, double Self, long Calls, long Alloc, long SelfAlloc)> totals = new();
                    foreach (FrameProfiler.FrameSnapshot frame in frames)
                        for (int i = 0; i < frame.pathEntryCount; i++)
                        {
                            var entry = frame.pathEntries[i];
                            totals.TryGetValue(entry.pathId, out var previous);
                            totals[entry.pathId] = (previous.Total + entry.totalMicroseconds, previous.Self + entry.selfMicroseconds,
                                previous.Calls + entry.calls, previous.Alloc + entry.allocBytes, previous.SelfAlloc + entry.selfAllocBytes);
                        }
                    var scopes = totals.Select(pair => new { Path = ProjectionProfilerPath(pair.Key), TotalUs = pair.Value.Total, SelfUs = pair.Value.Self,
                        pair.Value.Calls, AllocBytes = pair.Value.Alloc, SelfAllocBytes = pair.Value.SelfAlloc }).ToArray();
                    File.WriteAllText(Path.Combine(directory, scenario + ".json"), JsonSerializer.Serialize(new
                    {
                        Scenario = scenario, Frames = frames.Count, SourceMapSize = source!.Size.ToString(), SourcePawns = source.mapPawns.AllPawnsSpawned.Count,
                        Projections = count, RefreshFrames = refresh, Playable = play, Resolution = 1024, SourceRegion = "whole map", PausedGame = true,
                        ObservedRenders = observedRenders, ProfilerInstrumentation.InstrumentedMethodCount, FrameProfiler.AllocationTrackingSupported,
                        HostView = Find.CameraDriver.CurrentViewRect.ToString(), HostCamera = Current.Camera.transform.position.ToString(),
                        FrameProfiler.AllocationTracking, Scopes = scopes, NativeFrames = frames.Select(frame => new { frame.frameIndex, frame.totalMs, frame.tickDelta }),
                        Method = "Actual renderer in a native FrameProfiler outer scope; method instrumentation on map draw and UI; 35 warmup and 120 sampled frames; source map remains active; CPU timings include profiling overhead"
                    }, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
                    Check(count == 0 ? observedRenders == 0 : observedRenders > 0 && scopes.Any(scope => scope.Path.Contains("CQF_ProjectionRender") && scope.Calls > 0),
                        "native profiler and independent counters observe expected projection rendering: " + scenario);
                }
            }
            Find.WindowStack.Add(new Window_FrameProfiler());
            foreach (object? wait in Frames(4)) yield return wait;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "NativeProfiler.png"));
            foreach (object? wait in Frames(3)) yield return wait;
            Find.WindowStack.WindowOfType<Window_FrameProfiler>()!.Close();
        }
        finally
        {
            Bootstrap.ProfilerSampling = false;
            FrameProfiler.SetEnabled(false);
            prefs.instrumentationTargets = oldTargets;
            prefs.instrumentation = oldInstrumentation;
            prefs.allocTracking = oldAlloc;
            prefs.autoMuteTrivial = oldMute;
            prefs.mainThreadOnly = oldMain;
            prefs.historyCapacity = oldHistory;
            ProfilerPrefs.Apply();
            CQFMapProjectionContext.ReturnToHost();
            foreach (CQFMapProjection projection in probes) component.Remove(projection.id);
            foreach (CQFMapProjection projection in original) projection.enabled = true;
            GameComponent_CQFProjectionUI.Instance!.ReleaseSource(source!);
        }
    }

    private static string ProjectionProfilerPath(int id)
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
