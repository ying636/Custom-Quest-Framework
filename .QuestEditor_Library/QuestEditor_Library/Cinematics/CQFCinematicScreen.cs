using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFCinematicScreen : Window
{
    public CQFCinematicScreen(GameComponent_Cinematics host, CQFCinematicInstance instance)
    {
        this.host = host;
        this.instance = instance;
        layer = WindowLayer.GameUI;
        doWindowBackground = false;
        drawShadow = false;
        closeOnAccept = false;
        closeOnCancel = false;
        preventCameraMotion = instance.LockInput;
        absorbInputAroundWindow = instance.LockInput;
        forcePause = instance.PauseGame;
        focusWhenOpened = instance.LockInput;
        soundAppear = null;
        soundClose = null;
    }

    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);
    protected override float Margin => 0f;
    public Rect SkipButtonRect
    {
        get
        {
            float x = UI.screenWidth * 0.5f - 100f;
            float y = 16f;
            foreach (Vector2 position in Find.ColonistBar.DrawLocs)
                if (position.x < x + 200f && position.x + Find.ColonistBar.Size.x > x)
                    y = Mathf.Max(y, position.y + Find.ColonistBar.Size.y + 32f);
            return new Rect(x, Mathf.Min(y, Mathf.Max(16f, UI.screenHeight - 96f)), 200f, 32f);
        }
    }

    public override void WindowOnGUI()
    {
        if (instance.LockInput) base.WindowOnGUI();
        else DoWindowContents(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight));
    }

    public override void DoWindowContents(Rect inRect)
    {
        try
        {
            instance.Presentation.Draw(inRect);
            if (instance.AllowSkip || instance.IsPreview)
            {
                Rect skip = SkipButtonRect;
                if (CQFUIStyle.ButtonText(skip, "CQF_Cinematic_Skip".Translate())) host.Stop(instance, CQFCinematicOutcome.Skipped);
            }
            if (instance.LockInput && Event.current.type is EventType.KeyDown or EventType.MouseDown or EventType.MouseDrag or EventType.ScrollWheel)
                Event.current.Use();
        }
        catch (Exception error)
        {
            Log.Error("[CQF] Cinematic screen failed: " + error);
            host.Stop(instance, CQFCinematicOutcome.Failed);
        }
    }

    public override void OnCancelKeyPressed()
    {
        if (instance.AllowSkip || instance.IsPreview) host.Stop(instance, CQFCinematicOutcome.Skipped);
        Event.current.Use();
    }

    public override void PostClose()
    {
        if (!released) host.Stop(instance, CQFCinematicOutcome.Cancelled);
    }

    public void Release()
    {
        released = true;
        Close(false);
    }

    private readonly GameComponent_Cinematics host;
    private readonly CQFCinematicInstance instance;
    private bool released;
}
