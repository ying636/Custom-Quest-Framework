using System.Buffers.Binary;
using UnityEngine;
using Verse;

namespace CQF.DomainContestRuntimeChecks;

public sealed class AssetChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainContestChecks.txt");

    private void Update()
    {
        if (finished) return;
        try
        {
            if (samples.Count == 0)
            {
                Find.TickManager.Pause();
                string root = LoadedModManager.RunningModsListForReading.Single(mod =>
                    mod.PackageIdPlayerFacing.Equals("HaiLuan.VMech", StringComparison.OrdinalIgnoreCase)).RootDir;
                string textures = Path.Combine(root, "Textures");
                int count = 0;
                foreach (string path in Directory.EnumerateFiles(textures, "*.png", SearchOption.AllDirectories))
                {
                    string resource = Path.ChangeExtension(Path.GetRelativePath(textures, path), null).Replace('\\', '/');
                    Texture2D loaded = ContentFinder<Texture2D>.Get(resource);
                    byte[] header = new byte[24];
                    using (FileStream source = File.OpenRead(path)) source.ReadExactly(header);
                    int width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16));
                    int height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20));
                    if (loaded == null || loaded == BaseContent.BadTex || loaded.width != width || loaded.height != height)
                        throw new InvalidOperationException("Loaded texture dimensions differ from source: " + resource);
                    count++;
                }
                foreach (string resource in new[]
                {
                    "Things/WuDeLiDao/BodyAttack/VM_WuDeLiDao_Body_South_8",
                    "Things/WuDeLiDao/VM_WuDeLiDao_Front",
                    "Things/Pawn/Mechanoid/VMSpider/Full/VM_Spider_Normal_south"
                })
                {
                    string copy = Path.Combine(GenFilePaths.SaveDataFolderPath, Path.GetFileName(resource) + ".png");
                    File.Copy(Path.Combine(textures, resource + ".png"), copy);
                    samples.Add((resource, ContentFinder<Texture2D>.Get(resource), Texture2D.LoadFromFile(copy)));
                }
                File.AppendAllText(ResultPath, "PASS asset coverage: " + count + " textures retain their source dimensions\n");
                return;
            }
            frames++;
            string screenshot = Path.Combine(GenFilePaths.SaveDataFolderPath, "AssetQuality.png");
            if (frames == 12)
            {
                ScreenCapture.CaptureScreenshot(screenshot);
                capturedAt = Time.realtimeSinceStartup;
            }
            if (frames < 20) return;
            if (!File.Exists(screenshot) || new FileInfo(screenshot).Length == 0)
            {
                if (Time.realtimeSinceStartup - capturedAt < 15f) return;
                throw new IOException("Asset quality screenshot was not produced.");
            }
            File.AppendAllText(ResultPath, "PASS asset comparison screenshot captured\nCOMPLETE\n");
            finished = true;
            Application.Quit();
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("[CQF] Asset runtime checks failed: " + error);
            finished = true;
            Application.Quit(1);
        }
    }

    private void OnGUI()
    {
        if (finished || samples.Count == 0) return;
        float size = Math.Min(480f, Math.Min((Screen.height - 160f) / 3f, (Screen.width - 80f) / 2f));
        Color saved = GUI.color;
        GUI.color = new Color(0.08f, 0.08f, 0.08f);
        GUI.DrawTexture(new Rect(0f, 0f, size * 2f + 60f, size * 3f + 150f), BaseContent.WhiteTex);
        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 0f, size, 30f), "Loaded mod texture");
        GUI.Label(new Rect(size + 40f, 0f, size, 30f), "Original PNG");
        for (int row = 0; row < samples.Count; row++)
        {
            GUI.Label(new Rect(20f, 30f + row * (size + 30f), size * 2f, 30f), Path.GetFileName(samples[row].Name));
            for (int column = 0; column < 2; column++)
            {
                Rect rect = new(20f + column * (size + 20f), 60f + row * (size + 30f), size, size);
                for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float shade = (x + y) % 2 == 0 ? 0.25f : 0.4f;
                    GUI.color = new Color(shade, shade, shade);
                    GUI.DrawTexture(new Rect(rect.x + x * size / 16f, rect.y + y * size / 16f, size / 16f, size / 16f), BaseContent.WhiteTex);
                }
                GUI.color = Color.white;
                GUI.DrawTexture(rect, column == 0 ? samples[row].Loaded : samples[row].Original, ScaleMode.ScaleToFit, true);
            }
        }
        GUI.color = saved;
    }

    private readonly List<(string Name, Texture2D Loaded, Texture2D Original)> samples = new();
    private int frames;
    private float capturedAt;
    private bool finished;
}
