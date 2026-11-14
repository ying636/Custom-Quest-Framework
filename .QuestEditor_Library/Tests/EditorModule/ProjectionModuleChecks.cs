using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using QuestEditor_Library;
using Verse;

internal static class ProjectionModuleChecks
{
    public static void Run(string game, string mode)
    {
        FieldInfo prefs = AccessTools.Field(typeof(Prefs), "data");
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        new Harmony("CQF.Checks.ProjectionModules").Patch(AccessTools.Method(typeof(Log), nameof(Log.Error)),
            prefix: new HarmonyMethod(typeof(ChecksLogHandler), nameof(ChecksLogHandler.RecordError)));
        string root = Environment.GetEnvironmentVariable("CQF_CHECKS_PROVIDER_ROOT") ?? throw new InvalidOperationException("CQF_CHECKS_PROVIDER_ROOT");
        string fixture = Path.Combine(Path.GetTempPath(), "CQF_ProjectionModuleChecks_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(fixture, "Request"));
        string request = mode switch
        {
            "projections-both" => "Domain,MapProjections",
            "projections-domain" => "Domain",
            "projections-duplicate" => "MapProjections,MapProjections",
            "projections-unknown" => "MapProjections,CQF_Unknown",
            _ => "MapProjections"
        };
        string declaration = mode == "projections-disabled" ? "" : "<li RequiresCQFModule=\"" + request + "\""
            + (mode == "projections-filtered" ? " IfModActive=\"CQF_Absent\"" : "") + ">Request</li>";
        File.WriteAllText(Path.Combine(fixture, "LoadFolders.xml"), "<loadFolders><v1.6><li RequiresCQFModule=\"CQF_OldVersion\">Old</li></v1.6><v1.7><li>/</li>" + declaration + "</v1.7></loadFolders>");
        if (mode == "projections-early")
        {
            Directory.CreateDirectory(Path.Combine(fixture, "Request", "Assemblies"));
            File.WriteAllBytes(Path.Combine(fixture, "Request", "Assemblies", "CQF_TooEarly.dll"), Array.Empty<byte>());
        }
        ModContentPack owner = CreateContent(root, "hailuan.customquestframework");
        owner.assemblies.loadedAssemblies.Add(typeof(CQFModuleLoader).Assembly);
        ModContentPack child = CreateContent(fixture, "cqf.check.projection.requester");
        List<ModContentPack> active = new() { owner, child };
        LoadedModManager.RunningModsListForReading.AddRange(active);
        Check(!typeof(CQFModuleLoader).Assembly.GetReferencedAssemblies().Any(assembly => assembly.Name is "CQF.MapProjections" or "CQF.Domain"), "core has no optional module assembly references");
        Check(typeof(CQFModuleLoader).Assembly.GetType("QuestEditor_Library.CQFMapProjection") == null, "projection types are absent from the core assembly");
        foreach (ModContentPack content in active)
            Check(!ModContentPack.GetAllFilesForMod(content, "Assemblies", extension => extension == ".dll").Values
                .Any(file => file.Name is "CQF.MapProjections.dll" or "CQF.Domain.dll"), "native automatic assembly discovery excludes optional DLLs");
        if (mode is "projections-duplicate" or "projections-unknown" or "projections-early")
        {
            bool rejected = false;
            try { CQFModuleLoader.Load(owner, active); }
            catch (InvalidDataException error)
            {
                rejected = error.ToString().Contains(child.PackageId, StringComparison.Ordinal);
            }
            Check(rejected && ChecksLogHandler.ErrorCount > 0, "invalid projection declarations fail visibly and identify the requesting submod");
            Check(!CQFModuleLoader.IsLoaded("MapProjections"), "invalid declarations do not initialize the projection module");
            return;
        }
        CQFModuleLoader.Load(owner, active);
        bool projections = mode is "projections-enabled" or "projections-both";
        bool domain = mode is "projections-domain" or "projections-both";
        Check(CQFModuleLoader.IsLoaded("MapProjections") == projections && CQFModuleLoader.DomainLoaded == domain, "module requests activate only their selected modules");
        Check(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.MapProjections") == projections, "projection assembly loading matches demand");
        Check(owner.foldersToLoadDescendingOrder.Any(path => path.EndsWith(Path.Combine("Optional", "MapProjections"))) == projections, "unrequested projection content is excluded");
        Check(owner.foldersToLoadDescendingOrder.Any(path => path.EndsWith(Path.Combine("Optional", "Domain"))) == domain, "unrequested domain content is excluded even when projections are requested");
        Check((GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.CQFMapProjection") != null) == projections, "projection XML type discovery matches demand");
        Check(Harmony.GetAllPatchedMethods().Any(method => Harmony.GetPatchInfo(method)?.Owners.Contains("CQF.MapProjections") == true) == projections, "projection Harmony installation matches demand");
        Game state = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        state.components = new();
        GameComponent_MapManager manager = new(state);
        state.components.Add(manager);
        Current.Game = state;
        Check((manager.Module != null) == domain, "domain runtime remains independent of optional projection state");
        manager.DisposeMaps();
        Console.WriteLine("Projection module checks passed: " + mode);
    }

    private static ModContentPack CreateContent(string root, string id)
    {
        ModContentPack content = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        AccessTools.Field(typeof(ModContentPack), "rootDirInt").SetValue(content, new DirectoryInfo(root));
        AccessTools.Field(typeof(ModContentPack), "packageIdInt").SetValue(content, id);
        XmlDocument xml = new();
        xml.LoadXml(File.ReadAllText(Path.Combine(root, "LoadFolders.xml"), System.Text.Encoding.UTF8));
        ModLoadFolders folders = DirectXmlToObject.ObjectFromXml<ModLoadFolders>(xml.DocumentElement!, false);
        content.foldersToLoadDescendingOrder = folders.FoldersForVersion("1.7")
            .Where(folder => folder.requiredAnyOfPackageIds.NullOrEmpty() && folder.requiredAllOfPackageIds.NullOrEmpty())
            .Reverse().Select(folder => Path.GetFullPath(Path.Combine(root, folder.folderName == "/" ? "." : folder.folderName))).ToList();
        content.assemblies = new ModAssemblyHandler(content);
        return content;
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }
}
