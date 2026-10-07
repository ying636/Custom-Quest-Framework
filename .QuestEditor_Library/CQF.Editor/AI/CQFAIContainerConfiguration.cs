namespace QuestEditor_Library
{
    public sealed class CQFAIContainerConfiguration : ICQFAIEditableData
    {
        public int tickToOpen = 100;
        public List<LootData> innerThings = new List<LootData>();
        public List<CQFAction> openingActions = new List<CQFAction>();
        public List<DialogCondition> openingConditions = new List<DialogCondition>();
    }
}
