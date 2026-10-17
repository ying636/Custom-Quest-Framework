using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFSerialization
    {
        public static XElement XElementFromObject(object obj, Type expectedType, string nodeName, FieldInfo owningField = null, bool saveDefsAsRefs = false)
        {
            if (obj is ISaveable saveable) return saveable.SaveToXElement(nodeName);
            if (owningField?.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute defaultValue && defaultValue.ObjIsDefault(obj))
                return null;
            if (obj == null) return new XElement(nodeName, new XAttribute("IsNull", "True"));
            if (obj is IntVec3 position) return new XElement(nodeName, $"({position.x},{position.y},{position.z})");
            Type type = obj.GetType();
            XElement result = new XElement(nodeName);
            if (DirectXmlSaver.IsSimpleTextType(type)) result.Add(new XText(obj.ToString()));
            else if (saveDefsAsRefs && obj is Def def) result.Add(new XText(def.defName));
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                foreach (object item in (IList)obj)
                    result.Add(XElementFromObject(item, itemType, "li", null, true));
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type[] arguments = type.GetGenericArguments();
                foreach (DictionaryEntry entry in (IDictionary)obj)
                    result.Add(new XElement("li", XElementFromObject(entry.Key, arguments[0], "key", null, true),
                        XElementFromObject(entry.Value, arguments[1], "value", null, true)));
            }
            else
            {
                if (type != expectedType) result.SetAttributeValue("Class", GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(field => field.MetadataToken))
                    if (!field.IsDefined(typeof(UnsavedAttribute)))
                        result.Add(XElementFromObject(field.GetValue(obj), field.FieldType, field.Name, field));
            }
            return result;
        }

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
