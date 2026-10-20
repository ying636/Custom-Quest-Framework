using UnityEngine;
using Verse;

namespace QuestEditor_Library;

#if RIMWORLD_1_7
public sealed class DomainFusionTransition : DomainTransition
{
    public DomainFusionTransition(DomainOpening_Fusion fusion) : base(false)
    {
        try
        {
            Camera camera = Find.Camera;
            snapshot = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32)
                { name = "CQF_FusedDomainSource", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            if (!snapshot.Create()) throw new InvalidOperationException("[CQF] Could not allocate fused domain snapshot.");
            material = new Material(DefDatabase<ShaderDef>.GetNamed("CQF_DomainFusionTransition").Shader);
            if (fusion.First.Definition.surface != null) firstSurface = new DomainSurfaceVisuals(fusion.First.Definition.surface);
            if (fusion.Second.Definition.surface != null) secondSurface = new DomainSurfaceVisuals(fusion.Second.Definition.surface);
            material.SetFloat("_Mode", firstSurface == null || secondSurface == null ? 1f : 0f);
            Vector3 first = camera.WorldToViewportPoint(fusion.First.Position.ToVector3Shifted());
            Vector3 second = camera.WorldToViewportPoint(fusion.Second.Position.ToVector3Shifted());
            view = new Vector4(0f, 0f, 30f * camera.aspect, 30f);
            firstOrigin = new Vector2(first.x * view.z, (1f - first.y) * view.w);
            secondOrigin = new Vector2(second.x * view.z, (1f - second.y) * view.w);
            maximumRadius = Mathf.Max(
                new Vector2(Mathf.Max(Mathf.Abs(firstOrigin.x), Mathf.Abs(view.z-firstOrigin.x)), Mathf.Max(Mathf.Abs(firstOrigin.y), Mathf.Abs(view.w-firstOrigin.y))).magnitude,
                new Vector2(Mathf.Max(Mathf.Abs(secondOrigin.x), Mathf.Abs(view.z-secondOrigin.x)), Mathf.Max(Mathf.Abs(secondOrigin.y), Mathf.Abs(view.w-secondOrigin.y))).magnitude) + 1f;
            surfaceStartedAt = Time.realtimeSinceStartup;
            material.SetVector("_SourceUV", new Vector4(0f, 1f, 1f, -1f));
            material.SetFloat("_SourceSaturation", Find.CameraColor.saturation);
            RenderTexture previous = camera.targetTexture;
            try
            {
                camera.targetTexture = snapshot;
#if RIMWORLD_1_7
                List<Waylith.Rendering.MeshDrawCommand> commands = new();
                Waylith.Rendering.MeshDrawSubmission.CopyPendingCommands(commands, camera.cullingMask);
                commands = commands.OrderBy(command => command.RenderQueueOverride > 0
                    ? command.RenderQueueOverride : command.Material.RenderQueue).ToList();
                UnityStubs.Rendering.ICameraTextureRenderer renderer = UnityStubs.Rendering.CameraTextureRenderer.Current
                    ?? throw new InvalidOperationException("[CQF] Fused domain snapshot renderer is unavailable.");
                renderer.Render(snapshot, commands, UnityStubs.Rendering.CameraViewFactory.GetView(camera), camera.rect,
                    camera.clearFlags is CameraClearFlags.Skybox or CameraClearFlags.Color ? camera.backgroundColor : null);
#else
                camera.Render();
#endif
            }
            finally { camera.targetTexture = previous; }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public override float Duration => 2f;
    protected override float CoverProgress => 0.42f;
    protected override float RevealProgress => 0.46f;

    protected override void DrawEffect(float progress)
    {
        material!.SetFloat("_Progress", progress);
        Rect rect = new(0f, 0f, UI.screenWidth, UI.screenHeight);
        GenUI.DrawTextureWithMaterial(rect, snapshot!, material);
        float radius = maximumRadius * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / CoverProgress));
        float aperture = Mathf.Lerp(-1f, new Vector2(view.z, view.w).magnitude + 1f,
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RevealProgress, 0.95f, progress)));
        Vector4 first = new(firstOrigin.x, firstOrigin.y, radius, 1f);
        Vector4 second = new(secondOrigin.x, secondOrigin.y, radius, 1f);
        Vector4 reveal = new(view.z * 0.5f, view.w * 0.5f, aperture, 1f);
        float time = Time.realtimeSinceStartup - surfaceStartedAt;
        float opacity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.025f));
        firstSurface?.DrawScreen(rect, view, first, second, reveal, time, opacity);
        first.w = 2f;
        secondSurface?.DrawScreen(rect, view, second, first, reveal, time + 6f, opacity);
    }

    protected override void ReleaseVisuals()
    {
        firstSurface?.Dispose();
        secondSurface?.Dispose();
        firstSurface = secondSurface = null;
        if (snapshot != null)
        {
            snapshot.Release();
            UnityEngine.Object.Destroy(snapshot);
            snapshot = null;
        }
        if (material != null) UnityEngine.Object.Destroy(material);
        material = null;
    }

    private RenderTexture? snapshot;
    private Material? material;
    private DomainSurfaceVisuals? firstSurface;
    private DomainSurfaceVisuals? secondSurface;
    private readonly Vector4 view;
    private readonly Vector2 firstOrigin;
    private readonly Vector2 secondOrigin;
    private readonly float maximumRadius;
    private readonly float surfaceStartedAt;
}
#endif
