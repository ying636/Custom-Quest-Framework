using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DialogElement_Option(string text, Action action) : IDialogElement
{
    internal string DisplayLabel => this.disabled && !string.IsNullOrWhiteSpace(this.disableReason)
        ? this.text + "\n" + this.disableReason
        : this.text;

    internal float HeightForWidth(float width) => Mathf.Max(42f, Text.CalcHeight(this.DisplayLabel, Mathf.Max(1f, width - 12f)) + 12f) + 8f;

    public string GetText()
    {
        return "Option:" + this.text;
    }

    public virtual void Draw(ref float y, Rect inRect)
    {
        using CQFUIScope scope = new CQFUIScope();
        string label = this.DisplayLabel;
        float rowHeight = this.HeightForWidth(inRect.width);
        float height = rowHeight - 8f;
        if (CQFUIStyle.ButtonText(new Rect(inRect.x, y, inRect.width, height), label, true,
                !this.disabled, this.disabled ? CQFUIStyle.Muted : CQFUIStyle.TextColor,
                !this.disabled, TextAnchor.MiddleLeft) && !this.disabled)
        {
            this.action?.Invoke();
        }
        y += rowHeight;
    }

    public string disableReason = "";
    public bool disabled = false;
    public string text = text;
    public Action action = action;
    public int? nextIndex;
}
