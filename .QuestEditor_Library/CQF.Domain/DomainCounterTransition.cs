using UnityEngine;
using Verse;

namespace QuestEditor_Library;

#if RIMWORLD_1_7
public sealed class DomainCounterTransition : DomainTransition
{
    public DomainCounterTransition(DomainOpening opening) : base(false)
    {
        incoming = opening.Definition;
        defending = opening.Map.GetComponent<MapComponent_Domain>().Definition;
        battlefield = opening.Map;
        origin = opening.Position;
        initialRadius = opening.Radius;
    }

    public override float Duration => 1.4f;
    protected override float CoverProgress => 0.04f;
    protected override float RevealProgress => 0.15f;
    protected override bool ReadyToCommit => lastFieldFrame >= 0;

    public void DrawField(Map map)
    {
        if (!prepared || map != battlefield || Find.CurrentMap != map || Finished) return;
        float progress = Progress;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RevealProgress, 0.82f, progress));
        float opacity = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1f, progress));
        DomainTerritoryPartition? partition = map.GetComponent<MapComponent_Domain>().Contest?.Partition;
        float time = surfaceTime + progress * Duration;
        Vector3 center = new(map.Size.x * 0.5f, AltitudeLayer.LayingPawn.AltitudeFor() - 0.01f, map.Size.z * 0.5f);
        Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(map.Size.x, 1f, map.Size.z));
        defendingSurface?.Draw(MeshPool.plane10, matrix, time, 2901, opacity: opacity);
        incomingSurface?.Draw(MeshPool.plane10, matrix, time + 6f * (1f - blend),
            new Vector4(origin.x + 0.5f, origin.z + 0.5f, initialRadius, 0f),
            new Vector4(partition?.BoundaryX ?? map.Size.x * 0.5f, partition?.Reversed == true ? 1f : -1f, 0f, 0f),
            opacity, new Vector4(blend, 0f, 0f, 4f));
        lastFieldFrame = Time.frameCount;
    }

    protected override void PreparePresentation()
    {
        if (defending.surface != null) defendingSurface = new DomainSurfaceVisuals(defending.surface);
        if (incoming.surface != null) incomingSurface = new DomainSurfaceVisuals(incoming.surface);
        surfaceTime = GenTicks.TicksGame / 60f;
        prepared = true;
    }

    protected override void DrawEffect(float progress) { }

    protected override void ReleaseVisuals()
    {
        defendingSurface?.Dispose();
        incomingSurface?.Dispose();
        defendingSurface = incomingSurface = null;
        prepared = false;
    }

    private readonly DomainDef incoming;
    private readonly DomainDef defending;
    private readonly Map battlefield;
    private readonly IntVec3 origin;
    private readonly float initialRadius;
    private DomainSurfaceVisuals? defendingSurface;
    private DomainSurfaceVisuals? incomingSurface;
    private float surfaceTime;
    private bool prepared;
    private int lastFieldFrame = -1;
}
#endif
