using System.Text.Json;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.DomainPerformanceChecks;

public sealed partial class DomainPerformanceDriver
{
    private static IEnumerable<object?> Profile()
    {
        ProfilerPrefsData prefs = ProfilerPrefs.Current;
        List<string> oldTargets = prefs.instrumentationTargets;
        bool oldInstrumentation = prefs.instrumentation, oldAlloc = prefs.allocTracking, oldMute = prefs.autoMuteTrivial, oldMain = prefs.mainThreadOnly;
        int oldHistory = prefs.historyCapacity;
        prefs.instrumentationTargets = new()
        {
            "CQF.Domain!QuestEditor_Library.DomainTerritoryGeometry::Append",
            "CQF.Domain!QuestEditor_Library.DomainTerritoryField::Refresh",
            "CQF.Domain!QuestEditor_Library.DomainMultiTerritory::SolveWeights"
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
        var invalidateField = AccessTools.FieldRefAccess<DomainTerritoryField, int>("revision");
        MaterialPropertyBlock properties = new();
        try
        {
            Check(ProfilerInstrumentation.IsSupported && ProfilerInstrumentation.IsEnabled && FrameProfiler.AllocationTrackingSupported,
                "native profiler instrumentation and allocation tracking are active");
            foreach ((int count, int size) in new[] { (2, 90), (4, 128), (8, 180) })
            {
                IntVec3[] centers = Enumerable.Range(0, count).Select(index => new IntVec3(size / 2 + (int)(Math.Cos(index * Math.PI * 2 / count) * size * 0.2), 0,
                    size / 2 + (int)(Math.Sin(index * Math.PI * 2 / count) * size * 0.2))).ToArray();
                float[] health = Enumerable.Repeat(100000f, count).ToArray();
                foreach (string workload in new[] { "Geometry", "Fields", "Health" })
                {
                    DomainTerritoryPartition partition = count == 2 ? new(size, size, 0.5f, false, centers[0], centers[1])
                        : new(size, size, centers, health);
                    DomainTerritoryGeometry geometry = new(partition);
                    DomainTerritoryField[] fields = Enumerable.Range(0, count).Select(_ => new DomainTerritoryField(partition)).ToArray();
                    try
                    {
                        session.SetPaused(false);
                        string label = "CQF_DomainWorkload_" + count + "_" + workload;
                        long allocated = 0, calls = 0;
                        for (int frame = 0; frame < 150; frame++)
                        {
                            if (frame == 30) session.ClearFrames();
                            long before = GC.GetAllocatedBytesForCurrentThread();
                            using (FrameProfiler.Scope(label))
                            {
                                if (workload == "Geometry")
                                    for (int side = 0; side < count; side++)
                                    {
                                        vertices.Clear(); triangles.Clear(); colors.Clear();
                                        geometry.Append(new Rect(0f, 0f, size, size), side, 0f, vertices, triangles, colors);
                                    }
                                else if (workload == "Fields")
                                    for (int side = 0; side < count; side++)
                                    {
                                        invalidateField(fields[side]) = -1;
                                        fields[side].Configure(properties, side);
                                    }
                                else
                                {
                                    health[0] = 80000f + frame % 2 * 5000f;
                                    partition.Advance(health, 1f / 120f);
                                }
                            }
                            if (frame >= 30) { allocated += GC.GetAllocatedBytesForCurrentThread() - before; calls++; }
                            yield return null;
                        }
                        session.SetPaused(true);
                        for (int i = 0; i < 12; i++) yield return null;
                        session.JumpToNewest();
                        session.Poll();
                        List<FrameProfiler.FrameSnapshot> frames = new();
                        session.CollectFrames(120, frames);
                        Check(frames.Count == 120 && frames.All(frame => frame.overflowedScopes == 0 && frame.unbalancedEnds == 0 && frame.droppedEvents == 0),
                            "native profiler exports complete frames: " + label);
                        Dictionary<int, (double Total, double Self, long Calls, long Alloc)> totals = new();
                        foreach (FrameProfiler.FrameSnapshot frame in frames)
                            for (int i = 0; i < frame.pathEntryCount; i++)
                            {
                                var entry = frame.pathEntries[i];
                                totals.TryGetValue(entry.pathId, out var previous);
                                totals[entry.pathId] = (previous.Total + entry.totalMicroseconds, previous.Self + entry.selfMicroseconds,
                                    previous.Calls + entry.calls, previous.Alloc + entry.allocBytes);
                            }
                        var scopes = totals.Select(pair => new { Path = ProfilerPath(pair.Key), TotalUs = pair.Value.Total, SelfUs = pair.Value.Self,
                            pair.Value.Calls, AllocBytes = pair.Value.Alloc }).ToArray();
                        Check(calls == 120 && scopes.Any(scope => scope.Path.EndsWith(label, StringComparison.Ordinal) && scope.Calls == 120),
                            "measured workload count matches native scopes: " + label);
                        results.Add(new { Domains = count, Size = size, Workload = workload, Frames = 120, DirectAllocatedBytes = allocated,
                            OwnershipBoundarySHA256 = StateHash(partition), GeometrySHA256 = GeometryHash(partition, geometry),
                            TextureSHA256 = CheckPixels(partition, fields),
                            ProfilerInstrumentation.InstrumentedMethodCount, Scopes = scopes });
                        File.WriteAllText(Path.Combine(Bootstrap.Output, "ProfilerResults.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    finally { foreach (DomainTerritoryField field in fields) field.Dispose(); }
                }
            }
            Find.WindowStack.Add(new Window_FrameProfiler());
            for (int i = 0; i < 4; i++) yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "01-DomainProfiler.png"));
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
