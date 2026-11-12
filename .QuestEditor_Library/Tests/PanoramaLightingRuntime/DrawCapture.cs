using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.PanoramaLightingChecks;

public static class DrawCapture
{
    public static void Install()
    {
        harmony.Patch(AccessTools.Method(typeof(MapDrawLayer_CQFPanoramaLighting), "DrawRegion"),
            transpiler: new HarmonyMethod(typeof(DrawCapture), nameof(Transpile)));
        harmony.Patch(AccessTools.Method(typeof(CameraDriver), "ApplyPositionToGameObject"),
            postfix: new HarmonyMethod(typeof(DrawCapture), nameof(LockCamera)));
        harmony.Patch(AccessTools.Method(typeof(CameraDriver), "Update"),
            postfix: new HarmonyMethod(typeof(DrawCapture), nameof(LockCamera)));
    }

    public static void RemoveDrawObserver()
    {
        harmony.Unpatch(AccessTools.Method(typeof(MapDrawLayer_CQFPanoramaLighting), "DrawRegion"),
            HarmonyPatchType.Transpiler, harmony.Id);
    }

    public static void LockCamera()
    {
        if (!CameraLocked || Find.Camera == null) return;
        Find.Camera.transform.position = new Vector3(Center.x, 65f, Center.z);
        Find.Camera.orthographicSize = Size;
        Find.Camera.nearClipPlane = 0.5f;
        Find.Camera.farClipPlane = 2000f;
    }

    public static void RecordSubmission(Mesh mesh, Matrix4x4 matrix, Material material, int layer)
    {
        Graphics.DrawMesh(mesh, matrix, material, layer);
        if (Frame != Time.frameCount)
        {
            Frame = Time.frameCount;
            Calls.Clear();
        }
        Vector3 min = matrix.MultiplyPoint3x4(new Vector3(-0.5f, 0f, -0.5f));
        Vector3 max = matrix.MultiplyPoint3x4(new Vector3(0.5f, 0f, 0.5f));
        Calls.Add(new Submission(Rect.MinMaxRect(min.x, min.z, max.x, max.z), min.y, material, layer, mesh));
    }

    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo original = AccessTools.Method(typeof(Graphics), "DrawMesh", new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });
        MethodInfo replacement = AccessTools.Method(typeof(DrawCapture), nameof(RecordSubmission));
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException("DrawMesh call site: " + replaced);
    }

    public static bool CameraLocked;
    public static Vector3 Center;
    public static float Size;
    public static int Frame = -1;
    public static readonly List<Submission> Calls = new(8);
    private static readonly Harmony harmony = new("local.CQF.PanoramaLightingChecks");
}

public readonly record struct Submission(Rect Area, float Altitude, Material Material, int Layer, Mesh Mesh);
