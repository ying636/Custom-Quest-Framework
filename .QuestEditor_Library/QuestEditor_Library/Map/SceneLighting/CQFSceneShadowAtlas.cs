#if RIMWORLD_1_7
using UnityEngine;

namespace QuestEditor_Library;

public sealed class CQFSceneShadowAtlas : IDisposable
{
    public Texture2D? Texture => this.texture;
    public int Revision { get; private set; }
    public IReadOnlyList<Vector4> Origins => this.origins ?? Array.Empty<Vector4>();

    public void Rebuild(IReadOnlyList<CQFSceneLight> lights, IReadOnlyList<Bounds> occluders)
    {
        int height = Resolution * Math.Max(1, lights.Count);
        if (this.texture == null || this.texture.height != height || this.origins?.Length != lights.Count)
        {
            this.Dispose();
            this.texture = new Texture2D(Resolution * 6, height, TextureFormat.RFloat, false, true)
            {
                name = "CQF_SceneShadowAtlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            this.depth = new float[Resolution * 6 * height];
            this.origins = new Vector4[lights.Count];
        }
        Array.Clear(this.depth!);
        for (int slot = 0; slot < lights.Count; slot++)
        {
            CQFSceneLight light = lights[slot];
            Vector3 origin = light.kind == CQFSceneLightKind.Strip ? (light.position + light.end) * .5f : light.position;
            this.origins![slot] = new Vector4(origin.x, origin.y, origin.z, light.range);
            if (!light.castsShadows) continue;
            foreach (Bounds box in occluders)
            {
                if (box.Contains(origin) || box.SqrDistance(origin) >= light.range * light.range) continue;
                this.RasterizeBox(box, origin, light.range, slot);
            }
        }
        this.texture.SetPixelData(this.depth!, 0);
        this.texture.Apply(false, false);
        this.Revision++;
    }

    public void Dispose()
    {
        if (this.texture != null) UnityEngine.Object.Destroy(this.texture);
        this.texture = null;
        this.depth = null;
        this.origins = null;
    }

    private void RasterizeBox(Bounds box, Vector3 origin, float range, int slot)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        Span<Vector3> input = stackalloc Vector3[3];
        Span<Vector3> clipped = stackalloc Vector3[4];
        Vector3 min = box.min - origin, max = box.max - origin;
        for (int i = 0; i < 8; i++) corners[i] = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
        for (int face = 0; face < 6; face++)
        {
            for (int triangle = 0; triangle < Triangles.Length; triangle += 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = corners[Triangles[triangle + i]];
                    input[i] = face switch
                    {
                        0 => new Vector3(-p.z, p.y, p.x), 1 => new Vector3(p.z, p.y, -p.x),
                        2 => new Vector3(p.x, -p.z, p.y), 3 => new Vector3(p.x, p.z, -p.y),
                        4 => new Vector3(p.x, p.y, p.z), _ => new Vector3(-p.x, p.y, -p.z)
                    };
                }
                int count = 0;
                Vector3 previous = input[2];
                bool previousInside = previous.z >= .03f;
                for (int i = 0; i < 3; i++)
                {
                    Vector3 current = input[i];
                    bool inside = current.z >= .03f;
                    if (inside != previousInside) clipped[count++] = previous + (current - previous) * ((.03f - previous.z) / (current.z - previous.z));
                    if (inside) clipped[count++] = current;
                    previous = current;
                    previousInside = inside;
                }
                for (int i = 1; i + 1 < count; i++) this.RasterizeTriangle(clipped[0], clipped[i], clipped[i + 1], range, slot, face);
            }
        }
    }

    private void RasterizeTriangle(Vector3 a, Vector3 b, Vector3 c, float range, int slot, int face)
    {
        float half = Resolution * .5f;
        Vector2 pa = new Vector2(a.x / a.z + 1f, a.y / a.z + 1f) * half;
        Vector2 pb = new Vector2(b.x / b.z + 1f, b.y / b.z + 1f) * half;
        Vector2 pc = new Vector2(c.x / c.z + 1f, c.y / c.z + 1f) * half;
        float area = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
        if (Math.Abs(area) < .00001f) return;
        int left = Math.Max(0, (int)Math.Floor(Math.Min(pa.x, Math.Min(pb.x, pc.x))));
        int right = Math.Min(Resolution - 1, (int)Math.Ceiling(Math.Max(pa.x, Math.Max(pb.x, pc.x))));
        int bottom = Math.Max(0, (int)Math.Floor(Math.Min(pa.y, Math.Min(pb.y, pc.y))));
        int top = Math.Min(Resolution - 1, (int)Math.Ceiling(Math.Max(pa.y, Math.Max(pb.y, pc.y))));
        for (int y = bottom; y <= top; y++)
        {
            for (int x = left; x <= right; x++)
            {
                float px = x + .5f, py = y + .5f;
                float wa = ((pb.x - px) * (pc.y - py) - (pb.y - py) * (pc.x - px)) / area;
                float wb = ((pc.x - px) * (pa.y - py) - (pc.y - py) * (pa.x - px)) / area;
                float wc = 1f - wa - wb;
                if (wa < -.00001f || wb < -.00001f || wc < -.00001f) continue;
                float value = (wa / a.z + wb / b.z + wc / c.z) * range;
                int index = (slot * Resolution + y) * Resolution * 6 + face * Resolution + x;
                if (value > this.depth![index]) this.depth[index] = value;
            }
        }
    }

    public const int Resolution = 64;
    private static readonly int[] Triangles = { 0, 1, 3, 0, 3, 2, 4, 6, 7, 4, 7, 5, 0, 4, 5, 0, 5, 1, 2, 3, 7, 2, 7, 6, 0, 2, 6, 0, 6, 4, 1, 5, 7, 1, 7, 3 };
    private Texture2D? texture;
    private float[]? depth;
    private Vector4[]? origins;
}
#endif
