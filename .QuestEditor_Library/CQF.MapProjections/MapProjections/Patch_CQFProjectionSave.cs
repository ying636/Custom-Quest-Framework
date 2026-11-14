#if RIMWORLD_1_7
using HarmonyLib;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.SaveGame))]
public static class Patch_CQFProjectionSave
{
    public static void Prefix()
    {
        if (Scribe.mode == LoadSaveMode.Inactive) GameComponent_CQFProjectionUI.Instance?.PrepareForSave();
    }
}
#endif
