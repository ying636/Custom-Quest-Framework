using System.Reflection;
using System.Xml;
using RimWorld;
using Verse;

namespace QuestEditor_Library;

public static class CQFModuleLoader
{
    public static bool DomainLoaded => mapModuleFactory != null;

    public static bool IsLoaded(string module) => loadedModules.Contains(module);

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
            foreach (CQFModuleFolder request in requests) ValidateFolder(request);
            HashSet<string> requested = new(requests.SelectMany(folder => folder.Modules), StringComparer.Ordinal);
            List<CQFModuleFolder> selectedProviders = new();
            foreach (string module in requested.OrderBy(name => name, StringComparer.Ordinal))
            {
                CQFModuleFolder[] providers = declarations.Where(folder => folder.Provider && folder.Modules.Contains(module)).ToArray();
                string requesters = string.Join(", ", requests.Where(folder => folder.Modules.Contains(module)).Select(folder => folder.Content.PackageId).Distinct());
                if (providers.Length != 1)
                    throw new InvalidDataException("[CQF] " + module + " requested by " + requesters
                        + " but exactly one selected IfCQFModule=" + module + " folder is required.");
                ValidateFolder(providers[0]);
                string modulePath = Path.Combine(providers[0].Path, "CQF." + module + ".dll");
                if (!File.Exists(modulePath)) throw new FileNotFoundException("[CQF] " + module + " module requested by " + requesters, modulePath);
                selectedProviders.Add(providers[0]);
            }
            foreach (CQFModuleFolder provider in selectedProviders)
            {
                string module = provider.Modules[0];
                Assembly moduleAssembly = LoadAssembly(provider, Path.Combine(provider.Path, "CQF." + module + ".dll"));
                Type entry = moduleAssembly.GetType(moduleEntries[module], true)!;
                ((ICQFModule)Activator.CreateInstance(entry)!).Initialize();
                if (module == "Domain" && !DomainLoaded) throw new InvalidOperationException("[CQF] Domain module did not register its map runtime.");
                if (!loadedModules.Add(module)) throw new InvalidOperationException("[CQF] Module initialized more than once: " + module);
            }
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
                original.Key.foldersToLoadDescendingOrder.AddRange(original.Value.Where(path => !declarations.Any(folder => folder.Provider
                    && folder.Content == original.Key && SamePath(folder.Path, path) && !requested.Contains(folder.Modules[0]))));
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

    public static void RegisterMapLifetimeModule(ICQFMapLifetimeModule module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        if (mapLifetimeModules.Any(existing => existing.GetType() == module.GetType()))
            throw new InvalidOperationException("[CQF] A map lifetime module is already registered: " + module.GetType().FullName);
        mapLifetimeModules.Add(module);
    }

    internal static ICQFMapModule? CreateMapModule(GameComponent_MapManager host) => mapModuleFactory?.Invoke(host);

    internal static bool KeepsMapActive(Game game, Map map)
    {
        foreach (ICQFMapLifetimeModule module in mapLifetimeModules)
            if (module.KeepsMapActive(game, map)) return true;
        return false;
    }

    internal static void BeforeMapsDisposed(Game game)
    {
        List<Exception>? errors = null;
        foreach (ICQFMapLifetimeModule module in mapLifetimeModules)
        {
            try { module.BeforeMapsDisposed(game); }
            catch (Exception error) { (errors ??= new()).Add(error); }
        }
        if (errors != null) throw new AggregateException("[CQF] Optional map modules failed to release game state.", errors);
    }

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
            string[] modules = (provider ?? request)!.Value.Split(',').Select(name => name.Trim()).ToArray();
            foreach (string module in modules)
                if (!moduleEntries.ContainsKey(module)) throw new InvalidDataException("[CQF] Unknown module requested by " + content.PackageId + ": " + module);
            if (modules.Distinct(StringComparer.Ordinal).Count() != modules.Length || provider != null && modules.Length != 1)
                throw new InvalidDataException("[CQF] Invalid module list for " + content.PackageId + ": " + relative);
            if ((provider != null) != (content == framework))
                throw new InvalidDataException("[CQF] IfCQFModule belongs to CQF; submods must use RequiresCQFModule: " + content.PackageId);
            if (!declared.Add(path)) throw new InvalidDataException("[CQF] Repeated selected module folder: " + content.PackageId + ": " + relative);
            yield return new CQFModuleFolder(content, path, modules, provider != null);
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
    private static readonly HashSet<string> loadedModules = new(StringComparer.Ordinal);
    private static readonly List<ICQFMapLifetimeModule> mapLifetimeModules = new();
    private static readonly Dictionary<string, string> moduleEntries = new(StringComparer.Ordinal)
    {
        ["Domain"] = "QuestEditor_Library.CQFDomainModule",
        ["MapProjections"] = "QuestEditor_Library.CQFMapProjectionsModule"
    };
}
