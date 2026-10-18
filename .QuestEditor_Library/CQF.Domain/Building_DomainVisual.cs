using UnityEngine;
using Verse;
using Verse.AI;

namespace QuestEditor_Library;

public class Building_DomainVisual : Building_Domain, IAttackTarget
{
    Thing IAttackTarget.Thing => this;
    public LocalTargetInfo TargetCurrentlyAimingAt => LocalTargetInfo.Invalid;
    public float TargetPriorityFactor => 1f;

    public override CellRect? CustomRectForSelector
    {
        get
        {
            float radius = Domain?.Definition.visuals?.radius ?? 12f;
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                Vector3 offset = UI.MouseMapPosition() - Position.ToVector3Shifted();
                float height = Domain?.Definition.visuals?.curtainHeight ?? 4.5f;
                float verticalRadius = offset.z > 0f ? Mathf.Sqrt(radius * radius + height * height) : radius;
                if (offset.x * offset.x / (radius * radius) + offset.z * offset.z / (verticalRadius * verticalRadius) > 1f)
                    return null;
            }
            int extent = Mathf.CeilToInt(radius);
            return new CellRect(Position.x - extent, Position.z - extent, extent * 2 + 1, extent * 2 + 2);
        }
    }

    public bool ThreatDisabled(IAttackTargetSearcher disabledFor) => !Spawned || Domain?.Active != true;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        if (Domain is not { Active: true } domain) yield break;
        if (healthGizmo?.Domain != domain) healthGizmo = new Gizmo_DomainHealth(domain);
        yield return healthGizmo;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        visuals?.Dispose();
        visuals = null;
        base.DeSpawn(mode);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (Domain?.Active != true || visualError) return;
        try
        {
            DomainVisualDef definition = Domain.Definition.visuals
                ?? throw new InvalidOperationException("[CQF] Visual domain anchor requires domain visuals.");
            visuals ??= new DomainOpeningVisuals(definition);
            visuals.Draw(drawLoc, 1f, GenTicks.TicksGame / 60f);
        }
        catch (Exception error)
        {
            visualError = true;
            visuals?.Dispose();
            visuals = null;
            Log.Error("[CQF] Domain anchor visuals failed: " + error);
        }
    }

    private DomainOpeningVisuals? visuals;
    private Gizmo_DomainHealth? healthGizmo;
    private bool visualError;
}
