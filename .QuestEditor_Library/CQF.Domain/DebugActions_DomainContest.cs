using LudeonTK;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public static class DebugActions_DomainContest
{
    [DebugAction("CQF", "Spawn domain challenger", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void SpawnChallenger()
    {
        Map map = Find.CurrentMap;
        MapComponent_Domain battlefield = map.GetComponent<MapComponent_Domain>();
        IntVec3 center = Find.Selector.SingleSelectedThing?.Position ?? map.Center;
        IntVec3 cell = battlefield.FindFreeCell(map, center + new IntVec3(8, 0, 0), true);
        Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        GenSpawn.Spawn(pawn, cell, map);
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        pawn.abilities.GainAbility(DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion"));
        Find.Selector.ClearSelection();
        Find.Selector.Select(pawn);
    }

    [DebugAction("CQF", "Damage selected domain (250)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void DamageSelected() => SelectedDomain().DrainHealth(250f);

    [DebugAction("CQF", "Restore selected domain (250)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void RestoreSelected() => SelectedDomain().RestoreHealth(250f);

    private static MapComponent_Domain SelectedDomain()
    {
        Thing? selected = Find.Selector.SingleSelectedThing;
        MapComponent_Domain? domain = selected is Building_Domain anchor ? anchor.Domain
            : selected is Pawn pawn ? pawn.MapHeld?.GetComponent<MapComponent_Domain>().FindDomainFor(pawn) : null;
        return domain?.Active == true ? domain : throw new InvalidOperationException("[CQF] Select an active domain caster or anchor first.");
    }
}
