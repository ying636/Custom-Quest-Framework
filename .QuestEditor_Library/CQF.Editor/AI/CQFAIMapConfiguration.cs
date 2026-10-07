namespace QuestEditor_Library
{
    public sealed class CQFAIMapConfiguration : ICQFAIEditableData
    {
        public List<CQFEventArea> eventAreas = new List<CQFEventArea>();
        public List<CQFAIMapTriggerConfiguration> triggers = new List<CQFAIMapTriggerConfiguration>();
    }
}
