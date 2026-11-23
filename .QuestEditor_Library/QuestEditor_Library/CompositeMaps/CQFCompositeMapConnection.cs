using Verse;

namespace QuestEditor_Library;

public enum CQFCompositeMapEdge
{
    North, East, South, West
}

public sealed class CQFCompositeMapConnection
{
    public string from = "";
    public string to = "";
    public bool twoWay = true;
    public CQFCompositeMapEdge fromEdge;
    public CQFCompositeMapEdge toEdge;
    public int fromStart;
    public int toStart;
    public int length;
    public IntVec3 fromCell = IntVec3.Invalid;
    public IntVec3 toCell = IntVec3.Invalid;
}
