using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class SectionLayer_DomainContest : SectionLayer
{
    public SectionLayer_DomainContest(Section section) : base(section)
    {
        relevantChangeTypes = MapMeshFlagDefOf.Terrain;
        domain = Map.GetComponent<MapComponent_Domain>();
    }

    public override bool Visible => domain.Contest != null && domain.HasActiveDomain && DebugViewSettings.drawTerrain;

    public override void Regenerate()
    {
        ClearSubMeshes(MeshParts.All);
        if (!Visible) return;
        DomainContest contest = domain.Contest!;
        foreach (IntVec3 cell in section.CellRect)
        {
            if (!contest.CoversBoundaryCell(cell)) continue;
            foreach (MapComponent_Domain side in domain.Instances.Where(side => side.Active)) DrawCell(side, contest, cell, side.InstanceIndex);
        }
        FinalizeMesh(MeshParts.All);
    }

    private void DrawCell(MapComponent_Domain side, DomainContest contest, IntVec3 cell, int owner)
    {
        TerrainDef terrain = contest.TerrainAt(side, cell);
        if (!DebugViewSettings.drawTerrainWater && terrain.HasTag("Water")) return;
        Material? material = side.Worker?.GetTerrainMaterial(terrain);
        if (material == null) return;
        LayerSubMesh mesh = GetSubMesh(material);
        float y = AltitudeLayer.Terrain.AltitudeFor() + 0.002f;
        if (geometry?.Partition != contest.Partition) geometry = new DomainTerritoryGeometry(contest.Partition);
        geometry.Append(new Rect(cell.x, cell.z, 1f, 1f), owner, y, mesh.verts, mesh.tris, mesh.colors);
    }

    private readonly MapComponent_Domain domain;
    private DomainTerritoryGeometry? geometry;
}
