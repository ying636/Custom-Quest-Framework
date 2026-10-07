using UnityEngine;

namespace QuestEditor_Library
{
    public sealed class CQFAIWindowLayout
    {
        public CQFAIWindowLayout(Rect bounds, float textHeight, int statusRows)
        {
            Compact = bounds.width < 420f;
            Header = new Rect(bounds.x, bounds.y, bounds.width, 32f);
            Title = new Rect(bounds.x, bounds.y, Mathf.Max(0f, bounds.width - (Compact ? 5f : 6f) * 36f - 12f), 32f);
            float composerHeight = Mathf.Min(Mathf.Clamp(textHeight, 40f, 96f) + 56f, Mathf.Max(64f, bounds.height - 124f));
            Composer = new Rect(bounds.x, bounds.yMax - composerHeight - 4f, bounds.width, composerHeight);
            Status = new Rect(bounds.x + 4f, Composer.y - statusRows * 24f - 8f, bounds.width - 8f, statusRows * 24f);
            History = new Rect(bounds.x, Header.yMax + 12f, bounds.width, Mathf.Max(0f, Status.y - Header.yMax - 20f));
            Input = new Rect(Composer.x + 12f, Composer.y + 12f, Composer.width - 24f, Composer.height - 56f);
            Send = new Rect(Composer.xMax - 44f, Composer.yMax - 40f, 32f, 32f);
        }

        public bool Compact { get; }
        public Rect Header { get; }
        public Rect Title { get; }
        public Rect History { get; }
        public Rect Composer { get; }
        public Rect Status { get; }
        public Rect Input { get; }
        public Rect Send { get; }
        public Rect HeaderButton(int index) => new Rect(Header.xMax - 32f - index * 36f, Header.y, 32f, 32f);
    }
}
