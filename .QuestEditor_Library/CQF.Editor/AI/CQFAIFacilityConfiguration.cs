using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIFacilityConfiguration : ICQFAIEditableData
    {
        public string receiverId = "";
        public string linkedPowerId = "";
        public bool outputMode;
        public float targetPowerOutput;
        public bool startLandfill;
        public int tickToFill;
        public ThingDef? filled;
        public string iconPath = "";
        public string landfillText = "";
        public string triggerSignal = "";
        public DialogTreeDef? dialog;
        public List<CQFAction> outcomes = new List<CQFAction>();
        public string exitKey = "";
        public CustomMapGenerationSet? mapChoice;
    }
}
