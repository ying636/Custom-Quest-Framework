using System.Reflection;
using System.Text;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

namespace CQF.CinematicStudioChecks;

public static class StudioUsabilityChecks
{
    public static void Run(Action<bool, string> check, string directory)
    {
        MethodInfo? legacyStart = typeof(CQFCinematicTestSession).GetMethod(nameof(CQFCinematicTestSession.Start),
            new[] { typeof(CQFCinematicProject), typeof(Action<CQFCinematicProject>), typeof(bool), typeof(CQFCinematicStep) });
        check(legacyStart != null && legacyStart.GetParameters()[2].IsOptional && legacyStart.GetParameters()[3].IsOptional,
            "the published four-parameter cinematic test entry point remains available to compiled extensions");
        check(typeof(QuestEditor_Cinematics).GetMethod(nameof(QuestEditor_Cinematics.BindMapTarget), new[] { typeof(Action<string>) }) != null,
            "the published single-parameter map target picker remains available to compiled extensions");
        check(typeof(Window_CinematicInspector).GetConstructor(new[] { typeof(QuestEditor_Cinematics) }) != null,
            "the published inspector constructor remains available to compiled extensions");
        MethodInfo confirmReplace = typeof(QuestEditor_Cinematics).GetMethod("ConfirmReplace", BindingFlags.Instance | BindingFlags.NonPublic)!;
        void CheckReplacement(CQFCinematicProject candidate, bool expectedConfirmation, string message)
        {
            Window[] previous = Find.WindowStack.Windows.ToArray();
            bool replaced = false;
            confirmReplace.Invoke(new QuestEditor_Cinematics(candidate), new object[] { new Action(() => replaced = true) });
            Dialog_MessageBox? confirmation = Find.WindowStack.Windows.OfType<Dialog_MessageBox>().FirstOrDefault(window => !previous.Contains(window));
            check(replaced != expectedConfirmation && (confirmation != null) == expectedConfirmation, message);
            confirmation?.Close(false);
        }
        CheckReplacement(new CQFCinematicProject(), false, "an untouched empty project can be replaced without confirmation");
        CQFCinematicProject actorOnly = new();
        actorOnly.Definition.roles.Add(new CQFCinematicRole { key = "CQF_UnsavedActor", source = CQFCinematicRoleSource.Context, sourceKey = "Pawn" });
        CheckReplacement(actorOnly, true, "a project with only an actor requires confirmation before replacement");
        CQFCinematicProject locationOnly = new();
        locationOnly.BindTarget(new TargetInfo(Find.CurrentMap.Center, Find.CurrentMap));
        locationOnly.CaptureBindings();
        CheckReplacement(locationOnly, true, "a project with only an unused named location requires confirmation before replacement");
        CQFCinematicProject settingsOnly = new();
        settingsOnly.Definition.pauseGame = true;
        CheckReplacement(settingsOnly, true, "a project with only changed playback settings requires confirmation before replacement");
        CQFCinematicProject questOnly = new() { QuestID = "Quest_CQF_UnsavedAssociation" };
        CheckReplacement(questOnly, true, "a project with only a quest association requires confirmation before replacement");
        CQFCinematicProject namedOnly = new();
        namedOnly.Definition.defName = "CQF_UnsavedCustomName";
        CheckReplacement(namedOnly, true, "a project with only a custom definition name requires confirmation before replacement");
        CQFCinematicProject project = new();
        CQFCinematicStep_Subtitle subtitle = new() { textKey = "CQF_CinematicSubtitle_UXDraft" };
        CQFCinematicStep_Sequence group = new() { steps = new() { new CQFCinematicStep_Wait(), subtitle } };
        project.Definition.steps.Add(group);
        project.Subtitles.Add(new CQFCinematicSubtitleText(subtitle.textKey, "", "Unfinished draft"));
        project.QuestID = "Quest_CQF_Draft_Only";
        string snapshot = project.CaptureSnapshot();
        CQFCinematicProject restored = CQFCinematicProject.RestoreSnapshot(snapshot);
        check(restored.Subtitles.Single().Chinese == "" && restored.Subtitles.Single().English == "Unfinished draft",
            "draft snapshots preserve incomplete bilingual subtitle text without requiring publication");
        check(restored.QuestID == project.QuestID && restored.Targets.Count == 0,
            "draft restoration keeps a value quest identity without retaining live target objects");
        check(project.StepPath(subtitle) == "0/1" && restored.StepAtPath("0/1") is CQFCinematicStep_Subtitle
            && restored.StepAtPath("0/8") == null, "nested step paths restore selections and reject missing paths");
        CQFCinematicEditHistory history = new(snapshot, "0/1");
        project.DisplayName = "CQF Draft one";
        string first = project.CaptureSnapshot();
        history.Observe(first, "0", 1d);
        project.DisplayName = "CQF Draft two";
        string second = project.CaptureSnapshot();
        history.Observe(second, "0/1", 1.2d);
        check(history.Undo() && history.Snapshot == snapshot && history.SelectionPath == "0/1",
            "continuous text edits merge into one undo step while preserving the earlier selection");
        check(history.Redo() && history.Snapshot == second, "redo restores the complete merged edit");
        history.EndEdit();
        history.Observe(first, "0", 1.3d);
        check(history.Undo() && history.Snapshot == second, "explicit edit boundaries remain separate even within the typing interval");
        history.Observe(snapshot, "", 2d, false);
        check(!history.CanRedo, "editing after undo removes the abandoned redo branch");
        CQFCinematicDraftStore store = new(directory);
        string path = store.Write("CQF_UX_world_A", snapshot);
        check(File.Exists(path) && CQFCinematicProject.RestoreSnapshot(store.Read("CQF_UX_world_A")!).Subtitles.Single().English == "Unfinished draft",
            "an incomplete draft survives a real UTF-8 disk write and reload");
        check(store.Read("CQF_UX_world_B") == null && store.DraftPath("CQF_UX_world_A") != store.DraftPath("CQF_UX_world_B"),
            "different worlds cannot inherit each other's autosave");
        store.Write("CQF_UX_world_A", second);
        check(CQFCinematicProject.RestoreSnapshot(store.Read("CQF_UX_world_A")!).DisplayName == "CQF Draft two" && Directory.GetFiles(directory, "*.tmp").Length == 0,
            "replacing a draft is atomic and leaves no pending temporary files");
        XElement xml = XElement.Load(path);
        check(xml.Name == "CQFCinematicAutosave" && !xml.Descendants().Any(element => element.Name.LocalName is "Pawn" or "Game" or "Quest"),
            "the autosave stores authoring values rather than a serialized running world");
        byte[] broken = Encoding.UTF8.GetBytes("<CQF broken draft");
        File.WriteAllBytes(path, broken);
        string recovery = store.PreserveInvalidDraft("CQF_UX_world_A");
        store.Write("CQF_UX_world_A", snapshot);
        check(File.ReadAllBytes(recovery).SequenceEqual(broken), "recovering a corrupt draft preserves its exact original bytes before a fresh autosave");
        CQFCinematicProject selectedProject = new();
        CQFCinematicStep_Wait selected = new();
        selectedProject.Definition.steps.Add(selected);
        selectedProject.Definition.roles.Add(new CQFCinematicRole { key = "CQF_IncompleteOtherActor", source = CQFCinematicRoleSource.EntityBook, sourceKey = "" });
        selectedProject.Definition.skipActions.Add(new StudioProbeAction());
        selectedProject.Definition.completedSignal = "CQF_WholeSceneSignal";
        CQFCinematicTestSession selectedSession = (CQFCinematicTestSession)Activator.CreateInstance(typeof(CQFCinematicTestSession),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object?[] { selectedProject, new Action<CQFCinematicProject>(_ => { }), selected, false }, null)!;
        CQFCinematicProject selectedCopy = (CQFCinematicProject)typeof(CQFCinematicTestSession).GetField("testProject", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(selectedSession)!;
        check(selectedCopy.Definition.ConfigErrors().Count() == 0 && selectedCopy.Definition.roles.Count == 0 && selectedCopy.Definition.skipActions.Count == 0
            && selectedCopy.Definition.completedSignal == "", "testing one valid step excludes unrelated incomplete actors and whole-scene side effects");
        check(!selectedSession.UsesRehearsal && selectedCopy.RehearsalBindings.Count == 0 && selectedProject.Definition.roles.Count == 1
            && selectedProject.Definition.skipActions.Count == 1 && selectedProject.Definition.completedSignal == "CQF_WholeSceneSignal",
            "formal selected-step preparation leaves the authoring project and its other steps intact");
        CQFCinematicProject previewTexts = new();
        CQFCinematicStep_Subtitle authored = new();
        previewTexts.Definition.steps.Add(authored);
        previewTexts.SetSubtitle(authored, "CQF first", "CQF first");
        string earlier = previewTexts.CaptureSnapshot();
        previewTexts.SetSubtitle(authored, "CQF second", "CQF second");
        CQFCinematicProject earlierProject = CQFCinematicProject.RestoreSnapshot(earlier);
        earlierProject.RefreshPreviewTexts();
        check(authored.textKey.Translate().ToString() == "CQF first", "preview text refresh displays the restored subtitle after undo rather than the last edited translation");
        CQFCinematicProject locations = new();
        Map map = Find.CurrentMap;
        TargetInfo location = new(map.Center, map);
        string locationKey = locations.BindTarget(location);
        locations.TargetNames[locationKey] = "CQF Entrance";
        locations.CaptureBindings();
        check(locations.Definition.bindings.Count == 0 && locations.Locations.Count == 1,
            "an unused named location remains in authoring metadata without becoming a formal runtime binding");
        CQFCinematicProject locationCopy = CQFCinematicProject.RestoreSnapshot(locations.CaptureSnapshot());
        check(locationCopy.BindTarget(location) == locationKey && locationCopy.TargetNames[locationKey] == "CQF Entrance",
            "a named map location survives draft restoration and reuses its stable key at the same cell");
        locationCopy.Definition.steps.Add(new CQFCinematicStep_Camera { targetKey = locationKey });
        bool blocked = false;
        try { locationCopy.RemoveLocation(locationKey); } catch (InvalidOperationException) { blocked = true; }
        check(blocked && locationCopy.Locations.Count == 1, "a referenced location cannot be deleted from the project");
        locationCopy.Definition.steps.Clear();
        locationCopy.RemoveLocation(locationKey);
        check(locationCopy.Locations.Count == 0 && !locationCopy.TargetNames.ContainsKey(locationKey), "an unused location can be removed with its display name");
        CQFCinematicDef failureDefinition = new()
        {
            defName = "CQF_UsabilityNestedFailure",
            steps = new() { new CQFCinematicStep_Sequence { steps = new() { new CQFCinematicStep_Wait { duration = 0f }, new StudioFailureStep() } } }
        };
        CQFCinematicInstance failure = new(failureDefinition, new());
        failure.Start();
        bool thrown = false;
        try { failure.Advance(0.1f, 0); } catch (InvalidOperationException) { thrown = true; }
        check(thrown && failure.FailedStepPath == "0/1", "an actual nested step exception retains the deepest failing path without swallowing the error");
        failure.Finish(CQFCinematicOutcome.Failed);
    }

    public static void RunEditor(QuestEditor_Cinematics studio, Action<bool, string> check)
    {
        string original = studio.Project.DisplayName;
        studio.CheckpointEdit();
        studio.Project.DisplayName = "CQF Native edit";
        studio.CheckpointEdit();
        check(studio.CanUndo, "a direct inspector-style value change enters the studio undo history");
        studio.UndoEdit();
        check(studio.Project.DisplayName == original && studio.CanRedo, "studio undo restores authored values and enables redo");
        studio.RedoEdit();
        check(studio.Project.DisplayName == "CQF Native edit", "studio redo restores the authored value");
        studio.UndoEdit();
        CQFCinematicStep_Wait step = new() { duration = 1f };
        studio.Project.Definition.steps.Add(step);
        studio.SelectStep(step);
        studio.CheckpointEdit();
        step.duration = 3f;
        studio.CheckpointEdit();
        studio.UndoEdit();
        check(studio.Project.Definition.steps.Single() is CQFCinematicStep_Wait { duration: 1f },
            "undo restores a step property without retaining the mutated step instance");
        studio.UndoEdit();
        check(studio.Project.Definition.steps.Count == 0, "undo can remove a newly added timeline step");
        studio.OpenProjectSettings();
        check(Find.WindowStack.Windows.OfType<Window_CinematicInspector>().Count() == 1,
            "project settings have a direct entry and do not stack duplicate inspectors");
        studio.SelectStep(null);
    }
}
