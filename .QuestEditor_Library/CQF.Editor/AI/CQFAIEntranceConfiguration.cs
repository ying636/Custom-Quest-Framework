namespace QuestEditor_Library
{
    public sealed class CQFAIEntranceConfiguration : ICQFAIEditableData
    {
        public CustomMapDataDef? data;
        public string? exitName;
        public bool opended = true;
        public List<CQFAction> enterActions = new List<CQFAction>();
        public List<TagWithChance> tagWithChance = new List<TagWithChance>();
        public List<MapDefWithChance> mapDefWithChance = new List<MapDefWithChance>();
    }
}
