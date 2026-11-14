#if RIMWORLD_1_7
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(CameraJumper), "TrySelectInternal", new[] { typeof(Thing) })]
public static class Patch_CQFProjectionSelect
{
    public static bool Prefix(Thing thing)
    {
        if (!WorldRendererUtility.DrawingMap || !thing.Spawned || !thing.def.selectable
            || CQFMapProjectionContext.Focused is not { CanControl: true } projection || projection.sourceMap != thing.Map) return true;
        using CQFMapProjectionContext context = new(projection, CQFMapProjectionContext.Current?.HostCamera ?? Current.Camera);
        Find.Selector.ClearSelection();
        Find.Selector.Select(thing);
        return false;
    }
}
#endif
