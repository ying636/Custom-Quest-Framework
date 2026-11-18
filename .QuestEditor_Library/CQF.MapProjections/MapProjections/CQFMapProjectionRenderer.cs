#if RIMWORLD_1_7
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
using MeshDrawCommand = Waylith.Rendering.MeshDrawCommand;
using MeshDrawSubmission = Waylith.Rendering.MeshDrawSubmission;
using FinalDrawItem = Waylith.Rendering.FinalDrawItem;

namespace QuestEditor_Library;

public sealed class CQFMapProjectionRenderer : IDisposable
{
    public CQFMapProjectionRenderer(CQFMapProjection projection) => this.projection = projection;

    public RenderTexture? Texture => texture;
    public int RenderedFrame { get; private set; } = -1;
    public Rect RenderedBounds { get; private set; }

    public void Draw(Map host)
    {
        bool focused = ReferenceEquals(CQFMapProjectionContext.Focused, projection);
        Camera hostCamera = Find.Camera;
        Rect screen = projection.ScreenRect(hostCamera);
        Rect visible = Rect.MinMaxRect(Mathf.Max(0f, screen.xMin), Mathf.Max(0f, screen.yMin),
            Mathf.Min(UI.screenWidth, screen.xMax), Mathf.Min(UI.screenHeight, screen.yMax));
        if (visible.width <= 0f || visible.height <= 0f) return;
        Rect view = projection.ViewRect;
        Rect bounds = new(view.xMin + (visible.xMin - screen.xMin) / screen.width * view.width,
            view.yMin + (screen.yMax - visible.yMax) / screen.height * view.height,
            visible.width / screen.width * view.width, visible.height / screen.height * view.height);
        float aspect = bounds.width / bounds.height;
        int width, height;
        if (projection.autoResolution)
        {
            float pixelsX = visible.width * Prefs.UIScale, pixelsY = visible.height * Prefs.UIScale;
            float scale = Mathf.Min(1f, 4096f / Mathf.Max(pixelsX, pixelsY));
            width = Mathf.Clamp(Mathf.CeilToInt(pixelsX * scale / 64f) * 64, 16, 4096);
            height = Mathf.Clamp(Mathf.CeilToInt(pixelsY * scale / 64f) * 64, 16, 4096);
        }
        else
        {
            width = Mathf.Max(16, Mathf.RoundToInt(projection.resolution * Mathf.Min(1f, aspect)));
            height = Mathf.Max(16, Mathf.RoundToInt(projection.resolution * Mathf.Min(1f, 1f / aspect)));
        }
        if (texture == null || invalidated || bounds != RenderedBounds || width != texture.width || height != texture.height
            || focused != renderedFocused || Time.frameCount - RenderedFrame >= projection.refreshFrames)
            Render(host, focused, bounds, width, height);
        float altitude = projection.background ? AltitudeLayer.BelowTerrain.AltitudeFor() - 0.01f : AltitudeLayer.MetaOverlays.AltitudeFor() + 0.05f;
        material!.renderQueue = projection.background ? 1890 : 3950;
        Vector3 first = projection.ToHost(new Vector3(bounds.xMin, 0f, bounds.yMin));
        Vector3 last = projection.ToHost(new Vector3(bounds.xMax, 0f, bounds.yMax));
        Vector3 center = new((first.x + last.x) / 2f, altitude, (first.z + last.z) / 2f);
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(center, Quaternion.identity,
            new Vector3(last.x - first.x, 1f, last.z - first.z)), material, 0);
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
        Rect bounds = projection.ViewRect;
        float aspect = bounds.width / bounds.height;
        int width = Mathf.Max(16, Mathf.RoundToInt(projection.resolution * Mathf.Min(1f, aspect)));
        int height = Mathf.Max(16, Mathf.RoundToInt(projection.resolution * Mathf.Min(1f, 1f / aspect)));
        if (texture == null || invalidated || bounds != RenderedBounds || width != texture.width || height != texture.height
            || Time.frameCount - RenderedFrame >= projection.refreshFrames) Render(host, false, bounds, width, height);
        return texture!;
    }

    public void Invalidate() => invalidated = true;

    public void Dispose()
    {
        CQFProjectionRenderRetirement.Retire(texture, material);
        if (camera != null) UnityEngine.Object.Destroy(camera.gameObject);
        if (inputCamera != null) UnityEngine.Object.Destroy(inputCamera.gameObject);
        texture = null;
        material = null;
        camera = null;
        inputCamera = null;
        commands.Clear();
        orderedCommands.Clear();
        commandOrder.Clear();
        preparedItems.Clear();
        preparedBackend = null;
        renderPreparedItems = null;
    }

    private void Render(Map host, bool focused, Rect bounds, int width, int height)
    {
        if (MeshDrawSubmission.IsCapturing) throw new InvalidOperationException("[CQF] Nested map projection capture is not supported.");
        Map source = projection.sourceMap!;
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
        bool replace = texture == null || texture.width != width || texture.height != height;
        RenderTexture target = texture!;
        try
        {
            if (replace)
            {
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                    { name = "CQF_MapProjection_" + projection.id, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                if (!target.Create()) throw new InvalidOperationException("[CQF] Could not allocate map projection texture: " + projection.id);
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
                    SubmitCommands(backend, target);
                    if (replace)
                    {
                        Material nextMaterial = new(ShaderDatabase.Transparent)
                        {
                            mainTexture = target,
                            mainTextureScale = Vector2.one,
                            mainTextureOffset = Vector2.zero
                        };
                        CQFProjectionRenderRetirement.Retire(texture, material);
                        texture = target;
                        material = nextMaterial;
                    }
                    RenderedFrame = Time.frameCount;
                    RenderedBounds = bounds;
                    renderedFocused = focused;
                    invalidated = false;
                }
                finally
                {
                    PlantFallColors.SetFallShaderGlobals(host);
                    host.waterInfo.SetTextures();
                }
            }
        }
        catch
        {
            if (replace && !ReferenceEquals(target, texture) && target != null)
            {
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
            throw;
        }
    }

    private void SubmitCommands(ICameraTextureRenderer backend, RenderTexture target)
    {
        if (!ReferenceEquals(preparedBackend, backend))
        {
            preparedBackend = backend;
            renderPreparedItems = null;
            try
            {
                MethodInfo? method = backend.GetType().GetMethod("RenderItems", BindingFlags.Public | BindingFlags.Instance,
                    new[] { typeof(RenderTexture), typeof(List<FinalDrawItem>), typeof(Camera), typeof(Rect), typeof(Color?), typeof(System.Numerics.Vector3?) });
                if (method == null)
                    Log.Warning("[CQF] Projection drawing buffer reuse is unavailable for renderer " + backend.GetType().FullName + "; using its standard render interface.");
                else renderPreparedItems = method.CreateDelegate<Action<RenderTexture, List<FinalDrawItem>, Camera, Rect, Color?, System.Numerics.Vector3?>>(backend);
            }
            catch (Exception error)
            {
                Log.Error("[CQF] Could not bind projection drawing buffer reuse; using the standard render interface: " + error);
            }
        }
        if (renderPreparedItems == null || MeshDrawSubmission.DrawItemMaker is not { } factory || orderedCommands.Count == 0)
        {
            backend.Render(target, orderedCommands, CameraViewFactory.GetView(camera!), camera!.rect, Color.clear);
            return;
        }
        Waylith.Rendering.GpuTextureService.FlushPendingTextureCommands();
        string keywords = Shader.GlobalKeywordKey;
        try
        {
            FinalDrawItem? first = MeshDrawSubmission.TryMakeDrawItem(orderedCommands[0], keywords);
            if (first != null) preparedItems.Add(first);
            for (int i = 1; i < orderedCommands.Count; i++)
            {
                FinalDrawItem? item = factory.TryMake(orderedCommands[i], MeshDrawSubmission.ActiveArena, keywords, i);
                if (item != null) preparedItems.Add(item);
            }
            if (preparedItems.Count == 0)
                backend.Render(target, orderedCommands, CameraViewFactory.GetView(camera!), camera!.rect, Color.clear);
            else renderPreparedItems(target, preparedItems, camera!, camera!.rect, Color.clear, null);
        }
        finally
        {
            foreach (FinalDrawItem item in preparedItems) FinalDrawItem.Return(item);
            preparedItems.Clear();
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
    private readonly List<FinalDrawItem> preparedItems = new();
    private ICameraTextureRenderer? preparedBackend;
    private Action<RenderTexture, List<FinalDrawItem>, Camera, Rect, Color?, System.Numerics.Vector3?>? renderPreparedItems;
    private RenderTexture? texture;
    private Material? material;
    private Camera? camera;
    private Camera? inputCamera;
    private bool renderedFocused;
    private bool invalidated = true;
}
#endif
