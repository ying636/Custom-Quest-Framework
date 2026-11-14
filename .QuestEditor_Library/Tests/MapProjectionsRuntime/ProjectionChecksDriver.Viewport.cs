using QuestEditor_Library;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CQF.MapProjectionChecks;

public sealed partial class ProjectionChecksDriver
{
    private IEnumerable<object?> CheckViewport()
    {
        Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 25;
        GenSpawn.Spawn(steel, new IntVec3(40, 0, 25), source!);
        Pawn animal = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("Emu"));
        GenSpawn.Spawn(animal, new IntVec3(42, 0, 30), source!);
        guiAction = () =>
        {
            Camera hostCamera = Current.Camera;
            Vector3 cameraPosition = hostCamera.transform.position;
            float cameraSize = hostCamera.orthographicSize;
            FirePlaying(actor!.Position.ToVector3Shifted(), EventType.ScrollWheel, delta: new Vector2(0f, -3f));
            Check(control!.ViewZoom < 1f && hostCamera.transform.position == cameraPosition && hostCamera.orthographicSize == cameraSize,
                "wheel zoom changes only the small viewport and leaves the host camera unchanged");
            Vector2 start = control.ScreenRect(hostCamera).center;
            Rect oldView = control.ViewRect;
            FirePlayingScreen(start, EventType.MouseDown, 2);
            FirePlayingScreen(start + new Vector2(24f, 12f), EventType.MouseDrag, 2);
            FirePlayingScreen(start + new Vector2(24f, 12f), EventType.MouseUp, 2);
            Check(control.ViewRect.center != oldView.center && hostCamera.transform.position == cameraPosition && hostCamera.orthographicSize == cameraSize,
                "middle-button drag pans only the small viewport");
            control.ResetView();
            control.ZoomView(0.65f, actor.Position.ToVector3Shifted());
            ClickPlaying(new Vector3(30.5f, 0f, 20.5f));
            Thing wall = new IntVec3(30, 0, 20).GetThingList(source!).First(thing => thing.def == ThingDefOf.Wall);
            Check(Find.Selector.IsSelected(wall), "gameplay viewport selects source buildings through the native inspector");
            ClickPlaying(steel.DrawPos);
            Check(Find.Selector.IsSelected(steel), "gameplay viewport selects source item stacks");
            ClickPlaying(animal.DrawPos);
            Check(Find.Selector.IsSelected(animal), "gameplay viewport selects source animals");
            ClickPlaying(secondActor!.DrawPos);
            using (new CQFMapProjectionContext(control, hostCamera))
            {
                Command_Toggle draft = secondActor.GetGizmos().OfType<Command_Toggle>().First(command => command.tutorTag is "Draft" or "Undraft");
                bool before = secondActor.drafter.Drafted;
                draft.ProcessInput(new Event { type = EventType.MouseDown, button = 0 });
                Check(secondActor.drafter.Drafted != before && secondActor.Map == source, "native selected-pawn gizmos act on the source pawn");
                draft.ProcessInput(new Event { type = EventType.MouseDown, button = 0 });
                Designator_Build build = new(ThingDefOf.Wall);
                build.SetStuffDef(ThingDefOf.Steel);
                Find.DesignatorManager.Select(build);
            }
            IntVec3 buildCell = new(38, 0, 22);
            ClickPlaying(buildCell.ToVector3Shifted());
            Check(buildCell.GetThingList(source!).OfType<Blueprint_Build>().Any() && !buildCell.GetThingList(host!).OfType<Blueprint_Build>().Any(),
                "native build designator places its blueprint on the source map only");
            using (new CQFMapProjectionContext(control, hostCamera)) Find.DesignatorManager.Select(new Designator_ZoneAddStockpile_Resources());
            FirePlaying(new Vector3(36.5f, 0f, 24.5f), EventType.MouseDown);
            FirePlaying(new Vector3(39.5f, 0f, 26.5f), EventType.MouseDrag);
            FirePlaying(new Vector3(39.5f, 0f, 26.5f), EventType.MouseUp);
            Check(source!.zoneManager.ZoneAt(new IntVec3(37, 0, 25)) is Zone_Stockpile && host!.zoneManager.ZoneAt(new IntVec3(37, 0, 25)) == null,
                "native stockpile drag paints the source map without changing host zones");
            Find.DesignatorManager.Deselect();
            CameraJumper.TryJumpAndSelect(new GlobalTargetInfo(actor));
            Check(Current.Game.currentMapIndex == host!.Index && CQFMapProjectionContext.Focused == control && Find.Selector.IsSelected(actor),
                "native jump-and-select keeps the host map and selects inside the viewport");
            CellRect hostView = Find.CameraDriver.CurrentViewRect;
            using (new CQFMapProjectionContext(control, hostCamera))
                Check(Find.CameraDriver.CurrentViewRect == control.VisibleCells, "viewport culling follows the zoomed source view");
            Check(Find.CameraDriver.CurrentViewRect == hostView, "viewport culling does not overwrite the native host view cache");
            savedViewportZoom = control.ViewZoom;
            savedViewportRect = control.ViewRect;
        };
        runInput = true;
        while (runInput) yield return null;
        if (inputError != null) throw inputError;
        foreach (object? wait in Frames(6)) yield return wait;
        ScreenCapture.CaptureScreenshot(Path.Combine(Bootstrap.Output, "10-PlayingViewport.png"));
        foreach (object? wait in Frames(3)) yield return wait;
    }

    private void ClickPlaying(Vector3 sourcePosition)
    {
        FirePlaying(sourcePosition, EventType.MouseDown);
        FirePlaying(sourcePosition, EventType.MouseUp);
    }

    private void FirePlaying(Vector3 sourcePosition, EventType type, int button = 0, Vector2 delta = default)
    {
        Vector3 point = Current.Camera.WorldToScreenPoint(control!.ToHost(sourcePosition));
        FirePlayingScreen(new Vector2(point.x / Prefs.UIScale, UI.screenHeight - point.y / Prefs.UIScale), type, button, delta);
    }

    private void FirePlayingScreen(Vector2 point, EventType type, int button = 0, Vector2 delta = default)
    {
        Event before = Event.current;
        try
        {
            SetPointer(point, type, button);
            Event.current.delta = delta;
            Find.UIRoot.UIRootOnGUI();
            using (CQFMapProjectionContext? scope = CQFMapProjectionContext.BeginGUI())
                if (Find.DesignatorManager.SelectedDesignator != null) Find.DesignatorManager.Dragger.DraggerUpdate();
            Current.CameraDriver.CameraDriverOnGUI();
            Current.CameraDriver.Update();
        }
        finally { Bootstrap.MousePosition = null; Event.current = before; }
    }

    private float savedViewportZoom;
    private Rect savedViewportRect;
}
