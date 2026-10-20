using UnityEngine;

namespace QuestEditor_Library;

public readonly struct DomainFusionState
{
    public DomainFusionState(Vector3 center, float radius, Vector3 rival, float rivalRadius, bool second, float time, float blend, float bias)
    {
        circle = new Vector4(center.x, center.z, radius, second ? 2f : 1f);
        other = new Vector4(rival.x, rival.z, rivalRadius, 0f);
        state = new Vector4(time, blend, bias, 1f);
    }

    public void Apply(MaterialPropertyBlock properties)
    {
        properties.SetVector("_FusionCircle", circle);
        properties.SetVector("_FusionRival", other);
        properties.SetVector("_FusionState", state);
    }

    private readonly Vector4 circle;
    private readonly Vector4 other;
    private readonly Vector4 state;
}
