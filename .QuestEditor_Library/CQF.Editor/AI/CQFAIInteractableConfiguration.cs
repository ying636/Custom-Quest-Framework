namespace QuestEditor_Library
{
    public sealed class CQFAIInteractableConfiguration : ICQFAIEditableData
    {
        public List<InteractionOperation> operations = new List<InteractionOperation>();
        public List<InteractionDataDef> operationDefs = new List<InteractionDataDef>();
    }
}
