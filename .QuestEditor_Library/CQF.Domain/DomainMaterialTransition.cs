using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainMaterialTransition : DomainTransition
{
    public DomainMaterialTransition(DomainVisualDef definition, IntVec3 origin, bool returning, Material? template = null) : base(returning)
    {
        this.definition = definition;
        try
        {
            Camera camera = Find.Camera;
            Vector3 center = camera.WorldToViewportPoint(origin.ToVector3Shifted());
            snapshot = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32)
                { name = "CQF_DomainSourceMap", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            if (!snapshot.Create()) throw new InvalidOperationException("[CQF] Could not allocate domain transition snapshot.");
            material = template == null ? definition.CreateMaterial(true) : new Material(template);
            material.SetVector("_Origin", new Vector4(Mathf.Clamp01(center.x), Mathf.Clamp01(1f - center.y), camera.aspect, 0f));
            material.SetFloat("_Returning", returning ? 1f : 0f);
#if RIMWORLD_1_7
            material.SetVector("_SourceUV", new Vector4(0f, 1f, 1f, -1f));
            material.SetFloat("_SourceSaturation", Find.CameraColor.saturation);
#else
            material.SetVector("_SourceUV", new Vector4(0f, 0f, 1f, 1f));
            material.SetFloat("_SourceSaturation", 1f);
#endif
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
                    ?? throw new InvalidOperationException("[CQF] Camera snapshot renderer is unavailable.");
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

    public override float Duration => IsReturning ? definition.returnDuration : definition.entryDuration;
    protected override float CoverProgress => definition.coverProgress;
    protected override float RevealProgress => definition.revealProgress;

    protected override void DrawEffect(float progress)
    {
        material!.SetFloat("_Progress", progress);
        if (IsReturning)
        {
            Vector3 focus = Find.Camera.WorldToViewportPoint(destinationFocus.ToVector3Shifted());
            material.SetVector("_Destination", new Vector4(focus.x, 1f - focus.y, Find.Camera.aspect, 0f));
        }
        GenUI.DrawTextureWithMaterial(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), snapshot!, material);
    }

    protected override void ReleaseVisuals()
    {
        if (snapshot != null)
        {
            snapshot.Release();
            UnityEngine.Object.Destroy(snapshot);
            snapshot = null;
        }
        if (material != null) UnityEngine.Object.Destroy(material);
        material = null;
    }

    private readonly DomainVisualDef definition;
    private RenderTexture? snapshot;
    private Material? material;
}
