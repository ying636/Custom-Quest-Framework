using System.Security.Cryptography;
using System.Text.Json;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;
using Verse;
using Verse.ProfilerUI;
using Waylith.Profiling;
using Waylith.Profiling.Instrumentation;

namespace CQF.DomainPerformanceChecks;

public sealed partial class DomainPerformanceDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return;
        try
        {
            routine ??= Run().GetEnumerator();
            if (routine.MoveNext()) return;
            routine.Dispose();
            routine = null;
            finished = true;
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "COMPLETE\n");
            Application.Quit(0);
        }
        catch (Exception error)
        {
            File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "FAIL " + error + "\n");
            Log.Error("[CQF.DomainPerformanceChecks] " + error);
            finished = true;
            routine?.Dispose();
            Application.Quit(1);
        }
    }

    private IEnumerable<object?> Run()
    {
        Find.TickManager.Pause();
        Check(CQFModuleLoader.DomainLoaded && !CQFModuleLoader.IsLoaded("MapProjections"), "domain performance fixture requests only the domain module");
        string save = Path.Combine(GenFilePaths.SaveDataFolderPath, "Saves", "CQF_DomainPerformance.rws");
        if (!File.Exists(save)) GameDataSaveLoader.SaveGame("CQF_DomainPerformance");
        yield return null;
        string sceneHash = Hash(File.ReadAllBytes(save));
        Game previous = Current.Game;
        GameDataSaveLoader.LoadGame("CQF_DomainPerformance");
        yield return null;
        Check(Current.Game != previous, "native fixed scene reload completes");
        Find.TickManager.Pause();
        File.WriteAllText(Path.Combine(Bootstrap.Output, "Scene.json"), JsonSerializer.Serialize(new
        {
            Save = save, SHA256 = sceneHash,
            CoreSHA256 = Hash(File.ReadAllBytes(typeof(CQFModuleLoader).Assembly.Location)),
            DomainSHA256 = Hash(File.ReadAllBytes(typeof(DomainTerritoryPartition).Assembly.Location)),
            Method = "Native Frame profiler fixed-workload comparison: 2/4/8-domain partitions, changing health, complete geometry rebuild and forced field refresh; CPU workloads, not full game FPS"
        }, new JsonSerializerOptions { WriteIndented = true }));
        if (GenCommandLine.CommandLineArgPassed("cqfdomaingameplaychecks"))
        {
            foreach (object? wait in Gameplay()) yield return wait;
        }
        else
            foreach (object? wait in Profile()) yield return wait;
    }

    private static string GeometryHash(DomainTerritoryPartition partition, DomainTerritoryGeometry geometry)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        for (int side = 0; side < partition.DomainCount; side++)
        {
            vertices.Clear(); triangles.Clear(); colors.Clear();
            geometry.Append(new Rect(0f, 0f, partition.Width, partition.Height), side, 0f, vertices, triangles, colors);
            writer.Write(vertices.Count);
            foreach (Vector3 value in vertices) { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
            writer.Write(triangles.Count);
            foreach (int index in triangles) writer.Write(index);
        }
        return Hash(stream.ToArray());
    }

    private static string StateHash(DomainTerritoryPartition partition)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        for (int z = 0; z < partition.Height; z++)
        for (int x = 0; x < partition.Width; x++)
        {
            IntVec3 cell = new(x, 0, z);
            writer.Write(partition.OwnerIndex(cell));
            writer.Write(partition.IsBoundaryCell(cell));
        }
        return Hash(stream.ToArray());
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string CheckPixels(DomainTerritoryPartition partition, DomainTerritoryField[] fields)
    {
        Texture2D reference = new(partition.Width, partition.Height, TextureFormat.RGBA32, false, true);
        try
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            Color[] pixels = new Color[partition.CellCount];
            MaterialPropertyBlock properties = new();
            for (int side = 0; side < partition.DomainCount; side++)
            {
                for (int z = 0; z < partition.Height; z++)
                for (int x = 0; x < partition.Width; x++)
                {
                    IntVec3 cell = new(x, 0, z);
                    float distance = partition.IsMultilateral ? ReferenceDistance(partition, side, x + 0.5f, z + 0.5f)
                        : partition.BaseSignedDistance(x + 0.5f, z + 0.5f);
                    if (partition.IsMultilateral && BitConverter.SingleToInt32Bits(distance)
                        != BitConverter.SingleToInt32Bits(partition.DistanceFor(side, x + 0.5f, z + 0.5f)))
                        throw new InvalidOperationException("Cached center distance differs from the original score calculation: " + cell + "/" + side);
                    pixels[z * partition.Width + x] = new Color((partition.IsMultilateral ? partition.OwnerIndex(cell) == side : partition.IsFirst(cell)) ? 1f : 0f,
                        partition.OverrideAt(x, z) != 0 ? 1f : 0f, Mathf.Clamp01(0.5f + distance / 16f), partition.UsesCurvedFront ? 1f : 0f);
                }
                reference.SetPixels(pixels);
                reference.Apply(false);
                fields[side].Configure(properties, side);
                Color32[] expected = reference.GetPixels32(), actual = fields[side].Texture.GetPixels32();
                if (!expected.SequenceEqual(actual)) throw new InvalidOperationException("RGBA32 field bytes differ from the original float texture upload: " + side);
                foreach (Color32 pixel in actual) { writer.Write(pixel.r); writer.Write(pixel.g); writer.Write(pixel.b); writer.Write(pixel.a); }
            }
            Check(true, "cached distances and texture bytes match the original calculation: " + partition.DomainCount + " domains");
            return Hash(stream.ToArray());
        }
        finally { UnityEngine.Object.Destroy(reference); }
    }

    private static float ReferenceDistance(DomainTerritoryPartition partition, int side, float x, float z)
    {
        if (partition.FractionFor(side) <= 0f) return -8f;
        int rival = -1;
        float best = float.NegativeInfinity;
        for (int index = 0; index < partition.DomainCount; index++)
        {
            if (index == side || partition.FractionFor(index) <= 0f) continue;
            float score = partition.ScoreFor(index, x, z);
            if (score > best) { best = score; rival = index; }
        }
        if (rival < 0) return 8f;
        float value = partition.ScoreFor(side, x, z) - best;
        float gx = partition.ScoreFor(side, x + 0.25f, z) - partition.ScoreFor(rival, x + 0.25f, z)
            - partition.ScoreFor(side, x - 0.25f, z) + partition.ScoreFor(rival, x - 0.25f, z);
        float gz = partition.ScoreFor(side, x, z + 0.25f) - partition.ScoreFor(rival, x, z + 0.25f)
            - partition.ScoreFor(side, x, z - 0.25f) + partition.ScoreFor(rival, x, z - 0.25f);
        return Mathf.Clamp(value / Mathf.Max(0.002f, Mathf.Sqrt(gx * gx + gz * gz) * 2f), -8f, 8f);
    }

    private static void Check(bool value, string text)
    {
        if (!value) throw new InvalidOperationException(text);
        File.AppendAllText(Path.Combine(Bootstrap.Output, "Checks.txt"), "PASS " + text + "\n");
    }

    private static readonly List<Vector3> vertices = new();
    private static readonly List<int> triangles = new();
    private static readonly List<Color32> colors = new();
    private IEnumerator<object?>? routine;
    private bool finished;
}
