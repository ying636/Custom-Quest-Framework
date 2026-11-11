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
        => Draw(mesh, matrix, time, clash.w > 1.5f ? 2902 : rival.w > 1.5f ? 2900 : 2899, circle, rival, opacity, clash);

    public void Draw(Mesh mesh, Matrix4x4 matrix, float time, int renderQueue, Vector4 circle = default, Vector4 rival = default, float opacity = 1f, Vector4 clash = default)
    {
        Configure(time, circle, rival, opacity);
        properties!.SetVector("_SurfaceClash", clash);
        if (material!.renderQueue != renderQueue) material.renderQueue = renderQueue;
        GenDraw.DrawMeshNowOrLater(mesh, matrix, material, false, properties);
    }

    public void DrawScreen(Rect rect, Vector4 view, Vector4 circle, Vector4 rival, Vector4 reveal, float time, float opacity = 1f)
        => DrawScreen(rect, view, circle, rival, reveal, time, opacity, default);

    public void DrawScreen(Rect rect, Vector4 view, Vector4 circle, Vector4 rival, Vector4 reveal, float time, float opacity, Vector4 clash)
    {
        if (material == null) throw new ObjectDisposedException(nameof(DomainSurfaceVisuals));
        screenMaterial ??= definition.CreateMaterial(true);
        definition.Configure(screenMaterial, time);
        screenMaterial.SetVector("_SurfaceState", new Vector4(time, 1f, Mathf.Clamp01(opacity), 1f));
        screenMaterial.SetVector("_SurfaceCircle", circle);
        screenMaterial.SetVector("_SurfaceRival", rival);
        screenMaterial.SetVector("_SurfaceView", view);
        screenMaterial.SetVector("_SurfaceReveal", reveal);
        screenMaterial.SetVector("_SurfaceClash", clash);
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
        => SetTerritory(partition, first ? 0 : 1);

    public void SetTerritory(DomainTerritoryPartition? partition, int owner)
    {
        territory = partition;
        territorySide = owner;
        territoryFirst = owner == 0;
        if (territoryField?.Partition != partition)
        {
            territoryField?.Dispose();
            territoryField = partition == null ? null : new DomainTerritoryField(partition);
        }
    }

    public void Dispose()
    {
        if (material != null) UnityEngine.Object.Destroy(material);
        if (screenMaterial != null) UnityEngine.Object.Destroy(screenMaterial);
        territoryField?.Dispose();
        territoryField = null;
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
#if RIMWORLD_1_7
        if (material.HasProperty("_TerritoryProtectionState"))
        {
            if (territoryField != null) territoryField.Configure(properties, territorySide);
            else properties.SetVector("_TerritoryProtectionState", default);
        }
#endif
    }

#if RIMWORLD_1_7
    private void ConfigureProtection()
    {
        if (territoryField != null) territoryField.Configure(screenMaterial!, territorySide);
        else screenMaterial!.SetVector("_TerritoryProtectionState", default);
    }
#endif

    private readonly DomainSurfaceDef definition;
    private readonly CQFDomainRuntime? runtime;
    private Material? material;
    private Material? screenMaterial;
    private MaterialPropertyBlock? properties;
    private DomainTerritoryPartition? territory;
    private bool territoryFirst;
    private int territorySide;
    private DomainTerritoryField? territoryField;
}
