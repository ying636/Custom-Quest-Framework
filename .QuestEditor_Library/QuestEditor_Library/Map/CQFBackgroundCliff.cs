#if RIMWORLD_1_7
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

[Flags]
public enum CQFCliffEdges
{
    None = 0,
    North = 1,
    East = 2,
    South = 4,
    West = 8,
    All = North | East | South | West
}

public sealed class CQFBackgroundCliff : IExposable, ISaveable
{
    public CQFBackgroundCliff Copy() => (CQFBackgroundCliff)MemberwiseClone();

    public void ExposeData()
    {
        Scribe_Values.Look(ref area, "area");
        Scribe_Values.Look(ref edges, "edges", CQFCliffEdges.All);
        Scribe_Values.Look(ref depth, "depth", 5f);
        Scribe_Values.Look(ref color, "color", Color.white);
    }

    public XElement SaveToXElement(string nodeName) => new(nodeName,
        new XElement("area", $"({area.minX},{area.minZ},{area.maxX},{area.maxZ})"), new XElement("edges", edges),
        new XElement("depth", depth), new XElement("color", color));

    public CellRect area;
    public CQFCliffEdges edges = CQFCliffEdges.All;
    public float depth = 5f;
    public Color color = Color.white;
}
#endif
