using Verse;

namespace QuestEditor_Library;

public sealed class CQFCompositeMapRegion : IExposable
{
    public void ExposeData()
    {
        Scribe_Values.Look(ref id!, "id", "");
        Scribe_Values.Look(ref mapKey!, "mapKey", "");
        Scribe_Values.Look(ref position, "position");
        Scribe_Values.Look(ref sourceRegion, "sourceRegion");
    }

    public string id = "";
    public string mapKey = "";
    public CustomMapDataDef map = null!;
    public IntVec2 position;
    internal string? sourceRegion;
}
