using System;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Dialog_CQFDialogVisualSettings : Window
{
    public Dialog_CQFDialogVisualSettings(Func<float, float> draw)
    {
        this.draw = draw;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        draggable = true;
        resizeable = true;
    }

    public override Vector2 InitialSize => new(560f, 680f);

    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope scope = new(inRect.width, inRect.height);
        Rect view = new(0f, 30f, inRect.width, inRect.height - 30f);
        float width = view.width - 22f;
        Widgets.BeginScrollView(view, ref scroll, new Rect(0f, 0f, width, Mathf.Max(view.height, height)));
        try { height = draw(width) + 12f; }
        finally { Widgets.EndScrollView(); }
    }

    private readonly Func<float, float> draw;
    private Vector2 scroll;
    private float height = 640f;
}
