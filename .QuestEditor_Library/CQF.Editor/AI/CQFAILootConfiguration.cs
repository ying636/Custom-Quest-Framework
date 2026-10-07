namespace QuestEditor_Library
{
    public sealed class CQFAILootConfiguration : ICQFAIEditableData
    {
        public string lootBoxName = "";
        public int tickToOpen = 100;
        public string openReport = "";
        public bool destroyAfterOpening;
        public bool useLootDef;
        public bool openWhenDestroyed = true;
        public List<LootData> loots = new List<LootData>();
        public LootDataDef? lootDef;
    }
}
