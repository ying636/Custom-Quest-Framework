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
        if (background.DrawOnCameraVisibleArea)
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

    public override void DrawLayer()
    {
        CustomMapBackgroundData background = this.Background;
        if (background is { Enabled: true, DrawOnCameraVisibleArea: true })
        {
            this.DrawCameraVisibleBackground(background);
            return;
        }
        base.DrawLayer();
    }

    private CustomMapBackgroundData Background => MapComponent_CustomMapData.GetComp(base.Map)?.background;

    private const int BackgroundRenderQueue = 1900;
    private const int CameraVisibleRenderQueue = 1880;

    private void DrawCameraVisibleBackground(CustomMapBackgroundData background)
    {
        Texture2D texture = ContentFinder<Texture2D>.Get(background.texPath, false);
        if (texture == null)
        {
            return;
        }
        texture.wrapMode = background.fitMode == CustomMapBackgroundFitMode.Tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        Color color = background.color;
        color.a *= background.alpha;
        Material material = MaterialPool.MatFrom(texture, ShaderDatabase.Transparent, color, CameraVisibleRenderQueue);
        float height = Find.Camera.orthographicSize * 2f;
        float width = height * Find.Camera.aspect;
        Vector3 position = Find.Camera.transform.position;
        this.SetCameraVisibleTextureTransform(background, texture, material, width, height, position);
        position.y = AltitudeLayer.BelowTerrain.AltitudeFor() - 0.02f;
        Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(width, 1f, height));
        Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
    }

    private void SetCameraVisibleTextureTransform(CustomMapBackgroundData background, Texture2D texture, Material material, float width, float height, Vector3 position)
    {
        switch (background.fitMode)
        {
            case CustomMapBackgroundFitMode.Stretch:
                material.mainTextureScale = Vector2.one;
                material.mainTextureOffset = Vector2.zero;
                break;
            case CustomMapBackgroundFitMode.Cover:
                float textureAspect = texture.width / (float)texture.height;
                float viewAspect = width / height;
                Vector2 textureScale = Vector2.one;
                Vector2 textureOffset = Vector2.zero;
                if (textureAspect > viewAspect)
                {
                    textureScale.x = viewAspect / textureAspect;
                    textureOffset.x = (1f - textureScale.x) / 2f;
                }
                else
                {
                    textureScale.y = textureAspect / viewAspect;
                    textureOffset.y = (1f - textureScale.y) / 2f;
                }
                material.mainTextureScale = textureScale;
                material.mainTextureOffset = textureOffset;
                break;
            default:
                Vector2 tileSize = background.drawSize == Vector2.zero
                    ? new Vector2(base.Map.Size.x, base.Map.Size.z)
                    : background.drawSize;
                float scale = Mathf.Max(0.01f, background.scale);
                tileSize *= scale;
                Vector2 min = new Vector2(position.x - width / 2f, position.z - height / 2f) + background.offset;
                material.mainTextureScale = new Vector2(width / tileSize.x, height / tileSize.y);
                material.mainTextureOffset = new Vector2(min.x / tileSize.x, min.y / tileSize.y);
                break;
        }
    }

    private void MakeBackgroundGeometry(CustomMapBackgroundData background, LayerSubMesh subMesh)
    { 
        Vector2 size = background.drawSize == Vector2.zero
            ? new Vector2(base.Map.Size.x, base.Map.Size.z)
            : background.drawSize;
        Vector2 offset = background.offset;
        this.MakeQuadGeometry(offset, size, subMesh, Vector2.zero, Vector2.one);
    }

    private void MakeQuadGeometry(Vector2 offset, Vector2 size, LayerSubMesh subMesh, Vector2 uvMin, Vector2 uvMax)
    {
        float y = AltitudeLayer.BelowTerrain.AltitudeFor();
        subMesh.verts.Add(new Vector3(offset.x, y, offset.y));
        subMesh.verts.Add(new Vector3(offset.x, y, offset.y + size.y));
        subMesh.verts.Add(new Vector3(offset.x + size.x, y, offset.y + size.y));
        subMesh.verts.Add(new Vector3(offset.x + size.x, y, offset.y));
        subMesh.uvs.Add(new Vector2(uvMin.x, uvMin.y));
        subMesh.uvs.Add(new Vector2(uvMin.x, uvMax.y));
        subMesh.uvs.Add(new Vector2(uvMax.x, uvMax.y));
        subMesh.uvs.Add(new Vector2(uvMax.x, uvMin.y));
        subMesh.tris.Add(0);
        subMesh.tris.Add(1);
        subMesh.tris.Add(2);
        subMesh.tris.Add(0);
        subMesh.tris.Add(2);
        subMesh.tris.Add(3);
    }
}
