#if RIMWORLD_1_7
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
using MeshDrawCommand = Waylith.Rendering.MeshDrawCommand;
using MeshDrawSubmission = Waylith.Rendering.MeshDrawSubmission;

namespace QuestEditor_Library;

public sealed class CQFMapProjectionRenderer : IDisposable
{
    public CQFMapProjectionRenderer(CQFMapProjection projection) => this.projection = projection;

    public RenderTexture? Texture => texture;
    public int RenderedFrame { get; private set; } = -1;

    public void Draw(Map host)
    {
        bool focused = ReferenceEquals(CQFMapProjectionContext.Focused, projection);
        if (texture == null || Time.frameCount - RenderedFrame >= (focused ? 1 : projection.refreshFrames)) Render(host, focused);
        float altitude = projection.background ? AltitudeLayer.BelowTerrain.AltitudeFor() - 0.01f : AltitudeLayer.MetaOverlays.AltitudeFor() + 0.05f;
        material!.renderQueue = projection.background ? 1890 : 3950;
        CellRect target = projection.DestinationBounds;
        Vector3 center = target.CenterVector3;
        center.y = altitude;
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(center, Quaternion.identity,
            new Vector3(target.Width, 1f, target.Height)), material, 0);
    }

    public Camera GetInputCamera(Camera hostCamera)
    {
        inputCamera ??= new GameObject("CQF_MapProjectionInputCamera").AddComponent<Camera>();
        inputCamera.CopyFrom(hostCamera);
        inputCamera.enabled = false;
        inputCamera.targetTexture = null;
        Rect bounds = projection.ViewRect;
        float scaleX = bounds.width / projection.DestinationBounds.Width;
        float scaleZ = bounds.height / projection.DestinationBounds.Height;
        inputCamera.transform.position = projection.ToSource(hostCamera.transform.position);
        inputCamera.orthographicSize = hostCamera.orthographicSize * scaleZ;
        inputCamera.aspect = hostCamera.aspect * scaleX / scaleZ;
        return inputCamera;
    }

    public RenderTexture RenderPreview(Map host)
    {
        if (texture == null || Time.frameCount - RenderedFrame >= projection.refreshFrames) Render(host, false);
        return texture!;
    }

    public void Invalidate() => RenderedFrame = -1;

    public void Dispose()
    {
        if (texture != null)
        {
            texture.Release();
            UnityEngine.Object.Destroy(texture);
        }
        if (material != null) UnityEngine.Object.Destroy(material);
        if (camera != null) UnityEngine.Object.Destroy(camera.gameObject);
        if (inputCamera != null) UnityEngine.Object.Destroy(inputCamera.gameObject);
        texture = null;
        material = null;
        camera = null;
        inputCamera = null;
        commands.Clear();
        orderedCommands.Clear();
        commandOrder.Clear();
    }

    private void Render(Map host, bool focused)
    {
        if (MeshDrawSubmission.IsCapturing) throw new InvalidOperationException("[CQF] Nested map projection capture is not supported.");
        Map source = projection.sourceMap!;
        Rect bounds = projection.ViewRect;
        Camera hostCamera = Find.Camera;
        camera ??= new GameObject("CQF_MapProjectionCamera").AddComponent<Camera>();
        camera.CopyFrom(hostCamera);
        camera.enabled = false;
        camera.targetTexture = null;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.aspect = bounds.width / bounds.height;
        camera.orthographic = true;
        camera.orthographicSize = bounds.height / 2f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 200f;
        camera.transform.position = new Vector3(bounds.center.x, 65f, bounds.center.y);
        int width = Mathf.Max(16, Mathf.RoundToInt(projection.resolution * Mathf.Min(1f, camera.aspect)));
        int height = Mathf.Max(16, Mathf.RoundToInt(projection.resolution * Mathf.Min(1f, 1f / camera.aspect)));
        if (texture == null || texture.width != width || texture.height != height)
        {
            if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); }
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                { name = "CQF_MapProjection_" + projection.id, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            if (!texture.Create()) throw new InvalidOperationException("[CQF] Could not allocate map projection texture: " + projection.id);
            material ??= new Material(ShaderDatabase.Transparent);
            material.mainTexture = texture;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
        }
        commands.Clear();
        using (CQFMapProjectionContext context = new(projection, hostCamera, camera))
        {
            try
            {
                GlobalRendererUtility.UpdateGlobalShadersParams();
                PlantFallColors.SetFallShaderGlobals(source);
                source.waterInfo.SetTextures();
                using (MeshDrawSubmission.BeginCapture(commands))
                {
                    source.mapDrawer.MapMeshDrawerUpdate_First();
                    source.mapDrawer.DrawMapMesh();
                    source.dynamicDrawManager.DrawDynamicThings();
                    source.gameConditionManager.GameConditionManagerDraw(source);
                    source.designationManager.DrawDesignations();
                    source.overlayDrawer.DrawAllOverlays();
                    source.temporaryThingDrawer.Draw();
                    source.flecks.FleckManagerDraw();
                    source.weatherManager.DrawAllWeather();
                    if (focused) Find.MapUI.MapInterfaceUpdate();
                    else MapComponentUtility.MapComponentOnDraw(source);
                }
                commandOrder.Clear();
                orderedCommands.Clear();
                for (int i = 0; i < commands.Count; i++) commandOrder.Add(i);
                commandOrder.Sort(CompareDrawOrder);
                foreach (int index in commandOrder) orderedCommands.Add(commands[index]);
                ICameraTextureRenderer backend = CameraTextureRenderer.Current
                    ?? throw new InvalidOperationException("[CQF] Map projection texture renderer is unavailable.");
                backend.Render(texture, orderedCommands, CameraViewFactory.GetView(camera), camera.rect, Color.clear);
                RenderedFrame = Time.frameCount;
            }
            finally
            {
                PlantFallColors.SetFallShaderGlobals(host);
                host.waterInfo.SetTextures();
            }
        }
    }

    private int CompareDrawOrder(int first, int second)
    {
        MeshDrawCommand a = commands[first], b = commands[second];
        int result = (a.RenderQueueOverride > 0 ? a.RenderQueueOverride : a.Material.RenderQueue)
            .CompareTo(b.RenderQueueOverride > 0 ? b.RenderQueueOverride : b.Material.RenderQueue);
        return result == 0 ? first.CompareTo(second) : result;
    }

    private readonly CQFMapProjection projection;
    private readonly List<MeshDrawCommand> commands = new();
    private readonly List<MeshDrawCommand> orderedCommands = new();
    private readonly List<int> commandOrder = new();
    private RenderTexture? texture;
    private Material? material;
    private Camera? camera;
    private Camera? inputCamera;
}
#endif
