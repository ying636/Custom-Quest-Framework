using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using QuestEditor_Library;
using Verse;

internal static class SerializationChecks
{
    public static void Run()
    {
        Fixture source = new();
        XElement xml = CQFSerialization.XElementFromObject(source, typeof(Fixture), "data");
        Check(xml.Element("custom")?.Element("written")?.Value == "CQF_Check_Custom", "nested ISaveable uses its own serializer");
        Check(xml.Element("cells")?.Element("li")?.Value == "(2,1,3)", "nested list preserves all IntVec3 coordinates");
        Check(xml.Element("entries")?.Element("li")?.Element("value")?.Element("written")?.Value == "CQF_Check_Custom",
            "dictionary values use the CQF serializer recursively");
        Check(xml.Element("absent")?.Attribute("IsNull")?.Value == "True", "null fields retain native XML null markers");
        Check(xml.Element("ignored") == null && xml.Element("defaulted") == null, "unsaved and default fields retain native filtering");
        Check(xml.Element("reference")?.Element("li")?.Value == "CQF_Check_Reference", "ordinary Def list elements remain defName references");
        Check(xml.Element("polymorphic")?.Attribute("Class")?.Value == typeof(PlainObject).FullName,
            "plain polymorphic objects retain their runtime class");
        XmlDocument document = new();
        document.LoadXml(CQFSerialization.XElementFromObject(new List<IntVec3> { new(2, 1, 3) }, typeof(List<IntVec3>), "cells").ToString());
        List<IntVec3> restored = DirectXmlToObject.ObjectFromXml<List<IntVec3>>(document.DocumentElement!, false);
        Check(restored.Single() == new IntVec3(2, 1, 3), "CQF coordinate list round-trips through native XML loader");
        XElement vanilla = DirectXmlSaver.XElementFromObject(new Saveable(), typeof(Saveable), "ordinary");
        Check(vanilla.Element("written") == null, "native XML serialization is unaffected by CQF ISaveable handling");
        Check(typeof(CustomMapDataDef).Assembly.GetType("QuestEditor_Library.Patch_SaveXml") == null, "CQF exports require no global XML patch");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }

    private sealed class Fixture
    {
        public Saveable custom = new();
        public List<IntVec3> cells = new() { new IntVec3(2, 1, 3) };
        public Dictionary<string, Saveable> entries = new() { ["CQF_Check_Key"] = new() };
        public object? absent = null;
        [Unsaved] public int ignored = 12;
        [DefaultValue(3)] public int defaulted = 3;
        public List<ThingDef> reference = new() { new ThingDef { defName = "CQF_Check_Reference" } };
        public object polymorphic = new PlainObject();
    }

    private sealed class Saveable : ISaveable
    {
        public XElement SaveToXElement(string nodeName) => new(nodeName, new XElement("written", "CQF_Check_Custom"));
    }

    private sealed class PlainObject
    {
        public string value = "CQF_Check_Polymorphic";
    }
}
