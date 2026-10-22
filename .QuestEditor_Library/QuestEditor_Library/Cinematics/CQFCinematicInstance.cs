using RimWorld;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicInstance : IExposable
{
    public CQFCinematicInstance() { }

    public CQFCinematicInstance(CQFCinematicDef definition, Dictionary<string, TargetInfo> targets, Quest? quest = null, bool preview = false)
    {
        this.definition = definition;
        definitionName = definition.defName;
        runningGame = Current.Game;
        this.quest = quest;
        isPreview = preview;
        Map fallbackMap = targets.Values.Select(target => target.Map).FirstOrDefault(candidate => candidate != null) ?? Find.CurrentMap;
        if (fallbackMap == null) throw new InvalidOperationException("[CQF] A cinematic requires an active map.");
        targets = definition.ResolveBindings(fallbackMap, targets, quest);
        parameters = targets.Select(pair => new CQFCinematicTarget(pair.Key, pair.Value)).ToList();
        IEnumerable<Map> availableMaps = Current.Game.Maps;
        if (GameComponent_MapManager.Instance != null) availableMaps = availableMaps.Concat(GameComponent_MapManager.Instance.HibernatingMaps);
        HashSet<string> referenced = definition.GetReferencedTargetKeys();
        Map? selectedMap = definition.roles.Where(role => referenced.Contains(role.key))
            .Select(role => targets[role.key])
            .Select(target => target.Map ?? availableMaps.FirstOrDefault(candidate => !candidate.Disposed
                && target.Thing != null && candidate.spawnedThings.Contains(target.Thing)))
            .FirstOrDefault(candidate => candidate != null);
        selectedMap ??= definition.bindings.Where(binding => referenced.Contains(binding.key))
            .Select(binding => targets[binding.key])
            .Select(target => target.Map ?? availableMaps.FirstOrDefault(candidate => !candidate.Disposed
                && target.Thing != null && candidate.spawnedThings.Contains(target.Thing)))
            .FirstOrDefault(candidate => candidate != null);
        selectedMap ??= targets.TryGetValue("Position", out TargetInfo position) && position.Map != null ? position.Map
            : targets.Values.Select(target => target.Map).FirstOrDefault(candidate => candidate != null);
        if (selectedMap == null)
        {
            selectedMap = availableMaps.FirstOrDefault(candidate => !candidate.Disposed
                && targets.Values.Any(target => target.Thing != null && candidate.spawnedThings.Contains(target.Thing))) ?? Find.CurrentMap;
        }
        map = selectedMap ?? throw new InvalidOperationException("[CQF] A cinematic requires an active map.");
        if (!parameters.Any(pair => pair.key == "Position"))
            parameters.Add(new CQFCinematicTarget("Position", new TargetInfo(map.Center, map)));
        steps = definition.steps.Select(step => step.Copy()).ToList();
        XmlDocument actionDocument = new();
        actionDocument.LoadXml(CQFSerialization.SaveList_Saveable(definition.skipActions, "actions").ToString());
        skipActions = DirectXmlToObject.ObjectFromXml<List<CQFAction>>(actionDocument.DocumentElement, false);
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        allowSkip = definition.allowSkip;
        lockInput = definition.lockInput;
        restoreCamera = definition.restoreCamera;
        pauseGame = definition.pauseGame;
        maxDuration = definition.maxDuration;
        completedSignal = definition.completedSignal;
        skippedSignal = definition.skippedSignal;
        cancelledSignal = definition.cancelledSignal;
        failedSignal = definition.failedSignal;
        addQuestPrefix = definition.addQuestPrefix;
    }

    public CQFCinematicDef Definition => definition;
    public Map Map => map;
    public Quest? Quest => quest;
    public bool IsPreview => isPreview;
    public bool IsEditorControlled => isEditorControlled;
    public bool PreviewPaused => previewPaused;
    public bool IsCurrentGame => ReferenceEquals(runningGame, Current.Game);
    public CQFCinematicOutcome Outcome => outcome;
    public CQFCinematicContext Context => context ??= new CQFCinematicContext(this);
    public Dictionary<string, TargetInfo> Targets => targets ??= parameters.ToDictionary(pair => pair.key, pair => pair.Target);
    public CQFCinematicPresentation Presentation => presentation;
    public Dictionary<string, int> SignalVersions => signalVersions;
    public bool AllowSkip => allowSkip;
    public bool LockInput => lockInput;
    public bool PauseGame => pauseGame;
    public float Elapsed => elapsed;
    public int StepIndex => state.cursor;
    public IReadOnlyList<CQFCinematicStep> Steps => steps;
    public string? FailedStepPath => failedStepPath;

    public void Start()
    {
        runningGame = Current.Game;
        if (outcome == CQFCinematicOutcome.Pending)
        {
            originalMap = Find.CurrentMap;
            originalCamera = Find.Camera.transform.position;
            originalSize = Find.CameraDriver.RootSize;
            cameraCaptured = true;
            outcome = CQFCinematicOutcome.Playing;
        }
        if (map == null || map.Disposed) throw new InvalidOperationException("[CQF] Cinematic map no longer exists.");
        GameComponent_MapManager.Instance?.Activate(map);
        Current.Game.CurrentMap = map;
        for (int index = 0; index < steps.Count; index++)
        {
            CQFCinematicStepState child = state.Child(index);
            if (child.entered && !child.completed) steps[index].Restore(Context, child);
        }
    }

    public bool Advance(float realSeconds, int gameTicks)
    {
        if (outcome != CQFCinematicOutcome.Playing) return false;
        if (float.IsNaN(realSeconds) || float.IsInfinity(realSeconds) || realSeconds < 0f || gameTicks < 0)
            throw new ArgumentOutOfRangeException(nameof(realSeconds));
        elapsed += realSeconds;
        if (elapsed > maxDuration) throw new TimeoutException("[CQF] Cinematic exceeded its timeout: " + definition?.defName);
        while (state.cursor < steps.Count)
        {
            if (outcome != CQFCinematicOutcome.Playing) return false;
            if (!steps[state.cursor].Advance(Context, state.Child(state.cursor), realSeconds, gameTicks)) return false;
            state.cursor++;
            realSeconds = 0f;
            gameTicks = 0;
        }
        return true;
    }

    public void Finish(CQFCinematicOutcome result, bool restoreView = true)
    {
        if (outcome is not (CQFCinematicOutcome.Pending or CQFCinematicOutcome.Playing)) return;
        if (result is CQFCinematicOutcome.Pending or CQFCinematicOutcome.Playing) throw new ArgumentOutOfRangeException(nameof(result));
        outcome = result;
        try
        {
            for (int index = 0; index < steps.Count; index++)
            {
                try { steps[index].Cancel(Context, state.Child(index)); }
                catch (Exception error)
                {
                    outcome = CQFCinematicOutcome.Failed;
                    Log.Error("[CQF] Cinematic step cleanup failed: " + steps[index].GetType().Name + "\n" + error);
                }
            }
            if (result == CQFCinematicOutcome.Skipped && !isPreview)
                foreach (CQFAction action in skipActions) action.Work(Targets, quest!);
        }
        catch
        {
            outcome = CQFCinematicOutcome.Failed;
            throw;
        }
        finally
        {
            presentation = new CQFCinematicPresentation();
            if (restoreView && restoreCamera && cameraCaptured && originalMap != null && !originalMap.Disposed && Current.Game != null)
            {
                Current.Game.CurrentMap = originalMap;
                Find.CameraDriver.SetRootPosAndSize(originalCamera, originalSize);
            }
        }
    }

    public void NotifyFinished()
    {
        if (notified) return;
        notified = true;
        Context.SendSignal(outcome switch
        {
            CQFCinematicOutcome.Completed => completedSignal,
            CQFCinematicOutcome.Skipped => skippedSignal,
            CQFCinematicOutcome.Cancelled => cancelledSignal,
            CQFCinematicOutcome.Failed => failedSignal,
            _ => ""
        }, addQuestPrefix);
    }

    internal void MarkFailed() => outcome = CQFCinematicOutcome.Failed;

    internal void RecordStepFailure(CQFCinematicStep step)
    {
        failedStepPath ??= FindStepPath(steps, step, "");
    }

    internal void SetEditorControlled(bool controlled) => isEditorControlled = controlled;

    internal void SetPreviewPaused(bool paused) => previewPaused = paused;

    internal int ConsumeControlTicks(float seconds)
    {
        controlTickRemainder += seconds * 60f;
        int ticks = Mathf.FloorToInt(controlTickRemainder);
        controlTickRemainder -= ticks;
        return ticks;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref definitionName, "definition", "");
        Scribe_References.Look(ref map, "map");
        Scribe_References.Look(ref quest, "quest");
        Scribe_References.Look(ref originalMap, "originalMap");
        Scribe_Values.Look(ref outcome, "outcome");
        Scribe_Values.Look(ref elapsed, "elapsed");
        Scribe_Values.Look(ref allowSkip, "allowSkip", true);
        Scribe_Values.Look(ref lockInput, "lockInput", true);
        Scribe_Values.Look(ref restoreCamera, "restoreCamera", true);
        Scribe_Values.Look(ref pauseGame, "pauseGame");
        Scribe_Values.Look(ref maxDuration, "maxDuration", 600f);
        Scribe_Values.Look(ref completedSignal, "completedSignal", "");
        Scribe_Values.Look(ref skippedSignal, "skippedSignal", "");
        Scribe_Values.Look(ref cancelledSignal, "cancelledSignal", "");
        Scribe_Values.Look(ref failedSignal, "failedSignal", "");
        Scribe_Values.Look(ref addQuestPrefix, "addQuestPrefix", true);
        Scribe_Values.Look(ref originalCamera, "originalCamera");
        Scribe_Values.Look(ref originalSize, "originalSize");
        Scribe_Values.Look(ref cameraCaptured, "cameraCaptured");
        Scribe_Values.Look(ref notified, "notified");
        Scribe_Deep.Look(ref state, "state");
        Scribe_Deep.Look(ref presentation, "presentation");
        Scribe_Collections.Look(ref parameters, "parameters", LookMode.Deep);
        Scribe_Collections.Look(ref steps, "steps", LookMode.Deep);
        Scribe_Collections.Look(ref skipActions, "skipActions", LookMode.Deep);
        Scribe_Collections.Look(ref signalVersions, "signals", LookMode.Value, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            parameters ??= new(); steps ??= new(); skipActions ??= new(); signalVersions ??= new();
            state ??= new(); presentation ??= new(); context = null; targets = null;
            if (string.IsNullOrWhiteSpace(definitionName) || map == null || state.cursor < 0 || state.cursor > steps.Count)
                throw new InvalidDataException("[CQF] Invalid saved cinematic instance.");
            definition = DefDatabase<CQFCinematicDef>.GetNamedSilentFail(definitionName) ?? new CQFCinematicDef { defName = definitionName };
        }
    }

    private static string? FindStepPath(List<CQFCinematicStep> entries, CQFCinematicStep failed, string prefix)
    {
        for (int index = 0; index < entries.Count; index++)
        {
            string path = prefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (ReferenceEquals(entries[index], failed)) return path;
            List<CQFCinematicStep>? children = entries[index] switch
            {
                CQFCinematicStep_Sequence sequence => sequence.steps,
                CQFCinematicStep_Parallel parallel => parallel.steps,
                _ => null
            };
            if (children != null && FindStepPath(children, failed, path + "/") is string found) return found;
        }
        return null;
    }

    private CQFCinematicDef definition = null!;
    private string definitionName = "";
    private Map map = null!;
    private Quest? quest;
    private bool isPreview;
    private bool isEditorControlled;
    private bool previewPaused;
    private float controlTickRemainder;
    private CQFCinematicOutcome outcome;
    private CQFCinematicStepState state = new();
    private CQFCinematicPresentation presentation = new();
    private List<CQFCinematicTarget> parameters = new();
    private List<CQFCinematicStep> steps = new();
    private List<CQFAction> skipActions = new();
    private Dictionary<string, int> signalVersions = new();
    private float elapsed;
    private bool allowSkip = true;
    private bool lockInput = true;
    private bool restoreCamera = true;
    private bool pauseGame;
    private float maxDuration = 600f;
    private string completedSignal = "";
    private string skippedSignal = "";
    private string cancelledSignal = "";
    private string failedSignal = "";
    private string? failedStepPath;
    private bool addQuestPrefix = true;
    private Map? originalMap;
    private Vector3 originalCamera;
    private float originalSize;
    private bool cameraCaptured;
    private bool notified;
    private CQFCinematicContext? context;
    private Dictionary<string, TargetInfo>? targets;
    private Game? runningGame;
}
