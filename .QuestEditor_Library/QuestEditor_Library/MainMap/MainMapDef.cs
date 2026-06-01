using System.Collections.Generic;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public class MainMapDef : Def
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", this.defName));
            if (!this.maps.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.maps, "maps"));
            }
            return result;
        }

        public List<MainMapAndCondition> maps = new List<MainMapAndCondition>();
    }
}
