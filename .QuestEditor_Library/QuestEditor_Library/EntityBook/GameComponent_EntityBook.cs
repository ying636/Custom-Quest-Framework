using RimWorld;
using Verse;

namespace QuestEditor_Library;

public sealed class GameComponent_EntityBook : GameComponent
{
    public GameComponent_EntityBook(Game game) { }

    public IReadOnlyList<CQFEntityRecord> Records => recordsView ??= records.AsReadOnly();

    public IReadOnlyList<CQFEntityRecord> GetRecords(string sourceId, Quest? quest = null)
    {
        EnsureRecordIndex();
        return recordViews.TryGetValue((sourceId, quest?.id ?? -1), out IReadOnlyList<CQFEntityRecord>? view)
            ? view : Array.Empty<CQFEntityRecord>();
    }

    public void RegisterBatch(string sourceId, IEnumerable<Pawn> pawns, Quest? quest = null, Map? map = null, string? sourceLabel = null)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("CQF_EntityBook_SourceRequired".Translate(), nameof(sourceId));
        if (pawns == null) throw new ArgumentNullException(nameof(pawns));
        EnsureRecordIndex();
        int questId = quest?.id ?? -1;
        (string SourceId, int QuestId) key = (sourceId, questId);
        if (!recordGroups.TryGetValue(key, out List<CQFEntityRecord>? existing))
        {
            existing = new List<CQFEntityRecord>();
            recordGroups.Add(key, existing);
            recordViews.Add(key, existing.AsReadOnly());
        }
        int nextIndex = existing.Count == 0 ? 0 : existing[existing.Count - 1].MemberIndex + 1;
        foreach (Pawn pawn in pawns.Where(pawn => pawn != null).Distinct())
        {
            string thingId = pawn.GetUniqueLoadID();
            CQFEntityRecord? previous = existing.FirstOrDefault(record => ReferenceEquals(record.Pawn, pawn) || record.ThingId == thingId);
            if (previous != null)
            {
                previous.Refresh(pawn, map, sourceLabel);
                continue;
            }
            CQFEntityRecord record = new(sourceId, sourceLabel ?? "", nextIndex++, pawn, quest, map);
            records.Add(record);
            existing.Add(record);
        }
    }

    public override void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.LoadingVars) loadedFromSave = true;
        Scribe_Collections.Look(ref records, "cqfEntityRecords", LookMode.Deep);
        if (Scribe.mode is LoadSaveMode.LoadingVars or LoadSaveMode.PostLoadInit)
        {
            records ??= new List<CQFEntityRecord>();
            recordsView = null;
            recordIndexReady = false;
            recordGroups.Clear();
            recordViews.Clear();
        }
    }

    public override void LoadedGame()
    {
        if (!loadedFromSave) CQFEntityBookSaveMigration.Restore(this);
        loadedFromSave = true;
    }

    public override void StartedNewGame()
    {
        loadedFromSave = true;
    }

    private void EnsureRecordIndex()
    {
        if (recordIndexReady) return;
        foreach (CQFEntityRecord record in records)
        {
            (string SourceId, int QuestId) key = (record.SourceId, record.QuestId);
            if (!recordGroups.TryGetValue(key, out List<CQFEntityRecord>? group))
            {
                group = new List<CQFEntityRecord>();
                recordGroups.Add(key, group);
                recordViews.Add(key, group.AsReadOnly());
            }
            group.Add(record);
        }
        foreach (List<CQFEntityRecord> group in recordGroups.Values)
            group.Sort((left, right) => left.MemberIndex.CompareTo(right.MemberIndex));
        recordIndexReady = true;
    }

    private List<CQFEntityRecord> records = new();
    private IReadOnlyList<CQFEntityRecord>? recordsView;
    private readonly Dictionary<(string SourceId, int QuestId), List<CQFEntityRecord>> recordGroups = new();
    private readonly Dictionary<(string SourceId, int QuestId), IReadOnlyList<CQFEntityRecord>> recordViews = new();
    private bool recordIndexReady;
    private bool loadedFromSave;
}
