using System.Reflection;
using HarmonyLib;
using QuestEditor_Library;
using UnityEngine;

namespace CQF.DomainContestRuntimeChecks;

public static class CounterPreparationSurfaceChecks
{
    public static void Install()
    {
        new Harmony("CQF.CounterPreparationSurfaceChecks").Patch(
            AccessTools.Method(typeof(DomainSurfaceVisuals), nameof(DomainSurfaceVisuals.Draw),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(float), typeof(Vector4), typeof(Vector4), typeof(float), typeof(Vector4) }),
            postfix: new HarmonyMethod(typeof(CounterPreparationSurfaceChecks), nameof(AfterDraw)));
    }

    public static DomainSurfaceVisuals? SurfaceFor(DomainOpening opening)
        => typeof(DomainOpening).GetField("counterSurface", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(opening)
            as DomainSurfaceVisuals;

    public static bool TryGetDraw(DomainOpening opening, out float radius)
    {
        radius = 0f;
        if (SurfaceFor(opening) is not { } surface || !draws.TryGetValue(surface, out var draw)) return false;
        radius = draw.Radius;
        return draw.Frame >= Time.frameCount - 2 && draw.Opacity > 0f && draw.Radius > 0f;
    }

    public static bool Released(DomainOpening opening, DomainSurfaceVisuals? surface)
        => SurfaceFor(opening) == null && surface != null
            && typeof(DomainSurfaceVisuals).GetField("material", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(surface) == null;

    private static void AfterDraw(DomainSurfaceVisuals __instance, Vector4 circle, float opacity, Vector4 clash)
    {
        if (clash.w > 2.5f) draws[__instance] = (circle.z, opacity, Time.frameCount);
    }

    private static readonly Dictionary<DomainSurfaceVisuals, (float Radius, float Opacity, int Frame)> draws = new();
}
