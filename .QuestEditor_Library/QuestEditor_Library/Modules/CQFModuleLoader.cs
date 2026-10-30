using System.Reflection;
using System.Xml;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public static class CQFModuleLoader
{
    public static bool DomainLoaded => mapModuleFactory != null;

    public static void Load(ModContentPack framework, IReadOnlyList<ModContentPack> activeMods)
    {
        List<CQFModuleFolder> declarations = new();
        Dictionary<ModContentPack, List<string>> originals = new();
        try
        {
            foreach (ModContentPack content in activeMods)
            {
                originals.Add(content, content.foldersToLoadDescendingOrder.ToList());
                declarations.AddRange(ReadFolders(content, framework));
            }
            foreach (CQFModuleFolder folder in declarations)
                folder.Content.foldersToLoadDescendingOrder.RemoveAll(path => SamePath(path, folder.Path));
            List<CQFModuleFolder> requests = declarations.Where(folder => !folder.Provider).ToList();
            if (requests.Count == 0) return;
            CQFModuleFolder[] providers = declarations.Where(folder => folder.Provider && folder.Module == "Domain").ToArray();
            if (providers.Length != 1)
                throw new InvalidDataException("[CQF] Domain requested by " + string.Join(", ", requests.Select(folder => folder.Content.PackageId))
                    + " but exactly one selected IfCQFModule=Domain folder is required.");
            CQFModuleFolder provider = providers[0];
            ValidateFolder(provider);
            foreach (CQFModuleFolder request in requests) ValidateFolder(request);
            string modulePath = Path.Combine(provider.Path, "CQF.Domain.dll");
            if (!File.Exists(modulePath)) throw new FileNotFoundException("[CQF] Domain module requested by "
                + string.Join(", ", requests.Select(folder => folder.Content.PackageId)), modulePath);
            Assembly moduleAssembly = LoadAssembly(provider, modulePath);
            Type entry = moduleAssembly.GetType("QuestEditor_Library.CQFDomainModule", true)!;
            ((ICQFModule)Activator.CreateInstance(entry)!).Initialize();
            if (!DomainLoaded) throw new InvalidOperationException("[CQF] Domain module did not register its map runtime.");
            foreach (ModContentPack content in activeMods)
                foreach (string path in originals[content].AsEnumerable().Reverse())
                {
                    CQFModuleFolder? request = requests.FirstOrDefault(folder => folder.Content == content && SamePath(folder.Path, path));
                    if (request == null) continue;
                    ValidateFolder(request);
                    foreach (string file in Directory.GetFiles(request.Path, "*.dll", SearchOption.TopDirectoryOnly).OrderBy(file => file, StringComparer.Ordinal))
                        LoadAssembly(request, file);
                }
            foreach (KeyValuePair<ModContentPack, List<string>> original in originals)
            {
                original.Key.foldersToLoadDescendingOrder.Clear();
                original.Key.foldersToLoadDescendingOrder.AddRange(original.Value);
            }
            GenTypes.ClearCache();
        }
        catch (Exception error)
        {
            foreach (CQFModuleFolder folder in declarations)
                folder.Content.foldersToLoadDescendingOrder.RemoveAll(path => SamePath(path, folder.Path));
            string details = error.ToString();
            if (error is ReflectionTypeLoadException types && types.LoaderExceptions != null)
                details += "\n" + string.Join("\n", types.LoaderExceptions.Select(exception => exception?.ToString()));
            Log.Error("[CQF] Optional module loading failed: " + details);
            throw;
        }
    }

    public static void RegisterMapModuleFactory(Func<GameComponent_MapManager, ICQFMapModule> factory)
    {
        if (mapModuleFactory != null) throw new InvalidOperationException("[CQF] A map module factory is already registered.");
        mapModuleFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    internal static ICQFMapModule? CreateMapModule(GameComponent_MapManager host) => mapModuleFactory?.Invoke(host);

    private static IEnumerable<CQFModuleFolder> ReadFolders(ModContentPack content, ModContentPack framework)
    {
        string file = Path.Combine(content.RootDir, "LoadFolders.xml");
        if (!File.Exists(file)) yield break;
        XmlDocument xml = new() { XmlResolver = null };
        try
        {
            string contents = File.ReadAllText(file);
            if (contents.IndexOf("IfCQFModule", StringComparison.Ordinal) < 0 && contents.IndexOf("RequiresCQFModule", StringComparison.Ordinal) < 0)
                yield break;
            using StringReader source = new(contents);
            using XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            xml.Load(reader);
        }
        catch (Exception error)
        {
            throw new InvalidDataException("[CQF] Could not read module declarations for " + content.PackageId + ": " + file, error);
        }
        string root = Path.GetFullPath(content.RootDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        HashSet<string> selected = new(content.foldersToLoadDescendingOrder.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        HashSet<string> declared = new(StringComparer.OrdinalIgnoreCase);
        foreach (XmlNode node in xml.SelectNodes("/loadFolders/*/li[@IfCQFModule or @RequiresCQFModule]")!)
        {
            string version = node.ParentNode!.Name.ToLowerInvariant();
            if (version.StartsWith("v", StringComparison.Ordinal)) version = version.Substring(1);
            if (version != VersionControl.CurrentVersionStringWithoutBuild) continue;
            string relative = node.InnerText.Trim();
            string path = Path.GetFullPath(Path.Combine(root, relative));
            if (Path.IsPathRooted(relative) || !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("[CQF] Module folder escapes " + content.PackageId + ": " + relative);
            if (!selected.Contains(path)) continue;
            XmlAttribute? provider = node.Attributes!["IfCQFModule"];
            XmlAttribute? request = node.Attributes["RequiresCQFModule"];
            if (provider != null && request != null)
                throw new InvalidDataException("[CQF] Module folder cannot provide and request a module: " + content.PackageId + ": " + relative);
            string module = (provider ?? request)!.Value.Trim();
            if (module != "Domain") throw new InvalidDataException("[CQF] Unknown module requested by " + content.PackageId + ": " + module);
            if ((provider != null) != (content == framework))
                throw new InvalidDataException("[CQF] IfCQFModule belongs to CQF; submods must use RequiresCQFModule: " + content.PackageId);
            if (!declared.Add(path)) throw new InvalidDataException("[CQF] Repeated selected module folder: " + content.PackageId + ": " + relative);
            yield return new CQFModuleFolder(content, path, module, provider != null);
        }
    }

    private static void ValidateFolder(CQFModuleFolder folder)
    {
        if (!Directory.Exists(folder.Path)) throw new DirectoryNotFoundException("[CQF] Module folder for " + folder.Content.PackageId + ": " + folder.Path);
        string assemblies = Path.Combine(folder.Path, "Assemblies");
        if (Directory.Exists(assemblies) && Directory.GetFiles(assemblies, "*.dll", SearchOption.AllDirectories).Length != 0)
            throw new InvalidDataException("[CQF] Deferred DLLs must be at the module folder root, outside Assemblies: " + folder.Content.PackageId + ": " + folder.Path);
    }

    private static Assembly LoadAssembly(CQFModuleFolder folder, string path)
    {
        try
        {
            Assembly assembly = Assembly.LoadFrom(path);
            Type[] types = assembly.GetTypes();
            if (types.Any(type => !type.IsAbstract && typeof(Mod).IsAssignableFrom(type)))
                throw new InvalidDataException("Deferred module DLLs cannot contain Mod entry classes; keep independent Mod entry classes in normal Assemblies.");
            if (!folder.Content.assemblies.loadedAssemblies.Contains(assembly)) folder.Content.assemblies.loadedAssemblies.Add(assembly);
            GenTypes.ClearCache();
            return assembly;
        }
        catch (Exception error)
        {
            string details = error is ReflectionTypeLoadException types
                ? string.Join("\n", types.LoaderExceptions.Select(exception => exception?.ToString())) : error.Message;
            throw new InvalidDataException("[CQF] Could not load " + folder.Content.PackageId + ": " + path + "\n" + details, error);
        }
    }

    private static bool SamePath(string first, string second) => string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);

    private static Func<GameComponent_MapManager, ICQFMapModule>? mapModuleFactory;
}
