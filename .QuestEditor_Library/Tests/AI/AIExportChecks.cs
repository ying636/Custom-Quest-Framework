using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using QuestEditor_Library;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;

internal static class AIExportChecks
{
    public static void Run(CQFAIModel model, CustomMapDataDef map)
    {
        int errors = ChecksLogHandler.ErrorCount;
        foreach (Type type in model.Types.Where(type => type.Assembly == typeof(DialogTreeDef).Assembly && typeof(Def).IsAssignableFrom(type) && !type.IsAbstract))
        {
            Def definition = type == typeof(CustomMapDataDef) ? map : (Def)Activator.CreateInstance(type)!;
            definition.defName = "CQF_Check_Export_" + type.Name;
            definition.label = "CQF_Check_Export_Label";
            if (definition is DutyMapDef duty) duty.CreateNode();
            XElement xml = CQFAIDefDocument.Export(definition);
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml.ToString());
            MethodInfo parse = typeof(DirectXmlToObject).GetMethods().Single(method => method.Name == "ObjectFromXml" && method.IsGenericMethodDefinition);
            Def restored = (Def)parse.MakeGenericMethod(type).Invoke(null, new object[] { document.DocumentElement!, false })!;
            Check(restored.defName == definition.defName && restored.label == definition.label, "native XML roundtrip for " + type.Name);
        }
        QuestScriptDef quest = new QuestScriptDef { defName = "CQF_Check_Export_Quest", root = new QuestNode_DoCQFActions { inSignal = new SlateRef<string>("$CQF_Check_Signal") } };
        QuestScriptDef restoredQuest = Parse<QuestScriptDef>(CQFAIDefDocument.Export(quest));
        Check(((ISlateRef)((QuestNode_DoCQFActions)restoredQuest.root).inSignal).SlateRef == "$CQF_Check_Signal", "exported quest preserves SlateRef expression");
        DutyDef dutyDefinition = new DutyDef { defName = "CQF_Check_Export_Duty", thinkNode = new ThinkNode_Priority { subNodes = new List<ThinkNode> { new ThinkNode_Priority { tag = "CQF_Check_Tag" } } } };
        DutyDef restoredDuty = Parse<DutyDef>(CQFAIDefDocument.Export(dutyDefinition));
        Check(restoredDuty.thinkNode.subNodes.Single().tag == "CQF_Check_Tag", "exported DutyDef preserves nested ThinkNodes");
        CustomMapDataDef restoredMap = Parse<CustomMapDataDef>(CQFAIDefDocument.Export(map));
        Check(restoredMap.terrainsRect.Single().Value.Sum(rect => rect.Area) == map.size.x * map.size.z, "exported map retains terrain rectangles");
        map.terrains.Clear(); map.terrainsRect.Clear();
        map.terrains.Add("CQF_Check_Floor", new List<IntVec3> { new IntVec3(1, 0, 1), new IntVec3(2, 0, 2) });
        XElement cacheXml = CQFAIDefDocument.Export(map);
        Check(cacheXml.Element("terrainsRect")!.Descendants("li").Any() && map.terrainsRect.Count == 0, "cell edits export as rectangles without mutating source");
        Check(CQFSerialization.SaveDictionary(new Dictionary<string, int> { ["CQF_Check_A"] = 1, ["CQF_Check_B"] = 2 }, "values").Elements("li").Count() == 2, "native dictionary export writes one entry per key");
        Check(ChecksLogHandler.ErrorCount == errors, "all native export roundtrips report no parse errors");
        string root = Path.Combine(Path.GetTempPath(), "CQF_Export_Check_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            CQFContentPaths.Initialize(root);
            RuntimeHelpers.RunClassConstructor(typeof(CQFQuestDefBootstrap).TypeHandle);
            string questPath = CQFAIDefDocument.Save(quest);
            Check(Path.GetDirectoryName(questPath) == CQFContentPaths.Quests && DefDatabase<QuestScriptDef>.GetNamedSilentFail(quest.defName) == quest, "quest save uses bootstrap directory and registers live resource");
            ReplacementDataDef replacement = new ReplacementDataDef { defName = "CQF_Check_Export_Replacement" };
            string path = CQFAIDefDocument.Save(replacement);
            Check(Path.GetDirectoryName(path) == Path.Combine(CQFContentPaths.Quests, "AI") && DefDatabase<ReplacementDataDef>.GetNamedSilentFail(replacement.defName) == replacement, "additional CQF definition saves and registers");
            typeof(DefDatabase<ReplacementDataDef>).GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { replacement });
            Check(DefDatabase<ReplacementDataDef>.GetNamedSilentFail(replacement.defName) == null, "reload check removes in-memory definition first");
            typeof(CQFQuestDefBootstrap).GetMethod("LoadAdditionalDefs", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(ReplacementDataDef)).Invoke(null, null);
            Check(DefDatabase<ReplacementDataDef>.AllDefs.Count(def => def.defName == replacement.defName) == 1 && DefDatabase<ReplacementDataDef>.GetNamedSilentFail(replacement.defName) != replacement, "additional definition reload parses the saved file");
        }
        finally
        {
            string full = Path.GetFullPath(root);
            if (Path.GetDirectoryName(full) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(full).StartsWith("CQF_Export_Check_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected check directory.");
            Directory.Delete(full, true);
        }
    }
    private static T Parse<T>(XElement xml)
    {
        XmlDocument document = new XmlDocument(); document.LoadXml(xml.ToString());
        return DirectXmlToObject.ObjectFromXml<T>(document.DocumentElement!, false);
    }
    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
