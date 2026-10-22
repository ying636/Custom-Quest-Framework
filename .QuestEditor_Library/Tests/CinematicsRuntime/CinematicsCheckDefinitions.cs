using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CQF.CinematicsRuntimeChecks;

public static class CinematicsCheckDefinitions
{
    public static void Register()
    {
        Add("CQF_Runtime_Visual", new()
        {
            new CQFCinematicStep_Parallel
            {
                steps = new()
                {
                    new CQFCinematicStep_Camera { targetKey = "Trigger", size = 18f, duration = 0.6f },
                    new CQFCinematicStep_Letterbox { height = 0.12f, duration = 0.3f },
                    new CQFCinematicStep_Fade { color = new Color(0f, 0f, 0f, 0.4f), duration = 0.6f },
                    new CQFCinematicStep_Subtitle { textKey = "CQFCinematicStep_Subtitle", duration = 3f }
                }
            }
        }, true, "CQF_Runtime_VisualCompleted");
        Add("CQF_Runtime_Queued", new() { new CQFCinematicStep_Wait { duration = 0.6f } }, false, "CQF_Runtime_QueuedCompleted");
        Add("CQF_Runtime_Actor", new()
        {
            new CQFCinematicStep_PawnMove { pawnKey = "Trigger", targetKey = "Destination", timeout = 20f },
            new CQFCinematicStep_PawnWait { pawnKey = "Trigger", duration = 1f },
            new CQFCinematicStep_PawnJob { pawnKey = "Trigger", jobDef = JobDefOf.Goto, targetKey = "Origin", timeout = 20f }
        }, false, "CQF_Runtime_ActorCompleted");
        CQFCinematicDef skip = Add("CQF_Runtime_Skip", new()
        {
            new CQFCinematicStep_Subtitle { textKey = "CQFCinematicStep_Subtitle", duration = 30f }
        });
        skip.skippedSignal = "CQF_Runtime_Skipped";
        skip.skipActions.Add(new CinematicsCheckAction());
        CQFCinematicDef cancel = Add("CQF_Runtime_Cancel", new() { new CQFCinematicStep_PawnWait { pawnKey = "Trigger", duration = 20f } });
        cancel.cancelledSignal = "CQF_Runtime_Cancelled";
        Add("CQF_Runtime_Signal", new() { new CQFCinematicStep_WaitSignal { signal = "CQF_Runtime_Release", addQuestPrefix = false, timeout = 10f } }, false, "CQF_Runtime_SignalCompleted");
        Add("CQF_Runtime_Save", new() { new CQFCinematicStep_Subtitle { textKey = "CQFCinematicStep_Subtitle", duration = 12f } }, false, "CQF_Runtime_SaveCompleted");
        Add("CQF_Runtime_Leaving", new() { new CQFCinematicStep_Wait { duration = 30f } });
        JobDef instantSuccess = new() { defName = "CQF_Runtime_InstantSuccessJob", driverClass = typeof(JobDriver_CinematicInstantSuccess) };
        JobDef instantFailure = new() { defName = "CQF_Runtime_InstantFailureJob", driverClass = typeof(JobDriver_CinematicInstantFailure) };
        DefDatabase<JobDef>.Add(instantSuccess);
        DefDatabase<JobDef>.Add(instantFailure);
        Add("CQF_Runtime_InstantSuccess", new() { new CQFCinematicStep_PawnJob { pawnKey = "Trigger", jobDef = instantSuccess } }, false, "CQF_Runtime_InstantSucceeded");
        CQFCinematicDef failed = Add("CQF_Runtime_InstantFailure", new() { new CQFCinematicStep_PawnJob { pawnKey = "Trigger", jobDef = instantFailure } });
        failed.failedSignal = "CQF_Runtime_InstantFailed";
        Add("CQF_Runtime_FullCover", new()
        {
            new CQFCinematicStep_Fade { color = Color.black, duration = 0.2f },
            new CQFCinematicStep_Actions { actions = new() { new CinematicsCheckCoverAction() } },
            new CQFCinematicStep_Wait { duration = 0.6f }
        }, false, "CQF_Runtime_CoverCompleted");
        JobDef delayed = new() { defName = "CQF_Runtime_DelayedJob", driverClass = typeof(JobDriver_CinematicDelayed) };
        DefDatabase<JobDef>.Add(delayed);
        Add("CQF_Runtime_SaveJob", new() { new CQFCinematicStep_PawnJob { pawnKey = "Trigger", jobDef = delayed, timeout = 20f } }, false, "CQF_Runtime_SavedJobCompleted");
        DialogTreeDef tree = new()
        {
            defName = "CQF_Runtime_DialogTree", label = "CQF_Runtime_DialogTree", title = "CQFCinematicStep_Subtitle",
            nodeMoulds = new()
            {
                [0] = new DialogNode(0) { text = "CQFCinematicStep_Subtitle" },
                [1] = new DialogNode(1) { text = "CQFCinematicStep_Subtitle" }
            }
        };
        DefDatabase<DialogTreeDef>.Add(tree);
        Add("CQF_Runtime_SaveDialog", new() { new CQFCinematicStep_Dialog { tree = tree, interviewerKey = "Trigger", intervieweeKey = "Trigger", timeout = 30f } }, false, "CQF_Runtime_SavedDialogCompleted");
    }

    private static CQFCinematicDef Add(string name, List<CQFCinematicStep> steps, bool paused = false, string completed = "")
    {
        CQFCinematicDef definition = new()
        {
            defName = name, label = name, steps = steps, pauseGame = paused,
            completedSignal = completed, addQuestPrefix = false, maxDuration = 60f
        };
        DefDatabase<CQFCinematicDef>.Add(definition);
        return definition;
    }
}
