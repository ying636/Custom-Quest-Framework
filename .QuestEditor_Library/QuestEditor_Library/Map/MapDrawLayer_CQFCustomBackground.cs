using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class MapDrawLayer_CQFCustomBackground : MapDrawLayer
{
    public override bool Visible => this.Background?.Enabled ?? false;

    public MapDrawLayer_CQFCustomBackground(Map map) : base(map)
    {
        this.relevantChangeTypes = MapMeshFlagDefOf.Terrain;
    }

    public override void Regenerate()
    {
        base.ClearSubMeshes(MeshParts.All);
        CustomMapBackgroundData background = this.Background;
        if (background == null || !background.Enabled)
        {
            return;
        }
        Texture2D texture = ContentFinder<Texture2D>.Get(background.texPath, false);
        if (texture == null)
        {
            return;
        }
        Color color = background.color;
        color.a *= background.alpha;
        Material material = MaterialPool.MatFrom(texture, ShaderDatabase.Transparent, color, BackgroundRenderQueue);
        LayerSubMesh subMesh = base.GetSubMesh(material);
        if (subMesh == null)
        {
            return;
        }
        this.MakeBackgroundGeometry(background, subMesh);
        subMesh.FinalizeMesh(MeshParts.All);
    }

    private CustomMapBackgroundData Background => MapComponent_CustomMapData.GetComp(base.Map)?.background;

    private const int BackgroundRenderQueue = 1900;

    private void MakeBackgroundGeometry(CustomMapBackgroundData background, LayerSubMesh subMesh)
    {
        // drawSize 为 0 时默认覆盖整张地图，避免每张图都必须手填尺寸。
        Vector2 size = background.drawSize == Vector2.zero
            ? new Vector2(base.Map.Size.x, base.Map.Size.z)
            : background.drawSize;
        Vector2 offset = background.offset;
        float y = AltitudeLayer.BelowTerrain.AltitudeFor();
        subMesh.verts.Add(new Vector3(offset.x, y, offset.y));
        subMesh.verts.Add(new Vector3(offset.x, y, offset.y + size.y));
        subMesh.verts.Add(new Vector3(offset.x + size.x, y, offset.y + size.y));
        subMesh.verts.Add(new Vector3(offset.x + size.x, y, offset.y));
        subMesh.uvs.Add(new Vector2(0f, 0f));
        subMesh.uvs.Add(new Vector2(0f, 1f));
        subMesh.uvs.Add(new Vector2(1f, 1f));
        subMesh.uvs.Add(new Vector2(1f, 0f));
        subMesh.tris.Add(0);
        subMesh.tris.Add(1);
        subMesh.tris.Add(2);
        subMesh.tris.Add(0);
        subMesh.tris.Add(2);
        subMesh.tris.Add(3);
    }
}
