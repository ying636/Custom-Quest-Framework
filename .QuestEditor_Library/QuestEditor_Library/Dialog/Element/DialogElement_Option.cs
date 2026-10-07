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
        using CQFUIScope scope = new CQFUIScope();
        string label = this.text + (this.disabled ? $"({this.disableReason})" : null);
        float height = Mathf.Max(42f, Text.CalcHeight(label, inRect.width - 16f) + 12f);
        if (CQFUIStyle.ButtonText(new Rect(inRect.x, y, inRect.width, height), label, true,
                !this.disabled, this.disabled ? CQFUIStyle.Muted : CQFUIStyle.TextColor,
                !this.disabled, TextAnchor.MiddleLeft))
        {
            this.action?.Invoke();
        }
        y += height + 8f;
    }

    public string disableReason = "";
    public bool disabled = false;
    public string text = text;
    public Action action = action;
    public int? nextIndex;
}
