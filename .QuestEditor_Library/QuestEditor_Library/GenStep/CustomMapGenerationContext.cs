using RimWorld;

namespace QuestEditor_Library;

public sealed class CustomMapGenerationContext
{
    public CustomMapGenerationContext(Quest quest)
    {
        this.Quest = quest;
    }

    public Quest Quest { get; set; }
    public string RegionScope { get; set; } = "";
}
