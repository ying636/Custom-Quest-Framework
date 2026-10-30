using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using QuestEditor_Library;
using Verse;

internal static class OptionalModuleChecks
{
    public static void Run(string game, string mode)
    {
        typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!.FieldType));
        new Harmony("CQF.Checks.ModuleErrors").Patch(typeof(Log).GetMethod(nameof(Log.Error)),
            prefix: new HarmonyMethod(typeof(ChecksLogHandler).GetMethod(nameof(ChecksLogHandler.RecordError))));
        Assembly framework = typeof(GameComponent_MapManager).Assembly;
        Check(!framework.GetReferencedAssemblies().Any(reference => reference.Name == "CQF.Domain"), "core has no domain assembly reference");
        Check(framework.GetType("QuestEditor_Library.DomainDef") == null, "domain definitions are absent from core");
        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.Domain"), "domain starts unloaded");
        string providerRoot = Environment.GetEnvironmentVariable("CQF_CHECKS_PROVIDER_ROOT") ?? Path.Combine(game, "Mods", "CQF");
        ModContentPack owner = CreateContent(providerRoot, "hailuan.customquestframework", new[] { "hailuan.customquestframework" });
        owner.assemblies.loadedAssemblies.Add(framework);
        List<ModContentPack> active = new() { owner };
        string? fixture = null;
        bool enabled = mode is "modules-enabled" or "modules-adapter" or "modules-multiple" or "modules-encoding" or "modules-unrelated";
        if (enabled)
        {
            List<string> ids = new() { "hailuan.customquestframework", "hailuan.ritesofthesilenthouse" };
            if (mode == "modules-adapter")
            {
                ids.Add("hailuan.playerabilityframework");
                Assembly.LoadFrom(Path.Combine(game, "Mods", "玩家能力框架", "1.7", "Assemblies", "PlayerAbilityFramework.dll")).GetTypes();
            }
            active.Add(CreateContent(Path.Combine(game, "Mods", "寂屋邪祭"), ids[1], ids));
            if (mode == "modules-multiple")
            {
                fixture = Path.Combine(Path.GetTempPath(), "CQF_ModuleFixture_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path.Combine(fixture, "Domain"));
                File.WriteAllText(Path.Combine(fixture, "LoadFolders.xml"), "<loadFolders><v1.6><li RequiresCQFModule=\"CQF_Check_Unknown\">Domain</li></v1.6><v1.7><li>/</li><li RequiresCQFModule=\"Domain\">Domain</li></v1.7></loadFolders>");
                active.Add(CreateContent(fixture, "cqf.check.second.request", ids));
            }
        }
        else if (mode == "modules-filtered" || mode == "modules-invalid" || mode == "modules-layout")
        {
            fixture = Path.Combine(Path.GetTempPath(), "CQF_ModuleFixture_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fixture);
            string declaration = mode == "modules-filtered"
                ? "<li RequiresCQFModule=\"Domain\" IfModActive=\"CQF_Check_Absent\">NotSelected</li>"
                : mode == "modules-invalid" ? "<li RequiresCQFModule=\"CQF_Check_Unknown\">Selected</li>"
                : "<li RequiresCQFModule=\"Domain\">Selected</li>";
            File.WriteAllText(Path.Combine(fixture, "LoadFolders.xml"), "<loadFolders><v1.6><li RequiresCQFModule=\"Domain\">OldVersion</li></v1.6><v1.7><li>/</li>" + declaration + "</v1.7></loadFolders>");
            if (mode == "modules-layout")
            {
                Directory.CreateDirectory(Path.Combine(fixture, "Selected", "Assemblies"));
                File.WriteAllBytes(Path.Combine(fixture, "Selected", "Assemblies", "CQF_Check_TooEarly.dll"), Array.Empty<byte>());
            }
            active.Add(CreateContent(fixture, "cqf.check.module", Array.Empty<string>()));
        }
        if (mode is "modules-encoding" or "modules-unrelated" or "modules-malformed")
        {
            fixture = Path.Combine(Path.GetTempPath(), "CQF_ModuleFixture_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(fixture, "Domain"));
            string file = Path.Combine(fixture, "LoadFolders.xml");
            string declaration = mode == "modules-unrelated" ? "" : "<li RequiresCQFModule=\"Domain\">Domain</li>";
            string contents = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<loadFolders>\n<!-- CQF_Check_Encoding -->\n<v1.7><li>/</li>" + declaration + "</v1.7></loadFolders>";
            File.WriteAllText(file, contents);
            active.Add(CreateContent(fixture, "cqf.check.module", Array.Empty<string>()));
            if (mode == "modules-encoding")
            {
                byte[] prefix = System.Text.Encoding.UTF8.GetBytes(contents.Substring(0, contents.IndexOf("CQF_Check_Encoding", StringComparison.Ordinal)));
                byte[] suffix = System.Text.Encoding.UTF8.GetBytes(contents.Substring(contents.IndexOf(" -->", StringComparison.Ordinal)));
                File.WriteAllBytes(file, prefix.Concat(new byte[] { 0xd6, 0xd0 }).Concat(suffix).ToArray());
            }
            else File.WriteAllText(file, contents.Replace("</loadFolders>", ""));
        }
        LoadedModManager.RunningModsListForReading.AddRange(active);
        GenTypes.ClearCache();
        foreach (ModContentPack content in active)
            Check(!ModContentPack.GetAllFilesForMod(content, "Assemblies", extension => extension == ".dll").Values
                .Any(file => file.Name is "CQF.Domain.dll" or "RSH.dll" or "RSH.PlayerAbilityFramework.dll"),
                "native initial scan defers module DLLs for " + content.PackageId);
        if (mode is "modules-invalid" or "modules-layout" or "modules-malformed")
        {
            try { CQFModuleLoader.Load(owner, active); throw new InvalidOperationException("Invalid declaration was accepted."); }
            catch (InvalidDataException error)
            {
                Check(error.ToString().Contains("cqf.check.module"), "invalid module packaging identifies the requesting submod");
                Check(ChecksLogHandler.ErrorCount > 0, "invalid module packaging is logged");
                if (mode == "modules-malformed")
                {
                    Check(error.Message.Contains(Path.Combine(fixture!, "LoadFolders.xml")), "malformed module XML identifies the file");
                    Check(error.InnerException is XmlException, "malformed module XML preserves the parsing exception");
                }
            }
            return;
        }
        CQFModuleLoader.Load(owner, active);
        if (mode is "modules-encoding" or "modules-unrelated")
            Check(ChecksLogHandler.ErrorCount == 0, "unrelated XML and native-compatible comment encoding do not interrupt module loading");
        Check(CQFModuleLoader.DomainLoaded == enabled, "only selected child requests enable domain");
        Check(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.Domain") == enabled,
            "domain DLL loading follows the request");
        Game gameState = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        gameState.components = new();
        GameComponent_MapManager manager = new(gameState);
        gameState.components.Add(manager);
        Current.Game = gameState;
        Check((manager.Module != null) == enabled, "map manager creates domain state only when enabled");
        Check(owner.foldersToLoadDescendingOrder.Any(folder => folder.EndsWith(Path.Combine("Optional", "Domain"))) == enabled,
            "LoadFolders removes the unrequested provider");
        if (enabled)
        {
            Assembly domain = owner.assemblies.loadedAssemblies.Single(assembly => assembly.GetName().Name == "CQF.Domain");
            Check(!domain.GetTypes().Any(type => typeof(GameComponent).IsAssignableFrom(type)), "domain adds no GameComponent");
            Check(GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.DomainDef")?.Assembly == domain, "domain XML type resolves after module registration");
            Check(active[1].assemblies.loadedAssemblies.Any(assembly => assembly.GetName().Name == "RSH"), "dependent RSH DLL loads after domain");
            Check(active[1].assemblies.loadedAssemblies.Any(assembly => assembly.GetName().Name == "RSH.PlayerAbilityFramework") == (mode == "modules-adapter"),
                "native adapter condition is preserved");
            Game other = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            other.components = new();
            GameComponent_MapManager otherManager = new(other);
            other.components.Add(otherManager);
            Check(!ReferenceEquals(manager.Module, otherManager.Module), "separate games own separate domain runtimes");
        }
        for (int update = 0; update < 16; update++) manager.GameComponentUpdate();
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int update = 0; update < 256; update++) manager.GameComponentUpdate();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Check(allocated == 0, "idle module host allocates zero bytes (actual: " + allocated + ")");
        manager.DisposeMaps();
        Console.WriteLine("Optional module checks passed: " + mode);
    }

    private static ModContentPack CreateContent(string root, string packageId, IEnumerable<string> activeIds)
    {
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        typeof(ModContentPack).GetField("rootDirInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(content, new DirectoryInfo(root));
        typeof(ModContentPack).GetField("packageIdInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(content, packageId);
        XmlDocument xml = new();
        xml.LoadXml(File.ReadAllText(Path.Combine(root, "LoadFolders.xml"), System.Text.Encoding.UTF8));
        ModLoadFolders folders = DirectXmlToObject.ObjectFromXml<ModLoadFolders>(xml.DocumentElement!, false);
        HashSet<string> active = new(activeIds, StringComparer.OrdinalIgnoreCase);
        content.foldersToLoadDescendingOrder = folders.FoldersForVersion("1.7")
            .Where(folder => (folder.requiredAnyOfPackageIds.NullOrEmpty() || folder.requiredAnyOfPackageIds.Any(active.Contains))
                && (folder.requiredAllOfPackageIds.NullOrEmpty() || folder.requiredAllOfPackageIds.All(active.Contains))
                && (folder.disallowedAnyOfPackageIds.NullOrEmpty() || !folder.disallowedAnyOfPackageIds.Any(active.Contains)))
            .Reverse().Select(folder => Path.GetFullPath(Path.Combine(root, folder.folderName == "/" ? "." : folder.folderName))).ToList();
        content.assemblies = new ModAssemblyHandler(content);
        return content;
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
