using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public abstract class CustomMapStep : IDrawable, ISaveable
{
    public abstract void Generate(Map map, CustomMapDataDef def, CustomSitePartParams param);
    public abstract void Draw(ref float y, Rect inRect, float x);
    public virtual XElement SaveToXElement(string nodeName)
    {
        XElement result = new XElement(nodeName);
        result.SetAttributeValue("Class", this.GetType().FullName);
        return result;
    }
}