using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using QuestEditor_Library;
using SimManagementLib.SimMapComp;
using Verse;

namespace CQF_RSMF;

[HarmonyPatch(typeof(CustomerArrivalManager))]
public static class PatchCustomerArrivalManager
{
    [HarmonyPatch(nameof(CustomerArrivalManager.TrySpawnCustomerWave))]
    [HarmonyPrefix]
    public static void TrySpawnCustomerWavePrefix(CustomerArrivalManager __instance, ref HashSet<int> __state)
    {
        __state = CurrentSpawnedPawnIds(__instance);
    }

    [HarmonyPatch(nameof(CustomerArrivalManager.TrySpawnCustomerWave))]
    [HarmonyPostfix]
    public static void TrySpawnCustomerWavePostfix(CustomerArrivalManager __instance, bool __result, int spawnedCount, HashSet<int> __state)
    {
        TryApplySpecialPawnGenerate(__instance, __result, spawnedCount, __state);
    }

    [HarmonyPatch(nameof(CustomerArrivalManager.TrySpawnVendingMachineCustomer))]
    [HarmonyPrefix]
    public static void TrySpawnVendingMachineCustomerPrefix(CustomerArrivalManager __instance, ref HashSet<int> __state)
    {
        __state = CurrentSpawnedPawnIds(__instance);
    }

    [HarmonyPatch(nameof(CustomerArrivalManager.TrySpawnVendingMachineCustomer))]
    [HarmonyPostfix]
    public static void TrySpawnVendingMachineCustomerPostfix(CustomerArrivalManager __instance, bool __result, int spawnedCount, HashSet<int> __state)
    {
        TryApplySpecialPawnGenerate(__instance, __result, spawnedCount, __state);
    }

    private static void TryApplySpecialPawnGenerate(CustomerArrivalManager manager, bool result, int spawnedCount, HashSet<int> previousPawnIds)
    {
        if (!result || spawnedCount <= 0 || previousPawnIds == null || !Rand.Chance(SpecialPawnGenerateChance))
        {
            return;
        }

        List<Pawn> pawns = NewlySpawnedPawns(manager, previousPawnIds);
        if (!pawns.Any())
        {
            return;
        }

        try
        {
            SpecialPawnGenerateDef def = DefDatabase<SpecialPawnGenerateDef>.AllDefsListForReading
                .Where(d => d.generator != null)
                .RandomElementByWeightWithFallback(d => d.commonality);

            def?.generator?.Work(pawns);
        }
        catch (Exception ex)
        {
            Log.ErrorOnce($"[CQF_RSMF] Failed to run SpecialPawnGenerateDef for RimSim customer: {ex}", ErrorLogId);
        }
    }

    private static HashSet<int> CurrentSpawnedPawnIds(CustomerArrivalManager manager)
    {
        return manager?.map?.mapPawns?.AllPawnsSpawned
            .Select(p => p.thingIDNumber)
            .ToHashSet() ?? new HashSet<int>();
    }

    private static List<Pawn> NewlySpawnedPawns(CustomerArrivalManager manager, HashSet<int> previousPawnIds)
    {
        return manager?.map?.mapPawns?.AllPawnsSpawned
            .Where(p => !previousPawnIds.Contains(p.thingIDNumber))
            .ToList() ?? new List<Pawn>();
    }

    private const float SpecialPawnGenerateChance = 0.3f;
    private const int ErrorLogId = 186947301;
}
