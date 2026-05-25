using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public static class QE_SaveToolbase
    {
        public static XElement SaveToXml(FieldInfo field, object def)
        {
            XElement result = new XElement(field.Name);   
            object rootNode = field.GetValue(def); 
            result.SetAttributeValue("Class",GetName(rootNode.GetType().FullName));
            SaveFields(rootNode).ForEach(n => result.Add(n));
            return result;
        }
        private static XElement SaveObject(object ob,string nodeName) 
        {
            XElement result = new XElement(nodeName);
            if (nodeName == "myTypeShort") 
            {
                return null;
            }
            if (DirectXmlSaver.IsSimpleTextType(ob.GetType()))
            {
                result = new XElement(nodeName, ob.ToString());
            }
            else if (GenTypes.IsDef(ob.GetType()))
            {
                string defName = ((Def)ob).defName;
                result = new XElement(nodeName, defName);
            }
            else if (ob is IntVec3 pos)
            {
                result.Add(new XElement(nodeName, $"({pos.x},{pos.y},{pos.z})"));
                return result;
            }
            else 
            {
                SaveFields(ob).ForEach(x => result.Add(x));
            }
            return result;
        }
        private static List<XElement> SaveFields(object ob)
        {
            List<XElement> result = new List<XElement>();
            foreach (FieldInfo f in ob.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Type type = f.FieldType;
                object fieledOb = f.GetValue(ob);
                if (fieledOb == null || (fieledOb is string text && text.NullOrEmpty()) || type.Name == "myTypeShort")
                {
                    continue;
                }
                if (f.Name == "myTypeShort")
                {
                    continue;
                }
                if (DirectXmlSaver.IsSimpleTextType(type))
                {
                    result.Add(new XElement(f.Name, fieledOb.ToString()));
                    continue;
                }
                if (GenTypes.IsDef(type))
                {
                    string defName = ((Def)fieledOb).defName;
                    result.Add(new XElement(f.Name, defName));
                    continue;
                }
                if(type == typeof(IntVec3)) 
                {
                    IntVec3 pos2 = (IntVec3)fieledOb;
                    result.Add(new XElement(f.Name,$"({pos2.x},{pos2.y},{pos2.z})"));
                    continue;
                }

                if (type.IsGenericType)
                {
                    result.AddRange(SaveGeneric(f, type, fieledOb));
                    continue;
                }
                if (typeof(QuestNode).IsAssignableFrom(f.FieldType))
                {
                    XElement nodeXml = new XElement(f.Name);
                    var fullName = GetName(fieledOb.GetType().FullName);
                    nodeXml.SetAttributeValue("Class", fullName);

                    SaveFields(fieledOb).ForEach(n => nodeXml.Add(n));
                    result.Add(nodeXml);
                    continue;
                }
                XElement node = DirectXmlSaver.XElementFromObject(fieledOb,type,f.Name, null, false);
                result.Add(node);
            } 
            return result;
        }

        public static string GetName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return fullName; 
            foreach (var ns in Names.Value)
            {
                if (fullName.StartsWith(ns + ".", StringComparison.Ordinal))
                {
                    return fullName.Substring(ns.Length + 1);
                }
            }

            return fullName; // 不在忽略列表内，保持原样
        }

        public static List<XElement> SaveGeneric(FieldInfo f, Type type, object fieledOb)
        {
            if (fieledOb == null) 
            {
                return null;
            }
            List<XElement> result = new List<XElement>();
            Type genericType = type.GetGenericTypeDefinition();   

            if (genericType == typeof(Nullable<>))
            {
                if (type.GetMethod("GetValueOrDefault", BindingFlags.Instance | BindingFlags.Public).Invoke(fieledOb,null) is object o2 && o2 != null)
                {
                   SaveFields(o2).ForEach(x =>  result.Add(x));
                }
            }
            if (genericType == typeof(SlateRef<>))
            {
                if (type.GetField("slateRef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fieledOb) is string slateRef && slateRef != null && slateRef != "")
                {
                    result.Add(new XElement(f.Name, slateRef));
                }
            }
            if (genericType == typeof(List<>))
            {
                XElement listNode = new XElement(f.Name);
                Type expectedType = type.GetGenericArguments()[0];
                int num = (int)type.GetProperty("Count").GetValue(fieledOb, null);
                for (int i = 0; i < num; i++)
                {
                    object[] index = new object[]
                    {
                        i
                    };
                    object liOb = type.GetProperty("Item").GetValue(fieledOb, index);
                    XElement li = SaveObject(liOb, "li");
                    if (expectedType != liOb.GetType())
                    {
                        li.SetAttributeValue("Class", GetName(liOb.GetType().FullName));
                    }
                    listNode.Add(li);
                }
                result.Add(listNode);
            }
            if (genericType == typeof(Dictionary<,>))
            {
                XElement listNode = new XElement(f.Name);
                Type expectedType3 = type.GetGenericArguments()[0];
                Type expectedType4 = type.GetGenericArguments()[1];
                IEnumerator enumerator = (fieledOb as IEnumerable).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    object obj2 = enumerator.Current;
                    object keyOb = obj2.GetType().GetProperty("Key").GetValue(obj2, null);
                    object valueOb = obj2.GetType().GetProperty("Value").GetValue(obj2, null);
                    XElement xelement3 = new XElement("li");
                    xelement3.Add(SaveObject(keyOb, "key"));
                    xelement3.Add(SaveObject(valueOb, "value"));
                    listNode.Add(xelement3);
                }
                result.Add(listNode);
            }
            return result;
        }

        private static Lazy<List<string>> Names = new Lazy<List<string>>(() => GenTypes.IgnoredNamespaceNames
            .OrderByDescending(n => n.Length).ToList());
    }
}
