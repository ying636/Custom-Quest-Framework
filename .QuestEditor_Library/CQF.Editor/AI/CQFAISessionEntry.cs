namespace QuestEditor_Library
{
    public sealed class CQFAISessionEntry
    {
        public CQFAISessionEntry(string id, string title, string preview, DateTime updated)
        {
            if (!Guid.TryParseExact(id, "N", out _) || title.Length > 100 || preview.Length > 120) throw new InvalidDataException("CQF_AI_InvalidHistory");
            Id = id; Title = title; Preview = preview; Updated = updated;
        }
        public string Id { get; }
        public string Title { get; }
        public string Preview { get; }
        public DateTime Updated { get; }
    }
}
