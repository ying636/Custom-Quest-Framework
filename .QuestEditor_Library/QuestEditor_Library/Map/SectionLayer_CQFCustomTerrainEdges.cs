using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class SectionLayer_CQFCustomTerrainEdges : SectionLayer
{
    public SectionLayer_CQFCustomTerrainEdges(Section section) : base(section)
    {
        this.relevantChangeTypes = MapMeshFlagDefOf.Terrain;
    }

    public override bool Visible => DebugViewSettings.drawTerrain && (this.Background?.enableTerrainEdges ?? false);

    public override void Regenerate()
    {
        base.ClearSubMeshes(MeshParts.All);
        TerrainGrid terrainGrid = base.Map.terrainGrid;
        CellRect cellRect = this.section.CellRect;
        float edgeAltitude = AltitudeLayer.TerrainScatter.AltitudeFor();
        float loopAltitude = AltitudeLayer.TerrainEdges.AltitudeFor();
        foreach (IntVec3 cell in cellRect)
        {
            if (this.ShouldDrawTerrainEdges(cell, terrainGrid, out EdgeDirections edgeDirections, out CornerDirections corners))
            {
                TerrainDef terrain = terrainGrid.BaseTerrainAt(cell);
                this.DrawEdges(terrain, cell, edgeDirections, edgeAltitude);
                this.DrawCorners(terrain, cell, edgeDirections, corners, edgeAltitude);
                if (this.ShouldDrawLoops(terrain) && this.ShouldDrawPassthrough(cell, terrainGrid, out edgeDirections, out corners))
                {
                    this.DrawLoop(cell + IntVec3.South, terrainGrid, edgeDirections, corners, loopAltitude);
                }
            }
            else if (this.ShouldDrawLoop(cell, terrainGrid, out edgeDirections, out corners))
            {
                TerrainDef northTerrain = terrainGrid.BaseTerrainAt(cell + IntVec3.North);
                if (this.ShouldDrawLoops(northTerrain))
                {
                    this.DrawLoop(cell, terrainGrid, edgeDirections, corners, loopAltitude);
                }
            }
        }
        base.FinalizeMesh(MeshParts.All);
    }

    private CustomMapBackgroundData Background => MapComponent_CustomMapData.GetComp(base.Map)?.background;

    private void DrawEdges(TerrainDef terrain, IntVec3 cell, EdgeDirections edgeDirections, float altitude)
    {
        if (edgeDirections.HasFlag(EdgeDirections.North))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.LoopSingle, cell, altitude, Rot4.North);
        }
        if (edgeDirections.HasFlag(EdgeDirections.East))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.LoopRight, cell, altitude, Rot4.North);
        }
        if (edgeDirections.HasFlag(EdgeDirections.South))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.Flat, cell, altitude, Rot4.North);
        }
        if (edgeDirections.HasFlag(EdgeDirections.West))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.LoopLeft, cell, altitude, Rot4.North);
        }
    }

    private void DrawCorners(TerrainDef terrain, IntVec3 cell, EdgeDirections edges, CornerDirections corners, float altitude)
    {
        if (corners.HasFlag(CornerDirections.NorthWest) && !edges.HasFlag(EdgeDirections.North) && !edges.HasFlag(EdgeDirections.West))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.CornerInner, cell, altitude, Rot4.East);
        }
        if (corners.HasFlag(CornerDirections.NorthEast) && !edges.HasFlag(EdgeDirections.North) && !edges.HasFlag(EdgeDirections.East))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.CornerInner, cell, altitude, Rot4.South);
        }
        if (corners.HasFlag(CornerDirections.SouthEast) && !edges.HasFlag(EdgeDirections.South) && !edges.HasFlag(EdgeDirections.East))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.CornerInner, cell, altitude, Rot4.West);
        }
        if (corners.HasFlag(CornerDirections.SouthWest) && !edges.HasFlag(EdgeDirections.South) && !edges.HasFlag(EdgeDirections.West))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.CornerInner, cell, altitude, Rot4.North);
        }
    }

    private void DrawLoop(IntVec3 cell, TerrainGrid grid, EdgeDirections edges, CornerDirections corners, float altitude)
    {
        if (!edges.HasFlag(EdgeDirections.North))
        {
            return;
        }
        TerrainDef terrain = grid.BaseTerrainAt(cell + IntVec3.North);
        float zFactor = (float)cell.z / base.Map.Size.z;
        altitude += 0.03658537f - zFactor * 0.03658537f;
        if (!corners.HasFlag(CornerDirections.NorthWest) && !corners.HasFlag(CornerDirections.NorthEast))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.LoopSingle, cell, altitude, Rot4.North);
        }
        if (corners.HasFlag(CornerDirections.NorthWest) && corners.HasFlag(CornerDirections.NorthEast))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.Loop, cell, altitude, Rot4.North, cell.GetHashCode());
        }
        if (!corners.HasFlag(CornerDirections.NorthWest) && corners.HasFlag(CornerDirections.NorthEast))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.LoopLeft, cell, altitude, Rot4.North);
        }
        if (!corners.HasFlag(CornerDirections.NorthEast) && corners.HasFlag(CornerDirections.NorthWest))
        {
            this.AddQuad(terrain, SectionLayer_TerrainEdges.EdgeType.LoopRight, cell, altitude, Rot4.North);
        }
    }

    private void AddQuad(TerrainDef terrain, SectionLayer_TerrainEdges.EdgeType edgeType, IntVec3 cell, float altitude, Rot4 rotation, int listIndexOffset = 0)
    {
        if (!this.CanDrawEdge(terrain, edgeType, listIndexOffset))
        {
            return;
        }
        Material material = terrain.spaceEdgeGraphicData.GetMaterial(terrain, edgeType, listIndexOffset);
        LayerSubMesh subMesh = base.GetSubMesh(material);
        int count = subMesh.verts.Count;
        float width = Mathf.Max((float)material.mainTexture.width / material.mainTexture.height, 1f);
        float height = Mathf.Max((float)material.mainTexture.height / material.mainTexture.width, 1f);
        int rotOffset = Mathf.Abs(4 - rotation.AsInt);
        for (int i = 0; i < 4; i++)
        {
            subMesh.verts.Add(new Vector3(cell.x + UVs[i].x * width, altitude, cell.z + UVs[i].y * height));
            subMesh.uvs.Add(UVs[(rotOffset + i) % 4]);
        }
        subMesh.tris.Add(count);
        subMesh.tris.Add(count + 1);
        subMesh.tris.Add(count + 2);
        subMesh.tris.Add(count);
        subMesh.tris.Add(count + 2);
        subMesh.tris.Add(count + 3);
    }

    private bool ShouldDrawTerrainEdges(IntVec3 cell, TerrainGrid grid, out EdgeDirections edges, out CornerDirections corners)
    {
        edges = EdgeDirections.None;
        corners = CornerDirections.None;
        TerrainDef terrainDef = grid.BaseTerrainAt(cell);
        if (!this.ShouldDrawCQFTerrainEdges(terrainDef))
        {
            return false;
        }
        for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
        {
            IntVec3 other = cell + GenAdj.CardinalDirections[i];
            if (!other.InBounds(base.Map))
            {
                if (!base.Map.DrawMapClippers)
                {
                    edges |= (EdgeDirections)(1 << i);
                }
            }
            else
            {
                TerrainDef otherTerrain = grid.TerrainAt(other);
                if (this.IsBackgroundTerrain(otherTerrain))
                {
                    edges |= (EdgeDirections)(1 << i);
                }
            }
        }
        for (int i = 0; i < GenAdj.DiagonalDirections.Length; i++)
        {
            IntVec3 other = cell + GenAdj.DiagonalDirections[i];
            if (!other.InBounds(base.Map))
            {
                if (!base.Map.DrawMapClippers)
                {
                    corners |= (CornerDirections)(1 << i);
                }
            }
            else
            {
                TerrainDef otherTerrain = grid.TerrainAt(other);
                if (this.IsBackgroundTerrain(otherTerrain))
                {
                    corners |= (CornerDirections)(1 << i);
                }
            }
        }
        return edges != EdgeDirections.None || corners != CornerDirections.None;
    }

    private bool ShouldDrawPassthrough(IntVec3 cell, TerrainGrid grid, out EdgeDirections edges, out CornerDirections corners)
    {
        edges = EdgeDirections.None;
        corners = CornerDirections.None;
        IntVec3 north = cell + IntVec3.North;
        if (!north.InBounds(base.Map))
        {
            return false;
        }
        TerrainDef northTerrain = grid.BaseTerrainAt(north);
        if (!this.ShouldDrawCQFTerrainEdges(northTerrain))
        {
            return false;
        }
        IntVec3 west = cell + IntVec3.West;
        IntVec3 east = cell + IntVec3.East;
        if (!west.InBounds(base.Map) || !east.InBounds(base.Map))
        {
            return false;
        }
        TerrainDef westTerrain = grid.TerrainAt(west);
        TerrainDef eastTerrain = grid.TerrainAt(east);
        if (this.IsBackgroundTerrain(eastTerrain) || this.IsBackgroundTerrain(westTerrain))
        {
            return false;
        }
        corners = CornerDirections.NorthWest | CornerDirections.NorthEast;
        edges = EdgeDirections.North;
        return true;
    }

    private bool ShouldDrawLoop(IntVec3 cell, TerrainGrid grid, out EdgeDirections edges, out CornerDirections corners)
    {
        edges = EdgeDirections.None;
        corners = CornerDirections.None;
        IntVec3 north = cell + IntVec3.North;
        TerrainDef terrainDef = grid.TerrainAt(cell);
        if (!this.IsBackgroundTerrain(terrainDef) || !north.InBounds(base.Map))
        {
            return false;
        }
        TerrainDef northTerrain = grid.BaseTerrainAt(north);
        if (!this.ShouldDrawCQFTerrainEdges(northTerrain))
        {
            return false;
        }
        for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
        {
            IntVec3 other = cell + GenAdj.CardinalDirections[i];
            if (!other.InBounds(base.Map))
            {
                if (!base.Map.DrawMapClippers)
                {
                    edges |= (EdgeDirections)(1 << i);
                }
            }
            else
            {
                TerrainDef otherTerrain = grid.TerrainAt(other);
                if (!this.IsBackgroundTerrain(otherTerrain))
                {
                    edges |= (EdgeDirections)(1 << i);
                }
            }
        }
        for (int i = 0; i < GenAdj.DiagonalDirections.Length; i++)
        {
            IntVec3 other = cell + GenAdj.DiagonalDirections[i];
            if (!other.InBounds(base.Map))
            {
                if (!base.Map.DrawMapClippers)
                {
                    corners |= (CornerDirections)(1 << i);
                }
            }
            else
            {
                TerrainDef otherTerrain = grid.TerrainAt(other);
                if (!this.IsBackgroundTerrain(otherTerrain))
                {
                    corners |= (CornerDirections)(1 << i);
                }
            }
        }
        return edges.HasFlag(EdgeDirections.North);
    }

    private bool CanDrawEdge(TerrainDef terrain, SectionLayer_TerrainEdges.EdgeType edgeType, int listIndexOffset = 0)
    {
        if (!this.ShouldDrawCQFTerrainEdges(terrain))
        {
            return false;
        }
        return this.TextureExists(this.GetTexturePath(terrain.spaceEdgeGraphicData, edgeType, listIndexOffset));
    }

    private bool ShouldDrawCQFTerrainEdges(TerrainDef terrain)
    {
        return terrain?.spaceEdgeGraphicData != null && terrain.GetModExtension<ModExtension_CQFTerrainEdges>() != null;
    }

    private string GetTexturePath(TerrainDef.SpaceEdgeGraphicData graphicData, SectionLayer_TerrainEdges.EdgeType edgeType, int listIndexOffset)
    {
        return edgeType switch
        {
            SectionLayer_TerrainEdges.EdgeType.OShape => graphicData.OShapeTexPath,
            SectionLayer_TerrainEdges.EdgeType.UShape => graphicData.UShapeTexPath,
            SectionLayer_TerrainEdges.EdgeType.CornerInner => graphicData.CornerInnerTexPath,
            SectionLayer_TerrainEdges.EdgeType.CornerOuter => graphicData.CornerOuterTexPath,
            SectionLayer_TerrainEdges.EdgeType.Flat => graphicData.FlatTexPath,
            SectionLayer_TerrainEdges.EdgeType.LoopLeft => graphicData.LoopLeftTexPath,
            SectionLayer_TerrainEdges.EdgeType.LoopRight => graphicData.LoopRightTexPath,
            SectionLayer_TerrainEdges.EdgeType.LoopSingle => graphicData.LoopSingleTexPath,
            SectionLayer_TerrainEdges.EdgeType.Loop => this.GetLoopTexturePath(graphicData, listIndexOffset),
            _ => null
        };
    }

    private string GetLoopTexturePath(TerrainDef.SpaceEdgeGraphicData graphicData, int listIndexOffset)
    {
        if (graphicData.LoopTexPaths.NullOrEmpty())
        {
            return null;
        }
        return graphicData.LoopTexPaths[Mathf.Abs(listIndexOffset) % graphicData.LoopTexPaths.Count];
    }

    private bool TextureExists(string texPath)
    {
        if (texPath.NullOrEmpty())
        {
            return false;
        }
        if (!TextureExistsCache.TryGetValue(texPath, out bool exists))
        {
            exists = ContentFinder<Texture2D>.Get(texPath, false) != null;
            TextureExistsCache.Add(texPath, exists);
        }
        return exists;
    }

    private bool IsBackgroundTerrain(TerrainDef terrain)
    {
        return terrain == null || terrain.dontRender || terrain.defName == "QE_Null" || terrain.defName == "QE_EtherealVoid";
    }

    private bool ShouldDrawLoops(TerrainDef terrain)
    {
        return (terrain?.GetModExtension<ModExtension_CQFTerrainEdges>()?.drawLoops ?? false) &&
            this.CanDrawEdge(terrain, SectionLayer_TerrainEdges.EdgeType.LoopSingle);
    }

    private static readonly Dictionary<string, bool> TextureExistsCache = new Dictionary<string, bool>();

    private static readonly Vector2[] UVs =
    {
        new(0f, 0f),
        new(0f, 1f),
        new(1f, 1f),
        new(1f, 0f)
    };

    [Flags]
    private enum EdgeDirections
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }

    [Flags]
    private enum CornerDirections
    {
        None = 0,
        SouthWest = 1,
        NorthWest = 2,
        NorthEast = 4,
        SouthEast = 8
    }
}
