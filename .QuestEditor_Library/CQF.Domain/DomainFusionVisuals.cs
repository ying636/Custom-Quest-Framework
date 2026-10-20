#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainFusionVisuals : IDisposable
{
    public DomainFusionVisuals(DomainDef first, DomainDef second)
    {
        try
        {
            if (first.surface != null) this.first = new DomainSurfaceVisuals(first.surface);
            if (second.surface != null) this.second = new DomainSurfaceVisuals(second.surface);
            boundary = new Material(DefDatabase<ShaderDef>.GetNamed("CQF_DomainFusionBoundary").Shader) { renderQueue = 2901 };
            boundary.SetColor("_Color", first.contestColor);
            boundary.SetColor("_SecondaryColor", second.contestColor);
        }
        catch { Dispose(); throw; }
    }

    public void Draw(Vector3 firstCenter, float firstRadius, Vector3 secondCenter, float secondRadius,
        float time, float contactAge, float opacity, float firstFraction)
    {
        if (boundary == null) throw new ObjectDisposedException(nameof(DomainFusionVisuals));
        Vector4 a = new(firstCenter.x, firstCenter.z, firstRadius, 1f);
        Vector4 b = new(secondCenter.x, secondCenter.z, secondRadius, 2f);
        float bias = (0.5f - Mathf.Clamp01(firstFraction)) * Math.Min(firstRadius, secondRadius) * 3f;
        Vector4 clash = new(bias, contactAge, 0f, 1f);
        firstCenter.y = secondCenter.y = AltitudeLayer.LayingPawn.AltitudeFor() - 0.008f;
        first?.Draw(MeshPool.plane10, Matrix4x4.TRS(firstCenter, Quaternion.identity, new Vector3(firstRadius * 2f + 1f, 1f, firstRadius * 2f + 1f)),
            time, a, new Vector4(b.x, b.y, b.z, 1f), opacity, clash);
        second?.Draw(MeshPool.plane10, Matrix4x4.TRS(secondCenter, Quaternion.identity, new Vector3(secondRadius * 2f + 1f, 1f, secondRadius * 2f + 1f)),
            time + 6f, new Vector4(b.x, b.y, b.z, 1f), new Vector4(a.x, a.y, a.z, 2f), opacity, clash);
        float minX = Math.Min(a.x - a.z, b.x - b.z) - 1f;
        float minZ = Math.Min(a.y - a.z, b.y - b.z) - 1f;
        float width = Math.Max(a.x + a.z, b.x + b.z) + 1f - minX;
        float height = Math.Max(a.y + a.z, b.y + b.z) + 1f - minZ;
        properties.SetVector("_SurfaceCircle", a);
        properties.SetVector("_SurfaceRival", b);
        properties.SetVector("_SurfaceClash", clash);
        properties.SetVector("_SurfaceState", new Vector4(time + 6f, 0f, Mathf.Clamp01(opacity), 1f));
        GenDraw.DrawMeshNowOrLater(MeshPool.plane10,
            Matrix4x4.TRS(new Vector3(minX + width * 0.5f, firstCenter.y + 0.001f, minZ + height * 0.5f), Quaternion.identity, new Vector3(width, 1f, height)),
            boundary, false, properties);
    }

    public void Dispose()
    {
        first?.Dispose();
        second?.Dispose();
        if (boundary != null) UnityEngine.Object.Destroy(boundary);
        first = second = null;
        boundary = null;
    }

    private readonly MaterialPropertyBlock properties = new();
    private DomainSurfaceVisuals? first;
    private DomainSurfaceVisuals? second;
    private Material? boundary;
}
#endif
