using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFEntityBookEntry
    {
        public CQFEntityBookEntry(string sourceId, string label, string origin, PawnSpawnData? recipe = null, ComplexPawnDef? template = null, bool saved = false)
        {
            SourceId = sourceId;
            Label = label;
            Origin = origin;
            Recipe = recipe;
            Template = template;
            Saved = saved;
        }

        public string SourceId { get; }
        public string Label { get; }
        public string Origin { get; }
        public PawnSpawnData? Recipe { get; }
        public ComplexPawnDef? Template { get; }
        public bool Saved { get; }
        public PawnKindDef? Kind => Template?.KindDef ?? (Recipe as PawnSpawnData_ComplexPawn)?.pawnDef?.KindDef ?? Recipe?.kind;
    }
}
