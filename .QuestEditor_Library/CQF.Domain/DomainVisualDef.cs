using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DomainVisualDef : Def
{
    public Material CreateMaterial(bool transition)
        => CreateMaterial(transition, null);

    public Material CreateMaterial(bool transition, string? shaderOverride)
    {
        string shaderName = shaderOverride ?? (transition ? transitionShader : openingShader);
#if RIMWORLD_1_7
        if (!Waylith.Rendering.ShaderRegistry.TryResolve(shaderName, out _))
            throw new InvalidOperationException("[CQF] Domain shader is not registered: " + shaderName);
        Shader shader = DefDatabase<ShaderDef>.GetNamed(shaderName).Shader;
#else
        shaderName = transition ? transitionUnityShader : openingUnityShader;
        Shader? shader = Shader.Find(shaderName);
        if (shader == null)
            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                shader = bundle.LoadAllAssets<Shader>().FirstOrDefault(candidate => candidate.name == shaderName);
                if (shader != null) break;
            }
        if (shader == null || !shader.isSupported || shader.passCount < 1)
            throw new InvalidOperationException("[CQF] Domain shader is missing or unsupported: " + shaderName);
#endif
        Material material = new(shader) { name = defName + (transition ? "_Transition" : "_Opening") };
        try
        {
            material.SetColor("_Color", color);
            material.SetColor("_SecondaryColor", secondaryColor);
            material.SetColor("_EdgeColor", edgeColor);
            material.SetVector("_Flow", new Vector4(flowSpeed.x, flowSpeed.y, textureScale, distortion));
            material.SetVector("_Stages", new Vector4(expansionEnd, curtainStart, curtainEnd, foldStart));
            material.SetFloat("_FoldEnd", foldEnd);
            material.SetFloat("_Height", curtainHeight);
            material.SetFloat("_EdgeWidth", edgeWidth);
            material.SetFloat("_CoverProgress", coverProgress);
            material.SetFloat("_RevealProgress", revealProgress);
            material.SetFloat("_RevealEnd", revealEnd);
            material.SetTexture(transition ? "_SurfaceTex" : "_MainTex", string.IsNullOrEmpty(texturePath)
                ? Texture2D.whiteTexture : ContentFinder<Texture2D>.Get(texturePath)
                    ?? throw new InvalidOperationException("[CQF] Domain texture is missing: " + texturePath));
            material.SetTexture("_NoiseTex", string.IsNullOrEmpty(noiseTexturePath)
                ? Texture2D.grayTexture : ContentFinder<Texture2D>.Get(noiseTexturePath)
                    ?? throw new InvalidOperationException("[CQF] Domain noise texture is missing: " + noiseTexturePath));
            return material;
        }
        catch
        {
            UnityEngine.Object.Destroy(material);
            throw;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (openingTicks <= 0 || closingTicks <= 0) yield return "Domain visuals require positive openingTicks and closingTicks.";
        foreach (float value in new[] { radius, curtainHeight, textureScale, edgeWidth, entryDuration, returnDuration })
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
                yield return "Domain visual dimensions, scale, edge width and durations must be finite and positive.";
        foreach (float value in new[] { expansionEnd, curtainStart, curtainEnd, foldStart, foldEnd, coverProgress, revealProgress, revealEnd })
            if (value < 0f || value > 1f || float.IsNaN(value)) yield return "Domain visual progress must be within [0,1].";
        if (expansionEnd <= 0f || curtainStart >= curtainEnd || foldStart < curtainStart || foldStart >= foldEnd)
            yield return "Domain opening stages must have positive, ordered intervals.";
        if (coverProgress <= 0f || coverProgress >= revealProgress || revealProgress >= revealEnd || revealEnd >= 1f)
            yield return "Domain transition requires 0 < coverProgress < revealProgress < revealEnd < 1.";
        foreach (float value in new[] { flowSpeed.x, flowSpeed.y, distortion, color.r, color.g, color.b, color.a,
            secondaryColor.r, secondaryColor.g, secondaryColor.b, secondaryColor.a, edgeColor.r, edgeColor.g, edgeColor.b, edgeColor.a })
            if (float.IsNaN(value) || float.IsInfinity(value)) yield return "Domain material parameters must be finite.";
        if (distortion < 0f) yield return "Domain distortion cannot be negative.";
        if (string.IsNullOrWhiteSpace(groundShader) || string.IsNullOrWhiteSpace(openingShader) || string.IsNullOrWhiteSpace(transitionShader)
            || string.IsNullOrWhiteSpace(openingUnityShader) || string.IsNullOrWhiteSpace(transitionUnityShader))
            yield return "Domain visuals require shader names for both renderer versions.";
    }

    public string groundShader = "CQF_DomainGround";
    public string openingShader = "CQF_DomainOpening";
    public string transitionShader = "CQF_DomainTransition";
    public string openingUnityShader = "CQF/DomainOpening";
    public string transitionUnityShader = "CQF/DomainTransition";
    public string? texturePath;
    public string? noiseTexturePath;
    public Color color = new(0.06f, 0.06f, 0.06f, 1f);
    public Color secondaryColor = new(0.3f, 0.3f, 0.3f, 1f);
    public Color edgeColor = Color.white;
    public Vector2 flowSpeed = new(0.1f, -0.2f);
    public float textureScale = 1f;
    public float distortion = 0.15f;
    public float edgeWidth = 0.12f;
    public float radius = 12f;
    public float curtainHeight = 4.5f;
    public int openingTicks = 360;
    public int closingTicks = 120;
    public float expansionEnd = 0.55f;
    public float curtainStart = 0.48f;
    public float curtainEnd = 0.72f;
    public float foldStart = 0.7f;
    public float foldEnd = 0.94f;
    public float entryDuration = 2f;
    public float returnDuration = 1.8f;
    public float coverProgress = 0.42f;
    public float revealProgress = 0.46f;
    public float revealEnd = 0.95f;
}
