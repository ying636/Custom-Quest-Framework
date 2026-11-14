#if RIMWORLD_1_7
using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(CameraJumper), "TryJumpInternal", new[] { typeof(IntVec3), typeof(Map), typeof(CameraJumper.MovementMode) })]
public static class Patch_CQFProjectionJump
{
    public static bool Prefix(IntVec3 cell, Map map)
    {
        if (CQFMapProjectionContext.Focused is not { CanControl: true } projection || projection.sourceMap != map || !cell.InBounds(map)) return true;
        projection.CenterView(cell);
        return false;
    }
}
#endif
