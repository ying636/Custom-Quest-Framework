#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFSceneScattering : IDisposable
{
    public CQFSceneScattering(Map map) => this.map = map;

    public RenderTexture? Texture => this.texture;
    public int RenderCount { get; private set; }

    public Texture Render(MapComponent_CQFSceneLighting lighting)
    {
        CQFSceneBeam beam = lighting.Beam!;
        int key = HashCode.Combine(lighting.PhaseTicks, lighting.BeamPlane, lighting.BeamGain, beam.color, beam.density, beam.dust, lighting.ShadowRevision);
        key = HashCode.Combine(key, beam.innerWidth, beam.outerWidth, beam.scatterHeight, lighting.Lights[beam.lightIndex].softness);
        if (this.texture != null && key == this.lastKey) return this.texture;
        this.material ??= new Material((beam.scatteringShader ?? throw new InvalidOperationException("CQF_SceneScatteringShader")).Shader);
        if (this.texture == null)
        {
            int height = Math.Max(8, ((int)Math.Ceiling(256f * this.map.Size.z / this.map.Size.x) + 7) / 8 * 8);
            this.texture = new RenderTexture(256, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "CQF_SceneScattering", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            if (!this.texture.Create()) throw new InvalidOperationException("CQF_SceneScatteringRenderTarget");
        }
        lighting.BindBeam(this.material);
        this.material.SetVector("_CQFScatterBounds", new Vector4(0f, 0f, this.map.Size.x, this.map.Size.z));
        RenderTexture previous = RenderTexture.active;
        try { Graphics.Blit(BaseContent.WhiteTex, this.texture, this.material); }
        finally { RenderTexture.active = previous; }
        this.lastKey = key;
        this.RenderCount++;
        return this.texture;
    }

    public void Dispose()
    {
        if (this.texture != null)
        {
            this.texture.Release();
            UnityEngine.Object.Destroy(this.texture);
        }
        if (this.material != null) UnityEngine.Object.Destroy(this.material);
        this.texture = null;
        this.material = null;
    }

    private readonly Map map;
    private RenderTexture? texture;
    private Material? material;
    private int lastKey;
}
#endif
