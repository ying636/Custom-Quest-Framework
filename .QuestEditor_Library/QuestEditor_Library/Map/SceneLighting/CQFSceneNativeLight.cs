#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

internal sealed class CQFSceneNativeLight
{
    public int Count => this.handles.Count;

    public bool Update(Map map, CQFSceneLight light, float? heightSlope)
    {
        if (!light.enabled || light.intensity <= 0f || light.glowRadius <= 0f) return this.Clear(map);

        int samples = light.kind == CQFSceneLightKind.Strip
            ? Mathf.Clamp(Mathf.CeilToInt((light.end - light.position).magnitude / 8f), 2, 8)
            : 1;
        float radius = Mathf.Min(40f, light.glowRadius);
        Color color = light.color * Mathf.Clamp01(light.intensity / Mathf.Sqrt(samples));
        color.a = 1f;
        ColorInt glowColor = new ColorInt(color);
        bool changed = false;
        int visibleSamples = 0;
        for (int i = 0; i < samples; i++)
        {
            Vector3 point = samples == 1 ? light.position : Vector3.Lerp(light.position, light.end, (i + .5f) / samples);
            if (heightSlope is float slope) point.x -= slope * point.y;
            IntVec3 cell = new IntVec3(Mathf.FloorToInt(point.x), 0, Mathf.FloorToInt(point.z));
            if (!cell.InBounds(map)) continue;

            var sample = (Cell: cell, Radius: radius, Color: glowColor);
            int index = visibleSamples++;
            if (index < this.handles.Count)
            {
                var previous = this.handles[index];
                if (previous.Sample.Equals(sample)) continue;
                int handle = map.glowGrid.RegisterCustomGlower(cell, radius, glowColor);
                map.glowGrid.DeregisterCustomGlower(previous.Handle);
                this.handles[index] = (sample, handle);
            }
            else this.handles.Add((sample, map.glowGrid.RegisterCustomGlower(cell, radius, glowColor)));
            changed = true;
        }
        return this.RemoveAfter(map, visibleSamples) || changed;
    }

    public bool Clear(Map map) => this.RemoveAfter(map, 0);

    private bool RemoveAfter(Map map, int count)
    {
        if (count == this.handles.Count) return false;
        for (int i = this.handles.Count - 1; i >= count; i--)
        {
            map.glowGrid.DeregisterCustomGlower(this.handles[i].Handle);
            this.handles.RemoveAt(i);
        }
        return true;
    }

    private readonly List<((IntVec3 Cell, float Radius, ColorInt Color) Sample, int Handle)> handles = new();
}
#endif
