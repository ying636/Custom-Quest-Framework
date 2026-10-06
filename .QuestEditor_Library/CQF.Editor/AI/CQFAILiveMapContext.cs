using Verse;

namespace QuestEditor_Library
{
    public static class CQFAILiveMapContext
    {
        public static CQFAIEditorContext Create(Map map) => Create(new CQFAILiveMap(map));
        public static CQFAIEditorContext Create(ICQFAILiveMap backend) => new CQFAIEditorContext("CQF_LiveMap", () => backend.Info,
            _ => throw new InvalidDataException("CQF_AI_LiveMapOperationRequired"), isValid: () => backend.IsValid, owner: backend.Identity, identity: backend.Identity);
    }
}
