using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomMapGenerationSet : IDrawable , IExposable,ISaveable
    {
        public CustomMapDataDef GetMap()
        {
            Dictionary<CustomMapDataDef, float> maps = new Dictionary<CustomMapDataDef, float>();
            foreach (CustomMapDataWithWeight item in datas)
            {
                maps.Add(item.data,item.weight);
            }
            foreach (CustomMapDataDef item in DefDatabase<CustomMapDataDef>.AllDefsListForReading)
            {
                if (!maps.ContainsKey(item) && item.tags.Find(t => this.tags.Exists(t2 => t2.tag == t)) is string tag)
                {
                    maps.Add(item, this.tags.Find(t3 => t3.tag == tag).weight);
                }
            }
            if (maps.Any())
            {
                return maps.RandomElementByWeight(m => m.Value).Key;
            }
            return null;
        }

        public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CustomMapGenerationSet.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.tags, "tags",LookMode.Deep);
            Scribe_Collections.Look(ref this.datas, "datas", LookMode.Deep);
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            if (!this.tags.NullOrEmpty()) 
            {
                XElement tags = new XElement("tags");
                foreach (var item in this.tags)
                {
                    tags.Add(item.SaveToXElement("li"));
                }
                result.Add(tags);
            }
            if (!this.datas.NullOrEmpty())
            {
                XElement datas = new XElement("datas");
                foreach (var item in this.datas)
                {
                    datas.Add(item.SaveToXElement("li"));
                }
                result.Add(datas);
            }
            return result;
        }

        public List<CustomMapDataTagWithWeight> tags = new List<CustomMapDataTagWithWeight>();
        public List<CustomMapDataWithWeight> datas = new List<CustomMapDataWithWeight>();
    }
}
