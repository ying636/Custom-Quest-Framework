#if RIMWORLD_1_7
using System.Globalization;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed class CQFBackgroundStructure : IExposable, ISaveable
{
    public Vector3 Center => this.position.ToVector3Shifted() + new Vector3(this.offset.x, 0f, this.offset.y);

    public CQFBackgroundStructure Copy() => (CQFBackgroundStructure)MemberwiseClone();

    public void SetCenter(Vector3 center)
    {
        this.position = new IntVec3(Mathf.FloorToInt(center.x), 0, Mathf.FloorToInt(center.z));
        this.offset = new Vector2(center.x - this.position.x - .5f, center.z - this.position.z - .5f);
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref position, "position");
        Scribe_Values.Look(ref offset, "offset", Vector2.zero);
        Scribe_Values.Look(ref rotation, "rotation");
        Scribe_Values.Look(ref scale, "scale", 1f);
        Scribe_Values.Look(ref depth, "depth");
    }

    public XElement SaveToXElement(string nodeName) => new(nodeName,
        new XElement("def", def.defName), new XElement("position", position),
        this.offset == Vector2.zero ? null : new XElement("offset", string.Create(CultureInfo.InvariantCulture, $"({this.offset.x:R},{this.offset.y:R})")),
        new XElement("rotation", rotation), new XElement("scale", scale), new XElement("depth", depth));

    public CQFBackgroundStructureDef def = null!;
    public IntVec3 position;
    public Vector2 offset;
    public float rotation;
    public float scale = 1f;
    public float depth;
}
#endif
