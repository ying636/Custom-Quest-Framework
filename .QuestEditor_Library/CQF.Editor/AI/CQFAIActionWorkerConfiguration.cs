namespace QuestEditor_Library
{
    public sealed class CQFAIActionWorkerConfiguration : ICQFAIEditableData
    {
        public int componentIndex;
        public List<ActionComp> comps = new List<ActionComp>();
    }
}
