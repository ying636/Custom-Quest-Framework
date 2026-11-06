using System.Text.Json;
using QuestEditor_Library;
using RimWorld;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.SymmetryChecks;

internal sealed class SymmetryProfilerChecks
{
    public SymmetryProfilerChecks(Map map, Action<bool, string> check)
    {
        this.symmetry = CQFEditorSymmetry.For(map);
        this.settings = map.GetComponent<MapComponent_CQFSymmetrySettings>();
        this.drawCallback = CQFEditorBridge.SymmetryUpdate;
        CQFEditorBridge.SymmetryUpdate = null;
        this.fullCells = this.quarterCells.SelectMany(cell => Enumerable.Range(0, 4)
            .Select(mask => this.symmetry.Transform(mask).Cell(cell))).Distinct().ToArray();
        this.check = check;
        this.original = ProfilerPrefs.Current;
        this.targets = new List<string>
        {
            "QuestEditor_Library!QuestEditor_Library.MapComponent_CQFSymmetrySettings::MapComponentUpdate",
            "CQF.Editor!QuestEditor_Library.CQFEditorSymmetry::Update",
            "CQF.Editor!QuestEditor_Library.CQFEditorSymmetry::ApplyCells",
            "CQF.Editor!QuestEditor_Library.CQFEditorSymmetry::Place",
            "CQF.Editor!QuestEditor_Library.Designator_CQFCells::DesignateSingleCell"
        };
        this.oldTargets = original.instrumentationTargets;
        this.oldInstrumentation = original.instrumentation;
        this.oldAlloc = original.allocTracking;
        this.oldMute = original.autoMuteTrivial;
        original.instrumentationTargets = this.targets;
        original.instrumentation = true;
        original.allocTracking = true;
        original.autoMuteTrivial = false;
        FrameProfiler.SetEnabled(true);
        ProfilerPrefs.Apply();
        File.WriteAllText(Path.Combine(GenFilePaths.SaveDataFolderPath, "Profiler-Instrumentation.json"), JsonSerializer.Serialize(new
        {
            ProfilerInstrumentation.IsSupported, ProfilerInstrumentation.IsEnabled,
            ProfilerInstrumentation.MatchedMethodCount, ProfilerInstrumentation.InstrumentedMethodCount, this.targets
        }, new JsonSerializerOptions { WriteIndented = true }));
        check(ProfilerInstrumentation.IsSupported && ProfilerInstrumentation.IsEnabled,
            "native profiler instrumentation is supported and enabled");
        this.session = new ProfilerSession();
    }

    public bool Update()
    {
        if (this.sample >= this.names.Length) return true;
        if (this.frame == 0)
        {
            this.symmetry.enabled = this.sample is 1 or 2 or 4;
            this.symmetry.showAxes = this.sample != 2;
            this.session.ClearFrames();
            this.session.SetPaused(false);
        }
        if (this.frame < 130)
        {
            if (this.frame == 10)
            {
                check(ProfilerInstrumentation.InstrumentedMethodCount >= 3, "native profiler has instrumented CQF symmetry and editing methods");
                this.session.ClearFrames();
            }
            CQFEditorBridge.SymmetryUpdate = this.drawCallback;
            using (FrameProfiler.Scope("CQF_Symmetry_Update")) this.settings.MapComponentUpdate();
            CQFEditorBridge.SymmetryUpdate = null;
            if (this.sample >= 3)
            {
                Designator_Terrain.terrain = this.frame % 2 == 0 ? TerrainDefOf.Concrete : TerrainDefOf.Soil;
                this.brush ??= new Designator_Terrain();
                using (FrameProfiler.Scope("CQF_Symmetry_Brush"))
                    this.brush.DesignateMultiCell(this.sample == 5 ? this.fullCells : this.quarterCells);
            }
            this.frame++;
            return false;
        }
        if (this.frame == 130)
        {
            this.session.SetPaused(true);
            this.session.JumpToNewest();
            this.session.Poll();
            this.session.BeginSelection(0);
            this.session.ExtendSelection(119);
            this.frame++;
        }
        this.session.Poll();
        List<FrameProfiler.FrameSnapshot> frames = new();
        this.session.CollectFrames(120, frames);
        check(frames.Count == 120, "native profiler exports 120 completed frames for " + this.names[this.sample]);
        check(frames.All(frame => frame.overflowedScopes == 0 && frame.unbalancedEnds == 0 && frame.droppedEvents == 0),
            "native sample has no dropped scopes, dropped events or unbalanced ends: " + this.names[this.sample]);
        Dictionary<int, (double total, double self, long calls, long alloc)> totals = new();
        foreach (var frame in frames)
            for (int index = 0; index < frame.pathEntryCount; index++)
            {
                var entry = frame.pathEntries[index];
                totals.TryGetValue(entry.pathId, out var previous);
                totals[entry.pathId] = (previous.total + entry.totalMicroseconds, previous.self + entry.selfMicroseconds,
                    previous.calls + entry.calls, previous.alloc + entry.allocBytes);
            }
        var scopes = totals.Select(pair => new
        {
            pathId = pair.Key, path = PathLabel(pair.Key), averageTotalMicroseconds = pair.Value.total / frames.Count,
            averageSelfMicroseconds = pair.Value.self / frames.Count, calls = pair.Value.calls, averageAllocBytes = (double)pair.Value.alloc / frames.Count
        }).ToArray();
        check(scopes.Any(row => row.path.Contains("CQF_Symmetry_Update") && row.calls == 120),
            "completed native frames contain exactly one measured symmetry update per frame: " + this.names[this.sample]);
        var summary = this.session.SummarizeSelection();
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Profiler-" + this.names[this.sample] + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            scenario = this.names[this.sample], game = VersionControl.CurrentVersionString, this.targets,
            matched = ProfilerInstrumentation.MatchedMethodCount, instrumented = ProfilerInstrumentation.InstrumentedMethodCount,
            FrameProfiler.AllocationTrackingSupported, FrameProfiler.AllocationTracking, FrameProfiler.MainThreadOnly,
            FrameProfiler.ScopeOverheadNanoseconds, measurement = "native outer scopes; the normal callback is moved into the fixture once per frame",
            pausedGame = true, sourceCellsPerStroke = this.sample == 5 ? 400 : this.sample >= 3 ? 100 : 0,
            actualCellsPerStroke = this.sample is 4 or 5 ? 400 : this.sample == 3 ? 100 : 0, summary,
            frames = frames.Select(frame => new { frame.frameIndex, frame.totalMs, frame.tickDelta, frame.overflowedScopes, frame.unbalancedEnds, frame.droppedEvents }), scopes
        }, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
        this.sample++;
        this.frame = 0;
        return this.sample >= this.names.Length;
    }

    public void Restore()
    {
        CQFEditorBridge.SymmetryUpdate = this.drawCallback;
        FrameProfiler.SetEnabled(false);
        original.instrumentationTargets = this.oldTargets;
        original.instrumentation = this.oldInstrumentation;
        original.allocTracking = this.oldAlloc;
        original.autoMuteTrivial = this.oldMute;
        ProfilerPrefs.Apply();
    }

    private static string PathLabel(int pathId)
    {
        List<string> labels = new();
        var entries = ProfilerPathTable.Entries;
        while (pathId > ProfilerPathTable.VirtualRoot)
        {
            labels.Add(FrameProfiler.GetLabel(entries[pathId].labelId));
            pathId = entries[pathId].parentPathId;
        }
        labels.Reverse();
        return string.Join(" / ", labels);
    }

    private readonly CQFEditorSymmetry symmetry;
    private readonly MapComponent_CQFSymmetrySettings settings;
    private readonly Action<Map>? drawCallback;
    private readonly Action<bool, string> check;
    private readonly ProfilerPrefsData original;
    private readonly List<string> targets;
    private readonly List<string> oldTargets;
    private readonly bool oldInstrumentation;
    private readonly bool oldAlloc;
    private readonly bool oldMute;
    private readonly ProfilerSession session;
    private readonly string[] names = { "disabled-idle", "enabled-axes", "enabled-no-axes", "disabled-brush100", "enabled-brush100", "disabled-brush400" };
    private readonly IntVec3[] quarterCells = new CellRect(2, 2, 10, 10).ToArray();
    private readonly IntVec3[] fullCells;
    private Designator_Terrain? brush;
    private int sample;
    private int frame;
}
