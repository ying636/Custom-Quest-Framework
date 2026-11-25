using RimWorld;
#if RIMWORLD_1_7
using Verse;
#endif

namespace QuestEditor_Library;

public sealed class CustomMapGenerationContext
{
    public CustomMapGenerationContext(Quest quest)
    {
        this.Quest = quest;
    }

    public Quest Quest { get; set; }
    public string RegionScope { get; set; } = "";
#if RIMWORLD_1_7
    public IntVec3 Origin { get; set; }
#endif
}
