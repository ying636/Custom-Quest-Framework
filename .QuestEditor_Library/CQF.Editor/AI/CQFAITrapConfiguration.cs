namespace QuestEditor_Library
{
    public sealed class CQFAITrapConfiguration : ICQFAIEditableData
    {
        public string trapName = "";
        public List<TrapComp> trapComps = new List<TrapComp>();
        public CQFAICaptureTrapConfiguration? capture;
    }
}
