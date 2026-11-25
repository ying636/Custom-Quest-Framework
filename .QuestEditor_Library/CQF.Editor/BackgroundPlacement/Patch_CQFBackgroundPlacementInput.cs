#if RIMWORLD_1_7
using HarmonyLib;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(DesignatorManager), nameof(DesignatorManager.ProcessInputEvents))]
internal static class Patch_CQFBackgroundPlacementInput
{
    private static void Prefix(DesignatorManager __instance)
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 &&
            __instance.SelectedDesignator is Designator_CQFBackgroundPlacement active && active.TryPickForEditing())
            Event.current.Use();
    }
}
#endif
