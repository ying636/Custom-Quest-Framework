using System.Reflection;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private void CheckCameraPlus()
    {
        Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 12f);
        Type fastUI = AccessTools.TypeByName("CameraPlus.FastUI")!;
        MethodInfo cellSize = AccessTools.PropertyGetter(fastUI, "CurUICellSize");
        FieldInfo frame = AccessTools.Field(fastUI, "lastUpdateFrameForCurUICellSize");
        Type decisions = AccessTools.TypeByName("CameraPlus.MarkerDecisionCache")!;
        AccessTools.Method(decisions, "Clear").Invoke(null, null);
        frame.SetValue(null, -1);
        CQFMapProjection projection = new()
        {
            id = "CQF_CameraPlusCheck", sourceMap = source, destination = new CellRect(40, 24, 4, 4)
        };
        float expected = UI.CurUICellSize();
        float projected;
        Camera camera = Find.Camera;
        using (new CQFMapProjectionContext(projection, camera)) projected = (float)cellSize.Invoke(null, null)!;
        float restored = (float)cellSize.Invoke(null, null)!;
        File.AppendAllText(Path.Combine(Bootstrap.Output, "CameraPlus.txt"), $"host={expected} projection={projected} restored={restored}\n");
        Check(Mathf.Abs(projected - expected * 4f / source!.Size.x) < 0.01f, "Camera+ observes the four-cell viewport scale inside the projection");
        Check(Mathf.Abs(restored - expected) < 0.01f, "Camera+ restores host scale in the same frame after leaving a projection");
        Type main = AccessTools.TypeByName("CameraPlus.CameraPlusMain")!;
        object settings = AccessTools.Field(main, "Settings").GetValue(null)!;
        FieldInfo style = AccessTools.Field(settings.GetType(), "dotStyle");
        object previousStyle = style.GetValue(settings)!;
        style.SetValue(settings, Enum.Parse(style.FieldType, "ClassicDots"));
        FieldInfo threshold = AccessTools.Field(settings.GetType(), "dotSize");
        int previousThreshold = (int)threshold.GetValue(settings)!;
        threshold.SetValue(settings, 20);
        FieldInfo reveal = AccessTools.Field(settings.GetType(), "mouseOverShowsLabels");
        bool previousReveal = (bool)reveal.GetValue(settings)!;
        reveal.SetValue(settings, false);
        Pawn hostPawn = host!.mapPawns.FreeColonistsSpawned.First();
        MethodInfo render = AccessTools.Method(AccessTools.TypeByName("CameraPlus.DotTools"), "ShouldRenderPawn");
        try
        {
            CheckCameraPlusInsets(projection, camera, settings);
            AccessTools.Method(decisions, "Clear").Invoke(null, null);
            using (new CQFMapProjectionContext(projection, camera))
                Check(!(bool)render.Invoke(null, new object[] { hostPawn })!, "Camera+ switches to distant markers inside a small viewport");
            Check((bool)render.Invoke(null, new object[] { hostPawn })!, "host pawn bodies remain visible after projected marker evaluation");
            using (new CQFMapProjectionContext(projection, camera))
            {
                using (new CQFMapProjectionContext(projection, camera)) { cellSize.Invoke(null, null); }
                Check(Mathf.Abs((float)cellSize.Invoke(null, null)! - projected) < 0.01f, "nested projection scopes retain their own Camera+ scale");
            }
            Check(Mathf.Abs((float)cellSize.Invoke(null, null)! - expected) < 0.01f, "nested scope disposal restores the host Camera+ scale");
        }
        finally
        {
            style.SetValue(settings, previousStyle);
            threshold.SetValue(settings, previousThreshold);
            reveal.SetValue(settings, previousReveal);
            AccessTools.Method(decisions, "Clear").Invoke(null, null);
            projection.Dispose();
            Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 32f);
        }
    }

    private void CheckCameraPlusInsets(CQFMapProjection projection, Camera camera, object settings)
    {
        Type insets = AccessTools.TypeByName("CameraPlus.EdgeUIInsets")!;
        FieldInfo enabled = AccessTools.Field(settings.GetType(), "indentVanillaUIForEdgeMarkers");
        object oldEnabled = enabled.GetValue(settings)!;
        System.Collections.IList markers = (System.Collections.IList)AccessTools.Field(insets, "markers").GetValue(null)!;
        object[] oldMarkers = new object[markers.Count];
        markers.CopyTo(oldMarkers, 0);
        FieldInfo[] counters = new[] { "markerObservationFrame", "leftMarkerCount", "rightMarkerCount", "topMarkerCount", "bottomMarkerCount" }
            .Select(name => AccessTools.Field(insets, name)).ToArray();
        object[] oldCounters = counters.Select(field => field.GetValue(null)!).ToArray();
        MethodInfo observe = AccessTools.Method(insets, "ObserveMarker");
        object edge = Enum.ToObject(observe.GetParameters()[0].ParameterType, 1);
        object[] arguments = { edge, new Rect(0f, UI.screenHeight - 160f, 180f, 100f), new Vector2(180f, 160f) };
        try
        {
            enabled.SetValue(settings, true);
            observe.Invoke(null, arguments);
            int hostCount = markers.Count;
            using (new CQFMapProjectionContext(projection, camera, camera)) observe.Invoke(null, arguments);
            File.AppendAllText(Path.Combine(Bootstrap.Output, "CameraPlus.txt"), "hostEdgeMarkers=" + hostCount + " afterProjection=" + markers.Count + "\n");
            Check(markers.Count == hostCount, "offscreen projection markers do not enter fullscreen Camera+ UI avoidance");
            observe.Invoke(null, arguments);
            Check(markers.Count == hostCount + 1, "real host edge markers still participate in Camera+ UI avoidance");
        }
        finally
        {
            enabled.SetValue(settings, oldEnabled);
            markers.Clear();
            foreach (object marker in oldMarkers) markers.Add(marker);
            for (int i = 0; i < counters.Length; i++) counters[i].SetValue(null, oldCounters[i]);
        }
    }
}
