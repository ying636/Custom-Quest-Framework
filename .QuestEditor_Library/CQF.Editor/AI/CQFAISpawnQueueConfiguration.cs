using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAISpawnQueueConfiguration : ICQFAIEditableData
    {
        public Dictionary<string, List<IntVec3>> routes = new Dictionary<string, List<IntVec3>>();
        public List<PawnDataWithPosAndTime> timed = new List<PawnDataWithPosAndTime>();
        public Dictionary<string, List<PawnSpawnData>> building = new Dictionary<string, List<PawnSpawnData>>();
    }
}
