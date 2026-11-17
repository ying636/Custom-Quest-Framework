using System.Reflection;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;
using UnityStubs.Rendering;
using Waylith.Rendering;

namespace CQF.ProjectionQualityChecks;

public sealed partial class QualityChecksDriver
{
    private IEnumerable<object?> CheckDrawingBuffers()
    {
        projection!.refreshFrames = 1;
        projection.PanView(Vector2.zero);
        projection.Draw(host!);
        object renderer = AccessTools.Field(typeof(CQFMapProjection), "renderer").GetValue(projection)!;
        var prepared = (List<FinalDrawItem>)AccessTools.Field(renderer.GetType(), "preparedItems").GetValue(renderer)!;
        FieldInfo binding = AccessTools.Field(renderer.GetType(), "renderPreparedItems");
        Check(binding.GetValue(renderer) != null && prepared.Count == 0, "native reusable submission binds and returns all prepared items after a successful render");
        ICameraTextureRenderer original = CameraTextureRenderer.Current;
        MethodInfo submit = AccessTools.Method(renderer.GetType(), "SubmitCommands");
        var commands = (List<MeshDrawCommand>)AccessTools.Field(renderer.GetType(), "orderedCommands").GetValue(renderer)!;
        MeshDrawCommand[] saved = commands.ToArray();
        try
        {
            Bootstrap.LegacyMode = true;
            submit.Invoke(renderer, new object[] { original });
            Check(original.TryReadPixels(projection.Texture!, out byte[] standard), "standard submission exposes native comparison pixels");
            Bootstrap.LegacyMode = false;
            submit.Invoke(renderer, new object[] { original });
            Check(original.TryReadPixels(projection.Texture!, out byte[] reused) && standard.AsSpan().SequenceEqual(reused),
                "standard and reusable submission produce byte-identical RGBA pixels for the same captured commands");
        }
        finally { Bootstrap.LegacyMode = false; }
        try
        {
            commands.Clear();
            submit.Invoke(renderer, new object[] { original });
            Check(original.TryReadPixels(projection.Texture!, out byte[] empty) && empty.All(value => value == 0),
                "zero-command captures clear the previous image instead of displaying stale content");
        }
        finally
        {
            commands.Clear();
            commands.AddRange(saved);
        }
        try
        {
            ProjectionBackendProbe wrapper = new(original);
            CameraTextureRenderer.Current = wrapper;
            projection.PanView(Vector2.zero);
            projection.Draw(host!);
            Check(wrapper.Calls > 0 && binding.GetValue(renderer) == null && prepared.Count == 0,
                "alternate renderer without RenderItems retains standard rendering with an explicit warning");
            bool rejected = false;
            try { submit.Invoke(renderer, new object[] { new ThrowingProjectionBackend(original) }); }
            catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException { Message: "CQF expected drawing-buffer fault" }) { rejected = true; }
            Check(rejected && prepared.Count == 0, "submission exceptions propagate and still return prepared items");
        }
        finally
        {
            CameraTextureRenderer.Current = original;
            projection.PanView(Vector2.zero);
            projection.Draw(host!);
        }
        foreach (object? wait in Frames(3)) yield return wait;
        Check(binding.GetValue(renderer) != null && original.TryReadPixels(projection.Texture!, out byte[] restored) && restored.Any(value => value != 0),
            "restoring the native backend rebuilds its binding and resumes visible rendering");
    }
}
