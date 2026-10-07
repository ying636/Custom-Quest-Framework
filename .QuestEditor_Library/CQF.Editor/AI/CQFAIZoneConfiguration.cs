using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIZoneConfiguration : ICQFAIEditableData
    {
        public string? generationKey;
        public Rot4 coreRotation = Rot4.Invalid;
        public CoreSize size = CoreSize.Empty;
        public bool prohibitRotatingDocking;
        public bool prohibitFlippingDocking;
        public bool isCenter = true;
        public bool destroyThings;
        public ThingData? reserveThing;
        public List<ZoneCondition> conditions = new List<ZoneCondition>();
        public List<string> coreTags = new List<string>();
    }
}
