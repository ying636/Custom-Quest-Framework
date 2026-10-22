using RimWorld;
using Verse;

public static class EntityBookRelationProbe
{
    public static bool Add(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
    {
        Tracker = __instance;
        Relation = def;
        Other = otherPawn;
        Calls++;
        return false;
    }

    public static Pawn_RelationsTracker? Tracker;
    public static PawnRelationDef? Relation;
    public static Pawn? Other;
    public static int Calls;
}
