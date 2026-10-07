namespace QuestEditor_Library
{
    public sealed class CQFAICaptureTrapConfiguration : ICQFAIEditableData
    {
        public string disarmReport = "";
        public int tickToDisarm = 100;
        public List<CQFAction> disarmActions = new List<CQFAction>();
    }
}
