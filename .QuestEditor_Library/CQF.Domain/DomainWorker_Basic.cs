#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainWorker_Basic : DomainWorker_Visual
{
    public override bool RendersSurfaceTerrain => true;
    protected override Type TerrainLayerType => typeof(SectionLayer_BasicDomainTerrain);

    public override void Update()
    {
        UpdateMaterial();
        base.Update();
    }

    public override Material? GetTerrainMaterial(TerrainDef terrain)
    {
        if (terrain.defName != "CQF_BasicDomainTerrain") return base.GetTerrainMaterial(terrain);
        UpdateMaterial();
        return material;
    }

    public override void Dispose()
    {
        if (material != null) UnityEngine.Object.Destroy(material);
        material = null;
    }

    private void UpdateMaterial()
    {
        if (materialFrame == Time.frameCount) return;
        DomainSurfaceDef surface = Domain.Definition.surface ?? throw new InvalidOperationException("[CQF] Basic domain requires a surface.");
        material ??= surface.CreateMaterial();
        material.renderQueue = 2400;
        surface.Configure(material, GenTicks.TicksGame / 60f);
        materialFrame = Time.frameCount;
    }

    private Material? material;
    private int materialFrame = -1;
}
#endif
