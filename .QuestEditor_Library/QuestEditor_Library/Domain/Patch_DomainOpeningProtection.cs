using HarmonyLib;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
public static class Patch_DomainOpeningProtection
{
    public static bool IsInvulnerable(Pawn pawn)
    {
        if (pawn.Dead || pawn.Downed) return false;
        if (pawn.MapHeld?.GetComponent<MapComponent_Domain>()?.ProtectsFromOpening(pawn) == true) return true;
        AbilityDef? ability = pawn.CurJob?.ability?.def;
        return ability != null && DefDatabase<DomainDef>.AllDefsListForReading.Any(definition => definition.openingAbility == ability);
    }

    public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
    {
        if (!IsInvulnerable(__instance)) return true;
        absorbed = true;
        dinfo.SetAmount(0f);
        return false;
    }
}
