using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private IEnumerable<object?> CheckProjectionConfiguration()
    {
        guiAction = () =>
        {
            control!.allowZoom = false;
            float zoom = control.ViewZoom;
            Vector3 camera = Current.Camera.transform.position;
            FirePlaying(actor!.DrawPos, EventType.ScrollWheel, delta: new Vector2(0f, -4f));
            control.ZoomView(0.5f, actor.DrawPos);
            Check(control.ViewZoom == zoom && Current.Camera.transform.position == camera, "disabled zoom blocks native wheel and public zoom API without moving the host");
            Vector2 start = control.ScreenRect(Current.Camera).center;
            Rect before = control.ViewRect;
            FirePlayingScreen(start, EventType.MouseDown, 2);
            FirePlayingScreen(start + new Vector2(12f, 6f), EventType.MouseDrag, 2);
            FirePlayingScreen(start + new Vector2(12f, 6f), EventType.MouseUp, 2);
            Check(control.ViewRect.center != before.center, "zoom-disabled viewports retain independent middle-button panning");
            control.allowZoom = true;
            control.ResetView();
            CellRect oldDestination = control.destination;
            control.destination = new CellRect(oldDestination.minX, oldDestination.minZ, 4, 4);
            control.matchCellSize = true;
            ClickPlaying(actor.DrawPos);
            FirePlaying(actor.DrawPos, EventType.ScrollWheel, delta: new Vector2(0f, -4f));
            Check(Find.Selector.IsSelected(actor) && control.ViewZoom == 1f && control.DestinationBounds == oldDestination,
                "one-to-one gameplay selects source pawns and locks wheel zoom with a derived host footprint");
            control.matchCellSize = false;
            control.destination = oldDestination;
            savedViewportZoom = control.ViewZoom;
            savedViewportRect = control.ViewRect;
            List<System.Reflection.MethodBase> methods = Harmony.GetAllPatchedMethods().Where(method =>
            {
                Patches? patches = Harmony.GetPatchInfo(method);
                return patches != null && patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Finalizers)
                    .Any(patch => patch.PatchMethod.DeclaringType?.Name.StartsWith("Patch_CQFProjection", StringComparison.Ordinal) == true);
            }).ToList();
            int expected = AccessTools.TypeByName("CameraPlus.EdgeUIInsets") == null ? 16 : 17;
            Check(methods.Count == expected && !methods.Any(method => method.DeclaringType == typeof(UI) && method.Name == "UIToMapPosition"),
                "projection runtime registers the reduced patch set and uses native screen-to-map conversion");
            SetPointer(new Vector2(20f, UI.screenHeight - 20f), EventType.Repaint);
            using (new CQFMapProjectionContext(control, Current.Camera))
                Check(UI.MouseCell() == IntVec3.Invalid && !UI.MouseMapPosition().InBounds(source!), "native camera conversion preserves both distinct mouse boundary guards");
            Bootstrap.MousePosition = null;
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        MapComponent_CQFMapProjections component = host!.GetComponent<MapComponent_CQFMapProjections>();
        CQFMapProjection cells = new() { id = "CQF_CellSizing", sourceMap = source, sourceRect = new CellRect(20, 12, 20, 10),
            destination = new CellRect(46, 22, 4, 4), matchCellSize = true, allowZoom = true };
        cells.Validate(host);
        Check(cells.DestinationBounds == new CellRect(46, 22, 20, 10) && !cells.CanZoom, "one-to-one configuration derives host dimensions from the source and overrides zoom permission");
        Vector3 origin = cells.ToHost(new Vector3(22.5f, 0f, 14.5f));
        Check(cells.ToHost(new Vector3(23.5f, 0f, 14.5f)) - origin == Vector3.right
            && cells.ToHost(new Vector3(22.5f, 0f, 15.5f)) - origin == Vector3.forward, "one source cell occupies exactly one host cell on both axes");
        cells.ZoomView(0.4f, new Vector3(25f, 0f, 15f));
        Check(cells.ViewZoom == 1f && cells.Copy().matchCellSize && cells.Copy().allowZoom, "one-to-one views resist programmatic zoom and copy both authoring settings");
        CQFAction_SetMapProjection action = new() { id = "CQF_CellSizingAction", sourceKey = "CQF_Source", sourceRect = cells.sourceRect,
            destination = cells.destination, allowZoom = false, matchCellSize = true };
        CQFAction_SetMapProjection copy = (CQFAction_SetMapProjection)action.Copy();
        Check(!copy.allowZoom && copy.matchCellSize, "submod action XML round trip preserves both projection settings");
        Dictionary<string, TargetInfo> targets = new() { ["Position"] = new TargetInfo(host.Center, host), ["CQF_Source"] = new TargetInfo(source!.Center, source) };
        copy.Work(targets, null!);
        CQFMapProjection saved = component.Projections.Single(projection => projection.id == action.id);
        Check(saved.DestinationBounds == cells.DestinationBounds && !saved.CanZoom, "submod action applies one-to-one sizing through the shared map component API");
        copy.destination = new CellRect(host.Size.x - 1, host.Size.z - 1, 1, 1);
        bool rejected = false;
        try { copy.Work(targets, null!); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && component.Projections.Contains(saved), "out-of-bounds one-to-one actions preserve the existing projection");
        component.Remove(saved.id);
        GameComponent_CQFProjectionUI ui = GameComponent_CQFProjectionUI.Instance!;
        ui.BeginEdit(host, source: source);
        CQFMapProjectionEditSession session = ui.Session!;
        session.SetSourceArea(new CellRect(20, 12, 20, 10));
        foreach (object? wait in Frames(3)) yield return wait;
        guiAction = () =>
        {
            session.SetMatchCellSize(true);
            Check(session.Draft.matchCellSize && !session.Draft.CanZoom, "editor changes one-to-one sizing through the shared public authoring configuration");
            session.Placement.BeginPlace(new IntVec3(46, 0, 22));
            session.Placement.UpdatePointer(new IntVec3(50, 0, 26));
            session.Placement.EndGesture();
            Check(session.Draft.DestinationBounds == new CellRect(50, 26, 20, 10), "one-to-one placement moves its fixed cell footprint without stretching");
            session.Placement.BeginResize(0, session.Draft.DestinationBounds.Min);
            Check(!session.Placement.IsDragging, "one-to-one window corners cannot resize the source cell scale");
            session.SetSourceArea(new CellRect(20, 12, 12, 6));
            Check(session.Draft.DestinationBounds.Width == 12 && session.Draft.DestinationBounds.Height == 6, "changing a one-to-one source selection updates its host footprint");
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        string directory = Path.Combine(Bootstrap.Output, "Configuration");
        Directory.CreateDirectory(directory);
        foreach (object? wait in Frames(4)) yield return wait;
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, "CellSettings.png"));
        foreach (object? wait in Frames(3)) yield return wait;
        session.Cancel();
        cells.Dispose();
    }
}
