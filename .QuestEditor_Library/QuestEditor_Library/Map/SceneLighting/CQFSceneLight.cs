#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFSceneLight : IExposable
{
    internal (Vector3 Position, Vector3 End, CQFSceneLightKind Kind, float Range, bool CastsShadows) ShadowGeometry =>
        (this.position, this.end, this.kind, this.range, this.castsShadows);

    public CQFSceneLight Copy() => (CQFSceneLight)this.MemberwiseClone();

    public void CopyTo(CQFSceneLight target)
    {
        target.kind = this.kind; target.position = this.position; target.end = this.end; target.direction = this.direction;
        target.color = this.color; target.range = this.range; target.intensity = this.intensity;
        target.innerAngle = this.innerAngle; target.outerAngle = this.outerAngle; target.softness = this.softness;
        target.castsShadows = this.castsShadows; target.enabled = this.enabled; target.glowRadius = this.glowRadius;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref this.kind, "kind");
        Scribe_Values.Look(ref this.position, "position");
        Scribe_Values.Look(ref this.end, "end");
        Scribe_Values.Look(ref this.direction, "direction", Vector3.down);
        Scribe_Values.Look(ref this.color, "color", Color.white);
        Scribe_Values.Look(ref this.range, "range", 32f);
        Scribe_Values.Look(ref this.intensity, "intensity", 1f);
        Scribe_Values.Look(ref this.outerAngle, "outerAngle", 65f);
        Scribe_Values.Look(ref this.innerAngle, "innerAngle", 40f);
        Scribe_Values.Look(ref this.softness, "softness", .6f);
        Scribe_Values.Look(ref this.castsShadows, "castsShadows", true);
        Scribe_Values.Look(ref this.enabled, "enabled", true);
        Scribe_Values.Look(ref this.glowRadius, "glowRadius");
    }

    internal bool HasSameConfiguration(CQFSceneLight other) =>
        this.position.Equals(other.position) && this.end.Equals(other.end) && this.direction.Equals(other.direction)
        && this.color.Equals(other.color) && this.kind == other.kind && this.range == other.range
        && this.intensity == other.intensity && this.enabled == other.enabled && this.castsShadows == other.castsShadows
        && this.softness == other.softness && this.innerAngle == other.innerAngle && this.outerAngle == other.outerAngle
        && this.glowRadius.Equals(other.glowRadius);

    public CQFSceneLightKind kind;
    public Vector3 position;
    public Vector3 end;
    public Vector3 direction = Vector3.down;
    public Color color = Color.white;
    public float range = 32f;
    public float intensity = 1f;
    public float outerAngle = 65f;
    public float innerAngle = 40f;
    public float softness = .6f;
    public bool castsShadows = true;
    public bool enabled = true;
    public float glowRadius;
}
#endif
