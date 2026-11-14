using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private IEnumerable<object?> CheckReadoutStability()
    {
        Find.Selector.ClearSelection();
        Find.MainTabsRoot.SetCurrentTab(null, playSound: false);
        CQFMapProjectionContext.Focus(host!, control);
        Vector3 point = Current.Camera.WorldToScreenPoint(control!.ToHost(new Vector3(40.5f, 0f, 25.5f)));
        Bootstrap.HoverPosition = point;
        Bootstrap.ReadoutOffsets.Clear();
        string directory = Path.Combine(Bootstrap.Output, "HoverReadout");
        Directory.CreateDirectory(directory);
        try
        {
            foreach (object? wait in Frames(120)) yield return wait;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "01-Settled.png"));
            foreach (object? wait in Frames(30)) yield return wait;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "02-Still.png"));
            foreach (object? wait in Frames(3)) yield return wait;
            List<Vector2> tail = Bootstrap.ReadoutOffsets.TakeLast(25).ToList();
            Check(tail.Count == 25 && tail.All(offset => (offset - tail[0]).sqrMagnitude < 0.01f),
                "stationary source hover readout keeps the same screen position across 25 repaint samples");
            File.WriteAllLines(Path.Combine(directory, "Offsets.txt"), Bootstrap.ReadoutOffsets.Select(offset => offset.ToString()));
        }
        finally { Bootstrap.HoverPosition = null; }
    }
}
