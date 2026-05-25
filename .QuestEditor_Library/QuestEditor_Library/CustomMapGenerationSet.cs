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
            y += 5f;
            CQFEditorTools.DrawEditableList(this.datas, ref y, (textField, t) =>
            {
                string buttomText = "CustomMapDef".Translate(t.data?.label);
                if (Widgets.ButtonText(textField, buttomText, false))
                {
                    List<CustomMapDataDef> list = new List<CustomMapDataDef>();
                    list.AddRange(DefDatabase<CustomMapDataDef>.AllDefsListForReading.ToList());
                    DirectoryInfo mapDir = new DirectoryInfo(Page_QuestEditor.Path + @"\Map\");
                    foreach (FileInfo file in mapDir.GetFiles("*.xml"))
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.Load(file.FullName);
                        foreach (XmlNode xmlNode in xml.SelectNodes("//QuestEditor_Library.CustomMapDataDef"))
                        {
                            list.Add(DirectXmlToObject.ObjectFromXml<CustomMapDataDef>(xmlNode, false));
                        }
                    }
                    CQFEditorTools.DrawFloatMenu(list, (d) => t.data = d, (d) => d.label);
                }
                Rect chance = new Rect(Text.CalcSize(buttomText).x + textField.x + 10f, textField.y, 100f, 25f);
                Widgets.Label(chance, "Chance".Translate());
                chance.x += 80f;
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.data?.label, "MapDefWithChance".Translate(), "MapDefWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
            CQFEditorTools.DrawEditableList(this.tags, ref y, (textField, t) =>
            {
                t.tag = Widgets.TextField(textField, t.tag);
                Rect chance = new Rect(textField.width + textField.x + 10f, textField.y, 100f, 25f);
                Widgets.Label(chance, "LootChance".Translate());
                chance.x += 80f;
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.tag, "TagWithChance".Translate(), "TagWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
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