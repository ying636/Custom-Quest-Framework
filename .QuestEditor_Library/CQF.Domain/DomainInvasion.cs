using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class DomainInvasion : IExposable
{
    public DomainInvasion() { }

    public DomainInvasion(int seed) { this.seed = seed; }

    public bool First => first;
    public float Strength => strength;
    public IReadOnlyList<Vector4> Shapes => shapes;
    public Vector4 Phases => phases;

    public bool Advance(float fraction, float step, int width, int height, bool reversed, bool finished)
    {
        float previous = strength;
        bool owner = fraction >= 0.5f;
        float target = width < 16 || height < 16 || finished ? 0f
            : Mathf.Clamp01((Math.Max(fraction, 1f - fraction) - 0.7f) / 0.2f);
        if (strength > 0f && owner != first) target = 0f;
        if (strength == 0f && target > 0f) first = owner;
        strength = finished ? 0f : Mathf.MoveTowards(strength, target, step);
        Rebuild(fraction, width, height, reversed);
        return previous != strength;
    }

    public void Rebuild(float fraction, int width, int height, bool reversed)
    {
        if (!normalizedReady) InitializeContours();
        float weakWidth = width * (first ? 1f - fraction : fraction);
        uint state = unchecked((uint)seed);
        for (int index = 0; index < shapes.Length; index++)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            float depth = 0.34f + (state >> 8) / 16777216f * 0.28f;
            state = unchecked(state * 1664525u + 1013904223u);
            float z = height * (0.2f + index * 0.3f + ((state >> 8) / 16777216f - 0.5f) * 0.04f);
            float x = first != reversed ? width - weakWidth * depth : weakWidth * depth;
            shapes[index] = new Vector4(x, z, Math.Min(width * 0.06f, weakWidth * 0.22f) * strength,
                Math.Min(height * 0.075f, weakWidth * 0.35f + 1f) * strength);
            for (int vertex = 0; vertex < Segments; vertex++)
            {
                Vector2 point = normalized[index * Segments + vertex];
                vertices[index * Segments + vertex] = new Vector2(x + point.x * shapes[index].z, z + point.y * shapes[index].w);
            }
        }
    }

    public float SignedDistance(float x, float z)
    {
        float result = float.NegativeInfinity;
        for (int index = 0; index < shapes.Length; index++)
        {
            Vector4 shape = shapes[index];
            if (shape.z <= 0f || shape.w <= 0f) continue;
            float dx = (x - shape.x) / shape.z;
            float dz = (z - shape.y) / shape.w;
            float radius = Mathf.Sqrt(dx * dx + dz * dz);
            if (radius > 1.5f) continue;
            float angle = Mathf.Atan2(dz, dx);
            if (angle < 0f) angle += Mathf.PI * 2f;
            int segment = Math.Min(Segments - 1, (int)(angle * (Segments / (Mathf.PI * 2f))));
            Vector2 a = normalized[index * Segments + segment];
            Vector2 b = normalized[index * Segments + (segment + 1) % Segments];
            Vector2 direction = radius > 0f ? new Vector2(dx / radius, dz / radius) : Vector2.right;
            float edge = (a.x * b.y - a.y * b.x) / (direction.x * (b.y - a.y) - direction.y * (b.x - a.x));
            result = Math.Max(result, (edge - radius) * Math.Min(shape.z, shape.w));
        }
        return result;
    }

    public double AreaOutside(float boundary, bool firstOnLeft)
        => AreaOutside(boundary, firstOnLeft, default, false);

    public double AreaOutside(float boundary, bool firstOnLeft, Rect clip)
        => AreaOutside(boundary, firstOnLeft, clip, true);

    public bool IntersectsBoundary(Rect rect)
    {
        foreach (Vector4 shape in shapes)
        {
            if (shape.z <= 0f || shape.w <= 0f) continue;
            float nearX = Mathf.Clamp(shape.x, rect.xMin, rect.xMax) - shape.x;
            float nearZ = Mathf.Clamp(shape.y, rect.yMin, rect.yMax) - shape.y;
            float farX = Math.Max(Math.Abs(rect.xMin - shape.x), Math.Abs(rect.xMax - shape.x));
            float farZ = Math.Max(Math.Abs(rect.yMin - shape.y), Math.Abs(rect.yMax - shape.y));
            if (nearX * nearX / (shape.z * shape.z) + nearZ * nearZ / (shape.w * shape.w) < 0.999999f
                && farX * farX / (shape.z * shape.z) + farZ * farZ / (shape.w * shape.w) >= 0.25f) return true;
        }
        return false;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref seed, "seed");
        Scribe_Values.Look(ref first, "first");
        Scribe_Values.Look(ref strength, "strength", -1f);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f || strength > 1f))
            throw new InvalidOperationException("[CQF] Invalid saved domain invasion strength.");
    }

    private double AreaOutside(float boundary, bool firstOnLeft, Rect clip, bool useClip)
    {
        double result = 0d;
        for (int index = 0; index < shapes.Length; index++)
        {
            Vector4 shape = shapes[index];
            if (shape.z <= 0f || shape.w <= 0f) continue;
            if (useClip && (shape.x + shape.z <= clip.xMin || shape.x - shape.z >= clip.xMax
                || shape.y + shape.w <= clip.yMin || shape.y - shape.w >= clip.yMax)) continue;
            int count = Segments;
            Array.Copy(vertices, index * Segments, clipped, 0, Segments);
            if (useClip)
            {
                count = Clip(count, 0, clip.xMin, false);
                count = Clip(count, 0, clip.xMax, true);
                count = Clip(count, 1, clip.yMin, false);
                count = Clip(count, 1, clip.yMax, true);
            }
            double total = Area(count);
            count = Clip(count, 0, boundary, true);
            double left = Area(count);
            double inFirstRectangle = firstOnLeft ? left : total - left;
            result += first ? total - inFirstRectangle : -inFirstRectangle;
        }
        return result;
    }

    private void InitializeContours()
    {
        uint state = unchecked((uint)seed ^ 0x9e3779b9u);
        for (int index = 0; index < shapes.Length; index++)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            float phase = (state >> 8) / 16777216f * Mathf.PI * 2f;
            phases[index] = phase;
            for (int vertex = 0; vertex < Segments; vertex++)
            {
                float angle = vertex * (Mathf.PI * 2f / Segments);
                float radius = 0.76f + 0.16f * Mathf.Sin(angle * 3f + phase) + 0.08f * Mathf.Sin(angle * 7f - phase * 1.7f);
                normalized[index * Segments + vertex] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
        }
        normalizedReady = true;
    }

    private int Clip(int count, int axis, float boundary, bool less)
    {
        if (count == 0) return 0;
        int output = 0;
        Vector2 before = clipped[count - 1];
        bool beforeInside = less ? before[axis] <= boundary : before[axis] >= boundary;
        for (int index = 0; index < count; index++)
        {
            Vector2 after = clipped[index];
            bool afterInside = less ? after[axis] <= boundary : after[axis] >= boundary;
            if (beforeInside != afterInside)
                scratch[output++] = Vector2.Lerp(before, after, (boundary - before[axis]) / (after[axis] - before[axis]));
            if (afterInside) scratch[output++] = after;
            before = after;
            beforeInside = afterInside;
        }
        (clipped, scratch) = (scratch, clipped);
        return output;
    }

    private double Area(int count)
    {
        if (count < 3) return 0d;
        double result = 0d;
        Vector2 before = clipped[count - 1];
        for (int index = 0; index < count; index++)
        {
            Vector2 after = clipped[index];
            result += before.x * (double)after.y - after.x * (double)before.y;
            before = after;
        }
        return Math.Abs(result) * 0.5d;
    }

    private int seed;
    private bool first;
    private float strength;
    private readonly Vector4[] shapes = new Vector4[3];
    private Vector4 phases;
    private bool normalizedReady;
    private readonly Vector2[] normalized = new Vector2[Segments * 3];
    private readonly Vector2[] vertices = new Vector2[Segments * 3];
    private Vector2[] clipped = new Vector2[Segments * 4];
    private Vector2[] scratch = new Vector2[Segments * 4];
    private const int Segments = 48;
}
