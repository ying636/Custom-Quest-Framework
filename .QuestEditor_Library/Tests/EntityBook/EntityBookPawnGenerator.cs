using Verse;

public static class EntityBookPawnGenerator
{
    public static bool Generate(PawnGenerationRequest request, ref Pawn __result)
    {
        Requests.Add(request);
        __result = Pawn;
        return false;
    }

    public static Pawn Pawn = null!;
    public static readonly List<PawnGenerationRequest> Requests = new();
}
