using UnityEngine;

namespace QuestEditor_Library;

public interface IDialogElement
{
    public string GetText();
    public void Draw(ref float y, Rect inRect);
}
