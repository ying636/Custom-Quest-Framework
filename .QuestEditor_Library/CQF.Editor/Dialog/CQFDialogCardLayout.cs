using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFDialogCardLayout
    {
        public CQFDialogCardLayout(CQFDialogNodeLayout layout, Rect bounds)
        {
            this.layout = layout;
            this.Bounds = bounds;
            this.Scale = bounds.width / CQFDialogNodeLayout.Width;
            this.Header = this.Place(1f, 1f, CQFDialogNodeLayout.Width - 2f, CQFDialogNodeLayout.HeaderHeight - 2f);
            this.Title = this.Place(10f, 3f, CQFDialogNodeLayout.Width - 20f, 24f);
            this.Body = this.Place(14f, 38f, CQFDialogNodeLayout.Width - 28f, layout.BodyHeight);
            this.Input = InputAnchor(bounds);
            this.Output = new Vector2(bounds.xMax, this.Input.y);
        }
        public Rect Bounds { get; }
        public float Scale { get; }
        public Rect Header { get; }
        public Rect Title { get; }
        public Rect Body { get; }
        public Vector2 Input { get; }
        public Vector2 Output { get; }
        public Rect Row(int index) => this.Place(10f, this.layout.OptionsTop + index * CQFDialogNodeLayout.OptionHeight, CQFDialogNodeLayout.Width - 20f, CQFDialogNodeLayout.OptionHeight - 3f);
        public Rect RowText(int index) => this.Row(index).ContractedBy(3f * this.Scale);
        public Vector2 RowOutput(int index) => new Vector2(this.Bounds.xMax, this.Bounds.y + (this.layout.OptionsTop + (index + 0.5f) * CQFDialogNodeLayout.OptionHeight) * this.Scale);
        public Rect PortHit(Vector2 point) => HitArea(point, this.Scale);
        public static Vector2 InputAnchor(Rect bounds) => new Vector2(bounds.x, bounds.y + CQFDialogNodeLayout.HeaderHeight * (bounds.width / CQFDialogNodeLayout.Width) / 2f);
        public static Rect HitArea(Vector2 point, float scale)
        {
            float radius = Mathf.Clamp(9f * scale, 3f, 12f);
            return new Rect(point.x - radius, point.y - radius, radius * 2f, radius * 2f);
        }
        public int RowAt(Vector2 point) => Mathf.FloorToInt((point.y - this.Bounds.y - this.layout.OptionsTop * this.Scale) / (CQFDialogNodeLayout.OptionHeight * this.Scale));
        private Rect Place(float x, float y, float width, float height) => new Rect(this.Bounds.x + x * this.Scale, this.Bounds.y + y * this.Scale, width * this.Scale, height * this.Scale);
        private readonly CQFDialogNodeLayout layout;
    }
}
