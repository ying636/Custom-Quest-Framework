using Verse;

namespace QuestEditor_Library;

internal static class CQFEntityBookSaveMigration
{
    public static void Restore(GameComponent_EntityBook book)
    {
        GameComponent_Editor? editor = Current.Game?.GetComponent<GameComponent_Editor>();
        if (editor?.pawns == null) return;
        foreach (KeyValuePair<string, Pawn> entry in editor.pawns)
        {
            Pawn pawn = entry.Value;
            ComplexPawnDef? template = DefDatabase<ComplexPawnDef>.GetNamedSilentFail(entry.Key);
            if (template?.Unique != true || pawn == null || pawn.Destroyed || pawn.Discarded) continue;
            string sourceId = "template:" + entry.Key;
            if (book.Records.Any(record => record.SourceId == sourceId)) continue;
            book.RegisterBatch(sourceId, new[] { pawn }, sourceLabel: template.label);
        }
    }
}
