namespace QuestEditor_Library
{
    public sealed class CQFAILiveThingConfiguration : ICQFAIEditableData
    {
        public int hitPoints;
        public int stackCount;
        public CQFAIInteractableConfiguration? interaction;
        public CQFAIEntranceConfiguration? entrance;
        public CQFAIExitConfiguration? exit;
        public CQFAILootConfiguration? loot;
        public CQFAITrapConfiguration? trap;
        public CQFAIDoorConfiguration? door;
        public CQFAIContainerConfiguration? container;
        public CQFAISpawnerConfiguration? spawner;
        public CQFAIZoneConfiguration? zone;
        public CQFAIGenerationActionsConfiguration? generation;
        public List<InteractionOperation> extraInteractions = new List<InteractionOperation>();
        public List<CQFAICustomTextConfiguration> customTexts = new List<CQFAICustomTextConfiguration>();
        public List<CQFAIActionWorkerConfiguration> actionWorkers = new List<CQFAIActionWorkerConfiguration>();
    }
}
