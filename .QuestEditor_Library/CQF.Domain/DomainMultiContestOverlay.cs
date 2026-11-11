using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainMultiContestOverlay : IDisposable
{
    public DomainMultiContestOverlay(MapComponent_Domain battlefield)
    {
        surfaces = new DomainSurfaceVisuals?[battlefield.InstanceCount];
        meshes = new Mesh?[surfaces.Length];
        fields = new DomainTerritoryField?[surfaces.Length];
        try
        {
            foreach (MapComponent_Domain side in battlefield.Instances)
            {
                int index = side.InstanceIndex;
                if (side.Definition.surface != null)
                {
                    surfaces[index] = new DomainSurfaceVisuals(side.Definition.surface);
                    if (side.Worker?.RendersSurfaceTerrain != true) meshes[index] = new Mesh { name = "CQF_DomainTerritory_" + index };
                }
            }
#if RIMWORLD_1_7
            material = new Material(DefDatabase<ShaderDef>.GetNamed("CQF_DomainContestFront").Shader) { renderQueue = 2951 };
#else
            material = new Material(ShaderDatabase.Transparent) { mainTexture = Texture2D.whiteTexture, renderQueue = 2951 };
#endif
            contact = new Mesh { name = "CQF_MultipleDomainContact" };
        }
        catch { Dispose(); throw; }
    }

    public void Draw(MapComponent_Domain battlefield, DomainTerritoryPartition partition)
    {
        List<MapComponent_Domain> sides = battlefield.Instances.ToList();
        if (geometry?.Partition != partition)
        {
            geometry = new DomainTerritoryGeometry(partition);
            for (int index = 0; index < fields.Length; index++)
            {
                fields[index]?.Dispose();
                fields[index] = new DomainTerritoryField(partition);
                surfaces[index]?.SetTerritory(partition, index);
            }
            revision = -1;
        }
        if (revision != partition.Revision)
        {
            geometry.RebuildUnclaimed(contact!, new Rect(0, 0, partition.Width, partition.Height), AltitudeLayer.FloorCoverings.AltitudeFor() + 0.001f);
            for (int index = 0; index < meshes.Length; index++)
                if (meshes[index] != null) geometry.Rebuild(meshes[index]!, index, partition.Width, partition.Height, AltitudeLayer.FloorCoverings.AltitudeFor());
            revision = partition.Revision;
        }
        float time = GenTicks.TicksGame / 60f;
        for (int index = 0; index < sides.Count; index++)
        {
            MapComponent_Domain side = sides[index];
            if (!side.Active) continue;
            if (meshes[index]?.vertexCount > 0) surfaces[index]!.Draw(meshes[index]!, Matrix4x4.identity, time);
            if (battlefield.ActiveDomainCount < 2 || contact!.vertexCount == 0) continue;
            float target = (float)(side.Health / Math.Max(1d, sides.Where(candidate => candidate.Active).Sum(candidate => (double)candidate.Health)));
            float pressure = Mathf.Clamp((target - partition.FractionFor(index)) * partition.Width * 0.35f, -1f, 1f);
            DomainContest contest = battlefield.Contest!;
            surfaces[index]?.Draw(contact, Matrix4x4.identity, time, new Vector4(0f, 1f, 1.6f, 0f),
                opacity: 1f, clash: new Vector4(pressure, contest.Recoil, 0f, 5f));
            Color rival = Color.clear;
            float total = 0f;
            foreach (MapComponent_Domain other in sides.Where(candidate => candidate.Active && candidate != side))
            { rival += other.Definition.contestColor; total++; }
            rival = total > 0f ? rival / total : Color.white;
            properties.SetColor("_Color", side.Definition.contestColor);
            properties.SetColor("_SecondaryColor", rival);
            properties.SetVector("_FrontState", new Vector4(0f, time, pressure, contest.Recoil));
            properties.SetVector("_FrontShape", new Vector4(1.6f, -1f, target, contest.RecoilAge));
            fields[index]!.Configure(properties, index);
            GenDraw.DrawMeshNowOrLater(contact, Matrix4x4.identity, material!, false, properties);
        }
    }

    public void Dispose()
    {
        foreach (DomainSurfaceVisuals? surface in surfaces) surface?.Dispose();
        foreach (Mesh? mesh in meshes) if (mesh != null) UnityEngine.Object.Destroy(mesh);
        foreach (DomainTerritoryField? field in fields) field?.Dispose();
        if (contact != null) UnityEngine.Object.Destroy(contact);
        if (material != null) UnityEngine.Object.Destroy(material);
    }

    private readonly DomainSurfaceVisuals?[] surfaces;
    private readonly Mesh?[] meshes;
    private readonly DomainTerritoryField?[] fields;
    private readonly Mesh? contact;
    private readonly Material? material;
    private readonly MaterialPropertyBlock properties = new();
    private DomainTerritoryGeometry? geometry;
    private int revision = -1;
}
