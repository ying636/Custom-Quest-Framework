using LudeonTK;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public static class DebugActions_TestDomain
{
    [DebugAction("CQF", "CQF_TestDomainExpansion", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static DebugActionNode GiveTestDomainExpansion()
    {
        DebugActionNode root = new("CQF_TestDomainExpansion");
        foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Dead || pawn.Faction != Faction.OfPlayer || !pawn.RaceProps.Humanlike) continue;
            root.AddChild(new DebugActionNode(pawn.LabelShortCap, action: () =>
            {
                pawn.abilities ??= new Pawn_AbilityTracker(pawn);
                pawn.abilities.GainAbility(DefDatabase<AbilityDef>.GetNamed("CQF_TestDomainExpansion"));
                Find.Selector.ClearSelection();
                Find.Selector.Select(pawn);
            }));
        }
        return root;
    }
}
