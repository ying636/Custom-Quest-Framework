using RimWorld;
using System.Xml;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicTestSession
{
    private CQFCinematicTestSession(CQFCinematicProject project, Action<CQFCinematicProject> reopen, CQFCinematicStep? selected, bool useRehearsal)
    {
        this.project = project.CloneForTest();
        testProject = project.CloneForTest();
        foreach (KeyValuePair<string, TargetInfo> target in project.Targets) testProject.Targets.Add(target.Key, target.Value);
        if (selected != null)
        {
            testProject.Definition.steps = new List<CQFCinematicStep> { selected.Copy() };
            testProject.Definition.skipActions.Clear();
            testProject.Definition.completedSignal = "";
            testProject.Definition.skippedSignal = "";
            testProject.Definition.cancelledSignal = "";
            testProject.Definition.failedSignal = "";
        }
        testProject.CaptureBindings();
        testProject.Targets.Clear();
        selectedPath = selected == null ? null : project.StepPath(selected);
        UsesRehearsal = useRehearsal;
        if (!useRehearsal) testProject.RehearsalBindings.Clear();
        this.reopen = reopen;
        originalGame = Verse.Current.Game;
        originalTimeSpeed = Find.TickManager.CurTimeSpeed;
        originalPermadeathName = originalGame.Info.permadeathModeUniqueName;
        worldSeed = originalGame.World.info.seedString;
        worldID = originalGame.World.info.persistentRandomValue;
        checkpointName = "CQF_CinematicTest_" + Guid.NewGuid().ToString("N");
        savesDirectory = Path.GetFullPath(GenFilePaths.SavedGamesFolderPath);
        checkpointPath = Path.GetFullPath(GenFilePaths.FilePathForSavedGame(checkpointName));
        ValidateOwnedPath(checkpointPath);
        if (File.Exists(checkpointPath)) throw new IOException("CQF_Cinematic_TestCheckpointFailed".Translate(checkpointPath));
    }

    public static CQFCinematicTestSession? Current => current;
    public bool IsBusy => phase is not (TestPhase.Finished or TestPhase.Interrupted);
    public CQFCinematicInstance? Instance => instance;
    public bool Paused => paused;
    public bool UsesRehearsal { get; }
    public string? FailedStepPath { get; private set; }

    public static CQFCinematicTestSession Start(CQFCinematicProject project, Action<CQFCinematicProject> reopen, bool selectedOnly = false, CQFCinematicStep? selected = null)
        => Start(project, reopen, selectedOnly, selected, true);

    public static CQFCinematicTestSession Start(CQFCinematicProject project, Action<CQFCinematicProject> reopen, bool selectedOnly, CQFCinematicStep? selected, bool useRehearsal)
    {
        if (current != null || CurrentGameUnavailable()) throw new InvalidOperationException("CQF_Cinematic_TestBusy".Translate());
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (reopen == null) throw new ArgumentNullException(nameof(reopen));
        if (selectedOnly && selected == null) throw new ArgumentNullException(nameof(selected));
        GameComponent_Cinematics host = GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("CQF_Cinematic_TestBusy".Translate());
        if (host.Active?.IsPreview == false || host.Queue.Any(candidate => !candidate.IsPreview))
            throw new InvalidOperationException("CQF_Cinematic_TestBusy".Translate());
        foreach (CQFCinematicInstance preview in host.Queue.ToArray()) host.Stop(preview);
        if (host.Active is { } activePreview) host.Stop(activePreview);
        if (CQFPresentationGate.For(Verse.Current.Game).IsBusy)
            throw new InvalidOperationException("CQF_Cinematic_TestBusy".Translate());
        project.RefreshPreviewTexts();
        CQFCinematicTestSession session = new(project, reopen, selectedOnly ? selected : null, useRehearsal);
        string[] errors = session.testProject.Definition.ConfigErrors().ToArray();
        if (errors.Length > 0) throw new InvalidDataException("CQF_Cinematic_InvalidProject".Translate() + "\n" + string.Join("\n", errors));
        session.testProject.RebindTargets();
        _ = session.testProject.Quest;
        current = session;
        try
        {
            session.guard = new CQFCinematicTestSaveGuard(session);
            GameDataSaveLoader.SaveGame(session.checkpointName);
            session.VerifyCheckpoint();
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic checkpoint save failed: " + session.checkpointPath + "\n" + error);
            session.Finish(false);
            throw new IOException("CQF_Cinematic_TestCheckpointFailed".Translate(session.checkpointPath), error);
        }
        GameObject owner = new("CQF_CinematicTestSession");
        UnityEngine.Object.DontDestroyOnLoad(owner);
        session.driver = owner.AddComponent<CQFCinematicTestDriver>();
        session.driver.Initialize(session);
        try { session.BeginLoad(false); }
        catch (Exception error) { session.HandleFailure(error); }
        return session;
    }

    public void Pause(bool value)
    {
        if (!IsBusy) return;
        paused = value;
        if (instance?.IsCurrentGame != true) return;
        if (instance.Outcome == CQFCinematicOutcome.Pending)
            GameComponent_Cinematics.Instance!.RegisterEditorControl(instance, value);
        else if (instance.Outcome == CQFCinematicOutcome.Playing)
            GameComponent_Cinematics.Instance!.SetPreviewPaused(instance, value);
    }

    public void Step(float seconds = 1f / 60f)
    {
        if (instance?.Outcome == CQFCinematicOutcome.Pending && instance.IsCurrentGame)
        {
            Pause(true);
            return;
        }
        if (instance?.Outcome != CQFCinematicOutcome.Playing || !instance.IsCurrentGame) return;
        paused = true;
        GameComponent_Cinematics.Instance!.StepPreview(instance, seconds);
    }

    public void StopAndReturn()
    {
        if (!IsBusy || phase == TestPhase.Restoring) return;
        returnRequested = true;
        if (phase == TestPhase.LoadingTest) return;
        try { BeginLoad(true); }
        catch (Exception error) { HandleFailure(error); }
    }

    internal bool ShouldBlockSave()
    {
        return OwnsCurrentGame();
    }

    internal bool ShouldBlockLoadingTick()
    {
        return (phase is TestPhase.LoadingTest or TestPhase.Restoring or TestPhase.AwaitingPlayback)
            && OwnsCurrentGame();
    }

    internal void NotifySaveBlocked()
    {
        if (!saveBlockReported)
        {
            saveBlockReported = true;
            Log.Message("[CQF] Cinematic test isolation is blocking normal game saves until the checkpoint is restored.");
        }
        float now = Time.realtimeSinceStartup;
        if (now - lastSaveNotice < 2f) return;
        lastSaveNotice = now;
        Messages.Message("CQF_Cinematic_TestSaveBlocked".Translate(), MessageTypeDefOf.RejectInput, false);
    }

    internal void Update()
    {
        if (!IsBusy) return;
        try
        {
            if (phase is TestPhase.LoadingTest or TestPhase.Restoring)
            {
                if (Time.realtimeSinceStartup > loadDeadline) throw new TimeoutException("[CQF] Cinematic checkpoint load timed out before its world became playable.");
                if (LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
                Game? loadedGame = Verse.Current.Game;
                if (Verse.Current.ProgramState != ProgramState.Playing || loadedGame == null || ReferenceEquals(loadedGame, gameBeforeLoad)
                    || !loadedGame.PlayerHasControl || Find.CurrentMap == null || Find.CurrentMap.Disposed
                    || !loadedGame.Maps.Contains(Find.CurrentMap) || loadedGame.World?.info == null
                    || loadedGame.World.info.seedString != worldSeed || loadedGame.World.info.persistentRandomValue != worldID) return;
                if (phase == TestPhase.Restoring) CompleteReturn();
                else StartPlayback();
                return;
            }
            if (!ReferenceEquals(Verse.Current.Game, testGame) || Verse.Current.ProgramState != ProgramState.Playing)
            {
                Interrupt(new InvalidOperationException("[CQF] Cinematic test game was closed before checkpoint recovery."));
                return;
            }
            if (phase == TestPhase.AwaitingPlayback)
            {
                if (instance?.Outcome == CQFCinematicOutcome.Playing) phase = TestPhase.Playing;
                else if (instance?.Outcome != CQFCinematicOutcome.Pending) BeginLoad(true);
                else if (Time.realtimeSinceStartup > loadDeadline) throw new TimeoutException("[CQF] Cinematic test did not start.");
            }
            else if (instance?.Outcome is not (CQFCinematicOutcome.Pending or CQFCinematicOutcome.Playing)) BeginLoad(true);
        }
        catch (Exception error) { HandleFailure(error); }
    }

    internal void NotifyApplicationQuit()
    {
        if (!IsBusy) return;
        if (OwnsCurrentGame()) Verse.Current.Game.Info.permadeathMode = false;
        Interrupt(new InvalidOperationException("[CQF] Application exited during cinematic testing."));
    }

    internal void NotifyDriverLost()
    {
        if (IsBusy) Interrupt(new InvalidOperationException("[CQF] Cinematic test recovery controller was destroyed."));
    }

    private static bool CurrentGameUnavailable()
    {
        return Verse.Current.ProgramState != ProgramState.Playing || Verse.Current.Game == null || Find.CurrentMap == null
            || !Verse.Current.Game.PlayerHasControl || Scribe.mode != LoadSaveMode.Inactive
            || LongEventHandler.AnyEventNowOrWaiting || GameDataSaveLoader.SavingIsTemporarilyDisabled;
    }

    private bool OwnsCurrentGame()
    {
        Game? game = Verse.Current.Game;
        return (phase is TestPhase.LoadingTest or TestPhase.AwaitingPlayback or TestPhase.Playing or TestPhase.Restoring)
            && game != null && !ReferenceEquals(game, originalGame) && game.World != null
            && game.World.info.seedString == worldSeed && game.World.info.persistentRandomValue == worldID;
    }

    private void BeginLoad(bool restoring)
    {
        VerifyCheckpoint();
        if (restoring && instance?.Outcome == CQFCinematicOutcome.Failed)
        {
            string? failed = instance.FailedStepPath ?? (instance.StepIndex >= 0 && instance.StepIndex < testProject.Definition.steps.Count
                ? instance.StepIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
            FailedStepPath = selectedPath == null ? failed : failed is { Length: > 1 } && failed.StartsWith("0/", StringComparison.Ordinal)
                ? selectedPath + failed.Substring(1) : selectedPath;
        }
        if (restoring && instance?.IsCurrentGame == true)
            GameComponent_Cinematics.Instance?.Stop(instance);
        phase = restoring ? TestPhase.Restoring : TestPhase.LoadingTest;
        gameBeforeLoad = Verse.Current.Game;
        loadDeadline = Time.realtimeSinceStartup + 600f;
        GameDataSaveLoader.LoadGame(checkpointName);
    }

    private void StartPlayback()
    {
        testGame = Verse.Current.Game;
        if (testGame.World.info.seedString != worldSeed || testGame.World.info.persistentRandomValue != worldID)
            throw new InvalidDataException("[CQF] Cinematic checkpoint loaded a different world.");
        Find.TickManager.CurTimeSpeed = originalTimeSpeed == TimeSpeed.Paused ? TimeSpeed.Normal : originalTimeSpeed;
        testProject.RebindTargets();
        GameComponent_Cinematics host = GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("[CQF] Cinematic host is unavailable after checkpoint loading.");
        instance = host.Play(testProject.Definition, testProject.Targets, testProject.Quest, false);
        host.RegisterEditorControl(instance, paused);
        phase = TestPhase.AwaitingPlayback;
        loadDeadline = Time.realtimeSinceStartup + 60f;
        Find.WindowStack.Add(new Window_CinematicTestPlayback(this));
        if (returnRequested) BeginLoad(true);
    }

    private void CompleteReturn()
    {
        if (Verse.Current.Game.World.info.seedString != worldSeed || Verse.Current.Game.World.info.persistentRandomValue != worldID)
            throw new InvalidDataException("[CQF] Cinematic recovery loaded a different world.");
        Verse.Current.Game.Info.permadeathModeUniqueName = originalPermadeathName;
        Find.TickManager.CurTimeSpeed = originalTimeSpeed;
        project.RebindTargets();
        _ = project.Quest;
        reopen(project);
        Finish(true);
    }

    private void HandleFailure(Exception error)
    {
        Log.Error("[CQF] Cinematic test failed: " + checkpointPath + "\n" + error);
        if (phase == TestPhase.Restoring)
        {
            NotifyRecovery("CQF_Cinematic_TestRecoveryFailed");
            Finish(false);
            return;
        }
        if (Verse.Current.ProgramState == ProgramState.Playing && Verse.Current.Game != null)
            Messages.Message("CQF_Cinematic_TestFailed".Translate(error.Message.CanTranslate() ? error.Message.Translate().ToString() : error.Message), MessageTypeDefOf.RejectInput, false);
        try { BeginLoad(true); }
        catch (Exception restoreError)
        {
            Log.Error("[CQF] Cinematic checkpoint recovery could not start: " + checkpointPath + "\n" + restoreError);
            NotifyRecovery("CQF_Cinematic_TestRecoveryFailed");
            Finish(false);
        }
    }

    private void Interrupt(Exception error)
    {
        Log.Error("[CQF] Cinematic test interrupted; checkpoint retained: " + checkpointPath + "\n" + error);
        NotifyRecovery("CQF_Cinematic_TestInterrupted");
        Finish(false);
    }

    private void NotifyRecovery(string key)
    {
        string message = key.Translate(checkpointPath);
        Log.Error(message);
        if (Verse.Current.ProgramState == ProgramState.Playing && Verse.Current.Game != null)
            Messages.Message(message, MessageTypeDefOf.RejectInput, false);
    }

    private void VerifyCheckpoint()
    {
        ValidateOwnedPath(checkpointPath);
        if (!File.Exists(checkpointPath) || new FileInfo(checkpointPath).Length == 0)
            throw new IOException("CQF_Cinematic_TestCheckpointFailed".Translate(checkpointPath));
        using XmlReader reader = XmlReader.Create(checkpointPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        bool root = false, game = false, world = false;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.Depth == 0) root = reader.Name == "savegame";
            else if (reader.Depth == 1 && reader.Name == "game") game = root;
            else if (reader.Depth == 2 && reader.Name == "world") world = game;
        }
        if (!root || !game || !world)
            throw new InvalidDataException("CQF_Cinematic_TestCheckpointFailed".Translate(checkpointPath));
    }

    private void ValidateOwnedPath(string path)
    {
        string absolute = Path.GetFullPath(path);
        if (!string.Equals(Path.GetDirectoryName(absolute), savesDirectory, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(absolute, checkpointPath, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetFileName(absolute), checkpointName + ".rws", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("[CQF] Refusing a cinematic checkpoint path outside the owned Saves file.");
    }

    private void Finish(bool deleteCheckpoint)
    {
        if (!deleteCheckpoint && OwnsCurrentGame())
        {
            Verse.Current.Game.Info.permadeathMode = false;
            if (instance?.IsCurrentGame == true) GameComponent_Cinematics.Instance?.Stop(instance);
            if (Verse.Current.ProgramState == ProgramState.Playing && Verse.Current.Game.PlayerHasControl)
                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        }
        phase = TestPhase.Finished;
        try { guard?.Dispose(); }
        catch (Exception error) { Log.Error("[CQF] Cinematic save guard removal failed: " + error); }
        guard = null;
        if (ReferenceEquals(current, this)) current = null;
        CQFCinematicTestDriver? closing = driver;
        driver = null;
        closing?.Release();
        if (!deleteCheckpoint) return;
        try
        {
            ValidateOwnedPath(checkpointPath);
            File.Delete(checkpointPath);
        }
        catch (Exception error) { Log.Error("[CQF] Cinematic checkpoint cleanup failed: " + checkpointPath + "\n" + error); }
    }

    private enum TestPhase { Saving, LoadingTest, AwaitingPlayback, Playing, Restoring, Finished, Interrupted }

    private static CQFCinematicTestSession? current;
    private readonly CQFCinematicProject project;
    private readonly CQFCinematicProject testProject;
    private readonly Action<CQFCinematicProject> reopen;
    private readonly Game originalGame;
    private readonly TimeSpeed originalTimeSpeed;
    private readonly string originalPermadeathName;
    private readonly string worldSeed;
    private readonly int worldID;
    private readonly string checkpointName;
    private readonly string checkpointPath;
    private readonly string savesDirectory;
    private readonly string? selectedPath;
    private CQFCinematicInstance? instance;
    private CQFCinematicTestSaveGuard? guard;
    private CQFCinematicTestDriver? driver;
    private Game? testGame;
    private Game? gameBeforeLoad;
    private TestPhase phase;
    private bool paused;
    private bool returnRequested;
    private bool saveBlockReported;
    private float lastSaveNotice = float.NegativeInfinity;
    private float loadDeadline;
}
