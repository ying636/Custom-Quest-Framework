using LudeonTK;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public static class DebugActions_TestDomain
{
    [DebugAction("CQF", "CQF_TestDomainExpansion", actionType = DebugActionType.ToolMapForPawns,
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void GiveTestDomainExpansion(Pawn pawn)
    {
        if (pawn.Destroyed || !pawn.Spawned || pawn.Dead) return;
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        pawn.abilities.GainAbility(DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion"));
        DebugActionsUtility.DustPuffFrom(pawn);
    }
}
