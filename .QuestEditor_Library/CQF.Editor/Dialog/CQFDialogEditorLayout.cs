using UnityEngine;

namespace QuestEditor_Library
{
    public sealed class CQFDialogEditorLayout
    {
        public CQFDialogEditorLayout(Rect bounds, bool showPanel)
        {
            Canvas = new Rect(bounds.x, bounds.y + 88f, bounds.width, Mathf.Max(0f, bounds.height - 88f));
            if (!showPanel) return;
            Overlay = bounds.width < 840f;
            float panelWidth = Mathf.Min(bounds.width, Mathf.Clamp(bounds.width * 0.34f, 340f, 420f));
            Panel = new Rect(bounds.xMax - panelWidth, Canvas.y, panelWidth, Canvas.height);
            if (!Overlay) Canvas = new Rect(Canvas.x, Canvas.y, bounds.width - panelWidth - 12f, Canvas.height);
        }

        public Rect Canvas { get; }
        public Rect Panel { get; }
        public bool Overlay { get; }
    }
}
