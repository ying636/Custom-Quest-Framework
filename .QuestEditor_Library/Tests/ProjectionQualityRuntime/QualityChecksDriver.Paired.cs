using System.Diagnostics;
using UnityEngine;
using Verse;

namespace CQF.ProjectionQualityChecks;

public sealed partial class QualityChecksDriver
{
    private IEnumerable<object?> PairedFrames()
    {
        try
        {
            for (int round = 0; round < 6; round++)
                foreach ((string name, bool enabled, float size, bool focused, int refresh) in Scenarios().Where(s => s.Item1 is "CloseDisplay" or "ClosePlayableEvery3" or "FullDisplay"))
                    foreach (bool legacy in round % 2 == 0 ? new[] { true, false } : new[] { false, true })
                    {
                        Configure(enabled, size, focused, refresh);
                        Bootstrap.LegacyMode = legacy;
                        foreach (object? wait in Frames(120)) yield return wait;
                        Bootstrap.RenderCalls = 0;
                        Bootstrap.SubmitCalls = 0;
                        Bootstrap.SubmitAlloc = 0;
                        Bootstrap.SubmitTicks = 0;
                        Bootstrap.Sampling = true;
                        List<double> durations = new();
                        long previous = Stopwatch.GetTimestamp(), started = previous;
                        for (int i = 0; i < 600 || (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency < 2d; i++)
                        {
                            yield return null;
                            long now = Stopwatch.GetTimestamp();
                            durations.Add((now - previous) * 1000d / Stopwatch.Frequency);
                            previous = now;
                        }
                        Bootstrap.Sampling = false;
                        double[] sorted = durations.Order().ToArray();
                        Write($"Paired-{round}-{name}-{(legacy ? "Before" : "After")}.json", new
                        {
                            Scenario = name, Round = round, Legacy = legacy, Frames = durations.Count,
                            FPS = durations.Count * 1000d / durations.Sum(), P95Ms = sorted[(int)(sorted.Length * 0.95)],
                            Bootstrap.RenderCalls, FrameTimesMs = durations, Paused = true, FrameLimit = -1, VSync = "Off",
                            Bootstrap.SubmitCalls, Bootstrap.SubmitAlloc,
                            SubmitMs = Bootstrap.SubmitTicks * 1000d / Stopwatch.Frequency,
                            Method = GenCommandLine.CommandLineArgPassed("cqfprojectionpoolpaired")
                                ? "Alternate standard Render and reusable RenderItems with identical commands, camera, texture and refresh rate in one process; reverse order every round"
                                : "Alternate old and new rendering paths within one process and fixed save; reverse order every round; old renderer source from parent commit"
                        });
                        Check(Bootstrap.RenderCalls > 0 && (GenCommandLine.CommandLineArgPassed("cqfprojectionpoolpaired") || !legacy || refresh != 3 || Bootstrap.RenderCalls >= durations.Count - 1),
                            $"paired native rendering executes expected path: {name} round {round} legacy={legacy}");
                    }
        }
        finally
        {
            Bootstrap.Sampling = false;
            Bootstrap.LegacyMode = false;
            Bootstrap.LegacyRenderer?.Dispose();
            Bootstrap.LegacyRenderer = null;
        }
    }
}
