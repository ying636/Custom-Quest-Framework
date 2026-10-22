using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class DomainWorker_Visual : DomainWorker
{
    public override bool SupportsContests => true;

#if RIMWORLD_1_7
    public override bool HandlesEntryView => CQFDomainRuntime.Instance.Transition != null;
    protected virtual Type TerrainLayerType => typeof(SectionLayer_Terrain);

    public override bool PrepareReturn(bool visible)
    {
        if (returnTransition != null && ReferenceEquals(CQFDomainRuntime.Instance.Transition, returnTransition)
            && !returnTransition.Finished)
            return returnTransition.ReturnCovered;
        BeforeReturn(visible);
        if (returnTransition == null) return true;
        returnTransition.BeginReturn(Domain);
        return false;
    }

    public override void Update()
    {
        if (Find.CurrentMap != map || !WorldRendererUtility.DrawingMap) return;
        if (CQFDomainRuntime.Instance.Transition == null) return;
        CellRect view = Find.CameraDriver.CurrentViewRect;
        view.ClipInsideMap(map);
        if (view.IsEmpty) return;
        SectionLayer? terrain = map.mapDrawer.SectionAt(view.CenterCell).GetLayer(TerrainLayerType);
        if (terrain?.Visible != true) return;
        terrainCommands.Clear();
        try
        {
            Waylith.Rendering.MeshDrawSubmission.CopyPendingCommands(terrainCommands, Find.Camera.cullingMask);
            foreach (LayerSubMesh subMesh in terrain.subMeshes)
                if (subMesh.finalized && !subMesh.disabled && subMesh.mesh.vertexCount > 0)
                    foreach (Waylith.Rendering.MeshDrawCommand command in terrainCommands)
                        if (ReferenceEquals(command.Mesh, subMesh.mesh.EngineMesh))
                        {
                            LastTerrainRenderedFrame = Time.frameCount;
                            return;
                        }
        }
        finally { terrainCommands.Clear(); }
    }
#endif

    public override void BeforeReturn(bool visible)
    {
        returnTransition = null;
        if (!visible || Domain.Definition.visuals == null || !WorldRendererUtility.DrawingMap || Screen.width <= 0 || Screen.height <= 0) return;
        CQFDomainRuntime manager = CQFDomainRuntime.Instance;
        manager.SetTransition(null);
        try
        {
            returnTransition = new DomainMaterialTransition(Domain.Definition.visuals, caster?.PositionHeld ?? map.Center, true);
            manager.SetTransition(returnTransition);
        }
        catch (Exception error) { Log.Error("[CQF] Could not capture basic domain return transition: " + error); }
    }

    public override void Returned(Map destination, IntVec3 focus, IReadOnlyList<Pawn> pawns)
    {
        if (returnTransition != null && ReferenceEquals(CQFDomainRuntime.Instance.Transition, returnTransition))
            returnTransition.BindReturn(destination, focus, pawns);
        returnTransition = null;
    }

    public override void OnGUI() => DomainHealthBar.Draw(Domain);

#if RIMWORLD_1_7
    private readonly List<Waylith.Rendering.MeshDrawCommand> terrainCommands = new();
#endif
    private DomainMaterialTransition? returnTransition;
}
