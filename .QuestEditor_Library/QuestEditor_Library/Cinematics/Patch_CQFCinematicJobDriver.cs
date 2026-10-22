using HarmonyLib;
using Verse.AI;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Job), nameof(Job.MakeDriver))]
internal static class Patch_CQFCinematicJobDriver
{
    private static void Postfix(Job __instance, JobDriver __result)
    {
        CQFCinematicJobBinding.AttachDriver(__instance, __result);
    }
}
