namespace QuestEditor_Library
{
    public sealed class CQFAICustomTextConfiguration : ICQFAIEditableData
    {
        public int componentIndex;
        public bool useCustomName;
        public bool useCustomDescription;
        public bool useCustomInspectText;
        public string? customName;
        public string? customDescription;
        public string? customInspectText;
    }
}
