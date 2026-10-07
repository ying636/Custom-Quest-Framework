namespace QuestEditor_Library
{
    public sealed class CQFAIMapTriggerConfiguration : ICQFAIEditableData
    {
        public string key = string.Empty;
        public ActionTriggerMode mode = ActionTriggerMode.Damaged;
        public List<string> thingIds = new List<string>();
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
