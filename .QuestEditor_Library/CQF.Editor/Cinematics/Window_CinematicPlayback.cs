using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Window_CinematicPlayback : Window
{
    public Window_CinematicPlayback(CQFCinematicInstance instance, Action reopen, string? displayName = null)
    {
        this.instance = instance;
        this.reopen = reopen;
        this.displayName = displayName ?? instance.Definition.LabelCap.ToString();
        layer = WindowLayer.Super;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnAccept = false;
        closeOnCancel = false;
        forcePause = instance.IsPreview;
        draggable = true;
    }

    public override Vector2 InitialSize => new(Mathf.Min(440f, UI.screenWidth - 32f), 124f);
    protected override float Margin => 10f;

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (!instance.IsCurrentGame) Close(false);
    }

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        bool playing = instance.Outcome == CQFCinematicOutcome.Playing;
        bool canStop = instance.Outcome is CQFCinematicOutcome.Pending or CQFCinematicOutcome.Playing;
        Widgets.LabelEllipses(new Rect(0f, 0f, inRect.width, 25f), displayName + " · " + ("CQF_Cinematic_Outcome_" + instance.Outcome).Translate());
        Widgets.Label(new Rect(0f, 26f, inRect.width, 22f), "CQF_Cinematic_Elapsed".Translate(instance.Elapsed.ToString("0.00")));
        float width = (inRect.width - 18f) / 4f;
        if (CQFUIStyle.ButtonText(new Rect(0f, 57f, width, 28f), (instance.PreviewPaused ? "CQF_Cinematic_ResumePreview" : "CQF_Cinematic_PausePreview").Translate(), active: playing))
            Control(host => host.SetPreviewPaused(instance, !instance.PreviewPaused));
        if (CQFUIStyle.ButtonText(new Rect(width + 6f, 57f, width, 28f), "CQF_Cinematic_StepPreview".Translate(), active: playing))
            Control(host => host.StepPreview(instance));
        if (CQFUIStyle.ButtonText(new Rect((width + 6f) * 2f, 57f, width, 28f), "CQF_Cinematic_Stop".Translate(), active: canStop))
            Stop();
        if (CQFUIStyle.ButtonText(new Rect((width + 6f) * 3f, 57f, width, 28f), "CQF_Cinematic_ReturnToMap".Translate()))
        {
            if (!Stop()) return;
            Close(false);
            reopen();
        }
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 size = InitialSize;
        windowRect = new Rect(16f, 64f, size.x, size.y);
    }

    private bool Stop()
    {
        if (instance.Outcome is not (CQFCinematicOutcome.Pending or CQFCinematicOutcome.Playing)) return true;
        return Control(host => host.Stop(instance, CQFCinematicOutcome.Cancelled));
    }

    private bool Control(Action<GameComponent_Cinematics> operation)
    {
        try
        {
            operation(GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("CQF_Cinematic_NoGame"));
            return true;
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic preview control failed: " + error);
            Messages.Message("CQF_Cinematic_PlayFailed".Translate() + ": " + (error.Message.CanTranslate() ? error.Message.Translate().ToString() : error.Message), MessageTypeDefOf.RejectInput);
            return false;
        }
    }

    private readonly CQFCinematicInstance instance;
    private readonly Action reopen;
    private readonly string displayName;
}
