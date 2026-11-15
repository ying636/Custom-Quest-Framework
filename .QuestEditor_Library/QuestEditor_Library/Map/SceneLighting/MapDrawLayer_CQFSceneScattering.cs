#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class MapDrawLayer_CQFSceneScattering : MapDrawLayer
{
    public MapDrawLayer_CQFSceneScattering(Map map) : base(map)
    {
    }

    public override bool Visible => this.Lighting.Beam is { enabled: true, density: > 0f };
    private MapComponent_CQFSceneLighting Lighting => this.Map.GetComponent<MapComponent_CQFSceneLighting>();

    public override void Regenerate()
    {
        if (!this.Visible || this.subMeshes.Count != 0) return;
        Shader shader = (this.Lighting.Beam!.compositeShader ?? throw new InvalidOperationException("CQF_SceneScatteringCompositeShader")).Shader;
        Material template = MaterialPool.MatFrom(BaseContent.WhiteTex, shader, Color.white, 3171);
        LayerSubMesh mesh = this.GetSubMesh(this.Lighting.MaterialFor(template));
        float y = AltitudeLayer.MoteOverheadLow.AltitudeFor();
        mesh.verts.AddRange(new[] { new Vector3(0f, y, 0f), new Vector3(0f, y, this.Map.Size.z), new Vector3(this.Map.Size.x, y, this.Map.Size.z), new Vector3(this.Map.Size.x, y, 0f) });
        mesh.uvs.AddRange(new[] { Vector3.zero, Vector3.up, new Vector3(1f, 1f, 0f), Vector3.right });
        mesh.colors.AddRange(new[] { (Color32)Color.white, (Color32)Color.white, (Color32)Color.white, (Color32)Color.white });
        mesh.tris.AddRange(new[] { 0, 1, 2, 0, 2, 3 });
        this.FinalizeMesh(MeshParts.All);
    }

    public override void DrawLayer()
    {
        if (!this.Visible) return;
        this.Regenerate();
        Texture? texture = this.Lighting.RenderScattering();
        if (texture == null) return;
        Material material = this.subMeshes[0].material;
        material.mainTexture = texture;
        this.Lighting.BindBeam(material);
        base.DrawLayer();
    }

    public override void RefreshSubMeshBounds()
    {
        foreach (LayerSubMesh mesh in this.subMeshes) mesh.mesh.RecalculateBounds();
    }

}
#endif
