using UnityEngine;
using UnityStubs.Rendering;
using Waylith.Rendering;

namespace CQF.ProjectionQualityChecks;

public sealed class ThrowingProjectionBackend : ProjectionBackendProbe
{
    public ThrowingProjectionBackend(ICameraTextureRenderer inner) : base(inner) { }

    public void RenderItems(UnityEngine.RenderTexture target, List<FinalDrawItem> items, Camera camera, Rect viewportRect, Color? clearColor, System.Numerics.Vector3? sunDirection)
        => throw new InvalidOperationException("CQF expected drawing-buffer fault");
}
