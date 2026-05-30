using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DialogElement_Option(string text, Action action) : IDialogElement
{
    public string GetText()
    {
        return "Option:" + this.text;
    }

    public virtual void Draw(ref float y, Rect inRect)
    {
        float height = Text.CalcHeight(this.text, inRect.width);
        if (Widgets.ButtonText(new Rect(inRect.x, y, inRect.width, height), this.text
                 + (this.disabled ? $"({this.disableReason})" : null), false,
                !this.disabled, this.disabled ? Color.gray : ColorLibrary.SkyBlue,
                !this.disabled))
        {
            this.action?.Invoke();
        }
        y += height;
    }

    public string disableReason = "";
    public bool disabled = false;
    public string text = text;
    public Action action = action;
    public int? nextIndex;
}
