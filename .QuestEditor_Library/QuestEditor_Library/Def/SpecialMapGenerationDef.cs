using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public class SpecialMapGenerationDef : Def
    {
        public List<CustomMapDataTagWithWeight> customMapDataTagsToReplace = new List<CustomMapDataTagWithWeight>();
        public List<CustomMapDataWithWeight> customMapDatasToReplace = new List<CustomMapDataWithWeight>();
        public FactionDef factionOfReplacedSettlement;
        public bool replaceOutpost = false;
        public bool replaceSettlement = true;
    }

    public class CustomMapDataWithWeight : IExposable , ISaveable
    {
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "data", xmlRoot.Name, null, null, null);
            this.weight = float.Parse(xmlRoot.InnerText);
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.data, "data");
            Scribe_Values.Look(ref this.weight, "weight");
            Scribe_Values.Look(ref this.buffer, "buffer");
        }
        public XElement SaveToXElement(string nodeName)
        {
            if (this.data == null) 
            {
                return null;
            }
            XElement result = new XElement(this.data.defName,this.weight);
            return result;
        }

        public string buffer;
        public CustomMapDataDef data;
        public float weight = 1;
    }

    public class CustomMapDataTagWithWeight : IExposable,ISaveable
    {
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            this.tag = xmlRoot.Name;
            this.weight = float.Parse(xmlRoot.InnerText);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.tag,"tag");
            Scribe_Values.Look(ref this.weight, "weight");
            Scribe_Values.Look(ref this.buffer, "buffer");
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(this.tag, this.weight);
            return result;
        }

        public string buffer;
        public string tag;
        public float weight = 1;
    }
}
