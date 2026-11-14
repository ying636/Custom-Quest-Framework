#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapProjection : IExposable, IDisposable
{
    public string DisplayName => !name.NullOrEmpty() ? name : "CQF_MP_AutoName".Translate(SourceName);
    public string SourceName => sourceMap == null || sourceMap.Disposed ? "CQF_MP_NoSource".Translate()
        : sourceMap.Parent?.LabelCap is { Length: > 0 } label ? label : "CQF_MP_MapName".Translate(sourceMap.uniqueID);
    public CellRect SourceBounds => sourceMap == null ? CellRect.Empty : sourceRect.IsEmpty ? CellRect.WholeMap(sourceMap) : sourceRect;
    public CellRect DestinationBounds => matchCellSize ? new CellRect(destination.minX, destination.minZ, SourceBounds.Width, SourceBounds.Height) : destination;
    public Rect ViewRect
    {
        get
        {
            CellRect bounds = SourceBounds;
            float width = bounds.Width * ViewZoom, height = bounds.Height * ViewZoom;
            Vector2 center = ViewZoom >= 1f ? new Vector2(bounds.minX + bounds.Width / 2f, bounds.minZ + bounds.Height / 2f) : viewCenter;
            center.x = Mathf.Clamp(center.x, bounds.minX + width / 2f, bounds.maxX + 1f - width / 2f);
            center.y = Mathf.Clamp(center.y, bounds.minZ + height / 2f, bounds.maxZ + 1f - height / 2f);
            return new Rect(center.x - width / 2f, center.y - height / 2f, width, height);
        }
    }
    public CellRect VisibleCells
    {
        get
        {
            Rect view = ViewRect;
            return CellRect.FromLimits(Mathf.FloorToInt(view.xMin), Mathf.FloorToInt(view.yMin), Mathf.CeilToInt(view.xMax) - 1, Mathf.CeilToInt(view.yMax) - 1);
        }
    }
    public float ViewZoom => matchCellSize ? 1f : viewZoom;
    public bool CanZoom => allowZoom && !matchCellSize;
    public bool CanControl => enabled && mode == CQFMapProjectionMode.Control && sourceMap is { Disposed: false };
    public RenderTexture? Texture => renderer?.Texture;
    public int RenderedFrame => renderer?.RenderedFrame ?? -1;

    public Vector3 ToSource(Vector3 hostPosition)
    {
        Rect bounds = ViewRect;
        CellRect target = DestinationBounds;
        return new Vector3(bounds.xMin + (hostPosition.x - target.minX) * bounds.width / target.Width,
            hostPosition.y, bounds.yMin + (hostPosition.z - target.minZ) * bounds.height / target.Height);
    }

    public Vector3 ToHost(Vector3 sourcePosition)
    {
        Rect bounds = ViewRect;
        CellRect target = DestinationBounds;
        return new Vector3(target.minX + (sourcePosition.x - bounds.xMin) * target.Width / bounds.width,
            sourcePosition.y, target.minZ + (sourcePosition.z - bounds.yMin) * target.Height / bounds.height);
    }

    public bool Contains(Vector3 position) => position.x >= DestinationBounds.minX && position.x < DestinationBounds.maxX + 1f
        && position.z >= DestinationBounds.minZ && position.z < DestinationBounds.maxZ + 1f;

    public CQFMapProjection Copy() => new()
    {
        id = id, name = name, sourceMap = sourceMap, sourceRect = sourceRect, destination = destination,
        mode = mode, background = background, enabled = enabled, resolution = resolution, refreshFrames = refreshFrames,
        allowZoom = allowZoom, matchCellSize = matchCellSize,
        viewZoom = viewZoom, viewCenter = viewCenter
    };

    public Rect ScreenRect(Camera hostCamera)
    {
        CellRect target = DestinationBounds;
        Vector3 first = hostCamera.WorldToScreenPoint(new Vector3(target.minX, 0f, target.minZ)) / Prefs.UIScale;
        Vector3 last = hostCamera.WorldToScreenPoint(new Vector3(target.maxX + 1f, 0f, target.maxZ + 1f)) / Prefs.UIScale;
        return Rect.MinMaxRect(Mathf.Min(first.x, last.x), UI.screenHeight - Mathf.Max(first.y, last.y),
            Mathf.Max(first.x, last.x), UI.screenHeight - Mathf.Min(first.y, last.y));
    }

    public void ZoomView(float factor, Vector3 sourceAnchor)
    {
        if (!CanZoom) return;
        Rect before = ViewRect;
        CellRect area = SourceBounds;
        float minimum = Mathf.Min(1f, 4f / Mathf.Min(area.Width, area.Height));
        float next = Mathf.Clamp(viewZoom * factor, minimum, 1f);
        float ratio = next / viewZoom;
        viewCenter = new Vector2(sourceAnchor.x + (before.center.x - sourceAnchor.x) * ratio,
            sourceAnchor.z + (before.center.y - sourceAnchor.z) * ratio);
        viewZoom = next;
        NotifyViewChanged();
    }

    public void PanView(Vector2 delta)
    {
        viewCenter = ViewRect.center + delta;
        NotifyViewChanged();
    }

    public void CenterView(IntVec3 cell)
    {
        viewCenter = new Vector2(cell.x + 0.5f, cell.z + 0.5f);
        NotifyViewChanged();
    }

    public void ResetView()
    {
        viewZoom = 1f;
        viewCenter = Vector2.zero;
        NotifyViewChanged();
    }

    public void Validate(Map host)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("[CQF] Map projection requires an ID.");
        if (host == null || host.Disposed || sourceMap == null || sourceMap.Disposed || sourceMap == host)
            throw new InvalidOperationException("[CQF] Map projection requires two distinct, available maps: " + id);
        CellRect bounds = SourceBounds;
        if (bounds.IsEmpty || !bounds.Min.InBounds(sourceMap) || !bounds.Max.InBounds(sourceMap))
            throw new InvalidOperationException("[CQF] Map projection source rectangle is outside its map: " + id);
        CellRect target = DestinationBounds;
        if (matchCellSize && (target.IsEmpty || !target.Min.InBounds(host) || !target.Max.InBounds(host)))
            throw new InvalidOperationException("[CQF] One-to-one map projection does not fit in its host map: " + id);
        if (target.IsEmpty || !target.Min.InBounds(host) || !target.Max.InBounds(host))
            throw new InvalidOperationException("[CQF] Map projection destination rectangle is outside its map: " + id);
        if (!Enum.IsDefined(mode)) throw new InvalidOperationException("[CQF] Invalid map projection mode: " + id);
        if (resolution < 128 || resolution > 4096) throw new InvalidOperationException("[CQF] Invalid map projection resolution: " + id);
        if (refreshFrames < 1 || refreshFrames > 60) throw new InvalidOperationException("[CQF] Invalid map projection refresh interval: " + id);
        if (!float.IsFinite(viewZoom) || viewZoom <= 0f || viewZoom > 1f)
            throw new InvalidOperationException("[CQF] Invalid map projection view zoom: " + id);
        if (!float.IsFinite(viewCenter.x) || !float.IsFinite(viewCenter.y))
            throw new InvalidOperationException("[CQF] Invalid map projection view center: " + id);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref id, "id", string.Empty);
        Scribe_Values.Look(ref name, "name", string.Empty);
        Scribe_References.Look(ref sourceMap, "sourceMap");
        Scribe_Values.Look(ref sourceRect, "sourceRect", CellRect.Empty);
        Scribe_Values.Look(ref destination, "destination");
        Scribe_Values.Look(ref mode, "mode");
        Scribe_Values.Look(ref background, "background");
        Scribe_Values.Look(ref enabled, "enabled", true);
        Scribe_Values.Look(ref resolution, "resolution", 1024);
        Scribe_Values.Look(ref refreshFrames, "refreshFrames", 1);
        Scribe_Values.Look(ref allowZoom, "allowZoom", true);
        Scribe_Values.Look(ref matchCellSize, "matchCellSize");
        Scribe_Values.Look(ref viewZoom, "viewZoom", 1f);
        Scribe_Values.Look(ref viewCenter, "viewCenter", Vector2.zero);
    }

    public void Draw(Map host)
    {
        if (!enabled || CQFMapProjectionContext.IsRendering) return;
        Validate(host);
        if (!Find.CameraDriver.CurrentViewRect.Overlaps(DestinationBounds)) return;
        renderer ??= new CQFMapProjectionRenderer(this);
        renderer.Draw(host);
    }

    public Camera GetInputCamera(Camera hostCamera)
    {
        renderer ??= new CQFMapProjectionRenderer(this);
        return renderer.GetInputCamera(hostCamera);
    }

    public RenderTexture RenderPreview(Map host)
    {
        Validate(host);
        renderer ??= new CQFMapProjectionRenderer(this);
        return renderer.RenderPreview(host);
    }

    public void Dispose()
    {
        renderer?.Dispose();
        renderer = null;
        CQFMapProjectionContext.Release(this);
    }

    private void NotifyViewChanged()
    {
        viewCenter = ViewRect.center;
        renderer?.Invalidate();
        CQFMapProjectionContext.NotifyViewChanged(this);
    }

    public string id = string.Empty;
    public string name = string.Empty;
    public Map? sourceMap;
    public CellRect sourceRect = CellRect.Empty;
    public CellRect destination;
    public CQFMapProjectionMode mode;
    public bool background;
    public bool enabled = true;
    public int resolution = 1024;
    public int refreshFrames = 1;
    public bool allowZoom = true;
    public bool matchCellSize;
    private CQFMapProjectionRenderer? renderer;
    private Vector2 viewCenter;
    private float viewZoom = 1f;
}
#endif
