namespace QuestEditor_Library
{
    public sealed class CQFAIExitConfiguration : ICQFAIEditableData
    {
        public string? exitName;
        public List<CQFAction> enterActions = new List<CQFAction>();
    }
}
