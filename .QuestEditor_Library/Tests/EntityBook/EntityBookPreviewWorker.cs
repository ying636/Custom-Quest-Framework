using QuestEditor_Library;
using RimWorld;
using Verse;

public sealed class EntityBookPreviewWorker : PawnModWorker
{
    public override void ApplyToPawn(ComplexPawnDef pawnDef, Pawn pawn, bool preview) => Modes.Add(preview);

    public override void OnPawnSpawned(ComplexPawnDef pawnDef, Pawn pawn, Quest quest) => SpawnNotifications++;

    public static readonly List<bool> Modes = new();
    public static int SpawnNotifications;
}
