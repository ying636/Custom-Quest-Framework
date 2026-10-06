using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAILiveMapInfo : ICQFAIEditableData
    {
        public int mapId;
        public IntVec3 size;
        public IntVec3 center;
        public string selectedThingId = string.Empty;
        public bool live = true;
        [Unsaved] public ICQFAILiveMap? backend;
    }
}
