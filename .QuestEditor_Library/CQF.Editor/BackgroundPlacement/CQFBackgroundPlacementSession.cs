#if RIMWORLD_1_7
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFBackgroundPlacementSession
{
    public CQFBackgroundPlacementSession(Map map)
    {
        this.Map = map;
        this.component = MapComponent_CustomMapData.GetComp(map);
        this.Draft = this.component.background?.Copy() ?? new CustomMapBackgroundData();
        this.component.SetBackgroundPreview(this.Draft);
    }

    public Map Map { get; }
    public CustomMapBackgroundData Draft { get; private set; }
    public bool Finished { get; private set; }
    public bool CanEditMap => !this.Finished && !this.Map.Disposed && Find.CurrentMap == this.Map && WorldRendererUtility.DrawingMap;
    public bool CanUndo => this.undo.Count > 0;
    public bool CanPlace => this.Cliffs || this.Pending != null || this.MovingStructure != null;
    public CQFBackgroundStructure? Selection { get; private set; }
    public CQFBackgroundStructure? Pending { get; private set; }
    public CQFBackgroundStructure? MovingStructure { get; private set; }
    public CQFBackgroundCliff? CliffSelection { get; private set; }
    public bool Cliffs { get; private set; } = true;
    public CQFCliffEdges CliffEdges { get; private set; } = CQFCliffEdges.All;
    public float CliffDepth { get; private set; } = 5f;

    public void Change(Action<CustomMapBackgroundData> change)
    {
        if (!this.CanEditMap) return;
        bool terrainEdges = this.Draft.enableTerrainEdges;
        this.Remember();
        change(this.Draft);
        if (terrainEdges != this.Draft.enableTerrainEdges) this.Map.mapDrawer.RegenerateEverythingNow();
        else this.component.RefreshBackgroundDrawing();
    }

    public void Undo()
    {
        if (!this.CanUndo || this.Finished) return;
        this.Draft = this.undo[this.undo.Count - 1];
        this.undo.RemoveAt(this.undo.Count - 1);
        this.Selection = null;
        this.CliffSelection = null;
        this.MovingStructure = null;
        this.component.SetBackgroundPreview(this.Draft);
    }

    public void ChooseCliffs()
    {
        this.Cliffs = true;
        this.Selection = null;
        this.Pending = null;
        this.CliffSelection = null;
        this.MovingStructure = null;
    }

    public void SelectCliff(CQFBackgroundCliff item)
    {
        this.ChooseCliffs();
        this.CliffSelection = item;
        this.CliffEdges = item.edges;
        this.CliffDepth = item.depth;
    }

    public void ChooseStructure(CQFBackgroundStructureDef definition)
    {
        this.Cliffs = false;
        this.Selection = null;
        this.CliffSelection = null;
        this.MovingStructure = null;
        this.Pending = new CQFBackgroundStructure { def = definition };
    }

    public void Select(CQFBackgroundStructure item)
    {
        this.Cliffs = false;
        this.Selection = item;
        this.CliffSelection = null;
        this.Pending = null;
        this.MovingStructure = null;
    }

    public void StartMove()
    {
        this.MovingStructure = this.Selection?.Copy();
    }

    public void CancelMove() => this.MovingStructure = null;

    public void Duplicate()
    {
        if (this.Selection == null) return;
        this.Pending = this.Selection.Copy();
        this.Selection = null;
        this.MovingStructure = null;
    }

    public void DeleteSelection()
    {
        if (this.Selection is not { } item) return;
        this.Change(data => data.structures.Remove(item));
        this.Selection = null;
        this.MovingStructure = null;
    }

    public void ToggleEdge(CQFCliffEdges edge)
    {
        this.CliffEdges ^= edge;
        if (this.CliffSelection is { } item) this.Change(_ => item.edges = this.CliffEdges);
    }

    public void SetCliffDepth(float value)
    {
        this.CliffDepth = value;
        if (this.CliffSelection is { } item) this.Change(_ => item.depth = value);
    }

    public void PlaceCliff(CellRect area)
    {
        if (this.CliffSelection is { } selected)
        {
            this.Change(_ => selected.area = area);
            return;
        }
        if (this.CliffEdges == CQFCliffEdges.None) return;
        var item = new CQFBackgroundCliff { area = area, edges = this.CliffEdges, depth = this.CliffDepth };
        this.Change(data => data.cliffs.Add(item));
    }

    public void RemoveCliff()
    {
        if (this.CliffSelection is not { } item) return;
        this.Change(data => data.cliffs.Remove(item));
        this.CliffSelection = null;
    }

    public void MoveCliff(CQFBackgroundCliff moved)
    {
        if (this.CliffSelection is not { } selected || (selected.area.Equals(moved.area) && selected.edges == moved.edges)) return;
        this.Change(_ =>
        {
            selected.area = moved.area;
            selected.edges = moved.edges;
        });
        this.CliffEdges = moved.edges;
    }

    public void PlaceStructure(IntVec3 cell) => this.PlaceStructure(cell.ToVector3Shifted());

    public void PlaceStructure(Vector3 center)
    {
        if (this.MovingStructure is { } moving && this.Selection is { } item)
        {
            this.Change(_ =>
            {
                item.SetCenter(center);
                item.rotation = moving.rotation;
                item.scale = moving.scale;
                item.depth = moving.depth;
            });
            this.MovingStructure = null;
            return;
        }
        if (this.Pending == null) return;
        CQFBackgroundStructure place = this.Pending.Copy();
        place.SetCenter(center);
        this.Change(data => data.structures.Add(place));
        this.Select(place);
    }

    public void AlterStructure(Action<CQFBackgroundStructure> edit)
    {
        if (this.MovingStructure is { } moving) edit(moving);
        else if (this.Selection is { } item) this.Change(_ => edit(item));
        else if (this.Pending != null) edit(this.Pending);
    }

    public void Rotate(float degrees) => this.AlterStructure(item => item.rotation = Mathf.Repeat(item.rotation + degrees, 360f));

    public void Apply()
    {
        if (!this.CanEditMap) return;
        this.component.background = this.Draft.Copy();
        this.undo.Clear();
    }

    public void Finish(bool apply)
    {
        if (this.Finished) return;
        if (apply) this.Apply();
        this.Finished = true;
        if (this.Map.Disposed) return;
        this.component.SetBackgroundPreview(null);
    }

    private void Remember()
    {
        if (this.undo.Count >= 48) this.undo.RemoveAt(0);
        this.undo.Add(this.Draft.Copy());
    }

    private readonly MapComponent_CustomMapData component;
    private readonly List<CustomMapBackgroundData> undo = new();
}
#endif
