using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFMapRegionConnection : IExposable, ISaveable
{
    public CQFMapRegionConnection Copy() => new CQFMapRegionConnection { cell = cell, direction = direction, width = width };

    public void ExposeData()
    {
        Scribe_Values.Look(ref cell, "cell");
        Scribe_Values.Look(ref direction, "direction", Rot4.North);
        Scribe_Values.Look(ref width, "width", 3);
    }

    public XElement SaveToXElement(string nodeName) => new XElement(nodeName,
        new XElement("cell", cell), new XElement("direction", direction), new XElement("width", width));

    public IntVec3 cell;
    public Rot4 direction = Rot4.North;
    public int width = 3;
}
