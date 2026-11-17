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
        CheckQuestBookTargets();
    }

    private static void CheckQuestBookTargets()
    {
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
        prefs.SetValue(null, System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        ModContentPack content = (ModContentPack)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        content.assemblies = new ModAssemblyHandler(content);
        content.assemblies.loadedAssemblies.Add(typeof(QuestBookDef).Assembly);
        LoadedModManager.RunningModsListForReading.Add(content);
        GenTypes.ClearCache();
        ResearchProjectDef research = new() { defName = "CQF_Check_ResearchTarget" };
        ThingDef thing = new() { defName = "CQF_Check_ThingTarget" };
        DefDatabase<ResearchProjectDef>.Add(research);
        DefDatabase<ThingDef>.Add(thing);
        QuestBookStep step = new()
        {
            id = "CQF_Check_Step",
            objectives = new()
            {
                new QuestBookObjective_Research { TargetResearch = research },
                new QuestBookObjective_Building { TargetThingDef = thing, TargetCount = 3 },
                new QuestBookObjective_Resource { TargetThingDef = thing, TargetCount = 7 }
            }
        };
        QuestBookDef book = new()
        {
            defName = "CQF_Check_Book",
            chapters = new() { new QuestBookChapter { id = "CQF_Check_Chapter", steps = new() { step } } }
        };
        XElement xml = book.SaveToXElement("QuestEditor_Library.QuestBookDef");
        XElement[] objectives = xml.Descendants("objectives").Single().Elements("li").ToArray();
        Check(objectives[0].Element("targetResearch")?.Value == research.defName, "quest book exports the selected research as a defName reference");
        Check(objectives[1].Element("targetThingDef")?.Value == thing.defName, "quest book exports the selected building as a defName reference");
        Check(objectives[2].Element("targetThingDef")?.Value == thing.defName, "quest book exports the selected resource as a defName reference");
        XmlDocument document = new();
        document.LoadXml(xml.ToString());
        QuestBookDef restored = DirectXmlToObject.ObjectFromXml<QuestBookDef>(document.DocumentElement!, false);
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        List<QuestBookObjective> restoredObjectives = restored.chapters.Single().steps.Single().objectives;
        Check(ReferenceEquals(restoredObjectives[0].TargetResearch, research), "selected research survives quest book XML reload");
        Check(ReferenceEquals(restoredObjectives[1].TargetThingDef, thing) && restoredObjectives[1].TargetCount == 3,
            "selected building and target count survive quest book XML reload");
        Check(ReferenceEquals(restoredObjectives[2].TargetThingDef, thing) && restoredObjectives[2].TargetCount == 7,
            "selected resource and target count survive quest book XML reload");
        Check(new QuestBookObjective_Research().SaveToXElement("li").Element("targetResearch") == null,
            "an unselected research target remains absent from exported XML");
        Check(new QuestBookObjective_Building().SaveToXElement("li").Element("targetThingDef") == null,
            "an unselected thing target remains absent from exported XML");
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
