using System.Reflection;
using System.Xml;
using QuestEditor_Library;
using Verse;

internal static class EditorDesignatorPatchChecks
{
    public static void Run(string game, Assembly? editor, ModContentPack content)
    {
        string modRoot = Path.Combine(game, "Mods", "CQF");
        foreach (string version in new[] { "1.4", "1.5" })
        {
            Dictionary<string, FileInfo> legacy = ModContentPack.GetAllFilesForMod(null!, "Patches", foldersToLoadDebug: new List<string> { Path.Combine(modRoot, version), modRoot });
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(File.ReadAllText(legacy[Path.Combine("Patches", "CQF_EditorDesignators.xml")].FullName, System.Text.Encoding.UTF8));
            if (xml.SelectSingleNode("/Patch/Operation")!.Attributes!["Class"]!.Value != "PatchOperationAdd")
                throw new InvalidOperationException("Legacy editor patch is not native XML.");
        }
        Console.WriteLine("PASS legacy versions retain native editor patches");
        Dictionary<string, FileInfo> files = ModContentPack.GetAllFilesForMod(content, "Patches");
        string key = Path.Combine("Patches", "CQF_EditorDesignators.xml");
        if (editor == null)
        {
            if (files.ContainsKey(key)) throw new InvalidOperationException("Disabled editor patch entered native file discovery.");
            foreach (FileInfo file in files.Values)
                if (File.ReadAllText(file.FullName, System.Text.Encoding.UTF8).Contains("QuestEditor_Library.Designator_DestroyThing"))
                    throw new InvalidOperationException("Editor designators remain in an unconditional patch.");
            if (DirectXmlLoader.XmlAssetsInModFolder(content, "Patches/").Any(asset => asset.name == "CQF_EditorDesignators.xml"))
                throw new InvalidOperationException("Disabled editor XML asset was loaded.");
            Console.WriteLine("PASS disabled editor XML never enters native patch loading");
            return;
        }
        string expectedFile = Path.Combine(modRoot, "1.6", "Optional", "Editor", key);
        if (files[key].FullName != expectedFile) throw new InvalidOperationException("Editor patch did not come from its conditional folder.");
        if (files.Values.Count(file => file.Name == "CQF_EditorDesignators.xml") != 1)
            throw new InvalidOperationException("Editor patch was loaded more than once.");
        if (DirectXmlLoader.XmlAssetsInModFolder(content, "Patches/").Count(asset => asset.name == "CQF_EditorDesignators.xml" && asset.xmlDoc != null) != 1)
            throw new InvalidOperationException("Native XML loader did not select the editor patch exactly once.");
        XmlDocument patchXml = new XmlDocument();
        patchXml.LoadXml(File.ReadAllText(expectedFile, System.Text.Encoding.UTF8));
        XmlNode operation = patchXml.SelectSingleNode("/Patch/Operation")!;
        if (operation.Attributes!["Class"]!.Value != "PatchOperationAdd")
            throw new InvalidOperationException("Editor patch still requires a custom patch operation.");
        PatchOperation patch = DirectXmlToObject.ObjectFromXml<PatchOperation>(operation, false);
        if (patch.GetType() != typeof(PatchOperationAdd)) throw new InvalidOperationException("Editor patch is not the native operation type.");
        XmlDocument definitions = new XmlDocument();
        definitions.LoadXml("<Defs><DesignationCategoryDef><defName>Orders</defName><specialDesignatorClasses><li>RimWorld.Designator_Cancel</li></specialDesignatorClasses></DesignationCategoryDef></Defs>");
        if (!patch.Apply(definitions)) throw new InvalidOperationException("Editor designator patch failed.");
        string[] actual = definitions.SelectNodes("/Defs/DesignationCategoryDef/specialDesignatorClasses/li")!.Cast<XmlNode>().Skip(1).Select(node => node.InnerText).ToArray();
        string[] expected = operation.SelectNodes("value/li")!.Cast<XmlNode>().Select(node => node.InnerText).ToArray();
        if (!expected.SequenceEqual(actual)) throw new InvalidOperationException("Native editor patch did not preserve all designators.");
        foreach (string name in actual)
        {
            Type type = GenTypes.GetTypeInAnyAssembly(name);
            if (type == null || type.Assembly != editor || !typeof(Designator).IsAssignableFrom(type))
                throw new InvalidOperationException("Unresolved editor designator: " + name);
        }
        Console.WriteLine("PASS native XML patch injects and resolves all " + actual.Length + " designators");
        XmlDocument missingCategory = new XmlDocument();
        missingCategory.LoadXml("<Defs/>");
        if (patch.Apply(missingCategory)) throw new InvalidOperationException("Native patch hid a missing XPath error.");
        Console.WriteLine("PASS native patch preserves missing XPath failure");
    }
}
