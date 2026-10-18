using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;
using System.Xml;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using UnityEngine;
using Verse;
using Waylith.Rendering;

internal static class DomainVisualChecks
{
    public static void Run()
    {
        string root = Environment.GetEnvironmentVariable("CQF_CHECKS_PROVIDER_ROOT") ?? Directory.GetCurrentDirectory();
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        _ = new AbilityDef();
        new Harmony("CQF.Checks.DomainVisualXml").Patch(typeof(Log).GetMethod(nameof(Log.Error)),
            prefix: new HarmonyMethod(typeof(DomainVisualChecks).GetMethod(nameof(ReportXmlError), BindingFlags.Static | BindingFlags.Public)));
        ModContentPack moduleContent = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        typeof(ModContentPack).GetField("rootDirInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(moduleContent, new DirectoryInfo(root));
        moduleContent.foldersToLoadDescendingOrder = new List<string> { root };
        moduleContent.assetBundles = new ModAssetBundlesHandler(moduleContent);
        moduleContent.assemblies = new ModAssemblyHandler(moduleContent);
        moduleContent.assemblies.loadedAssemblies.Add(typeof(DomainDef).Assembly);
        moduleContent.assemblies.loadedAssemblies.Add(typeof(GameComponent_MapManager).Assembly);
        LoadedModManager.RunningModsListForReading.Add(moduleContent);
        GenTypes.ClearCache();
        DomainVisualDef valid = new() { defName = "CQF_Check_Visuals" };
        Check(!valid.ConfigErrors().Any(), "neutral visual preset has valid stage timing");
        DomainVisualDef invalid = new() { defName = "CQF_Check_InvalidVisuals", radius = float.NaN, openingTicks = 0,
            curtainStart = 0.8f, curtainEnd = 0.2f, coverProgress = 0.7f, revealProgress = 0.4f, flowSpeed = new Vector2(float.PositiveInfinity, 0f) };
        string errors = string.Join("\n", invalid.ConfigErrors());
        Check(errors.Contains("positive") && errors.Contains("ordered") && errors.Contains("coverProgress") && errors.Contains("finite"),
            "invalid dimensions, stages and material parameters report errors");
        Check(typeof(DomainOpening_Basic).IsSubclassOf(typeof(DomainOpening)) && typeof(DomainMaterialTransition).IsSubclassOf(typeof(DomainTransition)),
            "basic visuals reuse domain lifecycle interfaces");
        Check(!typeof(DomainVisualDef).Assembly.GetReferencedAssemblies().Any(name => name.Name == "RSH"), "visuals have no RSH dependency");
        string content = Path.Combine(root, ".QuestEditor_Library", "CQF.Domain", "Content");
        string shaderRoot = Directory.Exists(content) ? Path.Combine(content, "1.7", "Shaders")
            : Path.Combine(root, "1.7", "Optional", "Domain", "Shaders");
        ShaderSourceLoader.SetOverrideShaderDirectories(new[] { shaderRoot });
        string game = Environment.GetEnvironmentVariable("CQF_CHECKS_GAME_ROOT")
            ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) =>
            NativeLibrary.TryLoad(Path.Combine(game, name.EndsWith(".dll") ? name : name + ".dll"), out IntPtr handle) ? handle : IntPtr.Zero;
        RuntimeShaderCompiler.ConfigureCacheDirectory(Path.Combine(Path.GetTempPath(), "CQF_DomainShaderChecks_" + Guid.NewGuid().ToString("N")));
        try
        {
            foreach (string kind in new[] { "Opening", "Transition" })
            {
                string vertexPath = Path.Combine(shaderRoot, "CQF_Domain" + kind + ".vert.glsl");
                string fragmentPath = Path.Combine(shaderRoot, "CQF_Domain" + kind + ".frag.glsl");
                string vertex = File.ReadAllText(vertexPath);
                string fragment = File.ReadAllText(fragmentPath);
                Check(!ShaderSourceLoader.HasStageInterfaceErrors(vertexPath, fragmentPath), kind + " native shader stage interface");
                MaterialLayout layout = ShaderSourceLoader.CreateMaterialLayout(fragmentPath);
                Check(!layout.HasErrors && layout.UniformSize > 0 && layout.TextureSlots.Length == 3, kind + " native material layout");
                Check(!ShaderSourceLoader.CreateVertexInputLayout(vertexPath).HasErrors, kind + " native vertex annotations");
                foreach ((string code, string path, RuntimeShaderStage stage) in new[] {
                    (vertex, vertexPath, RuntimeShaderStage.Vertex), (fragment, fragmentPath, RuntimeShaderStage.Fragment) })
                {
                    byte[] spirv = RuntimeShaderCompiler.Compile(code, path, "main", RuntimeShaderLanguage.Glsl, stage);
                    Check(spirv.Length > 20 && BitConverter.ToUInt32(spirv, 0) == 0x07230203, kind + " " + stage + " compiles through game compiler");
                }
            }
        }
        finally { RuntimeShaderCompiler.ReleaseSessions(); }
        foreach (string version in new[] { "1.6", "1.7" })
        {
            string module = Directory.Exists(content) ? Path.Combine(content, version) : Path.Combine(root, version, "Optional", "Domain");
            XElement xml = XDocument.Load(Path.Combine(module, "Defs", "CQF_DomainVisuals.xml")).Root!;
            Check(xml.Element("QuestEditor_Library.DomainVisualDef")!.Element("defName")!.Value == "CQF_DomainVisuals"
                && xml.Element("ThingDef")!.Element("thingClass")!.Value == typeof(DomainOpening_Basic).FullName, version + " optional visual definitions");
            XmlDocument test = new();
            test.Load(Path.Combine(module, "Defs", "CQF_TestDomain.xml"));
            AbilityDef ability = DirectXmlToObject.ObjectFromXml<AbilityDef>(test.SelectSingleNode("/Defs/AbilityDef")!, false);
            Check(ability.defName == "CQF_TestDomainExpansion" && !ability.targetRequired && !ability.aiCanUse
                && ability.comps.Single() is AbilityCompProperties_TestDomainExpansion, version + " native test ability XML and component");
            Check(ability.verbProperties.verbClass == typeof(Verb_CastAbility) && ability.verbProperties.targetParams.canTargetSelf
                && ability.verbProperties.range == 0f, version + " test ability casts on its owner without a target");
            Check(test.SelectSingleNode("/Defs/QuestEditor_Library.DomainDef/preload")!.InnerText == "false"
                && test.SelectSingleNode("/Defs/QuestEditor_Library.DomainDef/visuals")!.InnerText == "CQF_DomainVisuals",
                version + " test domain uses base visuals without preloading a map");
            BiomeDef biome = DirectXmlToObject.ObjectFromXml<BiomeDef>(test.SelectSingleNode("/Defs/BiomeDef")!, false);
            Check(test.SelectSingleNode("/Defs/MapGeneratorDef/pocketMapProperties/biome")!.InnerText == biome.defName
                && !biome.generatesNaturally && !biome.canAutoChoose && biome.AllWildPlants.Count == 0
                && biome.plantDensity == 0f && biome.animalDensity == 0f, version + " isolated test biome has no world ecology");
            PocketMapEcologyChecks.Run(biome);
            Check(true, version + " native growth calculator reproduces the old overflow and completes with the fixed pocket biome");
            XElement translation = XDocument.Load(Path.Combine(module, "Languages", "ChineseSimplified (简体中文)", "DefInjected", "AbilityDef", "CQF_TestDomain.xml")).Root!;
            Check(translation.Element("CQF_TestDomainExpansion.label")!.Value == "展开基础领域", version + " ability uses the requested label");
        }
        Check(xmlErrors == 0, "native test ability XML loads without logged errors");
        Console.WriteLine("Domain visual checks passed: " + checks);
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    public static void ReportXmlError(string text)
    {
        xmlErrors++;
        Console.WriteLine("XML ERROR: " + text);
    }

    private static int checks;
    private static int xmlErrors;
}
