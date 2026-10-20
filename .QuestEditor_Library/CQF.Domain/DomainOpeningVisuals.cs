using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainOpeningVisuals : IDisposable
{
    public DomainOpeningVisuals(DomainVisualDef definition, Material? template = null)
    {
        this.definition = definition;
        runtime = GameComponent_MapManager.Instance?.Module as CQFDomainRuntime;
        try
        {
            back = template == null ? definition.CreateMaterial(false) : new Material(template);
            back.renderQueue = 3601;
            front = new Material(back) { renderQueue = 3603 };
#if RIMWORLD_1_7
            ground = template == null ? definition.CreateMaterial(false, definition.groundShader) : new Material(template);
            ground.renderQueue = 2900;
#else
            ground = new Material(back) { renderQueue = 3600 };
#endif
            runtime?.RegisterVisuals(this);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Draw(Vector3 center, float progress, float elapsedSeconds) => Draw(center, progress, elapsedSeconds, default);

    public void Draw(Vector3 center, float progress, float elapsedSeconds, DomainFusionState fusion)
    {
        if (ground == null) throw new ObjectDisposedException(nameof(DomainOpeningVisuals));
        properties ??= new MaterialPropertyBlock();
        properties.SetFloat("_Progress", Mathf.Clamp01(progress));
        properties.SetFloat("_EffectTime", elapsedSeconds);
        properties.SetFloat("_Radius", definition.radius);
        properties.SetFloat("_Mode", 0f);
        fusion.Apply(properties);
#if RIMWORLD_1_7
        center.y = AltitudeLayer.LayingPawn.AltitudeFor() - 0.01f;
#else
        center.y = AltitudeLayer.Filth.AltitudeFor();
#endif
        float diameter = (definition.radius + definition.edgeWidth + definition.distortion + 1f) * 2f;
        properties.SetFloat("_Diameter", diameter);
        GenDraw.DrawMeshNowOrLater(MeshPool.plane10, Matrix4x4.TRS(center, Quaternion.identity, new Vector3(diameter, 1f, diameter)),
            ground, false, properties);
        if (progress < definition.curtainStart) return;
        if (frontCurtain == null)
        {
            frontCurtain = BuildCurtain(false);
            backCurtain = BuildCurtain(true);
        }
        properties.SetFloat("_Mode", 1f);
        center.y = AltitudeLayer.Pawn.AltitudeFor();
        Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
        GenDraw.DrawMeshNowOrLater(backCurtain!, matrix, back!, false, properties);
        GenDraw.DrawMeshNowOrLater(frontCurtain, matrix, front!, false, properties);
    }

    public void Dispose()
    {
        if (frontCurtain != null) UnityEngine.Object.Destroy(frontCurtain);
        if (backCurtain != null) UnityEngine.Object.Destroy(backCurtain);
        if (ground != null) UnityEngine.Object.Destroy(ground);
        if (back != null) UnityEngine.Object.Destroy(back);
        if (front != null) UnityEngine.Object.Destroy(front);
        frontCurtain = backCurtain = null;
        ground = back = front = null;
        runtime?.UnregisterVisuals(this);
    }

    private Mesh BuildCurtain(bool rear)
    {
        List<Vector3> vertices = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        for (int band = 0; band < 24; band++)
            for (int segment = 0; segment < 64; segment++)
            {
                float angle = segment * Mathf.PI * 2f / 64f;
                float next = (segment + 1) * Mathf.PI * 2f / 64f;
                if ((Mathf.Sin((angle + next) * 0.5f) >= 0f) != rear) continue;
                Vector3 a = new(Mathf.Cos(angle) * definition.radius, 0f, Mathf.Sin(angle) * definition.radius);
                Vector3 b = new(Mathf.Cos(next) * definition.radius, 0f, Mathf.Sin(next) * definition.radius);
                int index = vertices.Count;
                vertices.Add(a); vertices.Add(a); vertices.Add(b); vertices.Add(b);
                uvs.Add(new Vector2(angle, band / 24f)); uvs.Add(new Vector2(angle, (band + 1) / 24f));
                uvs.Add(new Vector2(next, (band + 1) / 24f)); uvs.Add(new Vector2(next, band / 24f));
                triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
                triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
            }
        Mesh mesh = new() { name = "CQF_DomainCurtain" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(definition.radius * 2f + 2f, 2f,
            (definition.radius + definition.curtainHeight) * 2f + 4f));
        return mesh;
    }

    private readonly DomainVisualDef definition;
    private readonly CQFDomainRuntime? runtime;
    private Material? ground;
    private Material? back;
    private Material? front;
    private Mesh? frontCurtain;
    private Mesh? backCurtain;
    private MaterialPropertyBlock? properties;
}
