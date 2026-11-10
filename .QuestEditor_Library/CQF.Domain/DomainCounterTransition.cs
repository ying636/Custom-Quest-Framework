using UnityEngine;
using Verse;

namespace QuestEditor_Library;

#if RIMWORLD_1_7
public sealed class DomainCounterTransition : DomainTransition
{
    public DomainCounterTransition(DomainOpening opening) : base(false)
    {
        incoming = opening.Definition;
        defending = opening.Map.GetComponent<MapComponent_Domain>().Definition;
        origin = opening.Position;
        initialRadius = opening.Radius;
    }

    public override float Duration => 4f;
    protected override float CoverProgress => 0.65f;
    protected override float RevealProgress => 0.7f;

    protected override void PreparePresentation()
    {
        Camera camera = Find.Camera;
        Vector3 low = UI.UIToMapPosition(0f, 0f);
        Vector3 high = UI.UIToMapPosition(UI.screenWidth, UI.screenHeight);
        view = new Vector4(low.x, low.z, high.x - low.x, high.z - low.z);
        maximumRadius = Mathf.Max(initialRadius, new Vector2(Mathf.Max(Mathf.Abs(low.x - origin.x), Mathf.Abs(high.x - origin.x)),
            Mathf.Max(Mathf.Abs(low.z - origin.z), Mathf.Abs(high.z - origin.z))).magnitude) + 4f;
        snapshot = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32)
            { name = "CQF_CounterDomainSource", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        if (!snapshot.Create()) throw new InvalidOperationException("[CQF] Could not allocate counter domain snapshot.");
        material = new Material(DefDatabase<ShaderDef>.GetNamed("CQF_DomainFusionTransition").Shader);
        if (defending.surface != null) defendingSurface = new DomainSurfaceVisuals(defending.surface);
        if (incoming.surface != null) incomingSurface = new DomainSurfaceVisuals(incoming.surface);
        material.SetFloat("_Mode", incomingSurface == null || defendingSurface == null ? 1f : 0f);
        material.SetVector("_SourceUV", new Vector4(0f, 1f, 1f, -1f));
        material.SetFloat("_SourceSaturation", Find.CameraColor.saturation);
        surfaceTime = GenTicks.TicksGame / 60f;
        RenderTexture previous = camera.targetTexture;
        try
        {
            camera.targetTexture = snapshot;
            List<Waylith.Rendering.MeshDrawCommand> commands = new();
            Waylith.Rendering.MeshDrawSubmission.CopyPendingCommands(commands, camera.cullingMask);
            commands = commands.OrderBy(command => command.RenderQueueOverride > 0
                ? command.RenderQueueOverride : command.Material.RenderQueue).ToList();
            UnityStubs.Rendering.ICameraTextureRenderer renderer = UnityStubs.Rendering.CameraTextureRenderer.Current
                ?? throw new InvalidOperationException("[CQF] Counter domain snapshot renderer is unavailable.");
            renderer.Render(snapshot, commands, UnityStubs.Rendering.CameraViewFactory.GetView(camera), camera.rect,
                camera.clearFlags is CameraClearFlags.Skybox or CameraClearFlags.Color ? camera.backgroundColor : null);
        }
        finally { camera.targetTexture = previous; }
    }

    protected override void DrawEffect(float progress)
    {
        float cover = Mathf.Clamp01(progress / CoverProgress);
        float revealProgress = Mathf.InverseLerp(RevealProgress, 1f, progress);
        material!.SetFloat("_Progress", progress <= CoverProgress ? cover * 0.42f
            : progress <= RevealProgress ? Mathf.Lerp(0.42f, 0.46f, Mathf.InverseLerp(CoverProgress, RevealProgress, progress))
            : Mathf.Lerp(0.46f, 1f, revealProgress));
        Rect rect = new(0f, 0f, UI.screenWidth, UI.screenHeight);
        GenUI.DrawTextureWithMaterial(rect, snapshot!, material);
        float radius = Mathf.Lerp(initialRadius, maximumRadius, Mathf.SmoothStep(0f, 1f, cover));
        float aperture = Mathf.Lerp(-4f, new Vector2(view.z, view.w).magnitude + 4f,
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(revealProgress / 0.9f)));
        Vector4 reveal = new(view.x + view.z * 0.5f, view.y + view.w * 0.5f, aperture, 1f);
        float time = surfaceTime + progress * Duration;
        float opacity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(cover / 0.12f));
        defendingSurface?.DrawScreen(rect, view, default, default, reveal, time,
            opacity * Mathf.Lerp(0.15f, 1f, cover));
        incomingSurface?.DrawScreen(rect, view, new Vector4(origin.x + 0.5f, origin.z + 0.5f, radius, 0f),
            default, reveal, time + 6f, opacity, new Vector4(0f, 0f, 0f, 3f));
    }

    protected override void ReleaseVisuals()
    {
        defendingSurface?.Dispose();
        incomingSurface?.Dispose();
        defendingSurface = incomingSurface = null;
        if (snapshot != null)
        {
            snapshot.Release();
            UnityEngine.Object.Destroy(snapshot);
            snapshot = null;
        }
        if (material != null) UnityEngine.Object.Destroy(material);
        material = null;
    }

    private readonly DomainDef incoming;
    private readonly DomainDef defending;
    private readonly IntVec3 origin;
    private readonly float initialRadius;
    private RenderTexture? snapshot;
    private Material? material;
    private DomainSurfaceVisuals? defendingSurface;
    private DomainSurfaceVisuals? incomingSurface;
    private Vector4 view;
    private float maximumRadius;
    private float surfaceTime;
}
#endif
