using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using QuestEditor_Library;
using Verse;

internal static class ConditionalLoadFolderChecks
{
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "CQF_Conditional_Check_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string editor = Path.Combine(root, "Editor");
            foreach (string category in new[] { "Defs", "Patches", "Textures", "Strings", "Languages" })
            {
                Directory.CreateDirectory(Path.Combine(editor, category));
                File.WriteAllText(Path.Combine(editor, category, "CQF_Check.xml"), "<Defs />", new UTF8Encoding(false));
            }
            string loadFile = Path.Combine(root, "LoadFolders.xml");
            string config = "<loadFolders><v1.6><li>/</li><li IfCQFSetting=\"enableEditor\">Editor</li><li IfCQFSetting=\"enableEditor\" IfModActive=\"CQF_Check_Unloaded\">Absent</li></v1.6></loadFolders>";
            File.WriteAllText(loadFile, config, new UTF8Encoding(false));
            ModContentPack content = CreateContent(root, config);
            new CQFConditionalLoadFolders(content, new CustomQuestFramework_ModSetting());
            foreach (string category in new[] { "Defs", "Patches", "Textures", "Strings", "Languages" })
                Check(ModContentPack.GetAllFilesForMod(content, category).Count == 0, "disabled conditional folder excludes " + category);
            content = CreateContent(root, config);
            CQFConditionalLoadFolders enabled = new CQFConditionalLoadFolders(content, new CustomQuestFramework_ModSetting { enableEditor = true });
            Check(enabled.GetFolders("enableEditor").SequenceEqual(new[] { editor }), "enabled custom condition preserves native folder order");
            Check(!enabled.GetFolders("enableEditor").Contains(Path.Combine(root, "Absent")), "settings condition cannot re-enable a folder excluded by native conditions");
            foreach (string category in new[] { "Defs", "Patches", "Textures", "Strings", "Languages" })
                Check(ModContentPack.GetAllFilesForMod(content, category).Count == 1, "enabled conditional folder exposes " + category + " to native discovery");
            enabled.Exclude("enableEditor");
            Check(content.foldersToLoadDescendingOrder.SequenceEqual(new[] { root }), "failed module exclusion preserves ordinary folders");
            string invalid = config.Replace("enableEditor", "CQF_Check_Unknown");
            File.WriteAllText(loadFile, invalid, new UTF8Encoding(false));
            content = CreateContent(root, invalid);
            try
            {
                new CQFConditionalLoadFolders(content, new CustomQuestFramework_ModSetting());
                throw new Exception("Unknown setting was silently accepted.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("CQF_Check_Unknown"))
            {
                Check(!content.foldersToLoadDescendingOrder.Contains(editor), "unknown condition reports error and excludes its folder");
            }
            File.WriteAllText(loadFile, config, new UTF8Encoding(false));
            content = CreateContent(root, config);
            CQFConditionalLoadFolders missingModule = new CQFConditionalLoadFolders(content, new CustomQuestFramework_ModSetting { enableEditor = true });
            int errors = ChecksLogHandler.ErrorCount;
            CQFEditorLoader.Load(content, missingModule.GetFolders("enableEditor"));
            Check(CQFEditorLoader.LoadError != null && ChecksLogHandler.ErrorCount > errors && !CQFEditorBridge.IsLoaded, "missing DLL reports full failure before patch processing");
            missingModule.Exclude("enableEditor");
            Check(ModContentPack.GetAllFilesForMod(content, "Patches").Count == 0, "missing DLL cannot leave its patch enabled");
        }
        finally
        {
            string full = Path.GetFullPath(root);
            if (Path.GetDirectoryName(full) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) || !Path.GetFileName(full).StartsWith("CQF_Conditional_Check_", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected check directory: " + full);
            Directory.Delete(full, true);
        }
    }

    private static ModContentPack CreateContent(string root, string config)
    {
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        typeof(ModContentPack).GetField("rootDirInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(content, new DirectoryInfo(root));
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(config);
        ModLoadFolders native = DirectXmlToObject.ObjectFromXml<ModLoadFolders>(xml.DocumentElement!, false);
        LoadFolder[] declarations = native.FoldersForVersion("1.6").ToArray();
        Check(declarations.Single(folder => folder.folderName == "Absent").requiredAnyOfPackageIds.SequenceEqual(new[] { "CQF_Check_Unloaded" }), "native parser retains IfModActive alongside custom condition");
        content.foldersToLoadDescendingOrder = declarations.Where(folder => folder.requiredAnyOfPackageIds == null && folder.requiredAllOfPackageIds == null && folder.disallowedAnyOfPackageIds == null && folder.ShouldLoad)
            .Reverse().Select(folder => Path.Combine(root, folder.folderName)).ToList();
        return content;
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
