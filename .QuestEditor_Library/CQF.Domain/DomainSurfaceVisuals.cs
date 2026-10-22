using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainSurfaceVisuals : IDisposable
{
    public DomainSurfaceVisuals(DomainSurfaceDef definition)
    {
        this.definition = definition;
        runtime = GameComponent_MapManager.Instance?.Module as CQFDomainRuntime;
        try
        {
            material = definition.CreateMaterial();
            runtime?.RegisterSurface(this);
        }
        catch { Dispose(); throw; }
    }

    public void Draw(Mesh mesh, Matrix4x4 matrix, float time, Vector4 circle = default, Vector4 rival = default, float opacity = 1f, Vector4 clash = default)
    {
        Configure(time, circle, rival, opacity);
        properties!.SetVector("_SurfaceClash", clash);
        int queue = clash.w > 1.5f ? 2902 : rival.w > 1.5f ? 2900 : 2899;
        if (material!.renderQueue != queue) material.renderQueue = queue;
        GenDraw.DrawMeshNowOrLater(mesh, matrix, material, false, properties);
    }

    public void DrawScreen(Rect rect, Vector4 view, Vector4 circle, Vector4 rival, Vector4 reveal, float time, float opacity = 1f)
    {
        if (material == null) throw new ObjectDisposedException(nameof(DomainSurfaceVisuals));
        screenMaterial ??= definition.CreateMaterial(true);
        definition.Configure(screenMaterial, time);
        screenMaterial.SetVector("_SurfaceState", new Vector4(time, 1f, Mathf.Clamp01(opacity), 1f));
        screenMaterial.SetVector("_SurfaceCircle", circle);
        screenMaterial.SetVector("_SurfaceRival", rival);
        screenMaterial.SetVector("_SurfaceView", view);
        screenMaterial.SetVector("_SurfaceReveal", reveal);
#if RIMWORLD_1_7
        if (screenMaterial.HasProperty("_TerritoryState"))
        {
            screenMaterial.SetVector("_TerritoryState", territory == null ? default : new Vector4(territory.BoundaryX,
                territoryFirst != territory.Reversed ? -1f : 1f, 0f, 1f));
            ConfigureProtection();
        }
#endif
        GenUI.DrawTextureWithMaterial(rect, Texture2D.whiteTexture, screenMaterial);
    }

    public void SetTerritory(DomainTerritoryPartition? partition, bool first)
    {
        territory = partition;
        territoryFirst = first;
    }

    public void Dispose()
    {
        if (material != null) UnityEngine.Object.Destroy(material);
        if (screenMaterial != null) UnityEngine.Object.Destroy(screenMaterial);
        if (protectionTexture != null) UnityEngine.Object.Destroy(protectionTexture);
        protectionTexture = null;
        material = null;
        screenMaterial = null;
        runtime?.UnregisterSurface(this);
    }

    private void Configure(float time, Vector4 circle, Vector4 rival, float opacity)
    {
        if (material == null) throw new ObjectDisposedException(nameof(DomainSurfaceVisuals));
        properties ??= new MaterialPropertyBlock();
        definition.Configure(properties, time);
        properties.SetVector("_SurfaceState", new Vector4(time, 0f, Mathf.Clamp01(opacity), 1f));
        properties.SetVector("_SurfaceCircle", circle);
        properties.SetVector("_SurfaceRival", rival);
    }

#if RIMWORLD_1_7
    private void ConfigureProtection()
    {
        if (territory?.HasOverrides != true)
        {
            screenMaterial!.SetVector("_TerritoryProtectionState", default);
            screenMaterial.SetTexture("_TerritoryProtection", Texture2D.whiteTexture);
            return;
        }
        if (protectionTexture == null || protectionTexture.width != territory.Width || protectionTexture.height != territory.Height)
        {
            if (protectionTexture != null) UnityEngine.Object.Destroy(protectionTexture);
            protectionTexture = new Texture2D(territory.Width, territory.Height, TextureFormat.RGBA32, false, true)
            { name = "CQF_DomainProtection", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            protectionPixels = new Color[territory.CellCount];
            protectionRevision = -1;
        }
        if (protectionPartition != territory || protectionRevision != territory.Revision)
        {
            for (int z = 0; z < territory.Height; z++)
            for (int x = 0; x < territory.Width; x++)
            {
                int owner = territory.OverrideAt(x, z);
                protectionPixels![z * territory.Width + x] = new Color(owner == 1 ? 1f : 0f, owner != 0 ? 1f : 0f, 0f, 1f);
            }
            protectionTexture.SetPixels(protectionPixels);
            protectionTexture.Apply(false);
            protectionPartition = territory;
            protectionRevision = territory.Revision;
        }
        screenMaterial!.SetVector("_TerritoryProtectionState", new Vector4(territory.Width, territory.Height, territoryFirst ? 1f : 0f, 1f));
        screenMaterial.SetTexture("_TerritoryProtection", protectionTexture);
    }
#endif

    private readonly DomainSurfaceDef definition;
    private readonly CQFDomainRuntime? runtime;
    private Material? material;
    private Material? screenMaterial;
    private MaterialPropertyBlock? properties;
    private DomainTerritoryPartition? territory;
    private bool territoryFirst;
    private Texture2D? protectionTexture;
#if RIMWORLD_1_7
    private Color[]? protectionPixels;
    private DomainTerritoryPartition? protectionPartition;
    private int protectionRevision = -1;
#endif
}
