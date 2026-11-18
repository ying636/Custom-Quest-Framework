using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private IEnumerable<object?> CheckDraftSizing()
    {
        GameComponent_CQFProjectionUI ui = GameComponent_CQFProjectionUI.Instance!;
        Map smallHost = source!, largeSource = host!;
        MapComponent_CQFMapProjections component = smallHost.GetComponent<MapComponent_CQFMapProjections>();
        int count = component.Projections.Count;
        CQFMapProjectionContext.ReturnToHost();
        Current.Game.CurrentMap = smallHost;
        Find.CameraDriver.SetRootPosAndSize(new Vector3(smallHost.Size.x / 2f, 0f, smallHost.Size.z / 2f), 30f);
        try
        {
            ui.BeginEdit(smallHost, source: largeSource);
            CQFMapProjectionEditSession session = ui.Session!;
            session.SetSourceArea(new CellRect(20, 12, 20, 10));
            session.Placement.BeginPlace(new IntVec3(10, 0, 10));
            session.Placement.UpdatePointer(new IntVec3(29, 0, 19));
            session.Placement.EndGesture();
            session.SetMatchCellSize(true);
            foreach (object? wait in Frames(4)) yield return wait;
            Check(session.Placed && session.Draft.Texture != null, "small-host draft renders a valid one-to-one source crop");
            guiAction = () =>
            {
                FireEditor(session, session.Canvas.ScreenRect.center, EventType.MouseDown);
                Check(session.Canvas.IsDragging, "native source-crop gesture starts before a whole-map switch");
                session.SetSource(largeSource);
                Check(!session.Canvas.IsDragging, "switching the source ends its previous native crop gesture");
                FireEditor(session, session.Canvas.ScreenRect.center, EventType.MouseUp);
            };
            runInput = true;
            while (runInput) yield return null;
            if (inputError != null) throw inputError;
            foreach (object? wait in Frames(4)) yield return wait;
            Check(!session.Placed && !session.Placement.IsDragging && session.Draft.Texture == null,
                "switching a placed one-to-one crop to an oversized whole map leaves a non-rendering draft");
            Check(session.Draft.matchCellSize && session.Draft.sourceRect.IsEmpty && !session.Commit() && component.Projections.Count == count,
                "oversized whole-map drafts preserve one-to-one sizing and cannot alter live projections");
            string directory = Path.Combine(Bootstrap.Output, "DraftSizing");
            Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "01-Oversized.png"));
            foreach (object? wait in Frames(3)) yield return wait;
            session.SetSourceArea(new CellRect(20, 12, 16, 8));
            session.Placement.BeginPlace(new IntVec3(smallHost.Size.x - 1, 0, smallHost.Size.z - 1));
            session.Placement.EndGesture();
            session.Draft.Validate(smallHost);
            Check(session.Placed && session.Draft.DestinationBounds.Max.InBounds(smallHost),
                "reducing an oversized crop allows fixed-size placement clamped to the host edge");
            session.Placement.BeginMove(session.Draft.DestinationBounds.Min);
            session.SetSourceArea(new CellRect(0, 0, smallHost.Size.x, smallHost.Size.z));
            session.Placement.CancelGesture();
            session.Draft.Validate(smallHost);
            Check(!session.Placement.IsDragging && session.Draft.DestinationBounds == CellRect.WholeMap(smallHost),
                "changing crop size cancels stale movement before deriving an exact-fit footprint");
            session.SetSourceArea(new CellRect(0, 0, smallHost.Size.x + 1, 10));
            foreach (object? wait in Frames(2)) yield return wait;
            Check(!session.Placed && session.Draft.Texture == null, "width-only oversized crops remain editable without invalid host rendering");
            session.SetSourceArea(new CellRect(0, 0, 10, smallHost.Size.z + 1));
            foreach (object? wait in Frames(2)) yield return wait;
            Check(!session.Placed && session.Draft.Texture == null, "height-only oversized crops remain editable without invalid host rendering");
            session.SetMatchCellSize(false);
            session.Placement.BeginPlace(new IntVec3(10, 0, 10));
            session.Placement.UpdatePointer(new IntVec3(29, 0, 29));
            session.Placement.EndGesture();
            session.SetSource(largeSource);
            session.SetMatchCellSize(true);
            foreach (object? wait in Frames(3)) yield return wait;
            Check(!session.Placed && session.Draft.matchCellSize && session.Draft.Texture == null,
                "enabling one-to-one sizing on an oversized source withdraws the placed preview");
            session.SetSourceArea(new CellRect(20, 12, 16, 8));
            session.Placement.BeginPlace(new IntVec3(10, 0, 10));
            session.Placement.EndGesture();
            foreach (object? wait in Frames(4)) yield return wait;
            Check(session.Draft.Texture != null && session.Commit(), "an oversized draft can be cropped, rendered, and committed without restarting the editor");
            CQFMapProjection committed = component.Projections.Single(projection => projection.sourceMap == largeSource);
            CellRect committedArea = committed.sourceRect, committedDestination = committed.DestinationBounds;
            CQFMapProjectionContext.ReturnToHost();
            ui.BeginEdit(smallHost, committed);
            ui.Session!.SetSource(largeSource);
            foreach (object? wait in Frames(3)) yield return wait;
            ui.Session!.Cancel();
            Check(component.Projections.Contains(committed) && committed.sourceRect == committedArea && committed.DestinationBounds == committedDestination,
                "canceling an oversized edit preserves the original live projection");
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "02-Recovered.png"));
            foreach (object? wait in Frames(3)) yield return wait;
            component.Remove(committed.id);
        }
        finally
        {
            ui.Session?.Cancel();
            CQFMapProjectionContext.ReturnToHost();
            Current.Game.CurrentMap = largeSource;
            Find.CameraDriver.SetRootPosAndSize(new Vector3(44f, 0f, 34f), 32f);
        }
    }
}
