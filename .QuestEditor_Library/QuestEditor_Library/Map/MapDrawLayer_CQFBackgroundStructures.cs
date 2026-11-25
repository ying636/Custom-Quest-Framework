#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class MapDrawLayer_CQFBackgroundStructures : MapDrawLayer
{
    public MapDrawLayer_CQFBackgroundStructures(Map map) : base(map)
    {
        this.relevantChangeTypes = MapMeshFlagDefOf.Terrain;
    }

    internal MapDrawLayer_CQFBackgroundStructures(Map map, bool ghost) : this(map)
    {
        this.ghost = ghost;
    }

    public override bool Visible => DebugViewSettings.drawTerrain && this.Background?.HasStructures == true;

    private CustomMapBackgroundData? Background => MapComponent_CustomMapData.GetComp(this.Map)?.BackgroundForDrawing;

    public override void Regenerate()
    {
        this.ResetMeshContent();
        if (this.Background is not { } scene) return;
        FaceWriter face = this.AddFace;
        foreach (CQFBackgroundStructure item in scene.structures) DrawStructure(item, item.Center, face);
        if (scene.cliffs.Count > 0 && DefDatabase<TerrainDef>.GetNamedSilentFail("QE_EtherealVoid") is { } empty)
        {
            var lamps = GetWallLights(scene, this.Map, empty);
            foreach (CQFBackgroundCliff cliff in scene.cliffs) this.DrawCliff(cliff, empty, lamps);
        }
        this.FinalizeMesh(MeshParts.All);
    }

    public override void RefreshSubMeshBounds()
    {
        foreach (LayerSubMesh layer in this.subMeshes) layer.mesh.RecalculateBounds();
    }

    internal void RebuildGhost(CQFBackgroundStructure? structure, CQFBackgroundCliff? cliff)
    {
        this.ResetMeshContent();
        FaceWriter face = this.AddFace;
        if (structure != null) DrawStructure(structure, Vector3.zero, face);
        if (cliff != null && DefDatabase<TerrainDef>.GetNamedSilentFail("QE_EtherealVoid") is { } empty)
            this.DrawCliff(cliff, empty, null);
        this.FinalizeMesh(MeshParts.All);
        this.RefreshSubMeshBounds();
    }

    internal void DrawGhostAt(Vector3 position)
    {
        position.y = AltitudeLayer.Blueprint.AltitudeFor() - AltitudeLayer.BelowTerrain.AltitudeFor();
        Matrix4x4 transform = Matrix4x4.Translate(position);
        foreach (LayerSubMesh layer in this.subMeshes)
            if (layer.finalized && !layer.disabled) Graphics.DrawMesh(layer.mesh, transform, layer.material, layer.renderLayer);
    }

    internal (CQFBackgroundStructure? Structure, CQFBackgroundCliff? Cliff) PickElement(
        CustomMapBackgroundData scene, Vector3 point)
    {
        IntVec3 cell = new IntVec3(Mathf.FloorToInt(point.x), 0, Mathf.FloorToInt(point.z));
        TerrainDef? empty = DefDatabase<TerrainDef>.GetNamedSilentFail("QE_EtherealVoid");
        if (cell.InBounds(this.Map))
        {
            TerrainDef floor = cell.GetTerrain(this.Map);
            if (floor != empty && !floor.dontRender && floor.texturePath != "UI/Null") return default;
        }

        CQFBackgroundStructure? found = null;
        CQFBackgroundStructure? current = null;
        int highestQueue = 0;
        void MatchStructure(Texture2D texture, Color tint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int queue)
        {
            if (queue <= highestQueue || !Contains(point, a, b, c, d)) return;
            highestQueue = queue;
            found = current;
        }

        FaceWriter writer = MatchStructure;
        for (int index = scene.structures.Count - 1; index >= 0; index--)
        {
            current = scene.structures[index];
            DrawStructure(current, current.Center, writer);
        }
        if (found != null) return (found, null);
        if (empty == null || !cell.InBounds(this.Map) || cell.GetTerrain(this.Map) != empty) return default;

        var layer = this.ghost ? this.Map.mapDrawer.GetGlobalLayer<MapDrawLayer_CQFBackgroundStructures>() : this;
        for (int index = scene.cliffs.Count - 1; index >= 0; index--)
        {
            CQFBackgroundCliff cliff = scene.cliffs[index];
            if (!cliff.area.Contains(cell) || !CanDraw(cliff)) continue;
            if (layer?.Dirty == false && layer.cliffSurfaces.TryGetValue(cliff, out CQFCliffSurface? cached))
            {
                if (cached.VisibleAt(point)) return (null, cliff);
                continue;
            }

            int reach = Mathf.CeilToInt(Mathf.Min(32f, cliff.depth) * 1.3f) + 2;
            CellRect area = cliff.area.ClipInsideMap(this.Map);
            CellRect sample = CellRect.FromLimits(Math.Max(area.minX, cell.x - reach), Math.Max(area.minZ, cell.z - reach),
                Math.Min(area.maxX, cell.x + reach), Math.Min(area.maxZ, cell.z + reach));
            var local = new CQFCliffSurface(this.Map, sample, empty, cliff);
            if (local.HasEdges && local.VisibleAt(point)) return (null, cliff);
        }
        return default;
    }

    private void ResetMeshContent()
    {
        this.ClearSubMeshes(MeshParts.All);
        this.cliffSurfaces.Clear();
        this.cliffBatchIndex = 0;
        this.rimBatchIndex = 0;
    }

    private void DrawCliff(CQFBackgroundCliff cliff, TerrainDef empty, List<(Vector2 Center, float RadiusSquared)>? lamps)
    {
        if (!CanDraw(cliff)) return;
        CellRect area = cliff.area.ClipInsideMap(this.Map);
        if (area.IsEmpty) return;
        var surface = new CQFCliffSurface(this.Map, area, empty, cliff);
        if (!surface.HasEdges) return;
        if (!this.ghost)
        {
            this.cliffSurfaces[cliff] = surface;
            this.DrawCliffRim(surface, empty, cliff.color, lamps);
        }

        int divisions = area.Width * area.Height >= 24000 ? 1 : 2;
        int stride = divisions + 1;
        int verticesPerCell = stride * stride;
        float spacing = 1f / divisions;
        float altitude = AltitudeLayer.BelowTerrain.AltitudeFor();
        Span<Color> colors = stackalloc Color[9];
        Span<float> distances = stackalloc float[9];
        for (int z = 0; z < area.Height; z++)
        {
            for (int x = 0; x < area.Width; x++)
            {
                if (!surface.Covers(x, z)) continue;
                bool visible = false;
                for (int sample = 0; sample < verticesPerCell; sample++)
                {
                    colors[sample] = surface.Evaluate(x, z, (sample % stride) * spacing, (sample / stride) * spacing, out distances[sample]);
                    visible |= colors[sample].a > .005f;
                }

                if (!visible) continue;
                LayerSubMesh mesh = this.GetCliffMesh(verticesPerCell, false);
                int start = mesh.verts.Count;
                for (int sample = 0; sample < verticesPerCell; sample++)
                {
                    float wx = area.minX + x + (sample % stride) * spacing;
                    float wz = area.minZ + z + (sample / stride) * spacing;
                    mesh.verts.Add(new Vector3(wx, altitude, wz));
                    float light = this.ghost ? -1f : SampleWallLight(lamps, wx, wz, distances[sample]);
                    mesh.uvs.Add(new Vector2(distances[sample], light));
                    mesh.colors.Add(colors[sample]);
                }

                AddCliffTriangles(mesh, start, divisions);
            }
        }
    }

    private void DrawCliffRim(CQFCliffSurface surface, TerrainDef empty, Color tint,
        List<(Vector2 Center, float RadiusSquared)>? lamps)
    {
        CellRect area = surface.Area.ExpandedBy(1).ClipInsideMap(this.Map);
        float altitude = AltitudeLayer.TerrainScatter.AltitudeFor();
        foreach (IntVec3 cell in area.Cells)
        {
            if (!surface.TryGetRimCorners(cell.x - surface.Area.minX, cell.z - surface.Area.minZ, out var corners)) continue;
            TerrainDef terrain = cell.GetTerrain(this.Map);
            if (terrain == empty || terrain.dontRender) continue;

            LayerSubMesh mesh = this.GetCliffMesh(4, true);
            int start = mesh.verts.Count;
            for (int index = 0; index < 4; index++)
            {
                int x = cell.x + index % 2;
                int z = cell.z + index / 2;
                mesh.verts.Add(new Vector3(x, altitude, z));
                mesh.uvs.Add(new Vector2(-.001f - corners[index], SampleWallLight(lamps, x, z, 0f)));
                mesh.colors.Add(tint);
            }
            AddCliffTriangles(mesh, start, 1);
        }
    }

    private static void AddCliffTriangles(LayerSubMesh mesh, int start, int divisions)
    {
        int stride = divisions + 1;
        for (int row = 0; row < divisions; row++)
        {
            for (int column = 0; column < divisions; column++)
            {
                int corner = start + row * stride + column;
                mesh.tris.Add(corner);
                mesh.tris.Add(corner + stride);
                mesh.tris.Add(corner + 1);
                mesh.tris.Add(corner + 1);
                mesh.tris.Add(corner + stride);
                mesh.tris.Add(corner + stride + 1);
            }
        }
    }

    private LayerSubMesh GetCliffMesh(int vertices, bool rim)
    {
        List<LayerSubMesh> meshes = rim ? this.rimMeshes : this.cliffMeshes;
        ref int batchIndex = ref (rim ? ref this.rimBatchIndex : ref this.cliffBatchIndex);
        if (batchIndex < meshes.Count && meshes[batchIndex].verts.Count + vertices > 60000)
            batchIndex++;
        if (batchIndex == meshes.Count)
        {
            Color tint = this.ghost ? new Color(.60f, .89f, 1f, .67f) : Color.white;
            int queue = rim ? 2401 : this.ghost ? 3000 : 1916;
            Shader shader = DefDatabase<ShaderDef>.GetNamed("CQF_CliffSurface").Shader;
            Texture2D rock = ContentFinder<Texture2D>.Get("Terrain/Surfaces/RoughStone");
            Material material = MaterialPool.MatFrom(rock, shader, tint, queue);
            LayerSubMesh mesh = MapDrawLayer.CreateFreeSubMesh(material, this.Map);
            meshes.Add(mesh);
            this.subMeshes.Add(mesh);
        }
        return meshes[batchIndex];
    }

    private static bool CanDraw(CQFBackgroundCliff cliff) => !cliff.area.IsEmpty && cliff.edges != CQFCliffEdges.None
        && float.IsFinite(cliff.depth) && cliff.depth > 0f;

    private static List<(Vector2 Center, float RadiusSquared)>? GetWallLights(CustomMapBackgroundData scene, Map map, TerrainDef empty)
    {
        List<(Vector2 Center, float RadiusSquared)>? lamps = null;
        foreach (CQFBackgroundStructure item in scene.structures)
        {
            if (item.def == null || !float.IsFinite(item.def.wallLightRadius) || item.def.wallLightRadius <= 0f ||
                !float.IsFinite(item.scale) || item.scale <= 0f || !float.IsFinite(item.depth) || !float.IsFinite(item.rotation)) continue;
            Vector3 anchor = WallLampAnchor(item, item.Center);
            IntVec3 cell = new IntVec3(Mathf.FloorToInt(anchor.x), 0, Mathf.FloorToInt(anchor.z));
            if (!cell.InBounds(map) || cell.GetTerrain(map) != empty) continue;
            float radius = Mathf.Clamp(item.def.wallLightRadius * Mathf.Min(item.scale, 24f), .05f, 12f);
            lamps ??= new List<(Vector2 Center, float RadiusSquared)>();
            lamps.Add((new Vector2(anchor.x, anchor.z), radius * radius));
        }
        return lamps;
    }

    private static float SampleWallLight(List<(Vector2 Center, float RadiusSquared)>? lamps, float x, float z, float descent)
    {
        if (lamps == null || descent >= 5.2f) return 0f;
        float light = 0f;
        var point = new Vector2(x, z);
        foreach (var lamp in lamps)
        {
            float falloff = Mathf.Max(0f, 1f - (point - lamp.Center).sqrMagnitude / lamp.RadiusSquared);
            light += falloff * falloff;
        }
        return Mathf.Min(1f, light);
    }

    private static Vector3 WallLampAnchor(CQFBackgroundStructure item, Vector3 center)
    {
        float drop = Mathf.Clamp(item.depth, -128f, 128f);
        Vector3 descent = Quaternion.AngleAxis(item.rotation, Vector3.up) * Vector3.forward;
        Vector3 anchor = center + descent * drop;
        anchor.y = AltitudeLayer.BelowTerrain.AltitudeFor() + .0015f;
        return anchor;
    }

    private static void DrawStructure(CQFBackgroundStructure item, Vector3 center, FaceWriter face)
    {
        if (item.def == null || !float.IsFinite(item.scale) || !float.IsFinite(item.depth)
            || !float.IsFinite(item.rotation) || item.scale <= 0f) return;
        Texture2D texture = ContentFinder<Texture2D>.Get(item.def.texturePath);
        if (texture == null) return;
        float scale = Mathf.Min(item.scale, 24f);
        float x = item.def.size.x * scale * .5f;
        float z = item.def.size.y * scale * .5f;
        if (item.def.wallLightRadius > 0f)
        {
            Vector3 anchor = WallLampAnchor(item, center);
            Vector3 right = new Vector3(x, 0f, 0f);
            Vector3 front = new Vector3(0f, 0f, z);
            face(texture, item.def.color, anchor - right - front, anchor - right + front,
                anchor + right + front, anchor + right - front, 1922);
            return;
        }
        float end = item.def.height * scale;
        var turn = Quaternion.AngleAxis(item.rotation, Vector3.up);
        float drop = Mathf.Clamp(item.depth, -128f, 128f);
        Vector3 Project(float dx, float dz, float height)
        {
            Vector3 point = center + turn * new Vector3(dx, 0f, dz);
            point.x += (height - drop) * .36f;
            point.z += (height - drop) * .68f;
            point.y = AltitudeLayer.BelowTerrain.AltitudeFor() + Mathf.Clamp((height - drop) * .0001f, -.015f, .015f);
            return point;
        }

        Vector3 a = Project(-x, -z, 0), b = Project(-x, z, 0), c = Project(x, z, 0), d = Project(x, -z, 0);
        Vector3 ta = Project(-x, -z, end), tb = Project(-x, z, end), tc = Project(x, z, end), td = Project(x, -z, end);
        Color tint = item.def.color;
        face(texture, Shade(tint, .44f), b, c, tc, tb, 1918);
        face(texture, Shade(tint, .59f), d, c, tc, td, 1919);
        face(texture, Shade(tint, .71f), a, b, tb, ta, 1919);
        face(texture, Shade(tint, .80f), a, d, td, ta, 1920);
        face(texture, tint, ta, tb, tc, td, 1921);
    }

    private void AddFace(Texture2D texture, Color tint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int queue)
    {
        if (this.ghost)
        {
            tint = Color.Lerp(tint, new Color(.38f, .77f, .95f), .46f);
            tint.a = .56f;
            queue = 3000 + (queue - 1915);
        }
        LayerSubMesh mesh = this.GetSubMesh(MaterialPool.MatFrom(texture, ShaderDatabase.Transparent, tint, queue));
        int start = mesh.verts.Count;
        mesh.verts.Add(a);
        mesh.verts.Add(b);
        mesh.verts.Add(c);
        mesh.verts.Add(d);
        mesh.uvs.Add(Vector2.zero);
        mesh.uvs.Add(Vector2.right);
        mesh.uvs.Add(Vector2.one);
        mesh.uvs.Add(Vector2.up);
        bool up = Vector3.Cross(b - a, c - a).y >= 0f;
        mesh.tris.Add(start);
        mesh.tris.Add(start + (up ? 1 : 2));
        mesh.tris.Add(start + (up ? 2 : 1));
        mesh.tris.Add(start);
        mesh.tris.Add(start + (up ? 2 : 3));
        mesh.tris.Add(start + (up ? 3 : 2));
    }

    private static bool Contains(Vector3 point, Vector3 a, Vector3 b, Vector3 c, Vector3 d) =>
        InTriangle(point, a, b, c) || InTriangle(point, a, c, d);

    private static bool InTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
    {
        static float Cross(Vector3 from, Vector3 to, Vector3 position) =>
            (to.x - from.x) * (position.z - from.z) - (to.z - from.z) * (position.x - from.x);
        if (Mathf.Abs(Cross(a, b, c)) < .00001f) return false;
        float first = Cross(a, b, point);
        float second = Cross(b, c, point);
        float third = Cross(c, a, point);
        return (first >= 0f && second >= 0f && third >= 0f) || (first <= 0f && second <= 0f && third <= 0f);
    }

    private static Color Shade(Color color, float brightness) =>
        new(color.r * brightness, color.g * brightness, color.b * brightness, color.a);

    private delegate void FaceWriter(Texture2D texture, Color tint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int queue);

    private readonly bool ghost;
    private readonly List<LayerSubMesh> cliffMeshes = new();
    private readonly List<LayerSubMesh> rimMeshes = new();
    private readonly Dictionary<CQFBackgroundCliff, CQFCliffSurface> cliffSurfaces = new();
    private int cliffBatchIndex;
    private int rimBatchIndex;
}
#endif
