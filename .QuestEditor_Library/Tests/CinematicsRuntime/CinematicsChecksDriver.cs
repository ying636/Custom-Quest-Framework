using System.Reflection;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CQF.CinematicsRuntimeChecks;

public sealed class CinematicsChecksDriver : MonoBehaviour
{
    private string Root => GenFilePaths.SaveDataFolderPath;
    private string Results => Path.Combine(Root, "CinematicsChecks.txt");
    private GameComponent_Cinematics Host => GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("Cinematic host is missing.");
    private GameComponent_CinematicsChecks State => Current.Game.GetComponent<GameComponent_CinematicsChecks>();
    private float Age => Time.realtimeSinceStartup - stageStarted;

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 330f) throw new TimeoutException("Cinematic runtime checks exceeded 330 seconds at stage " + stage);
            if (stage != 0 && Time.realtimeSinceStartup - stageStarted > 65f) throw new TimeoutException("Cinematic stage timed out: " + stage);
            if (stage == 0)
            {
                if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
                source = Find.CurrentMap;
                DebugSettings.pauseOnError = false;
                Directory.CreateDirectory(Path.Combine(Root, "Screenshots"));
                actor = source.mapPawns.FreeColonistsSpawned.First();
                actor.drafter.Drafted = true;
                actor.jobs.EndCurrentJob(JobCondition.InterruptForced);
                State.Actor = actor;
                State.Origin = actor.Position;
                State.Destination = GenRadial.RadialCellsAround(actor.Position, 9f, false)
                    .First(cell => cell.InBounds(source) && cell.DistanceToSquared(actor.Position) >= 25f && cell.Standable(source)
                        && !cell.GetThingList(source).Any(thing => thing is Pawn)
                        && source.reachability.CanReach(actor.Position, cell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)));
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                Find.CameraDriver.SetRootPosAndSize(actor.Position.ToVector3Shifted(), 22f);
                Check(CQFEditorBridge.IsLoaded && GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.QuestEditor_Cinematics") != null, "the staged editor module is loaded");
                Check(typeof(CQFCinematicDef).Assembly.Location.Contains("Snapshot", StringComparison.OrdinalIgnoreCase), "core assembly comes from the isolated snapshot");
                gate = CQFPresentationGate.For(Current.Game);
                Check(gate.TryAcquire(blocker), "simulated domain presentation acquires the shared gate");
                first = Play("CQF_Runtime_Visual");
                second = Play("CQF_Runtime_Queued");
                Go(1);
            }
            else if (stage == 1)
            {
                if (Age < 0.5f) return;
                Check(Host.Active == null && Host.Queue.Count == 2 && first!.Elapsed == 0f, "host queue and clocks wait while the domain presentation owns the gate");
                gate!.Release(blocker);
                Go(2);
            }
            else if (stage == 2)
            {
                if (!ReferenceEquals(Host.Active, first)) return;
                if (pausedTick < 0) pausedTick = Find.TickManager.TicksGame;
                if (first!.Elapsed < 1f) return;
                Check(Find.TickManager.TicksGame == pausedTick && Find.TickManager.Paused, "real-time cinematic advances while its window pauses game ticks");
                Check(first.Presentation.HasCamera && first.Presentation.LetterboxHeight > 0.1f
                    && first.Presentation.FadeColor.a > 0.35f && !string.IsNullOrEmpty(first.Presentation.Subtitle), "camera letterbox fade and subtitle have reached the visible state");
                Check(gate!.IsHeldBy(first) && second!.Outcome == CQFCinematicOutcome.Pending, "only the first cinematic owns the presentation");
                Shot("01_PausedOverlay");
                Go(3);
            }
            else if (stage == 3)
            {
                if (second!.Outcome != CQFCinematicOutcome.Completed) return;
                Check(first!.Outcome == CQFCinematicOutcome.Completed && State.Count("CQF_Runtime_VisualCompleted") == 1
                    && State.Count("CQF_Runtime_QueuedCompleted") == 1, "both queued cinematics finish and send their native completion signals once");
                Check(State.Events.IndexOf("CQF_Runtime_VisualCompleted") < State.Events.IndexOf("CQF_Runtime_QueuedCompleted"), "completion signal order follows the playback queue");
                Check(!gate!.IsBusy && !Find.WindowStack.Windows.OfType<CQFCinematicScreen>().Any(), "completion removes the overlay and releases presentation access");
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                current = Play("CQF_Runtime_Actor");
                Go(4);
            }
            else if (stage == 4)
            {
                if (current!.StepIndex < 1) return;
                Check(actor!.Position == State.Destination && actor.CurJob?.def == JobDefOf.Wait, "actor movement reaches its cell and starts the actual wait job");
                actorWaitTick = Find.TickManager.TicksGame;
                Shot("02_ActorWait");
                Go(5);
            }
            else if (stage == 5)
            {
                if (current!.Outcome != CQFCinematicOutcome.Completed) return;
                Check(actor!.Position == State.Origin && State.Count("CQF_Runtime_ActorCompleted") == 1, "actor wait and generic Goto job finish in the live game");
                Check(Find.TickManager.TicksGame - actorWaitTick >= 50, "actor wait receives genuine game ticks");
                current = Play("CQF_Runtime_Signal");
                Go(6);
            }
            else if (stage == 6)
            {
                if (!ReferenceEquals(Host.Active, current) || current!.Elapsed < 0.3f) return;
                Check(current.Outcome == CQFCinematicOutcome.Playing, "signal wait remains active before the native signal arrives");
                Find.SignalManager.SendSignal(new Signal("CQF_Runtime_Release"));
                Go(7);
            }
            else if (stage == 7)
            {
                if (current!.Outcome != CQFCinematicOutcome.Completed) return;
                Check(State.Count("CQF_Runtime_SignalCompleted") == 1, "native signal delivery releases the cinematic wait");
                current = Play("CQF_Runtime_InstantSuccess");
                Go(20);
            }
            else if (stage == 20)
            {
                if (current!.Outcome == CQFCinematicOutcome.Failed) throw new InvalidOperationException("Instant successful job was reported as failed.");
                if (current.Outcome != CQFCinematicOutcome.Completed) return;
                Check(State.Count("CQF_Runtime_InstantSucceeded") == 1, "instant successful actor job preserves its native completion result");
                File.AppendAllText(Results, "EXPECTED ERROR next cinematic intentionally runs an incompletable instant job\n");
                current = Play("CQF_Runtime_InstantFailure");
                Go(21);
            }
            else if (stage == 21)
            {
                if (current!.Outcome != CQFCinematicOutcome.Failed) return;
                if (failureObservedAt < 0f) failureObservedAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - failureObservedAt < 0.4f) return;
                Check(State.Count("CQF_Runtime_InstantFailed") == 1 && !gate!.IsBusy, "instant failed actor job sends the failure result and releases presentation access");
                foreach (Window log in Find.WindowStack.Windows.Where(window => window.GetType().Name == "EditWindow_Log").ToArray()) log.Close(false);
                File.AppendAllText(Results, "TRACE expected negative-test error remains in the logs; its log window was closed to continue UI checks\n");
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                current = Play("CQF_Runtime_Skip");
                Go(8);
            }
            else if (stage == 8)
            {
                if (!ReferenceEquals(Host.Active, current) || current!.Elapsed < 0.4f) return;
                Shot("03_SkipButton");
                Go(9);
            }
            else if (stage == 9)
            {
                if (!skipAttempted)
                {
                    if (Age < 0.3f || !File.Exists(ShotPath("03_SkipButton"))) return;
                    skipAttempted = true;
                    clickSkip = true;
                    return;
                }
                if (clickSkip) return;
                Check(current!.Outcome == CQFCinematicOutcome.Skipped && State.SkipActions == 1
                    && State.Count("CQF_Runtime_Skipped") == 1, "skip button mouse events execute completion actions and the skipped signal once");
                Check(!gate!.IsBusy, "skip releases presentation ownership");
                current = Play("CQF_Runtime_Cancel");
                Go(10);
            }
            else if (stage == 10)
            {
                if (!ReferenceEquals(Host.Active, current) || actor!.CurJob?.def != JobDefOf.Wait || current!.Elapsed < 0.3f) return;
                int job = actor.CurJob.loadID;
                Host.Stop(current);
                Check(current.Outcome == CQFCinematicOutcome.Cancelled && actor.CurJob?.loadID != job
                    && State.Count("CQF_Runtime_Cancelled") == 1, "cancellation stops the owned actor job and sends its result once");
                Check(!gate!.IsBusy && !Find.WindowStack.Windows.OfType<CQFCinematicScreen>().Any(), "cancellation removes input and camera locks");
                current = Play("CQF_Runtime_Save");
                Go(11);
            }
            else if (stage == 11)
            {
                if (!ReferenceEquals(Host.Active, current) || current!.Elapsed < 1f) return;
                Shot("04_BeforeSave");
                State.SavedElapsed = current.Elapsed;
                State.SavedStep = current.StepIndex;
                savedElapsed = State.SavedElapsed;
                savedActor = actor!.thingIDNumber;
                oldGame = Current.Game;
                oldHost = Host;
                oldGate = gate;
                GameDataSaveLoader.SaveGame("CQF_CinematicsRuntime");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("CQF_CinematicsRuntime")), "a complete native save file was written");
                Go(12);
            }
            else if (stage == 12)
            {
                if (Age < 0.5f || !File.Exists(ShotPath("04_BeforeSave"))) return;
                Go(13);
                GameDataSaveLoader.LoadGame("CQF_CinematicsRuntime");
            }
            else if (stage == 13)
            {
                if (Current.Game == oldGame || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
                if (Host.Active == null) return;
                current = Host.Active;
                gate = CQFPresentationGate.For(Current.Game);
                actor = State.Actor;
                source = actor?.Map;
                Check(State.LoadCount == 1 && actor?.thingIDNumber == savedActor, "full load reconstructs the game component and preserves the actor reference");
                Check(current.Context.ResolveTarget("Origin").Map == actor!.Map && current.Context.ResolveTarget("Origin").Cell == State.Origin,
                    "cinematic cell targets retain the loaded map and cell coordinates");
                Check(current.Definition.defName == "CQF_Runtime_Save" && current.StepIndex == State.SavedStep
                    && current.Elapsed >= savedElapsed && current.Elapsed < savedElapsed + 2f, "active cinematic resumes the saved cursor and elapsed time");
                Check(!ReferenceEquals(gate, oldGate) && oldHost!.Active == null && !oldGate!.IsBusy
                    && gate.IsHeldBy(current), "load cleans the previous game and reacquires the new game presentation gate");
                Check(!string.IsNullOrEmpty(current.Presentation.Subtitle), "saved subtitle is restored on the live overlay");
                Shot("05_AfterLoad");
                Go(14);
            }
            else if (stage == 14)
            {
                if (current!.Outcome != CQFCinematicOutcome.Completed) return;
                Check(State.Count("CQF_Runtime_SaveCompleted") == 1 && !gate!.IsBusy, "resumed cinematic finishes and sends one completion signal");
                current = Play("CQF_Runtime_FullCover");
                Go(22);
            }
            else if (stage == 22)
            {
                if (State.CoverActions == 0) return;
                Check(State.CoverActions == 1 && State.ObservedCoverVersion > 0 && current!.Presentation.FadeColor.a >= 1f,
                    "full black fade reaches a real opaque repaint before the following action executes");
                Shot("08_FullCover");
                Go(23);
            }
            else if (stage == 23)
            {
                if (current!.Outcome != CQFCinematicOutcome.Completed) return;
                Check(State.Count("CQF_Runtime_CoverCompleted") == 1, "full cover action completes its cinematic once");
                current = Play("CQF_Runtime_SaveJob");
                Go(24);
            }
            else if (stage == 24)
            {
                if (!ReferenceEquals(Host.Active, current) || current!.Elapsed < 0.4f || actor!.CurJob?.def.defName != "CQF_Runtime_DelayedJob") return;
                savedJob = actor.CurJob.loadID;
                oldGame = Current.Game;
                oldHost = Host;
                oldGate = gate;
                Shot("09_JobBeforeSave");
                GameDataSaveLoader.SaveGame("CQF_CinematicsJobRuntime");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("CQF_CinematicsJobRuntime")), "native save captures a running cinematic actor job");
                Go(25);
            }
            else if (stage == 25)
            {
                if (Age < 0.4f || !File.Exists(ShotPath("09_JobBeforeSave"))) return;
                Go(26);
                GameDataSaveLoader.LoadGame("CQF_CinematicsJobRuntime");
            }
            else if (stage == 26)
            {
                if (Current.Game == oldGame || Current.ProgramState != ProgramState.Playing || Host.Active == null) return;
                actor = State.Actor;
                source = actor?.Map;
                current = Host.Active;
                gate = CQFPresentationGate.For(Current.Game);
                Check(State.LoadCount == 2 && current.Definition.defName == "CQF_Runtime_SaveJob"
                    && actor?.CurJob?.loadID == savedJob, "full load preserves the active pawn job and its exact job identity");
                Check(!oldGate!.IsBusy && oldHost!.Active == null && gate.IsHeldBy(current), "job reload transfers presentation ownership to the reconstructed game");
                Shot("10_JobAfterLoad");
                Go(27);
            }
            else if (stage == 27)
            {
                if (current!.Outcome == CQFCinematicOutcome.Failed) throw new InvalidOperationException("Restored cinematic actor job failed.");
                if (current.Outcome != CQFCinematicOutcome.Completed) return;
                Check(State.Count("CQF_Runtime_SavedJobCompleted") == 1 && actor!.CurJob?.loadID != savedJob,
                    "restored actor job reports native success through its reattached finish listener");
                current = Play("CQF_Runtime_SaveDialog");
                Go(28);
            }
            else if (stage == 28)
            {
                dialog = Find.WindowStack.Windows.OfType<CQFDialogTreeWindow>().SingleOrDefault();
                if (!ReferenceEquals(Host.Active, current) || dialog == null || current!.Elapsed < 0.4f) return;
                dialog.GoToNode(1);
                Check(dialog.CurrentNodeIndex == 1 && Find.TickManager.Paused, "cinematic dialog advances to a new node while waiting for user input");
                oldGame = Current.Game;
                oldHost = Host;
                oldGate = gate;
                Shot("11_DialogBeforeSave");
                GameDataSaveLoader.SaveGame("CQF_CinematicsDialogRuntime");
                Check(File.Exists(GenFilePaths.FilePathForSavedGame("CQF_CinematicsDialogRuntime")), "native save captures a waiting cinematic dialog");
                Go(29);
            }
            else if (stage == 29)
            {
                if (Age < 0.4f || !File.Exists(ShotPath("11_DialogBeforeSave"))) return;
                Go(30);
                GameDataSaveLoader.LoadGame("CQF_CinematicsDialogRuntime");
            }
            else if (stage == 30)
            {
                if (Current.Game == oldGame || Current.ProgramState != ProgramState.Playing || Host.Active == null) return;
                dialog = Find.WindowStack.Windows.OfType<CQFDialogTreeWindow>().SingleOrDefault();
                if (dialog == null) return;
                actor = State.Actor;
                source = actor?.Map;
                current = Host.Active;
                gate = CQFPresentationGate.For(Current.Game);
                Check(State.LoadCount == 3 && current.Definition.defName == "CQF_Runtime_SaveDialog" && dialog.CurrentNodeIndex == 1,
                    "full load restores the waiting dialog at its current node without replaying the entry node");
                Check(Find.TickManager.Paused && !oldGate!.IsBusy && gate.IsHeldBy(current), "restored dialog retains its pause and exclusive presentation ownership");
                Shot("12_DialogAfterLoad");
                Go(31);
            }
            else if (stage == 31)
            {
                if (Age < 0.6f || !File.Exists(ShotPath("12_DialogAfterLoad"))) return;
                Check(current!.Outcome == CQFCinematicOutcome.Playing, "restored dialog continues waiting until its real window closes");
                dialog!.Close(false);
                Go(32);
            }
            else if (stage == 32)
            {
                if (current!.Outcome != CQFCinematicOutcome.Completed) return;
                Check(State.Count("CQF_Runtime_SavedDialogCompleted") == 1 && !Find.WindowStack.Windows.OfType<CQFDialogTreeWindow>().Any()
                    && !gate!.IsBusy, "closing the restored dialog completes once and removes its presentation locks");
                Type editorType = GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.QuestEditor_Cinematics");
                Type projectType = GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.CQFCinematicProject");
                object editorProject = Activator.CreateInstance(projectType, DefDatabase<CQFCinematicDef>.GetNamed("CQF_Runtime_Visual"))!;
                editor = (Window)Activator.CreateInstance(editorType, editorProject)!;
                Find.WindowStack.Add(editor);
                Go(15);
            }
            else if (stage == 15)
            {
                if (Age < 0.7f) return;
                Check(Find.WindowStack.Windows.Contains(editor!), "the real cinematic editor page opens in game");
                Shot("06_Editor");
                Go(16);
            }
            else if (stage == 16)
            {
                if (Age < 0.4f || !File.Exists(ShotPath("06_Editor"))) return;
                editor!.Close(false);
                current = Play("CQF_Runtime_Leaving");
                second = Play("CQF_Runtime_Queued");
                Go(17);
            }
            else if (stage == 17)
            {
                if (!ReferenceEquals(Host.Active, current)) return;
                oldGame = Current.Game;
                oldHost = Host;
                oldGate = gate;
                GenScene.GoToMainMenu();
                Go(18);
            }
            else if (stage == 18)
            {
                if (Current.ProgramState != ProgramState.Entry || Current.Game != null || Age < 1f) return;
                Check(oldHost!.Active == null && oldHost.Queue.Count == 0 && !oldGate!.IsBusy, "returning to the main menu clears active queued and presentation state");
                Check(!Find.WindowStack.Windows.OfType<CQFCinematicScreen>().Any(), "main menu retains no cinematic input overlay");
                Shot("07_MainMenuCleanup");
                Go(19);
            }
            else if (stage == 19 && Age > 0.5f && File.Exists(ShotPath("07_MainMenuCleanup"))) Finish(true);
            if (stage > 3 && stage < 13 && current?.Outcome == CQFCinematicOutcome.Failed)
                throw new InvalidOperationException("Live cinematic failed at runtime stage " + stage + ": " + current.Definition.defName);
        }
        catch (Exception error)
        {
            File.AppendAllText(Results, "FAIL " + error + "\n");
            Log.Error("[CQF] Cinematic runtime checks failed: " + error);
            Finish(false);
        }
    }

    private void OnGUI()
    {
        if (!clickSkip || Event.current.type != EventType.Repaint) return;
        Event previous = Event.current;
        object? backend = null;
        FieldInfo? physicalField = null;
        object? physicalValue = null;
        try
        {
            CQFCinematicScreen screen = Find.WindowStack.Windows.OfType<CQFCinematicScreen>().Single();
            Rect skip = screen.SkipButtonRect;
            skip.position += screen.windowRect.position;
            Window[] windows = Find.WindowStack.Windows.ToArray();
            int screenIndex = Array.IndexOf(windows, screen);
            Check(windows.Skip(screenIndex + 1).All(window => !window.windowRect.Overlaps(skip)), "skip button rectangle is unobstructed by higher windows");
            Vector2 point = skip.center;
            backend = UnityStubs.Gui.GuiBackend.Current ?? throw new InvalidOperationException("Native GUI backend is missing.");
            physicalField = backend.GetType().GetField("physMouse", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException("Native GUI backend physical mouse field is missing.");
            physicalValue = physicalField.GetValue(backend);
            physicalField.SetValue(backend, Activator.CreateInstance(physicalField.FieldType, point.x, point.y));
            Event.current = new Event { type = EventType.MouseDown, rawType = EventType.MouseDown, button = 0, mousePosition = point };
            GUI.DispatchInputToWindows();
            Event.current = new Event { type = EventType.MouseUp, rawType = EventType.MouseUp, button = 0, mousePosition = point };
            GUI.DispatchInputToWindows();
            clickSkip = false;
        }
        catch (Exception error)
        {
            File.AppendAllText(Results, "FAIL Skip input " + error + "\n");
            Finish(false);
        }
        finally
        {
            Event.current = previous;
            if (physicalValue != null)
            {
                physicalField!.SetValue(backend, physicalValue);
                MethodInfo update = backend!.GetType().GetMethod("UpdateEventMouse", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException("Native GUI backend mouse restoration method is missing.");
                update.Invoke(backend, null);
            }
        }
    }

    private CQFCinematicInstance Play(string name)
        => Host.Play(DefDatabase<CQFCinematicDef>.GetNamed(name), new()
        {
            ["Trigger"] = new TargetInfo(actor!),
            ["Position"] = new TargetInfo(State.Origin, actor!.Map),
            ["Origin"] = new TargetInfo(State.Origin, actor.Map),
            ["Destination"] = new TargetInfo(State.Destination, actor.Map)
        });

    private void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        File.AppendAllText(Results, "PASS " + name + "\n");
    }

    private void Go(int value)
    {
        stage = value;
        stageStarted = Time.realtimeSinceStartup;
        File.AppendAllText(Results, "TRACE stage=" + stage + ", realtime=" + stageStarted + "\n");
    }

    private string ShotPath(string name) => Path.Combine(Root, "Screenshots", "Cinematics_" + name + ".png");

    private void Shot(string name) => ScreenCapture.CaptureScreenshot(ShotPath(name));

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(Results, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private readonly object blocker = new();
    private Map? source;
    private Pawn? actor;
    private CQFPresentationGate? gate;
    private CQFPresentationGate? oldGate;
    private Game? oldGame;
    private GameComponent_Cinematics? oldHost;
    private CQFCinematicInstance? first;
    private CQFCinematicInstance? second;
    private CQFCinematicInstance? current;
    private Window? editor;
    private CQFDialogTreeWindow? dialog;
    private float stageStarted;
    private float savedElapsed;
    private float failureObservedAt = -1f;
    private int stage;
    private int pausedTick = -1;
    private int actorWaitTick;
    private int savedActor;
    private int savedJob;
    private bool clickSkip;
    private bool skipAttempted;
    private bool finished;
}
