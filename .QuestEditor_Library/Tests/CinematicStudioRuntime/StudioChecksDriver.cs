using System.Reflection;
using System.Text;
using System.Xml.Linq;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using EngineApplication = Waylith.EngineApplication;

namespace CQF.CinematicStudioChecks;

public sealed class StudioChecksDriver : MonoBehaviour
{
    private string Root => GenFilePaths.SaveDataFolderPath;
    private string Results => Path.Combine(Root, "StudioChecks.txt");
    private string ProviderRoot => Path.GetDirectoryName(CQFContentPaths.Quests)!;
    private GameComponent_Cinematics Host => GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("Studio cinematic host is missing.");
    private float Age => Time.realtimeSinceStartup - stageStarted;
    private CQFCinematicTimeline Timeline => Private<CQFCinematicTimeline>(studio!, "timeline");

    private void Update()
    {
        if (finished) return;
        try
        {
            if (startedAt > 0f && Time.realtimeSinceStartup - startedAt > 180f)
                throw new TimeoutException("Studio checks exceeded 180 seconds at " + stage);
            if (startedAt == 0f && Time.realtimeSinceStartup > 220f)
                throw new TimeoutException("Studio quicktest did not reach a playable map.");
            if (LongEventHandler.AnyEventNowOrWaiting || LongEventHandler.ShouldWaitForEvent) return;
            if (stage != Stage.Boot && EngineApplication.Instance.RenderContext?.IsFramebufferMinimized() == true)
                throw new InvalidOperationException("The -NoWindow test framebuffer is minimized; no window will be restored or activated.");
            switch (stage)
            {
                case Stage.Boot:
                    if (Current.ProgramState != ProgramState.Playing || Current.Game == null || Find.CurrentMap == null || !Current.Game.PlayerHasControl) return;
                    Initialize();
                    break;
                case Stage.EnglishLayout:
                    if (Age < 0.4f) return;
                    RecordAndSave();
                    Select(cameraStep!);
                    Go(Stage.EnglishWorkspace);
                    break;
                case Stage.EnglishWorkspace:
                    if (Age >= 0.35f) Capture("01_English_Workspace", Stage.EnglishSubtitle);
                    break;
                case Stage.EnglishSubtitle:
                    if (Age >= 0.35f) Capture("02_English_SubtitleInspector", Stage.EnglishRolesSetup);
                    break;
                case Stage.EnglishRolesSetup:
                    studio!.OpenRoleBindings(roleKey);
                    Go(Stage.EnglishRoles);
                    break;
                case Stage.EnglishRoles:
                    if (Age >= 0.35f) Capture("06_English_RoleBindings", Stage.ChineseSetup);
                    break;
                case Stage.ChineseSetup:
                    SetLanguage("ChineseSimplified (简体中文)");
                    studio!.Close(false);
                    studio = new QuestEditor_Cinematics(project!);
                    Find.WindowStack.Add(studio);
                    Select(cameraStep!);
                    Go(Stage.ChineseWorkspace);
                    break;
                case Stage.ChineseWorkspace:
                    if (Age >= 0.35f) Capture("03_Chinese_Workspace", Stage.ChineseSubtitle);
                    break;
                case Stage.ChineseSubtitle:
                    if (Age >= 0.35f) Capture("04_Chinese_SubtitleInspector", Stage.ChineseRolesSetup);
                    break;
                case Stage.ChineseRolesSetup:
                    studio!.OpenRoleBindings(roleKey);
                    Go(Stage.ChineseRoles);
                    break;
                case Stage.ChineseRoles:
                    if (Age >= 0.35f) Capture("07_Chinese_RoleBindings", Stage.PreviewSetup);
                    break;
                case Stage.WaitScreenshot:
                    WaitScreenshot();
                    break;
                case Stage.PreviewSetup:
                    StartPreview();
                    break;
                case Stage.PreviewPlaying:
                    if (preview!.Outcome == CQFCinematicOutcome.Failed) throw new InvalidOperationException("Studio visual preview failed.");
                    if (preview.Outcome != CQFCinematicOutcome.Playing || preview.Elapsed < 0.45f) return;
                    Check(Find.TickManager.TicksGame == previewTick && Find.TickManager.Paused, "visual preview freezes the original map while real seconds advance");
                    CQFCinematicStepState previewState = Private<CQFCinematicStepState>(preview, "state");
                    Check(previewState.Child(0).Child(0).elapsed > 0.25f && preview.Presentation.LetterboxHeight > 0.1f
                        && preview.Presentation.Subtitle == subtitle!.textKey.Translate().ToString(), "game-time visual waits, bars and localized subtitle advance in preview");
                    Host.SetPreviewPaused(preview, true);
                    pausedElapsed = preview.Elapsed;
                    Go(Stage.PreviewPaused);
                    break;
                case Stage.PreviewPaused:
                    if (Age < 0.3f) return;
                    Check(preview!.Elapsed == pausedElapsed && Find.TickManager.TicksGame == previewTick, "preview pause freezes both clocks for 0.3 seconds");
                    Host.StepPreview(preview, 0.1f);
                    Check(Math.Abs(preview.Elapsed - pausedElapsed - 0.1f) < 0.0001f && preview.PreviewPaused
                        && Find.TickManager.TicksGame == previewTick, "preview single step adds exactly 0.1 seconds without ticking the world");
                    Capture("05_Chinese_PausedPreview", Stage.PreviewReturn);
                    break;
                case Stage.PreviewReturn:
                    Host.Stop(preview!);
                    playback!.Close(false);
                    studio = new QuestEditor_Cinematics();
                    Find.WindowStack.Add(studio);
                    Go(Stage.DraftRestored);
                    break;
                case Stage.DraftRestored:
                    if (Age < 0.3f) return;
                    Check((Find.Camera.transform.position - originalCamera).sqrMagnitude < 0.01f
                        && Math.Abs(Find.CameraDriver.RootSize - originalCameraSize) < 0.001f, "returning from preview restores the recorded camera view");
                    Check(studio!.Project.Definition.defName == project!.Definition.defName && !ReferenceEquals(studio.Project, project)
                        && studio.Project.Definition.steps.Count == project.Definition.steps.Count, "closing and reopening the map studio restores its pure-value draft");
                    Check(studio.Project.Targets.Values.All(target => target.Map != null && Current.Game.Maps.Contains(target.Map)), "reopened draft resolves its bindings against the current game");
                    CheckRoleEditingSafety();
                    StartFullTest();
                    break;
                case Stage.FullPlaying:
                    if (session == null || !session.IsBusy)
                        throw new InvalidOperationException("Studio full test session ended before its isolation checks.");
                    if (session!.Instance == null || session.Instance.Outcome == CQFCinematicOutcome.Pending) return;
                    if (session.Instance.Outcome != CQFCinematicOutcome.Playing) throw new InvalidOperationException("Studio full test ended before its isolation checks.");
                    if (StudioProbeAction.Executions == 0 || session.Instance.Elapsed < 0.15f) return;
                    Pawn currentActor = FindActor();
                    Check(session.Instance.IsEditorControlled && !session.Instance.IsPreview && !ReferenceEquals(Current.Game, originalGame), "full test runs a registered production cinematic in a loaded checkpoint copy");
                    File.AppendAllText(Results, "TRACE probe executions=" + StudioProbeAction.Executions + ", observedActorID=" + StudioProbeAction.ActorID
                        + ", expectedActorID=" + actorID + ", currentRotation=" + currentActor.Rotation + ", currentName=" + currentActor.Name?.ToStringFull + "\n", Encoding.UTF8);
                    Check(StudioProbeAction.Executions == 1, "probe action executes exactly once in the full test");
                    Check(StudioProbeAction.ActorID == actorID && currentActor.GetUniqueLoadID() == actorID, "probe action mutates the actual rebound actor identity");
                    Check(currentActor.Name is NameSingle && currentActor.Name.ToStringFull == mutationName,
                        "probe action applies its durable native serialized name to the rebound pawn");
                    GameDataSaveLoader.SaveGame(blockedSave);
                    Check(!File.Exists(OwnedSavePath(blockedSave)), "the real native save call cannot write another named save during full testing");
                    session.Pause(true);
                    pausedElapsed = session.Instance.Elapsed;
                    pausedTick = Find.TickManager.TicksGame;
                    Go(Stage.FullPaused);
                    break;
                case Stage.FullPaused:
                    if (Age < 0.3f) return;
                    Check(session!.Instance!.Elapsed == pausedElapsed && Find.TickManager.TicksGame == pausedTick, "full test pause freezes the world and cinematic for 0.3 seconds");
                    session.Step(0.1f);
                    Check(Math.Abs(session.Instance.Elapsed - pausedElapsed - 0.1f) < 0.0001f && Find.TickManager.TicksGame == pausedTick + 6,
                        "full test single step advances exactly six native world ticks and 0.1 seconds");
                    session.StopAndReturn();
                    Go(Stage.FullReturning);
                    break;
                case Stage.FullReturning:
                    if (returnedProject == null || CQFCinematicTestSession.Current != null || Current.Game == originalGame) return;
                    CheckRestoration();
                    Finish(true);
                    break;
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(Results, "FAIL stage=" + stage + "\n" + error + "\n", Encoding.UTF8);
            Log.Error("[CQF] Studio runtime checks failed: " + error);
            Finish(false);
        }
    }

    private void Initialize()
    {
        startedAt = Time.realtimeSinceStartup;
        Directory.CreateDirectory(Path.Combine(Root, "StudioScreenshots"));
        File.WriteAllText(Results, "TRACE fixture target selection uses the native Targeter action callback, not OS mouse input.\n", Encoding.UTF8);
        Check(typeof(CQFCinematicDef).Assembly.Location.Contains("GameSnapshot", StringComparison.OrdinalIgnoreCase)
            && typeof(CQFCinematicProject).Assembly.Location.Contains("GameSnapshot", StringComparison.OrdinalIgnoreCase)
            && Path.GetFullPath(ProviderRoot).Contains("GameSnapshot", StringComparison.OrdinalIgnoreCase), "all game assets and writable project resources belong to the isolated snapshot");
        Check(!EngineApplication.Instance.Headless && EngineApplication.Instance.RenderContext != null
            && !EngineApplication.Instance.RenderContext.IsFramebufferMinimized(), "native NoWindow rendering has an available framebuffer");
        DebugSettings.pauseOnError = false;
        CloseLogWindows();
        SetLanguage("English");
        actor = Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        actor.drafter.Drafted = true;
        actor.jobs.EndCurrentJob(JobCondition.InterruptForced);
        actorID = actor.GetUniqueLoadID();
        destination = GenRadial.RadialCellsAround(actor.Position, 5f, false)
            .First(cell => cell.InBounds(actor.Map) && cell.Standable(actor.Map) && cell.DistanceToSquared(actor.Position) >= 4f);
        Find.Selector.ClearSelection();
        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        Find.CameraDriver.SetRootPosAndSize(actor.Position.ToVector3Shifted() + new Vector3(0.17f, 0f, 0.29f), 22f);
        project = new CQFCinematicProject();
        studio = new QuestEditor_Cinematics(project);
        Find.WindowStack.Add(studio);
        Go(Stage.EnglishLayout);
    }

    private void RecordAndSave()
    {
        CloseLogWindows();
        Window_CinematicInspector inspector = Find.WindowStack.Windows.OfType<Window_CinematicInspector>().Single();
        Check(studio!.windowRect.height < UI.screenHeight * 0.4f && !studio.absorbInputAroundWindow && !studio.preventCameraMotion, "the map studio dock is below forty percent of screen height and leaves map controls available");
        Check(Find.WindowStack.GetWindowAt(new Vector2(UI.screenWidth * 0.5f, UI.screenHeight * 0.5f)) == null
            && !inspector.windowRect.Overlaps(studio.windowRect), "the map center is unobstructed and the inspector does not overlap the dock");
        Vector3 camera = Find.Camera.transform.position;
        camera.y = 0f;
        studio.RecordCamera();
        cameraStep = project!.Definition.steps.OfType<CQFCinematicStep_Camera>().Single();
        TargetInfo cameraTarget = project.Targets[cameraStep.targetKey];
        Check(cameraTarget.Map == actor!.Map && (cameraTarget.Cell.ToVector3Shifted() + cameraStep.offset - camera).sqrMagnitude < 0.0001f
            && Math.Abs(cameraStep.size - Find.CameraDriver.RootSize) < 0.0001f, "record camera preserves its map cell, fractional offset and zoom");
        string boundActor = project.BindTarget(new TargetInfo(actor));
        string boundDestination = project.BindTarget(new TargetInfo(destination, actor.Map));
        studio.RecordPawnMove();
        InjectTarget(new LocalTargetInfo(actor));
        InjectTarget(new LocalTargetInfo(destination));
        CQFCinematicStep_PawnMove move = project.Definition.steps.OfType<CQFCinematicStep_PawnMove>().Single();
        Check(move.pawnKey == boundActor && move.targetKey == boundDestination, "fixture target callbacks record the bound real pawn and movement destination");
        studio.RecordPawnFacing();
        InjectTarget(new LocalTargetInfo(actor));
        InjectTarget(new LocalTargetInfo(destination));
        CQFCinematicStep_PawnFace face = project.Definition.steps.OfType<CQFCinematicStep_PawnFace>().Single();
        Check(face.pawnKey == boundActor && face.targetKey == boundDestination && !Find.Targeter.IsTargeting,
            "fixture target callbacks record facing and reuse existing authored target keys");
        project.EnsureRoles();
        roleKey = boundActor;
        CQFCinematicRole role = project.Definition.roles.Single(entry => entry.key == boundActor);
        Check(role.source == CQFCinematicRoleSource.Recorded, "recorded pawn steps initially share one stable recorded actor role");
        role.label = "CQF_StudioGuard";
        project.SetRoleSource(boundActor, CQFCinematicRoleSource.QuestDatabase, "CQF_StudioGuard");
        project.SetRehearsalPawn(boundActor, actor);
        Quest roleQuest = Quest.MakeRaw();
        roleQuest.root = DefDatabase<QuestScriptDef>.AllDefsListForReading.First();
        roleQuest.name = "CQF_StudioRoleQuest";
        Find.QuestManager.Add(roleQuest);
        project.Quest = roleQuest;
        Pawn productionActor = actor.Map.mapPawns.AllPawnsSpawned.First(pawn => pawn != actor);
        GameComponent_Editor.Instance.GetQuestData(roleQuest).RecordTarget("CQF_StudioGuard", new TargetInfo(productionActor));
        subtitle = new CQFCinematicStep_Subtitle { duration = 5f };
        Timeline.Add(project.Definition, subtitle);
        chineseSubtitle = ReadDefaultSubtitle("ChineseSimplified (简体中文)");
        englishSubtitle = ReadDefaultSubtitle("English");
        project.SetSubtitle(subtitle, chineseSubtitle, englishSubtitle);
        string subtitleKey = subtitle.textKey;
        string projectPath = project.Save();
        Check(File.Exists(projectPath) && File.Exists(Path.ChangeExtension(projectPath, ".cqfscene")), "project saving writes the definition and separate authoring scene");
        XElement savedDefinition = XDocument.Parse(File.ReadAllText(projectPath, Encoding.UTF8)).Root!.Elements().Single();
        XElement savedScene = XDocument.Parse(File.ReadAllText(Path.ChangeExtension(projectPath, ".cqfscene"), Encoding.UTF8)).Root!;
        Check(savedDefinition.Element("roles")!.Elements("li").Any(entry => entry.Element("key")?.Value == boundActor
                && entry.Element("source")?.Value == nameof(CQFCinematicRoleSource.QuestDatabase))
            && !savedDefinition.Descendants("thingID").Any(entry => entry.Value == actorID)
            && !savedDefinition.Element("bindings")!.Elements("li").Any(entry => entry.Element("key")?.Value == boundActor),
            "formal dynamic-role XML keeps its source configuration without exporting the rehearsal actor ID");
        Check(savedScene.Element("rehearsalBindings")!.Elements("li").Any(entry => entry.Element("key")?.Value == boundActor
                && entry.Element("thingID")?.Value == actorID), "the authoring scene retains the rehearsal actor separately from the formal definition");
        string csvPath = Path.Combine(ProviderRoot, ".QuestEditor_Library", "TextReview", "Cinematics.csv");
        List<string[]> rows = ParseReview(csvPath);
        string[] row = rows.Single(item => item[0] == subtitleKey);
        row[7] = "CQF_STUDIO_REVIEW_CN";
        row[8] = "CQF_STUDIO_REVIEW_EN";
        row[9] = "CQF_STUDIO_REVIEW_COMMENT";
        File.WriteAllText(csvPath, "\uFEFF" + string.Join("\r\n", rows.Select(item => string.Join(",", item.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")))) + "\r\n", new UTF8Encoding(false));
        project.Save();
        Check(subtitle.textKey == subtitleKey && project.Subtitles.Count(item => item.Key == subtitleKey) == 1, "saving twice preserves the owned subtitle key without duplicate entries");
        row = ParseReview(csvPath).Single(item => item[0] == subtitleKey);
        Check(row[7] == "CQF_STUDIO_REVIEW_CN" && row[8] == "CQF_STUDIO_REVIEW_EN" && row[9] == "CQF_STUDIO_REVIEW_COMMENT", "project resaving preserves staged CSV suggestions and reviewer comments");
        CQFCinematicProject loaded = CQFCinematicProject.Load(projectPath);
        Check(loaded.Subtitles.Single(item => item.Key == subtitleKey).Chinese == chineseSubtitle
            && loaded.Subtitles.Single(item => item.Key == subtitleKey).English == englishSubtitle
            && loaded.Targets[boundActor].Thing == actor, "project loading restores both authored subtitle languages and live target bindings");
        Check(loaded.Definition.roles.Single(entry => entry.key == boundActor).source == CQFCinematicRoleSource.QuestDatabase
            && loaded.RehearsalBindings.Any(entry => entry.key == boundActor && entry.thingID == actorID)
            && loaded.Quest == roleQuest, "loaded authoring scenes restore the role source, rehearsal sample and real quest identity");
        CQFCinematicProject clone = loaded.CloneForTest();
        Check(clone.Targets.Count == 0 && !ReferenceEquals(clone.Definition, loaded.Definition), "test project cloning carries no live target dictionary or shared definition");
        clone.RebindTargets();
        Check(clone.Targets[boundActor].Thing == actor && clone.Targets[boundDestination].Cell == destination, "a pure-value test clone explicitly rebinds its pawn and cell");
        Check(clone.RehearsalBindings.Any(entry => entry.key == boundActor && entry.thingID == actorID)
            && !ReferenceEquals(clone.RehearsalBindings.Single(entry => entry.key == boundActor), loaded.RehearsalBindings.Single(entry => entry.key == boundActor)),
            "test clones preserve rehearsal metadata as independent pure-value bindings");
        Dictionary<string, TargetInfo> productionTargets = loaded.Definition.ResolveBindings(actor.Map, quest: roleQuest);
        Check(productionTargets[boundActor].Thing == productionActor && productionTargets[boundActor].Thing != actor,
            "production role resolution uses the actual quest database pawn instead of its rehearsal sample");
        CQFCinematicInstance productionInstance = new(loaded.Definition, new Dictionary<string, TargetInfo>(), roleQuest);
        Check(productionInstance.Quest == roleQuest && productionInstance.Targets[boundActor].Thing == productionActor,
            "production cinematic construction passes its quest through to dynamic role resolution");
        CQFCinematicRole contextRole = clone.Definition.roles.Single(entry => entry.key == boundActor);
        contextRole.source = CQFCinematicRoleSource.Context;
        contextRole.sourceKey = "Trigger";
        Check(clone.Definition.ResolveBindings(actor.Map, new Dictionary<string, TargetInfo> { ["Trigger"] = new TargetInfo(productionActor) })[boundActor].Thing == productionActor,
            "a Context Trigger can replace the sample actor without a same-slot rehearsal override");
        string expectedMissing = "CQF_Cinematic_RoleMissing".Translate(contextRole.DisplayName);
        bool missingRejected = false;
        try { clone.Definition.ResolveBindings(actor.Map); }
        catch (InvalidOperationException error) when (error.Message == expectedMissing) { missingRejected = true; }
        Check(missingRejected, "missing Context Trigger fails explicitly and never falls back to the retained rehearsal actor");
    }

    private void CheckRoleEditingSafety()
    {
        Pawn alternate = Find.CurrentMap.mapPawns.AllPawnsSpawned.First(pawn => pawn != actor);
        CQFCinematicProject clone = studio!.Project.CloneForTest();
        clone.RebindTargets();
        CQFCinematicRole role = clone.Definition.roles.Single(entry => entry.key == roleKey);
        string originalKey = role.key;
        CQFCinematicRoleSource originalSource = role.source;
        string originalSourceKey = role.sourceKey;
        clone.SetRehearsalPawn(originalKey, alternate);
        clone.CaptureBindings();
        Check(role.key == originalKey && role.source == originalSource && role.sourceKey == originalSourceKey
            && clone.Targets[originalKey].Thing == alternate && !clone.Definition.bindings.Any(binding => binding.key == originalKey),
            "changing a rehearsal actor preserves the stable role slot and dynamic source without adding a formal sample binding");
        CQFCinematicBinding missing = clone.RehearsalBindings.Single(binding => binding.key == originalKey);
        missing.thingID = "Thing_CQF_StudioMissing_" + runID;
        expectedRoleWarning = "CQF_Cinematic_RoleTestPawnMissing".Translate(role.DisplayName);
        roleWarningObserved = false;
        Harmony warningProbe = new("CQF.StudioRoleWarning." + runID);
        MethodInfo warningMethod = AccessTools.Method(typeof(Log), nameof(Log.Warning), new[] { typeof(string) });
        warningProbe.Patch(warningMethod, postfix: new HarmonyMethod(typeof(StudioChecksDriver), nameof(ObserveRoleWarning)));
        try
        {
            clone.RebindTargets();
            Check(roleWarningObserved && !clone.Targets.ContainsKey(originalKey) && role.key == originalKey
                && role.source == originalSource && role.sourceKey == originalSourceKey,
                "a missing same-world rehearsal pawn emits the expected native warning while preserving the role and clearing the stale sample");
        }
        finally
        {
            warningProbe.Unpatch(warningMethod, HarmonyPatchType.Postfix, warningProbe.Id);
            expectedRoleWarning = "";
        }
        clone.SetRehearsalPawn(originalKey, alternate);
        CloseLogWindows();
        CQFCinematicProject contextProject = new();
        CQFCinematicStep_PawnFace contextStep = new() { pawnKey = "Trigger" };
        contextProject.Definition.steps.Add(contextStep);
        contextProject.EnsureRoles();
        CQFCinematicRole contextRole = contextProject.Definition.roles.Single();
        bool separateSlot = contextRole.key != "Trigger" && contextStep.pawnKey == contextRole.key
            && contextRole.source == CQFCinematicRoleSource.Context && contextRole.sourceKey == "Trigger";
        contextProject.SetRoleSource(contextRole.key, CQFCinematicRoleSource.Context, "Interviewer");
        Dictionary<string, TargetInfo> resolved = contextProject.Definition.ResolveBindings(Find.CurrentMap,
            new Dictionary<string, TargetInfo> { ["Trigger"] = new TargetInfo(actor!), ["Interviewer"] = new TargetInfo(alternate) });
        Check(separateSlot && contextRole.sourceKey == "Interviewer" && resolved[contextRole.key].Thing == alternate,
            "a migrated Context role keeps its own stable slot so a new source key overrides the old Trigger context actor");
    }

    private static void ObserveRoleWarning(string text)
    {
        if (text.Contains(expectedRoleWarning, StringComparison.Ordinal)) roleWarningObserved = true;
    }

    private void Select(CQFCinematicStep step)
    {
        typeof(CQFCinematicTimeline).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Timeline, step);
        Timeline.SelectionChanged?.Invoke(step);
    }

    private void InjectTarget(LocalTargetInfo target)
    {
        Action<LocalTargetInfo> callback = Private<Action<LocalTargetInfo>>(Find.Targeter, "action");
        Find.Targeter.StopTargeting();
        callback(target);
        File.AppendAllText(Results, "TRACE injected native Targeter action callback: " + target + "\n", Encoding.UTF8);
    }

    private void SetLanguage(string folder)
    {
        LoadedLanguage language = LanguageDatabase.AllLoadedLanguages.Single(item => item.folderName == folder);
        LanguageDatabase.activeLanguage = language;
        language.LoadMetadata();
        language.LoadData();
        Check("CQF_Cinematic_RecordCamera".Translate().ToString() != "CQF_Cinematic_RecordCamera"
            && "CQF_Cinematic_SubtitleChinese".Translate().ToString() != "CQF_Cinematic_SubtitleChinese", "native translation labels resolve for exactly " + folder);
    }

    private string ReadDefaultSubtitle(string folder)
    {
        string path = Path.Combine(ProviderRoot, "Languages", folder, "Keyed", "CQF_Cinematics.xml");
        return XDocument.Parse(File.ReadAllText(path, Encoding.UTF8)).Root!.Element("CQFCinematicStep_Subtitle")!.Value;
    }

    private List<string[]> ParseReview(string path)
    {
        Type review = typeof(CQFCinematicProject).Assembly.GetType("QuestEditor_Library.CQFCinematicTextReview", true)!;
        return (List<string[]>)review.GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { File.ReadAllText(path, Encoding.UTF8) })!;
    }

    private void StartPreview()
    {
        originalCamera = Find.Camera.transform.position;
        originalCameraSize = Find.CameraDriver.RootSize;
        CQFCinematicDef definition = new()
        {
            defName = "CQF_StudioVisualPreview", lockInput = false, restoreCamera = true,
            steps = new()
            {
                new CQFCinematicStep_Parallel
                {
                    steps = new()
                    {
                        new CQFCinematicStep_Wait { duration = 5f, clock = CQFCinematicClock.GameTime },
                        new CQFCinematicStep_Letterbox { duration = 0.2f, height = 0.12f, clock = CQFCinematicClock.GameTime },
                        new CQFCinematicStep_Subtitle { duration = 5f, textKey = subtitle!.textKey },
                        new CQFCinematicStep_Camera { duration = 0.2f, targetKey = cameraStep!.targetKey, offset = new Vector3(8f, 0f, 6f), size = 14f }
                    }
                }
            }
        };
        previewTick = Find.TickManager.TicksGame;
        preview = Host.Play(definition, project!.Targets, preview: true);
        studio!.Close(false);
        playback = new Window_CinematicPlayback(preview, () => { }, project.DisplayName);
        Find.WindowStack.Add(playback);
        Go(Stage.PreviewPlaying);
    }

    private void StartFullTest()
    {
        actor = FindActor();
        actor.Rotation = Rot4.South;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        baselineTick = Find.TickManager.TicksGame;
        baselinePosition = actor.Position;
        baselineRotation = actor.Rotation;
        baselineName = actor.Name?.ToStringFull ?? "";
        baselineNameType = actor.Name?.GetType().FullName ?? "";
        mutationName = "CQF_StudioMutation_" + runID;
        originalGame = Current.Game;
        originalGame.Info.permadeathMode = true;
        originalPermadeathName = "CQF_StudioOriginal_" + runID;
        originalGame.Info.permadeathModeUniqueName = originalPermadeathName;
        CQFCinematicProject test = new();
        string pawnKey = test.BindTarget(new TargetInfo(actor));
        test.Definition.lockInput = false;
        test.Definition.steps = new()
        {
            new CQFCinematicStep_Actions { actions = new() { new StudioProbeAction { pawnKey = pawnKey, replacementName = mutationName } } },
            new CQFCinematicStep_Wait { duration = 4f }
        };
        blockedSave = "CQF_StudioBlocked_" + runID;
        Check(!File.Exists(OwnedSavePath(blockedSave)), "the isolation probe save name is unique inside the test SaveData folder");
        session = CQFCinematicTestSession.Start(test, restored =>
        {
            returnedProject = restored;
            studio = new QuestEditor_Cinematics(restored);
            Find.WindowStack.Add(studio);
        });
        checkpointPath = Private<string>(session, "checkpointPath");
        Check(File.Exists(checkpointPath) && Path.GetDirectoryName(Path.GetFullPath(checkpointPath)) == Path.GetFullPath(GenFilePaths.SavedGamesFolderPath), "full testing creates its unique native checkpoint inside the isolated Saves folder");
        studio!.Close(false);
        Go(Stage.FullPlaying);
    }

    private void CheckRestoration()
    {
        Pawn restoredActor = FindActor();
        Check(!ReferenceEquals(Current.Game, originalGame) && returnedProject != null && !session!.IsBusy, "return reloads a new game and completes the full-test session");
        Check(Find.TickManager.TicksGame == baselineTick && restoredActor.Position == baselinePosition && restoredActor.Rotation == baselineRotation,
            "checkpoint return restores world ticks, actor position and actor rotation");
        Check((restoredActor.Name?.ToStringFull ?? "") == baselineName && (restoredActor.Name?.GetType().FullName ?? "") == baselineNameType
            && restoredActor.Name?.ToStringFull != mutationName, "checkpoint return restores the original serialized pawn name and name type");
        Check(Current.Game.Info.permadeathMode && Current.Game.Info.permadeathModeUniqueName == originalPermadeathName
            && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal, "checkpoint return restores permadeath mode, its original name and original time speed");
        Check(!ReferenceEquals(restoredActor, actor) && returnedProject!.Targets.Values.Any(target => ReferenceEquals(target.Thing, restoredActor))
            && returnedProject.Targets.Values.All(target => target.Map != null && Current.Game.Maps.Contains(target.Map)), "returned authoring bindings point to the newly loaded actor and maps");
        Check(!File.Exists(checkpointPath), "successful return removes only its owned temporary checkpoint");
        string releasedSave = "CQF_StudioReleased_" + runID;
        string releasedPath = OwnedSavePath(releasedSave);
        Current.Game.Info.permadeathMode = false;
        GameDataSaveLoader.SaveGame(releasedSave);
        Check(File.Exists(releasedPath) && new FileInfo(releasedPath).Length > 0 && !File.Exists(OwnedSavePath(blockedSave)),
            "a real fixture save succeeds after guard removal while the blocked save remains absent");
    }

    private Pawn FindActor() => Find.CurrentMap.mapPawns.AllPawnsSpawned.Single(pawn => pawn.GetUniqueLoadID() == actorID);

    private string OwnedSavePath(string name)
    {
        string path = Path.GetFullPath(GenFilePaths.FilePathForSavedGame(name));
        string saves = Path.GetFullPath(Path.Combine(Root, "Saves"));
        if (!string.Equals(Path.GetDirectoryName(path), saves, StringComparison.OrdinalIgnoreCase) || !name.StartsWith("CQF_Studio", StringComparison.Ordinal))
            throw new InvalidOperationException("Fixture save path is outside this test's Saves folder.");
        return path;
    }

    private void Capture(string name, Stage next)
    {
        CloseLogWindows();
        screenshotPath = Path.Combine(Root, "StudioScreenshots", name + ".png");
        if (File.Exists(screenshotPath)) throw new IOException("Studio screenshot must be newly created: " + screenshotPath);
        screenshotRequested = DateTime.UtcNow;
        afterScreenshot = next;
        ScreenCapture.CaptureScreenshot(screenshotPath);
        Go(Stage.WaitScreenshot);
    }

    private void WaitScreenshot()
    {
        if (!File.Exists(screenshotPath) || new FileInfo(screenshotPath).Length < 512)
        {
            if (Age > 8f) throw new TimeoutException("No fresh rendered PNG within eight seconds under -NoWindow: " + screenshotPath);
            return;
        }
        byte[] signature = new byte[8];
        using (FileStream stream = File.OpenRead(screenshotPath)) stream.ReadExactly(signature);
        Check(signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && File.GetLastWriteTimeUtc(screenshotPath) >= screenshotRequested.AddSeconds(-1),
            "a fresh native screenshot was written: " + screenshotPath);
        if (afterScreenshot is Stage.EnglishSubtitle or Stage.ChineseSubtitle) Select(subtitle!);
        if (afterScreenshot is Stage.ChineseSetup or Stage.PreviewSetup)
        {
            foreach (Window_CinematicRoles window in Find.WindowStack.Windows.OfType<Window_CinematicRoles>().ToArray()) window.Close(false);
            Select(subtitle!);
        }
        Go(afterScreenshot);
    }

    private void CloseLogWindows()
    {
        foreach (Window window in Find.WindowStack.Windows.Where(window => window.GetType().Name == "EditWindow_Log").ToArray()) window.Close(false);
    }

    private static T Private<T>(object owner, string name)
        => (T)(owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner)
            ?? throw new MissingFieldException(owner.GetType().FullName, name));

    private void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        checks++;
        File.AppendAllText(Results, "PASS " + checks + " " + name + "\n", Encoding.UTF8);
    }

    private void Go(Stage next)
    {
        stage = next;
        stageStarted = Time.realtimeSinceStartup;
        File.AppendAllText(Results, "TRACE stage=" + stage + ", realtime=" + stageStarted + "\n", Encoding.UTF8);
    }

    private void Finish(bool success)
    {
        finished = true;
        if (Current.Game?.Info != null) Current.Game.Info.permadeathMode = false;
        File.AppendAllText(Results, success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8);
        Application.Quit(success ? 0 : 1);
    }

    private enum Stage { Boot, EnglishLayout, EnglishWorkspace, EnglishSubtitle, EnglishRolesSetup, EnglishRoles, ChineseSetup, ChineseWorkspace, ChineseSubtitle, ChineseRolesSetup, ChineseRoles, WaitScreenshot, PreviewSetup, PreviewPlaying, PreviewPaused, PreviewReturn, DraftRestored, FullPlaying, FullPaused, FullReturning }

    private readonly string runID = Guid.NewGuid().ToString("N");
    private static string expectedRoleWarning = "";
    private static bool roleWarningObserved;
    private QuestEditor_Cinematics? studio;
    private CQFCinematicProject? project;
    private CQFCinematicProject? returnedProject;
    private CQFCinematicStep_Camera? cameraStep;
    private CQFCinematicStep_Subtitle? subtitle;
    private CQFCinematicInstance? preview;
    private CQFCinematicTestSession? session;
    private Window_CinematicPlayback? playback;
    private Pawn? actor;
    private Game? originalGame;
    private string actorID = "";
    private string roleKey = "";
    private string baselineName = "";
    private string baselineNameType = "";
    private string mutationName = "";
    private string chineseSubtitle = "";
    private string englishSubtitle = "";
    private string blockedSave = "";
    private string checkpointPath = "";
    private string originalPermadeathName = "";
    private string screenshotPath = "";
    private DateTime screenshotRequested;
    private Stage stage;
    private Stage afterScreenshot;
    private IntVec3 destination;
    private IntVec3 baselinePosition;
    private Rot4 baselineRotation;
    private Vector3 originalCamera;
    private float originalCameraSize;
    private float startedAt;
    private float stageStarted;
    private float pausedElapsed;
    private int previewTick;
    private int pausedTick;
    private int baselineTick;
    private int checks;
    private bool finished;
}
