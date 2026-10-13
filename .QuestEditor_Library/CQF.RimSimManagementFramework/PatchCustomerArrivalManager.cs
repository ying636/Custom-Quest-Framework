using System.Reflection;
using HarmonyLib;
using QuestEditor_Library;
using SimManagementLib.SimMapComp;
using Verse;

namespace CQF_RSMF;

[HarmonyPatch]
public static class PatchCustomerArrivalManager
{
    [HarmonyPostfix]
    public static void Postfix(bool __result, Pawn spawnedPawn)
    {
        if (!__result || spawnedPawn == null || !Rand.Chance(SpecialPawnGenerateChance)) return;
        try
        {
            SpecialPawnGenerateDef? def = DefDatabase<SpecialPawnGenerateDef>.AllDefsListForReading
                .Where(def => def?.generator != null)
                .RandomElementByWeightWithFallback(def => def?.commonality ?? 0f);
            def?.generator?.Work(new List<Pawn> { spawnedPawn });
        }
        catch (Exception error)
        {
            Log.ErrorOnce($"[CQF_RSMF] Failed to run SpecialPawnGenerateDef for RimSim customer: {error}", ErrorLogId);
        }
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (string name in new[] { "TrySpawnCustomerWave", "TrySpawnVendingMachineCustomer" })
            yield return AccessTools.Method(typeof(CustomerArrivalManager), name)
                ?? throw new MissingMethodException(typeof(CustomerArrivalManager).FullName, name);
    }

    private const float SpecialPawnGenerateChance = 0.3f;
    private const int ErrorLogId = 186947301;
}
