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
    public class CustomMapGenerationSet : IDrawable, IExposable, ISaveable
    {
        public CustomMapDataDef GetMap()
        {
            Dictionary<CustomMapDataDef, float> maps = new Dictionary<CustomMapDataDef, float>();
            foreach (CustomMapDataWithWeight item in datas)
            {
                maps.Add(item.data, item.weight);
            }

            foreach (CustomMapDataDef item in DefDatabase<CustomMapDataDef>.AllDefsListForReading)
            {
                if (!maps.ContainsKey(item) && item.tags.Find(t => this.tags.Exists(t2 => t2.tag == t))is string tag)
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
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            y += 5f;
            CQFEditorTools.DrawEditableList(this.datas, ref y, (textField, t) =>
            {
                textField.width = Mathf.Min(textField.width, Mathf.Max(20f, inRect.width - textField.x - 192f));
                string buttomText = "CustomMapDef".Translate(t.data?.label);
                if (CQFUIStyle.ButtonText(textField, buttomText, false))
                {
                    CQFEditorTools.DrawFloatMenu(DefDatabase<CustomMapDataDef>.AllDefsListForReading, (d) => t.data = d, (d) => d.label);
                }

                Rect chance = new Rect(textField.xMax + 8f, textField.y, 80f, 25f);
                Widgets.Label(chance, "Chance".Translate());
                chance.x += 88f;
                chance.width = Mathf.Max(20f, Mathf.Min(80f, inRect.width - chance.x - 12f));
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.data?.label, "MapDefWithChance".Translate(), "MapDefWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
            CQFEditorTools.DrawEditableList(this.tags, ref y, (textField, t) =>
            {
                textField.width = Mathf.Min(textField.width, Mathf.Max(20f, inRect.width - textField.x - 192f));
                t.tag = Widgets.TextField(textField, t.tag);
                Rect chance = new Rect(textField.xMax + 8f, textField.y, 80f, 25f);
                Widgets.Label(chance, "LootChance".Translate());
                chance.x += 88f;
                chance.width = Mathf.Max(20f, Mathf.Min(80f, inRect.width - chance.x - 12f));
                Widgets.TextFieldPercent(chance, ref t.weight, ref t.buffer);
            }, t => t.tag, "TagWithChance".Translate(), "TagWithChance_Tip".Translate(), true, 15f, 350f);
            y += 5f;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.tags, "tags", LookMode.Deep);
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
