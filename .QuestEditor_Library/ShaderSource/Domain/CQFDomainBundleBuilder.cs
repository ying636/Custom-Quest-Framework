using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CQFDomainBundleBuilder
{
    public static void Build()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string output = Environment.GetEnvironmentVariable("CQF_DOMAIN_BUNDLE_OUTPUT");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("CQF_DOMAIN_BUNDLE_OUTPUT is required.");
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);
            string[] assets = { "Assets/Shaders/CQFDomainOpening.shader", "Assets/Shaders/CQFDomainTransition.shader" };
            string[] names = { "CQF/DomainOpening", "CQF/DomainTransition" };
            for (int index = 0; index < assets.Length; index++)
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(assets[index]);
                Debug.Log("[CQF] Checking " + assets[index] + ": name=" + (shader == null ? "null" : shader.name)
                    + ", errors=" + (shader != null && ShaderUtil.ShaderHasError(shader)));
                if (shader != null)
                    foreach (var message in ShaderUtil.GetShaderMessages(shader))
                        Debug.Log("[CQF] " + message.severity + " " + message.file + ":" + message.line + " " + message.message);
                if (shader == null || shader.name != names[index] || ShaderUtil.ShaderHasError(shader))
                    throw new InvalidDataException("Invalid domain shader: " + assets[index]);
            }
            const string bundleName = "cqf_domain_win";
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(output,
                new[] { new AssetBundleBuild { assetBundleName = bundleName, assetNames = assets } },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                BuildTarget.StandaloneWindows64);
            if (manifest == null) throw new InvalidOperationException("CQF domain bundle build failed.");
            string path = Path.Combine(output, bundleName);
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 100 || Encoding.ASCII.GetString(bytes, 0, 7) != "UnityFS")
                throw new InvalidDataException("Invalid CQF domain bundle header.");
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null) throw new InvalidDataException("Could not reload CQF domain bundle.");
            try
            {
                Shader[] shaders = bundle.LoadAllAssets<Shader>();
                foreach (string name in names)
                {
                    Shader shader = shaders.SingleOrDefault(item => item.name == name);
                    if (shader == null || !shader.isSupported || shader.passCount < 1)
                        throw new InvalidDataException("CQF domain shader failed verification: " + name);
                    Debug.Log("[CQF] Verified " + shader.name + ", passes=" + shader.passCount);
                }
                VerifyTransition(shaders.Single(shader => shader.name == "CQF/DomainTransition"), output);
                VerifyOpening(shaders.Single(shader => shader.name == "CQF/DomainOpening"), output);
            }
            finally { bundle.Unload(true); }
            File.WriteAllText(Path.Combine(output, "verified.txt"), string.Join("\n", names));
            Debug.Log("[CQF] Domain bundle built and verified: " + bytes.Length + " bytes.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void VerifyTransition(Shader shader, string output)
    {
        Material material = new Material(shader);
        RenderTexture target = new RenderTexture(96, 64, 0, RenderTextureFormat.ARGB32);
        Texture2D pixels = new Texture2D(96, 64, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            material.SetTexture("_NoiseTex", Texture2D.grayTexture);
            material.SetTexture("_SurfaceTex", Texture2D.whiteTexture);
            material.SetVector("_Origin", new Vector4(0.2f, 0.3f, 1.5f, 0f));
            material.SetVector("_Destination", new Vector4(0.8f, 0.7f, 1.5f, 0f));
            target.Create();
            foreach (float returning in new[] { 0f, 1f })
                foreach (float progress in new[] { 0f, 0.42f, 0.46f, 1f })
                {
                    RenderTexture.active = target;
                    GL.Clear(true, true, Color.cyan);
                    material.SetFloat("_Returning", returning);
                    material.SetFloat("_Progress", progress);
                    Graphics.Blit(Texture2D.whiteTexture, target, material);
                    pixels.ReadPixels(new Rect(0, 0, 96, 64), 0, 0);
                    pixels.Apply();
                    bool covered = progress == 0.42f || progress == 0.46f;
                    foreach (Color pixel in pixels.GetPixels())
                        if (covered ? pixel.g > 0.9f : pixel.r > 0.02f || pixel.g < 0.98f || pixel.b < 0.98f)
                            throw new InvalidDataException("Domain transition coverage failed: " + returning + "/" + progress);
                    File.WriteAllBytes(Path.Combine(output, "transition_" + returning + "_" + progress + ".png"), pixels.EncodeToPNG());
                }
            Debug.Log("[CQF] Verified entry/return full cover before switching and transparency at both ends (8 GPU readbacks).");
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    private static void VerifyOpening(Shader shader, string output)
    {
        Material material = new Material(shader);
        RenderTexture target = new RenderTexture(96, 96, 0, RenderTextureFormat.ARGB32);
        Texture2D pixels = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        Texture2D skin = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            target.Create();
            material.SetTexture("_NoiseTex", Texture2D.grayTexture);
            material.SetFloat("_Progress", 0.55f);
            material.SetFloat("_Mode", 0f);
            material.SetColor("_Color", Color.white);
            material.SetColor("_SecondaryColor", Color.white);
            material.SetColor("_EdgeColor", Color.clear);
            foreach (Color tint in new[] { Color.red, Color.blue })
            {
                skin.SetPixels(new[] { tint, tint, tint, tint });
                skin.Apply();
                material.SetTexture("_MainTex", skin);
                RenderTexture.active = target;
                GL.Clear(true, true, Color.cyan);
                Graphics.Blit(skin, target, material);
                pixels.ReadPixels(new Rect(0, 0, 96, 96), 0, 0);
                pixels.Apply();
                Color center = pixels.GetPixel(48, 48);
                if (tint == Color.red ? center.r < 0.9f || center.b > 0.05f : center.b < 0.9f || center.r > 0.05f)
                    throw new InvalidDataException("Domain opening ignores its skin texture.");
                Color corner = pixels.GetPixel(0, 0);
                if (corner.r > 0.05f || corner.g < 0.95f || corner.b < 0.95f)
                    throw new InvalidDataException("Domain opening radius does not clip the surface.");
                File.WriteAllBytes(Path.Combine(output, tint == Color.red ? "opening_red.png" : "opening_blue.png"), pixels.EncodeToPNG());
            }
            Debug.Log("[CQF] Verified opening texture replacement and radial clipping (2 GPU readbacks).");
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(skin);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
