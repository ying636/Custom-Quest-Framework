using HarmonyLib;
using Verse;

namespace CQF_RSMF;

[StaticConstructorOnStartup]
public static class CQFRSMFMod
{
    static CQFRSMFMod()
    {
        new Harmony("HaiLuan.CQF.RimSimManagementFramework").PatchAll();
    }
}
