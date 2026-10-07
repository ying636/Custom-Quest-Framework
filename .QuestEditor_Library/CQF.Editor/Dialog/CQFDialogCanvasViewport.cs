using UnityEngine;

namespace QuestEditor_Library
{
    public sealed class CQFDialogCanvasViewport
    {
        public Vector2 Pan { get; set; } = new Vector2(35f, 35f);
        public float Zoom { get => this.zoom; set => this.zoom = Mathf.Clamp(value, 0.25f, 1.6f); }
        public Vector2 Size { get; set; } = new Vector2(800f, 600f);
        public Vector2 ToScreen(Vector2 point) => point * this.Zoom + this.Pan;
        public Vector2 ToWorld(Vector2 point) => (point - this.Pan) / this.Zoom;
        public Rect ToScreen(Rect rect) => new Rect(this.ToScreen(rect.position), rect.size * this.Zoom);
        public void ZoomAt(Vector2 point, float factor)
        {
            Vector2 world = this.ToWorld(point);
            this.Zoom *= factor;
            this.Pan = point - world * this.Zoom;
        }
        public void Reset() { this.Pan = new Vector2(35f, 35f); this.Zoom = 0.85f; }
        private float zoom = 0.85f;
    }
}
