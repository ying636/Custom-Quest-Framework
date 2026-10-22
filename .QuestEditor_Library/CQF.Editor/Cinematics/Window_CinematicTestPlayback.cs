using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Window_CinematicTestPlayback : Window
{
    public Window_CinematicTestPlayback(CQFCinematicTestSession session)
    {
        this.session = session;
        layer = WindowLayer.Super;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        closeOnCancel = false;
        draggable = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(440f, UI.screenWidth - 32f), 116f);
    protected override float Margin => 10f;

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (!ReferenceEquals(CQFCinematicTestSession.Current, session)) Close(false);
    }

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        CQFCinematicInstance? instance = session.Instance;
        string outcome = instance == null ? "CQF_Cinematic_TestRunning".Translate() : ("CQF_Cinematic_Outcome_" + instance.Outcome).Translate();
        string mode = (session.UsesRehearsal ? "CQF_Cinematic_TestModeRehearsal" : "CQF_Cinematic_TestModeFormal").Translate();
        Widgets.LabelEllipses(new Rect(0f, 0f, inRect.width, 28f), mode + " · " + outcome);
        float width = (inRect.width - 12f) / 3f;
        bool playing = instance?.Outcome == CQFCinematicOutcome.Playing;
        if (CQFUIStyle.ButtonText(new Rect(0f, 43f, width, 30f), (session.Paused ? "CQF_Cinematic_ResumePreview" : "CQF_Cinematic_PausePreview").Translate(), active: playing))
            Control(() => session.Pause(!session.Paused));
        if (CQFUIStyle.ButtonText(new Rect(width + 6f, 43f, width, 30f), "CQF_Cinematic_StepPreview".Translate(), active: playing))
            Control(() => session.Step());
        if (CQFUIStyle.ButtonText(new Rect((width + 6f) * 2f, 43f, width, 30f), "CQF_Cinematic_ReturnFromTest".Translate(), active: instance != null))
            Control(session.StopAndReturn);
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 size = InitialSize;
        windowRect = new Rect(16f, 64f, size.x, size.y);
    }

    private void Control(Action operation)
    {
        try { operation(); }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic copy test control failed: " + error);
            Messages.Message("CQF_Cinematic_TestFailed".Translate(error.Message.CanTranslate() ? error.Message.Translate().ToString() : error.Message), MessageTypeDefOf.RejectInput);
        }
    }

    private readonly CQFCinematicTestSession session;
}
