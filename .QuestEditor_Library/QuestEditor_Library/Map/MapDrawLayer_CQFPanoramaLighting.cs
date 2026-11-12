using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class MapDrawLayer_CQFPanoramaLighting : MapDrawLayer
{
    public MapDrawLayer_CQFPanoramaLighting(Map map) : base(map)
    {
    }

    public override bool Visible => DebugViewSettings.drawLightingOverlay
        && MapComponent_CustomMapData.GetComp(this.Map)?.background
            is { Enabled: true, DrawOnCameraVisibleArea: true };

    public override void Regenerate()
    {
        if (this.subMeshes.Count != 0)
        {
            return;
        }
        this.InitializeSubMesh(MatBases.LightOverlay);
        this.InitializeSubMesh(MatBases.ShadowMask);
    }

    public override void DrawLayer()
    {
        if (!this.Visible)
        {
            return;
        }
        Camera camera = Find.Camera;
        Vector3 position = camera.transform.position;
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        float left = position.x - halfWidth;
        float right = position.x + halfWidth;
        float bottom = position.z - halfHeight;
        float top = position.z + halfHeight;
        float coveredRight = this.Map.Size.x + ExteriorLightingExtent;
        float coveredTop = this.Map.Size.z + ExteriorLightingExtent;
        if (left >= -ExteriorLightingExtent && right <= coveredRight
            && bottom >= -ExteriorLightingExtent && top <= coveredTop)
        {
            return;
        }
        this.Regenerate();
        this.DrawRegion(left, bottom, Mathf.Min(right, -ExteriorLightingExtent), top);
        this.DrawRegion(Mathf.Max(left, coveredRight), bottom, right, top);
        float innerLeft = Mathf.Max(left, -ExteriorLightingExtent);
        float innerRight = Mathf.Min(right, coveredRight);
        this.DrawRegion(innerLeft, bottom, innerRight, Mathf.Min(top, -ExteriorLightingExtent));
        this.DrawRegion(innerLeft, Mathf.Max(bottom, coveredTop), innerRight, top);
    }

    public override void RefreshSubMeshBounds()
    {
        for (int i = 0; i < this.subMeshes.Count; i++)
        {
            this.subMeshes[i].mesh.RecalculateBounds();
        }
    }

    private void InitializeSubMesh(Material material)
    {
        LayerSubMesh subMesh = this.GetSubMesh(material);
        subMesh.renderLayer = LayerMask.NameToLayer("GravshipExclude");
        subMesh.verts.Add(new Vector3(-0.5f, 0f, -0.5f));
        subMesh.verts.Add(new Vector3(-0.5f, 0f, 0.5f));
        subMesh.verts.Add(new Vector3(0.5f, 0f, 0.5f));
        subMesh.verts.Add(new Vector3(0.5f, 0f, -0.5f));
        subMesh.colors.Add(new Color32(0, 0, 0, 0));
        subMesh.colors.Add(new Color32(0, 0, 0, 0));
        subMesh.colors.Add(new Color32(0, 0, 0, 0));
        subMesh.colors.Add(new Color32(0, 0, 0, 0));
        subMesh.tris.Add(0);
        subMesh.tris.Add(1);
        subMesh.tris.Add(2);
        subMesh.tris.Add(0);
        subMesh.tris.Add(2);
        subMesh.tris.Add(3);
        subMesh.FinalizeMesh(MeshParts.Verts | MeshParts.Tris | MeshParts.Colors);
    }

    private void DrawRegion(float left, float bottom, float right, float top)
    {
        if (right <= left || top <= bottom)
        {
            return;
        }
        Matrix4x4 matrix = Matrix4x4.TRS(
            new Vector3((left + right) * 0.5f, AltitudeLayer.LightingOverlay.AltitudeFor(), (bottom + top) * 0.5f),
            Quaternion.identity, new Vector3(right - left, 1f, top - bottom));
        for (int i = 0; i < this.subMeshes.Count; i++)
        {
            LayerSubMesh subMesh = this.subMeshes[i];
            if (subMesh.finalized && !subMesh.disabled)
            {
                Graphics.DrawMesh(subMesh.mesh, matrix, subMesh.material, subMesh.renderLayer);
            }
        }
    }

    private const float ExteriorLightingExtent = 200f;
}
