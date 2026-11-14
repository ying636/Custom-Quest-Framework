using UnityEngine;

namespace QuestEditor_Library;

public abstract class CQFDialogRenderer : IDisposable
{
    public virtual float Margin => 0f;
    public virtual bool DrawWindowBackground => false;

    public abstract Vector2 GetInitialSize(Vector2 screenSize);
    public abstract void Draw(Rect rect, CQFDialogFrame frame);
    public virtual void Dispose() { }
}
