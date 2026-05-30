using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DialogElement_Text(string text) : IDialogElement
{
    public string GetText()
    {
        return this.text;
    }

    public virtual void Draw(ref float y, Rect inRect)
    {
        float height = Text.CalcHeight(this.text, inRect.width);
        Widgets.Label(new Rect(inRect.x, y, inRect.width, height), this.text);
        y += height;
    }

    public string text = text;
}
