using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DomainSurfaceDef : Def
{
    public virtual Material CreateMaterial(bool screen = false)
    {
        string shaderName = screen ? screenShader ?? shader : shader;
#if RIMWORLD_1_7
        Shader nativeShader = DefDatabase<ShaderDef>.GetNamed(shaderName).Shader;
#else
        Shader nativeShader = Shader.Find(shaderName) ?? throw new InvalidOperationException("[CQF] Domain surface shader is unavailable: " + shaderName);
#endif
        Material material = new(nativeShader) { name = defName + "_Surface", color = color };
        try
        {
            material.SetColor("_SecondaryColor", secondaryColor);
            material.SetVector("_Flow", new Vector4(flowSpeed.x, flowSpeed.y, textureScale, 0f));
            return material;
        }
        catch { UnityEngine.Object.Destroy(material); throw; }
    }

    public virtual void Configure(MaterialPropertyBlock properties, float time)
        => properties.SetFloat("_EffectTime", time);

    public virtual void Configure(Material material, float time)
        => material.SetFloat("_EffectTime", time);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (string.IsNullOrWhiteSpace(shader)) yield return "Domain surface requires a shader.";
        if (screenShader != null && string.IsNullOrWhiteSpace(screenShader)) yield return "Domain surface screen shader cannot be blank.";
        if (float.IsNaN(screenBorderWidth) || float.IsInfinity(screenBorderWidth) || screenBorderWidth < 0f || screenBorderWidth > 0.3f)
            yield return "Domain screen border width must be within [0,0.3].";
        if (float.IsNaN(screenBorderOpacity) || float.IsInfinity(screenBorderOpacity) || screenBorderOpacity < 0f || screenBorderOpacity > 1f)
            yield return "Domain screen border opacity must be within [0,1].";
        if (textureScale <= 0f || float.IsNaN(textureScale) || float.IsInfinity(textureScale))
            yield return "Domain surface texture scale must be finite and positive.";
        foreach (float value in new[] { flowSpeed.x, flowSpeed.y, color.r, color.g, color.b, color.a,
            secondaryColor.r, secondaryColor.g, secondaryColor.b, secondaryColor.a })
            if (float.IsNaN(value) || float.IsInfinity(value)) yield return "Domain surface parameters must be finite.";
    }

    public string shader = "CQF_DomainSurface";
    public string? screenShader;
    public float screenBorderWidth;
    public float screenBorderOpacity = 0.75f;
    public Color color = new(0.28f, 0.30f, 0.32f, 1f);
    public Color secondaryColor = new(0.85f, 0.87f, 0.90f, 1f);
    public Vector2 flowSpeed = new(0.1f, -0.2f);
    public float textureScale = 1f;
}
