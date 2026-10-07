namespace QuestEditor_Library
{
    public sealed class CQFAIDoorConfiguration : ICQFAIEditableData
    {
        public List<CQFAction> openingActions = new List<CQFAction>();
        public List<DialogCondition> openingConditions = new List<DialogCondition>();
    }
}
