using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class GameComponent_Cinematics : GameComponent, ISignalReceiver
{
    public GameComponent_Cinematics(Game game) { this.game = game; }

    public static GameComponent_Cinematics? Instance => Current.Game?.GetComponent<GameComponent_Cinematics>();
    public CQFCinematicInstance? Active => active;
    public IReadOnlyList<CQFCinematicInstance> Queue => queue;

    public CQFCinematicInstance Play(CQFCinematicDef definition, Dictionary<string, TargetInfo> targets, Quest? quest = null, bool preview = false)
    {
        if (disposed || !ReferenceEquals(game, Current.Game)) throw new InvalidOperationException("[CQF] Cinematic host is not current.");
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        string[] errors = definition.ConfigErrors().ToArray();
        if (errors.Length > 0) throw new InvalidDataException("[CQF] Invalid cinematic " + definition.defName + ":\n" + string.Join("\n", errors));
        if (queue.Count >= 64) throw new InvalidOperationException("[CQF] Cinematic queue is full.");
        CQFCinematicInstance instance = new(definition, targets, quest, preview);
        queue.Add(instance);
        EnsureLifetime();
        return instance;
    }

    public void Stop(CQFCinematicInstance instance, CQFCinematicOutcome outcome = CQFCinematicOutcome.Cancelled)
    {
        if (outcome is CQFCinematicOutcome.Pending or CQFCinematicOutcome.Playing) throw new ArgumentOutOfRangeException(nameof(outcome));
        if (outcome == CQFCinematicOutcome.Skipped && !instance.AllowSkip && !instance.IsPreview) return;
        if (!ReferenceEquals(instance, active) && !queue.Contains(instance)) return;
        bool current = ReferenceEquals(game, Current.Game) && Current.ProgramState == ProgramState.Playing;
        try { instance.Finish(outcome, current); }
        catch (Exception error)
        {
            instance.MarkFailed();
            Log.Error("[CQF] Cinematic termination failed: " + instance.Definition?.defName + "\n" + error);
        }
        finally
        {
            queue.Remove(instance);
            CQFPresentationGate.For(game).Release(instance);
            if (ReferenceEquals(active, instance))
            {
                active = null;
                CQFCinematicScreen? closing = screen;
                screen = null;
                closing?.Release();
            }
        }
        if (current)
        {
            try { instance.NotifyFinished(); }
            catch (Exception error) { Log.Error("[CQF] Cinematic result signal failed: " + error); }
        }
    }

    public void RegisterEditorControl(CQFCinematicInstance instance, bool paused = false)
    {
        if (disposed || !ReferenceEquals(game, Current.Game) || (!ReferenceEquals(active, instance) && !queue.Contains(instance)))
            throw new InvalidOperationException("[CQF] Cinematic editor control requires an instance owned by the current host.");
        instance.SetEditorControlled(true);
        instance.SetPreviewPaused(paused);
    }

    public void SetPreviewPaused(CQFCinematicInstance instance, bool paused)
    {
        ValidateManualControl(instance);
        instance.SetPreviewPaused(paused);
        if (screen != null) screen.forcePause = instance.PauseGame || paused;
        lastUpdate = Time.realtimeSinceStartup;
    }

    public void StepPreview(CQFCinematicInstance instance, float seconds = 1f / 60f)
    {
        ValidateManualControl(instance);
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f || seconds > 1f)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        SetPreviewPaused(instance, true);
        int ticks = instance.ConsumeControlTicks(seconds);
        if (!instance.IsPreview)
            for (int tick = 0; tick < ticks && ReferenceEquals(active, instance); tick++) Find.TickManager.DoSingleTick();
        if (ReferenceEquals(active, instance)) AdvanceActive(seconds, ticks);
    }

    public override void StartedNewGame() => Find.SignalManager.RegisterReceiver(this);

    public override void LoadedGame()
    {
        Find.SignalManager.RegisterReceiver(this);
        if (active != null) { queue.Insert(0, active); active = null; }
        lastUpdate = -1f;
        if (queue.Count > 0) EnsureLifetime();
    }

    public override void GameComponentTick()
    {
        if (disposed || Scribe.mode != LoadSaveMode.Inactive || active == null || active.PreviewPaused || active.IsPreview) return;
        AdvanceActive(0f, 1);
    }

    public override void GameComponentUpdate()
    {
        if (disposed || Scribe.mode != LoadSaveMode.Inactive) return;
        float now = Time.realtimeSinceStartup;
        float delta = lastUpdate < 0f ? 0f : Mathf.Max(0f, now - lastUpdate);
        lastUpdate = now;
        if (LongEventHandler.ShouldWaitForEvent) return;
        if (active == null)
        {
            if (queue.Count == 0 || !game.PlayerHasControl) return;
            CQFCinematicInstance candidate = queue[0];
            if (!CQFPresentationGate.For(game).TryAcquire(candidate)) return;
            queue.RemoveAt(0);
            active = candidate;
            try
            {
                active.Start();
                screen = new CQFCinematicScreen(this, active);
                screen.forcePause = active.PauseGame || active.PreviewPaused;
                Find.WindowStack.Add(screen);
                EnsureLifetime();
            }
            catch (Exception error)
            {
                Log.Error("[CQF] Cinematic start failed: " + candidate.Definition?.defName + "\n" + error);
                Stop(candidate, CQFCinematicOutcome.Failed);
                return;
            }
            delta = 0f;
        }
        if (!active.PreviewPaused) AdvanceActive(delta, active.IsPreview ? active.ConsumeControlTicks(delta) : 0);
    }

    public void Notify_SignalReceived(Signal signal)
    {
        CQFCinematicInstance? current = active;
        if (current == null) return;
        Dictionary<string, int> signals = current.SignalVersions;
        if (signals.TryGetValue(signal.tag, out int previous)) signals[signal.tag] = unchecked(previous + 1);
    }

    public override void ExposeData()
    {
        CQFCinematicInstance? savedActive = active?.IsPreview == true ? null : active;
        List<CQFCinematicInstance> savedQueue = Scribe.mode == LoadSaveMode.Saving ? queue.Where(instance => !instance.IsPreview).ToList() : queue;
        Scribe_Deep.Look(ref savedActive, "CQF_cinematic");
        Scribe_Collections.Look(ref savedQueue, "CQF_cinematicQueue", LookMode.Deep);
        if (Scribe.mode != LoadSaveMode.Saving)
        {
            active = savedActive;
            queue = savedQueue ?? new();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (CQFCinematicInstance instance in queue.ToArray()) Stop(instance);
        if (active != null) Stop(active);
        lifetime?.Release();
        lifetime = null;
    }

    private void AdvanceActive(float delta, int ticks)
    {
        if (active == null) return;
        CQFCinematicInstance current = active;
        try
        {
            if (current.Map == null || current.Map.Disposed || Find.CurrentMap != current.Map)
            {
                Stop(current, CQFCinematicOutcome.Cancelled);
                return;
            }
            if (current.Advance(delta, ticks) && ReferenceEquals(active, current)) Stop(current, CQFCinematicOutcome.Completed);
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic playback failed: " + current.Definition?.defName + ", step=" + current.StepIndex + "\n" + error);
            Stop(current, CQFCinematicOutcome.Failed);
        }
    }

    private void ValidateManualControl(CQFCinematicInstance instance)
    {
        if (disposed || !ReferenceEquals(game, Current.Game) || !ReferenceEquals(active, instance)
            || (!instance.IsPreview && !instance.IsEditorControlled) || instance.Outcome != CQFCinematicOutcome.Playing)
            throw new InvalidOperationException("[CQF] Manual cinematic control requires an active preview or registered editor test.");
    }

    private void EnsureLifetime()
    {
        if (lifetime != null) return;
        GameObject owner = new("CQF_CinematicLifetime");
        UnityEngine.Object.DontDestroyOnLoad(owner);
        lifetime = owner.AddComponent<CQFCinematicLifetime>();
        lifetime.Initialize(this, game);
    }

    private readonly Game game;
    private List<CQFCinematicInstance> queue = new();
    private CQFCinematicInstance? active;
    private CQFCinematicScreen? screen;
    private CQFCinematicLifetime? lifetime;
    private float lastUpdate = -1f;
    private bool disposed;
}
