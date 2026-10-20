#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class SectionLayer_BasicDomainTerrain : SectionLayer
{
    public SectionLayer_BasicDomainTerrain(Section section) : base(section)
    {
        relevantChangeTypes = MapMeshFlagDefOf.Terrain;
        domain = Map.GetComponent<MapComponent_Domain>();
    }

    public override bool Visible => DebugViewSettings.drawTerrain
        && (domain.Worker is DomainWorker_Basic || domain.Challenger?.Worker is DomainWorker_Basic);

    public override void Regenerate()
    {
        ClearSubMeshes(MeshParts.All);
        if (!Visible) return;
        foreach (IntVec3 cell in section.CellRect)
        {
            if (domain.Contest?.CoversBoundaryCell(cell) == true) continue;
            MapComponent_Domain side = domain.DomainAt(cell) ?? domain;
            TerrainDef terrain = Map.terrainGrid.TerrainAt(cell);
            if (side.Worker is not DomainWorker_Basic worker || terrain.defName != "CQF_BasicDomainTerrain") continue;
            Material? material = worker.GetTerrainMaterial(terrain);
            if (material == null) throw new InvalidOperationException("[CQF] Basic domain terrain has no material.");
            LayerSubMesh mesh = GetSubMesh(material);
            int index = mesh.verts.Count;
            float y = AltitudeLayer.Terrain.AltitudeFor();
            mesh.verts.Add(new Vector3(cell.x, y, cell.z));
            mesh.verts.Add(new Vector3(cell.x, y, cell.z + 1));
            mesh.verts.Add(new Vector3(cell.x + 1, y, cell.z + 1));
            mesh.verts.Add(new Vector3(cell.x + 1, y, cell.z));
            for (int vertex = 0; vertex < 4; vertex++) mesh.colors.Add(new Color32(255, 255, 255, 255));
            mesh.tris.Add(index); mesh.tris.Add(index + 1); mesh.tris.Add(index + 2);
            mesh.tris.Add(index); mesh.tris.Add(index + 2); mesh.tris.Add(index + 3);
        }
        FinalizeMesh(MeshParts.All);
    }

    private readonly MapComponent_Domain domain;
}
#endif
