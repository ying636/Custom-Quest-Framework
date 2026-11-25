#if RIMWORLD_1_7
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class Designator_CQFBackgroundPlacement : Designator
{
    public Designator_CQFBackgroundPlacement(CQFBackgroundPlacementSession session)
    {
        this.session = session;
        this.defaultLabel = "CQF_BackgroundPlacementStart".Translate();
        this.defaultDesc = "CQF_SceneryPickTip".Translate();
        this.icon = TexButton.OpenDebugActionsMenu;
    }

    public override DrawStyleCategoryDef? DrawStyleCategory => this.DrawingCliff ? DrawStyleCategoryDefOf.Areas : null;
    public override bool DragDrawMeasurements => this.DrawingCliff;
    public bool Picking => this.mode == PlacementMode.Pick;
    private bool DrawingCliff => this.mode == PlacementMode.Place && this.session.Cliffs;

    public override bool CanRemainSelected() => this.session.CanEditMap && (this.mode != PlacementMode.Place || this.session.CanPlace);

    public void SwitchMode()
    {
        this.mode = PlacementMode.Place;
        this.cliffBeingMoved = null;
        this.grabOffset = Vector3.zero;
        this.SelectTool();
    }

    public void StartPicking()
    {
        this.mode = PlacementMode.Pick;
        this.cliffBeingMoved = null;
        this.session.CancelMove();
        this.SelectTool();
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => this.CanRemainSelected()
        && (this.DrawingCliff ? loc.InBounds(this.session.Map) : loc.IsValid);

    public override void DesignateSingleCell(IntVec3 loc)
    {
        if (!this.CanDesignateCell(loc).Accepted) return;
        if (this.DrawingCliff)
        {
            this.session.PlaceCliff(new CellRect(loc.x, loc.z, 1, 1));
            return;
        }

        Vector3 point = UI.MouseCell() == loc ? UI.MouseMapPosition() : loc.ToVector3Shifted();
        this.HandleWorldClick(point);
    }

    internal void HandleWorldClick(Vector3 point)
    {
        if (this.Picking)
        {
            this.TryPick(point, true);
            return;
        }
        if (this.mode == PlacementMode.MoveCliff && this.cliffBeingMoved is { } cliff)
        {
            cliff.area = this.GetMovedCliffArea(point);
            this.session.MoveCliff(cliff);
        }
        else
        {
            this.session.PlaceStructure(point + this.grabOffset);
        }
        this.StartPicking();
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        if (!this.CanRemainSelected()) return;
        if (!this.DrawingCliff)
        {
            base.DesignateMultiCell(cells);
            return;
        }
        CellRect selected = this.GetCliffArea(cells);
        if (!selected.IsEmpty) this.session.PlaceCliff(selected);
        this.Finalize(!selected.IsEmpty);
    }

    public override void SelectedProcessInput(Event ev)
    {
        if (ev.type == EventType.KeyDown && this.CanRemainSelected())
        {
            if (ev.keyCode == KeyCode.Escape && (this.session.MovingStructure != null || this.cliffBeingMoved != null))
            {
                this.StartPicking();
                ev.Use();
                return;
            }
            bool rotateLeft = ev.keyCode == KeyCode.Q || KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent;
            bool rotateRight = ev.keyCode == KeyCode.E || KeyBindingDefOf.Designator_RotateRight.KeyDownEvent;
            if ((rotateLeft || rotateRight) && !this.Picking && !this.DrawingCliff)
            {
                this.Rotate(rotateLeft ? -90f : 90f);
                ev.Use();
                return;
            }
        }
        base.SelectedProcessInput(ev);
    }

    public override void SelectedUpdate()
    {
        base.SelectedUpdate();
        if (!this.CanRemainSelected() || MouseOverWindow()) return;
        Vector3 point = UI.MouseMapPosition();
        if (!this.Picking) this.DrawPlacementGhost(point);
    }

    public override void Deselected()
    {
        this.session.CancelMove();
        this.cliffBeingMoved = null;
        this.ghostLayer?.Dispose();
        this.ghostLayer = null;
        this.ghostStructure = null;
        this.ghostCliff = null;
        this.dragCell = IntVec3.Invalid;
        base.Deselected();
    }

    internal bool TryPickForEditing()
    {
        return this.CanRemainSelected() && !MouseOverWindow() && this.TryPick(UI.MouseMapPosition(), false);
    }

    internal bool TryPick(Vector3 point, bool move)
    {
        if (!this.session.CanEditMap) return false;
        this.ghostLayer ??= new MapDrawLayer_CQFBackgroundStructures(this.session.Map, true);
        var (structure, cliff) = this.ghostLayer.PickElement(this.session.Draft, point);
        if (structure == null && cliff == null) return false;
        this.cliffBeingMoved = null;
        if (structure != null)
        {
            this.session.Select(structure);
            if (move)
            {
                this.mode = PlacementMode.Place;
                this.grabOffset = structure.Center - point;
                this.session.StartMove();
            }
        }
        else if (cliff != null)
        {
            this.session.SelectCliff(cliff);
            if (move)
            {
                this.mode = PlacementMode.MoveCliff;
                this.cliffBeingMoved = cliff.Copy();
                this.grabOffset = (cliff.area.CenterCell - CellAt(point)).ToVector3();
            }
        }
        if (!move) this.mode = PlacementMode.Pick;
        this.SelectTool();
        this.ElementPicked?.Invoke();
        return true;
    }

    private void SelectTool()
    {
        if (!this.CanRemainSelected()) return;
        if (Find.DesignatorManager.SelectedDesignator != this)
        {
            Find.DesignatorManager.Select(this);
            return;
        }
        Find.DesignatorManager.Dragger.EndDrag();
        Find.DesignatorManager.SelectedStyle = this.DrawStyleCategory?.styles.FirstOrDefault();
    }

    private void Rotate(float degrees)
    {
        if (this.cliffBeingMoved is not { } cliff)
        {
            this.session.Rotate(degrees);
            return;
        }

        CellRect current = cliff.area;
        cliff.area = new CellRect(current.minX, current.minZ, current.Height, current.Width);
        int flags = (int)cliff.edges;
        int shift = degrees > 0f ? 1 : 3;
        cliff.edges = (CQFCliffEdges)(((flags << shift) | (flags >> (4 - shift))) & 15);
    }

    private void DrawPlacementGhost(Vector3 point)
    {
        if (this.DrawingCliff || this.cliffBeingMoved != null)
        {
            CellRect area = this.cliffBeingMoved != null ? this.GetMovedCliffArea(point) : this.GetCliffGhostArea(point);
            CQFBackgroundCliff? moving = this.cliffBeingMoved;
            float depth = moving?.depth ?? this.session.CliffDepth;
            var edges = moving?.edges ?? this.session.CliffEdges;
            Color color = (moving ?? this.session.CliffSelection)?.color ?? Color.white;
            if (area.IsEmpty || edges == CQFCliffEdges.None) return;
            this.ghostLayer ??= new MapDrawLayer_CQFBackgroundStructures(this.session.Map, true);
            var key = (area, depth, edges, color);
            if (this.ghostCliff != key)
            {
                this.ghostLayer.RebuildGhost(null, new CQFBackgroundCliff { area = area, depth = depth, edges = edges, color = color });
                this.ghostCliff = key;
                this.ghostStructure = null;
            }
            this.ghostLayer.DrawGhostAt(Vector3.zero);
        }
        else if ((this.session.MovingStructure ?? this.session.Pending) is { } structure)
        {
            this.ghostLayer ??= new MapDrawLayer_CQFBackgroundStructures(this.session.Map, true);
            var key = (structure.def, structure.rotation, structure.scale, structure.depth);
            if (this.ghostStructure != key)
            {
                this.ghostLayer.RebuildGhost(structure, null);
                this.ghostStructure = key;
                this.ghostCliff = null;
            }
            this.ghostLayer.DrawGhostAt(point + this.grabOffset);
        }
    }

    private CellRect GetCliffGhostArea(Vector3 point)
    {
        IntVec3 current = CellAt(point);
        var manager = Find.DesignatorManager;
        if (!manager.Dragger.Dragging)
        {
            this.dragCell = IntVec3.Invalid;
            return current.InBounds(this.session.Map) ? new CellRect(current.x, current.z, 1, 1) : CellRect.Empty;
        }
        if (this.dragCell != current || this.dragStyle != manager.SelectedStyle)
        {
            this.dragCell = current;
            this.dragStyle = manager.SelectedStyle;
            this.dragArea = this.GetCliffArea(manager.Dragger.DragCells);
        }
        return this.dragArea;
    }

    private CellRect GetMovedCliffArea(Vector3 point)
    {
        CellRect source = this.cliffBeingMoved!.area;
        int width = Mathf.Min(source.Width, this.session.Map.Size.x);
        int height = Mathf.Min(source.Height, this.session.Map.Size.z);
        IntVec3 center = CellAt(point + this.grabOffset);
        int x = Mathf.Clamp(center.x - width / 2, 0, this.session.Map.Size.x - width);
        int z = Mathf.Clamp(center.z - height / 2, 0, this.session.Map.Size.z - height);
        return new CellRect(x, z, width, height);
    }

    private CellRect GetCliffArea(IEnumerable<IntVec3> cells)
    {
        int minX = int.MaxValue;
        int minZ = int.MaxValue;
        int maxX = int.MinValue;
        int maxZ = int.MinValue;
        foreach (IntVec3 cell in cells)
        {
            if (!cell.InBounds(this.session.Map)) continue;
            minX = Math.Min(minX, cell.x);
            minZ = Math.Min(minZ, cell.z);
            maxX = Math.Max(maxX, cell.x);
            maxZ = Math.Max(maxZ, cell.z);
        }
        return minX <= maxX ? CellRect.FromLimits(minX, minZ, maxX, maxZ) : CellRect.Empty;
    }

    private static IntVec3 CellAt(Vector3 point) => new IntVec3(Mathf.FloorToInt(point.x), 0, Mathf.FloorToInt(point.z));

    private static bool MouseOverWindow()
    {
        Vector2 mouse = UI.MousePositionOnUIInverted;
        return mouse.y >= UI.screenHeight - MainButtonsHeight
            || Find.WindowStack.Windows.Any(window => window.windowRect.Contains(mouse));
    }

    public event Action? ElementPicked;

    private enum PlacementMode { Place, Pick, MoveCliff }

    private const float MainButtonsHeight = 35f;
    private readonly CQFBackgroundPlacementSession session;
    private PlacementMode mode;
    private CQFBackgroundCliff? cliffBeingMoved;
    private Vector3 grabOffset;
    private MapDrawLayer_CQFBackgroundStructures? ghostLayer;
    private (CQFBackgroundStructureDef Def, float Rotation, float Scale, float Depth)? ghostStructure;
    private (CellRect Area, float Depth, CQFCliffEdges Edges, Color Color)? ghostCliff;
    private IntVec3 dragCell = IntVec3.Invalid;
    private DrawStyleDef? dragStyle;
    private CellRect dragArea;
}
#endif
