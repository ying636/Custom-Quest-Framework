using UnityEngine;
using UnityStubs.Rendering;
using Waylith.Rendering;

namespace CQF.ProjectionQualityChecks;

public class ProjectionBackendProbe : ICameraTextureRenderer
{
    public ProjectionBackendProbe(ICameraTextureRenderer inner) => this.inner = inner;

    public int Calls { get; private set; }

    public void Render(UnityEngine.RenderTexture target, IReadOnlyList<MeshDrawCommand> commands, CameraView view, Rect viewportRect, Color? clearColor)
    {
        Calls++;
        inner.Render(target, commands, view, viewportRect, clearColor);
    }

    public bool TryReadPixels(UnityEngine.RenderTexture source, out byte[] rgbaTopDown) => inner.TryReadPixels(source, out rgbaTopDown);

    public void ReleaseTarget(UnityEngine.RenderTexture target) => inner.ReleaseTarget(target);

    private readonly ICameraTextureRenderer inner;
}
