namespace QuestEditor_Library
{
    public sealed class CQFAILiveThingConfiguration : ICQFAIEditableData
    {
        public int hitPoints;
        public int stackCount;
        public CQFAIInteractableConfiguration? interaction;
        public CQFAIEntranceConfiguration? entrance;
        public CQFAIExitConfiguration? exit;
    }
}
