using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFSerialization
    {
        public static string GetSaveValue<T>(T t)
        {
            return t is Def def ? def.defName : t.ToString();
        }

        public static XElement SaveDictionary<T, K>(Dictionary<T, K> dictionary, string nodeName)
        {
            XElement result = new XElement(nodeName);
            foreach (KeyValuePair<T, K> value in dictionary)
            {
                XElement li = new XElement("li");
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                li.Add(new XElement("value", GetSaveValue(value.Value)));
                result.Add(li);
            }
            return result;
        }

        public static XElement SaveDictionary_Saveable<T, K>(Dictionary<T, K> dictionary, string nodeName)
            where K : ISaveable
        {
            XElement result = new XElement(nodeName);
            foreach (KeyValuePair<T, K> value in dictionary)
            {
                XElement li = new XElement("li");
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                li.Add(value.Value.SaveToXElement("value"));
                result.Add(li);
            }
            return result;
        }

        public static XElement SaveDictionary_List<T, K>(Dictionary<T, List<K>> dictionary, string nodeName)
        {
            XElement result = new XElement(nodeName);
            foreach (KeyValuePair<T, List<K>> value in dictionary)
            {
                XElement li = new XElement("li");
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                XElement valueX = new XElement("value");
                value.Value.ForEach(v => valueX.Add(new XElement("li", GetSaveValue(v))));
                li.Add(valueX);
                result.Add(li);
            }

            return result;
        }

        public static XElement SaveDictionary_Saveable_List<T, K>(Dictionary<T, List<K>> dictionary, string nodeName)
            where K : ISaveable
        {
            XElement result = new XElement(nodeName);
            foreach (KeyValuePair<T, List<K>> value in dictionary)
            {
                XElement li = new XElement("li");
                li.Add(new XElement("key", GetSaveValue(value.Key)));
                XElement valueX = new XElement("value");
                value.Value.ForEach(v => valueX.Add(v.SaveToXElement("li")));
                li.Add(valueX);
                result.Add(li);
            }

            return result;
        }

        public static XElement SaveList<T>(List<T> list, string nodeName)
        {
            XElement result = new XElement(nodeName);
            list.ForEach(x => result.Add(new XElement("li", GetSaveValue(x))));
            return result;
        }

        public static XElement SaveList_Saveable<T>(List<T> list, string nodeName)
            where T : ISaveable
        {
            XElement result = new XElement(nodeName);
            list.ForEach(x => result.Add(x.SaveToXElement("li")));
            return result;
        }
    }
}
