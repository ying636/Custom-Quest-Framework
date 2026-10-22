using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;
using Verse;

internal static class Program
{
    private static int Main()
    {
        try
        {
            FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
            prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
            new Harmony("CQF.CinematicChecks.Log").Patch(AccessTools.Method(typeof(Log), nameof(Log.Error)),
                prefix: new HarmonyMethod(typeof(CinematicTestLog), nameof(CinematicTestLog.Error)));
            ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
            content.assemblies = new ModAssemblyHandler(content);
            content.assemblies.loadedAssemblies.Add(typeof(CQFCinematicStep).Assembly);
            content.assemblies.loadedAssemblies.Add(typeof(Program).Assembly);
            LoadedModManager.RunningModsListForReading.Add(content);
            GenTypes.ClearCache();
            checks += PresentationGateChecks.Run();
            checks += CinematicBindingChecks.Run();
            checks += CinematicRoleChecks.Run();
            checks += CinematicJobBindingChecks.Run();
            CheckTiming();
            CheckSignalsAndConditions();
            CheckPersistence();
            CheckPreviewAndTermination();
            CheckValidation();
            Check(CinematicTestLog.Errors.Count == 0, "native XML and Scribe produce no hidden errors: " + string.Join("\n", CinematicTestLog.Errors));
            Console.WriteLine("Cinematic checks passed: " + checks);
            return 0;
        }
        catch (Exception error) { Console.WriteLine(error); return 1; }
    }

    private static void CheckTiming()
    {
        CQFCinematicContext context = Context();
        CQFCinematicStep_Wait wait = new() { duration = 1f };
        CQFCinematicStepState state = new();
        Check(!wait.Advance(context, state, 0.5f, 0) && wait.Advance(context, state, 0.5f, 0), "real-time wait progresses during a paused game");
        wait = new() { duration = 1f, clock = CQFCinematicClock.GameTime };
        state = new();
        Check(!wait.Advance(context, state, 100f, 0), "game-time wait ignores real time while paused");
        Check(!wait.Advance(context, state, 0f, 59) && wait.Advance(context, state, 0f, 1), "game-time wait ends at exactly sixty ticks");
        CQFCinematicStep_Sequence sequence = new() { steps = new() { new CQFCinematicStep_Wait { duration = 1f }, new CQFCinematicStep_Wait { duration = 1f } } };
        state = new();
        Check(!sequence.Advance(context, state, 1f, 0) && state.Child(1).elapsed == 0f, "sequence never credits the same frame to two timed steps");
        Check(sequence.Advance(context, state, 1f, 0), "sequence advances after both waits finish");
        CQFCinematicStep_Parallel parallel = new() { steps = new() { new CQFCinematicStep_Wait { duration = 1f }, new CQFCinematicStep_Wait { duration = 2f } } };
        state = new();
        Check(!parallel.Advance(context, state, 1f, 0) && state.Child(0).completed && !state.Child(1).completed, "parallel waits for its slowest branch");
        Check(parallel.Advance(context, state, 1f, 0) && state.Child(0).elapsed == 1f, "completed parallel branches never tick twice");
        CQFCinematicStep_Fade fade = new() { duration = 2f, color = Color.black };
        state = new();
        Check(!fade.Advance(context, state, 1f, 0) && Math.Abs(context.Presentation.FadeColor.a - 0.5f) < 0.01f, "fade interpolates while game time is paused");
        Check(!fade.Advance(context, state, 1f, 0) && context.Presentation.FadeColor == Color.black, "full cover waits until it has actually been drawn");
        context.Presentation.CoverVersion++;
        Check(fade.Advance(context, state, 0f, 0), "subsequent steps run only after a fully covered frame");
    }

    private static void CheckSignalsAndConditions()
    {
        CQFCinematicContext context = Context();
        context.Instance.SignalVersions["CQF_Test"] = 1;
        CQFCinematicStep_WaitSignal wait = new() { signal = "CQF_Test", addQuestPrefix = false, timeout = 2f };
        CQFCinematicStepState state = new();
        Check(!wait.Advance(context, state, 0f, 0), "signal wait ignores events that predate the wait");
        context.Instance.SignalVersions["CQF_Test"]++;
        Check(wait.Advance(context, state, 0f, 0), "new signal releases a waiting step");
        state = new();
        Throws<TimeoutException>(() => wait.Advance(context, state, 2f, 0), "missing signal fails with a bounded timeout");
        foreach (bool reverse in new[] { false, true })
        {
            context = Context();
            CQFCinematicContext captured = context;
            CinematicProbeAction.Callback = () => captured.Instance.SignalVersions["CQF_Test"] = 1;
            CQFCinematicStep_Actions actions = new() { actions = new() { new CinematicProbeAction() } };
            CQFCinematicStep_Sequence nested = new() { steps = new() { new CQFCinematicStep_WaitSignal { signal = "CQF_Test", addQuestPrefix = false } } };
            CQFCinematicStep_Parallel parallel = new() { steps = reverse ? new() { nested, actions } : new() { actions, nested } };
            state = new();
            bool ended = parallel.Advance(context, state, 0f, 0);
            if (!ended) ended = parallel.Advance(context, state, 0f, 0);
            Check(ended, "parallel signal wait registers before sibling actions, reverse=" + reverse);
        }
        CinematicProbeAction.Callback = null;
        CQFCinematicStep_WaitCondition condition = new() { conditions = new() { new CinematicProbeCondition() }, timeout = 1f };
        state = new(); CinematicProbeCondition.Ready = false;
        Check(!condition.Advance(Context(), state, 0f, 0), "unsatisfied condition holds playback");
        CinematicProbeCondition.Ready = true;
        Check(condition.Advance(Context(), state, 0f, 0), "satisfied condition resumes playback");
        QuestPrefixCheck();
    }

    private static void QuestPrefixCheck()
    {
        CQFCinematicInstance instance = new();
        RimWorld.Quest quest = (RimWorld.Quest)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Quest));
        quest.id = 42;
        Set(instance, "quest", quest);
        Check(instance.Context.SignalName("CQF_End") == "Quest42.CQF_End" && instance.Context.SignalName("CQF_End", false) == "CQF_End", "signal prefix follows the owning quest");
    }

    private static void CheckPersistence()
    {
        CQFCinematicContext context = Context();
        CinematicProbeAction.Count = 0;
        CQFCinematicStep_Sequence sequence = new() { steps = new() { new CQFCinematicStep_Actions { actions = new() { new CinematicProbeAction() } }, new CQFCinematicStep_Wait { duration = 2f } } };
        CQFCinematicStepState state = new();
        sequence.Advance(context, state, 0f, 0);
        sequence.Advance(context, state, 1f, 0);
        state = RoundTrip(state);
        Check(state.cursor == 1 && state.Child(0).completed && state.Child(1).elapsed == 1f, "native Scribe preserves nested cursor and elapsed time");
        sequence.Restore(context, state);
        Check(sequence.Advance(context, state, 1f, 0) && CinematicProbeAction.Count == 1, "resumed sequence does not repeat completed actions");
        CQFCinematicStep_Camera camera = new() { offset = new Vector3(2f, 3f, 4f), size = 17f, duration = 2.5f, follow = true };
        CQFCinematicStep_Camera cameraCopy = (CQFCinematicStep_Camera)camera.Copy();
        Check(cameraCopy.offset == camera.offset && cameraCopy.size == 17f && cameraCopy.follow, "camera XML copy preserves vectors and parameters");
        CQFCinematicStep_Fade fade = new() { color = new Color(0.2f, 0.3f, 0.4f, 0.7f) };
        Check(((CQFCinematicStep_Fade)fade.Copy()).color == fade.color, "fade XML copy preserves colour channels");
        CQFCinematicStep_Image image = new() { imagePath = "CQF_Image", rectNormalized = new Rect(0.1f, 0.2f, 0.3f, 0.4f) };
        Check(((CQFCinematicStep_Image)image.Copy()).rectNormalized == image.rectNormalized, "image XML copy preserves normalized rectangle");
        CQFCinematicStep_PawnFace face = new() { rotation = Rot4.West };
        Check(((CQFCinematicStep_PawnFace)face.Copy()).rotation == Rot4.West, "facing XML copy preserves native rotation");
        CQFCinematicPresentation view = new() { Subtitle = "CQF_Test", FadeColor = Color.black, HasCamera = true, CameraPosition = new Vector3(4, 0, 5), CameraSize = 18f };
        view = RoundTrip(view);
        Check(view.HasCamera && view.CameraSize == 18f && view.Subtitle == "CQF_Test" && view.FadeColor == Color.black, "native Scribe rebuilds visual state");
        CQFCinematicStep copied = RoundTrip<CQFCinematicStep>(sequence);
        Check(copied is CQFCinematicStep_Sequence restored && restored.steps.Count == 2 && restored.steps[0] is CQFCinematicStep_Actions, "native Scribe preserves polymorphic steps and actions");
        Map map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.uniqueID = 77;
        CQFCinematicInstance instance = Context().Instance;
        Set(instance, "definitionName", "CQF_TransientCinematic");
        Set(instance, "map", map);
        Set(instance, "parameters", new List<CQFCinematicTarget> { new("Position", new TargetInfo(new IntVec3(4, 0, 5), map)) });
        Set(instance, "steps", new List<CQFCinematicStep> { sequence });
        CinematicProbeAction.Count = 0;
        Check(!instance.Advance(0f, 0) && CinematicProbeAction.Count == 1, "real instance snapshots its running step and action cursor");
        instance.Advance(0.75f, 0);
        instance.SignalVersions["CQF_Persist"] = 7;
        instance = RoundTrip(instance, map);
        Check(instance.Definition.defName == "CQF_TransientCinematic" && instance.Map == map, "instance restores even when an editor definition has never been published");
        Check(instance.Targets["Position"].Map == map && instance.Targets["Position"].Cell == new IntVec3(4, 0, 5), "instance preserves cell targets and cross-map references");
        Check(instance.SignalVersions["CQF_Persist"] == 7 && instance.Outcome == CQFCinematicOutcome.Playing, "instance preserves signal baselines and playback outcome");
        Check(instance.Advance(1.25f, 0) && CinematicProbeAction.Count == 1, "full instance resumes without repeating completed actions");
    }

    private static void CheckPreviewAndTermination()
    {
        CQFCinematicContext context = Context(true);
        CinematicProbeAction.Count = 0;
        CQFCinematicStep_Parallel preview = new() { steps = new() { new CQFCinematicStep_Actions { actions = new() { new CinematicProbeAction() } }, new CQFCinematicStep_PawnMove(), new CQFCinematicStep_Dialog(), new CQFCinematicStep_WaitSignal(), new CQFCinematicStep_Wait { duration = 0f } } };
        Check(preview.Advance(context, new(), 0f, 0) && CinematicProbeAction.Count == 0, "preview skips actions, actors, dialogs and external waits");
        context = Context();
        CinematicProbeStep.Entered = CinematicProbeStep.Exited = CinematicProbeStep.Cancelled = 0;
        CQFCinematicStep_Parallel parallel = new() { steps = new() { new CinematicProbeStep { duration = 10f }, new CinematicProbeStep { duration = 10f } } };
        CQFCinematicStepState state = new();
        parallel.Advance(context, state, 1f, 0); parallel.Cancel(context, state); parallel.Cancel(context, state);
        Check(CinematicProbeStep.Entered == 2 && CinematicProbeStep.Exited == 2 && CinematicProbeStep.Cancelled == 2, "cancellation releases all parallel branches exactly once");
        context = Context();
        CQFCinematicContext captured = context;
        CinematicProbeAction.Count = 0;
        CinematicProbeAction.Callback = () => captured.Instance.Finish(CQFCinematicOutcome.Cancelled, false);
        CQFCinematicStep_Actions actions = new() { actions = new() { new CinematicProbeAction(), new CinematicProbeAction() } };
        actions.Advance(context, new(), 0f, 0);
        Check(CinematicProbeAction.Count == 1, "synchronous stop prevents later actions in the same frame");
        context = Context();
        Set(context.Instance, "skipActions", new List<CQFAction> { new CinematicProbeAction() });
        CinematicProbeAction.Callback = () => throw new InvalidOperationException("CQF_EXPECTED_SKIP_FAILURE");
        Throws<InvalidOperationException>(() => context.Instance.Finish(CQFCinematicOutcome.Skipped, false), "skip cleanup failures are visible");
        Check(context.Instance.Outcome == CQFCinematicOutcome.Failed, "failed skip actions cannot report a successful skip");
        CinematicProbeAction.Callback = null;
    }

    private static void CheckValidation()
    {
        CQFCinematicDef definition = new() { defName = "RSH_Test", steps = new() { new CQFCinematicStep_Wait() } };
        Check(!definition.ConfigErrors().Any(), "submods may use their own prefixed definition names");
        definition.defName = "Test";
        Check(definition.ConfigErrors().Any(error => error.Contains("prefix")), "unprefixed cinematic definitions are rejected");
        definition.defName = "CQF_Test"; definition.pauseGame = true;
        definition.steps = new() { new CQFCinematicStep_Sequence { steps = new() { new CQFCinematicStep_PawnMove() } } };
        Check(definition.ConfigErrors().Any(error => error.Contains("paused")), "game-time actors cannot deadlock a force-paused cinematic");
        definition.steps = new() { new CQFCinematicStep_PawnMove { clock = CQFCinematicClock.RealTime } };
        Check(definition.ConfigErrors().Any(error => error.Contains("paused")), "real-time actor timeouts cannot bypass force-pause validation");
        definition.steps = new() { new CQFCinematicStep_PawnJob { clock = CQFCinematicClock.RealTime } };
        Check(definition.ConfigErrors().Any(error => error.Contains("paused")), "native jobs always require an unpaused world");
        CQFCinematicStep_Sequence cycle = new(); cycle.steps.Add(cycle);
        definition.pauseGame = false; definition.steps = new() { cycle };
        Check(definition.ConfigErrors().Any(error => error.Contains("cycle")), "cyclic step graphs fail validation without recursion overflow");
        CQFCinematicStep_Parallel conflict = new() { steps = new() { new CQFCinematicStep_Fade(), new CQFCinematicStep_Fade() } };
        Check(conflict.ConfigErrors().Any(error => error.Contains("conflicting")), "parallel branches cannot fight over the same presentation channel");
        definition.steps = new() { new CQFCinematicStep_Wait { duration = float.NaN } };
        Check(definition.ConfigErrors().Any(error => error.Contains("duration")), "nonfinite step durations fail validation");
    }

    private static CQFCinematicContext Context(bool preview = false)
    {
        CQFCinematicInstance instance = new();
        Set(instance, "outcome", CQFCinematicOutcome.Playing);
        Set(instance, "isPreview", preview);
        Set(instance, "targets", new Dictionary<string, TargetInfo>());
        return instance.Context;
    }

    private static T RoundTrip<T>(T source, params ILoadReferenceable[] references) where T : class, IExposable
    {
        string path = Path.Combine(Path.GetTempPath(), "CQF_Cinematic_" + Guid.NewGuid().ToString("N") + ".xml");
        T? value = source;
        try
        {
            Scribe.saver.InitSaving(path, "checks"); Scribe_Deep.Look(ref value, "state"); Scribe.saver.FinalizeSaving();
            value = null;
            Scribe.loader.InitLoading(path);
            LoadedObjectDirectory directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Scribe.loader.crossRefs)!;
            foreach (ILoadReferenceable reference in references) directory.RegisterLoaded(reference);
            Scribe_Deep.Look(ref value, "state"); Scribe.loader.FinalizeLoading();
            return value ?? throw new InvalidDataException("Native Scribe lost cinematic data.");
        }
        finally { Scribe.ForceStop(); if (File.Exists(path)) File.Delete(path); }
    }

    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        checks++;
    }

    private static void Throws<T>(Action action, string name) where T : Exception
    {
        try { action(); } catch (T) { Check(true, name); return; }
        throw new InvalidOperationException(name);
    }

    private static int checks;
}
