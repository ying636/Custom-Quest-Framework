using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public interface ICQFAILiveMap
    {
        object Identity { get; }
        bool IsValid { get; }
        CQFAILiveMapInfo Info { get; }
        XElement ReadRegion(CellRect region, int offset, int limit);
        XElement ReadThing(string id);
        IReadOnlyList<CQFAILiveMapEdit> Prepare(XElement changes, CQFAIModel model, string command, bool generateText);
        XElement Validate();
    }
}
