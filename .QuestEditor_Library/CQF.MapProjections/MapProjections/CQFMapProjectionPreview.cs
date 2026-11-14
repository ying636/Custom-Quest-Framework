#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapProjectionPreview : IDisposable
{
    public CQFMapProjectionPreview(Map host, Map source, int resolution = 384, CellRect? area = null)
    {
        this.host = host;
        owner = GameComponent_CQFProjectionUI.Instance ?? throw new InvalidOperationException("[CQF] Projection previews require a game UI component.");
        Source = source;
        projection = new CQFMapProjection
        {
            id = "CQF_Preview_" + Guid.NewGuid().ToString("N"), sourceMap = source,
            destination = new CellRect(0, 0, 1, 1), sourceRect = area ?? CellRect.Empty, resolution = resolution, refreshFrames = 30
        };
    }

    public Map Source { get; }
    public Texture? Texture => projection.Texture;

    public void Draw(Rect rect)
    {
        Widgets.DrawBoxSolid(rect, CQFUIStyle.Canvas);
        if (Event.current.type == EventType.Repaint && (projection.Texture == null && lastCaptureFrame != Time.frameCount
            || acquired && Time.frameCount - projection.RenderedFrame >= 30))
        {
            if (!acquired)
            {
                owner.AcquireSource(Source);
                acquired = true;
            }
            projection.RenderPreview(host);
            lastCaptureFrame = Time.frameCount;
        }
        if (projection.Texture != null) GUI.DrawTexture(rect, projection.Texture, ScaleMode.StretchToFill);
    }

    public Rect Fit(Rect outer)
    {
        float ratio = projection.SourceBounds.Width / (float)projection.SourceBounds.Height;
        float width = Mathf.Min(outer.width, outer.height * ratio);
        float height = width / ratio;
        return new Rect(outer.center.x - width / 2f, outer.center.y - height / 2f, width, height);
    }

    public void Dispose()
    {
        projection.Dispose();
        if (acquired)
        {
            acquired = false;
            owner.ReleaseSource(Source);
        }
    }

    private readonly Map host;
    private readonly GameComponent_CQFProjectionUI owner;
    private readonly CQFMapProjection projection;
    private bool acquired;
    private static int lastCaptureFrame = -1;
}
#endif
