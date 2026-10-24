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
        DomainSurfaceDef surfaceDefinition = new() { defName = "CQF_Check_Surface" };
        Check(!surfaceDefinition.ConfigErrors().Any(), "domain surface configuration is valid independently of opening visuals");
        surfaceDefinition.textureScale = float.PositiveInfinity;
        surfaceDefinition.flowSpeed = new Vector2(float.NaN, 0f);
        Check(surfaceDefinition.ConfigErrors().Count() >= 2, "invalid generic surface scale and flow report errors");
        surfaceDefinition.screenBorderWidth = float.NaN;
        surfaceDefinition.screenBorderOpacity = 2f;
        Check(surfaceDefinition.ConfigErrors().Count(error => error.Contains("border")) == 2,
            "invalid optional screen border width and opacity report errors");
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
            foreach (string kind in new[] { "Opening", "Transition", "FusionTransition", "FusionBoundary", "ContestFront", "Surface", "SurfaceScreen", "BloodSeaOpening", "BloodSeaTerrain", "BloodSeaSurfaceScreen", "BloodSeaTransition" })
            {
                bool blood = kind.StartsWith("BloodSea");
                string bloodShader = kind switch {
                    "BloodSeaTerrain" => "RSH_BloodMarshTerrain",
                    "BloodSeaSurfaceScreen" => "RSH_BloodMarshSurfaceScreen",
                    "BloodSeaTransition" => "RSH_BloodSeaDomainTransition",
                    _ => "RSH_BloodSeaDomainOpening" };
                string bloodRoot = Path.GetFullPath(Path.Combine(root, "..", "RitesOfTheSilentHouse", "1.7", "Shaders"));
                string vertexPath = blood ? Path.Combine(bloodRoot, bloodShader + ".vert.glsl")
                    : Path.Combine(shaderRoot, "CQF_Domain" + (kind == "FusionTransition" ? "Transition" : kind is "FusionBoundary" or "ContestFront" ? "Surface" : kind) + ".vert.glsl");
                string fragmentPath = blood ? Path.Combine(bloodRoot, bloodShader + ".frag.glsl")
                    : Path.Combine(shaderRoot, "CQF_Domain" + kind + ".frag.glsl");
                string vertex = File.ReadAllText(vertexPath);
                string fragment = File.ReadAllText(fragmentPath);
                Check(!ShaderSourceLoader.HasStageInterfaceErrors(vertexPath, fragmentPath), kind + " native shader stage interface");
                MaterialLayout layout = ShaderSourceLoader.CreateMaterialLayout(fragmentPath);
                Check(!layout.HasErrors && layout.UniformSize > 0 && layout.TextureSlots.Length == (kind == "BloodSeaTransition" || kind.StartsWith("Surface") ? 1 : blood || kind is "FusionBoundary" or "ContestFront" ? 0 : 3), kind + " native material layout");
                if (kind.Contains("Transition") || kind.EndsWith("Screen"))
                    Check(vertex.Contains("binding = 1) uniform FrameUniforms") && vertex.Contains("mat4 WorldToCamera; mat4 ViewProj;"),
                        kind + " uses the native GUI frame binding and matrix offsets");
                if (kind.EndsWith("Screen"))
                    Check(!fragment.Contains("vWorld") && fragment.Contains("_SurfaceView"),
                        kind + " reconstructs surface coordinates from GUI UVs without a missing world interpolator");
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
                version + " test domain uses base visuals and the configured preload policy");
            if (version == "1.7")
            {
                XmlDocument surfaceXml = new();
                surfaceXml.Load(Path.Combine(module, "Defs", "CQF_DomainSurfaces.xml"));
                DomainSurfaceDef surface = DirectXmlToObject.ObjectFromXml<DomainSurfaceDef>(surfaceXml.SelectSingleNode("/Defs/QuestEditor_Library.DomainSurfaceDef")!, false);
                XmlDocument terrainXml = new();
                terrainXml.Load(Path.Combine(module, "Defs", "CQF_DomainTerrains.xml"));
                TerrainDef terrain = DirectXmlToObject.ObjectFromXml<TerrainDef>(terrainXml.SelectSingleNode("/Defs/TerrainDef")!, false);
                Check(terrain.defName == "CQF_BasicDomainTerrain" && terrain.fertility == 0f && terrain.dontRender
                    && test.SelectSingleNode("/Defs/GenStepDef/genStep/terrain")!.InnerText == terrain.defName
                    && test.SelectSingleNode("/Defs/GenStepDef/genStep/gridTerrain")!.InnerText == terrain.defName,
                    "basic domain generates only its special terrain instead of soil or paved tiles");
                Check(test.SelectSingleNode("/Defs/QuestEditor_Library.DomainDef/surface")!.InnerText == surface.defName
                    && surface.shader == "CQF_DomainSurface" && surface.screenShader == "CQF_DomainSurfaceScreen",
                    "basic domain configures a reusable surface separately from its original opening shader");
                Check(surface.screenBorderWidth > 0f && surface.screenBorderWidth < 0.3f && surface.screenBorderOpacity <= 1f,
                    "basic domain enables a narrow screen border independently of its opening animation");
                ThingDef anchor = DirectXmlToObject.ObjectFromXml<ThingDef>(test.SelectSingleNode("/Defs/ThingDef")!, false);
                Check(test.SelectSingleNode("/Defs/QuestEditor_Library.DomainDef/anchor")!.InnerText == anchor.defName
                    && anchor.thingClass == typeof(Building_DomainVisual) && anchor.selectable && anchor.useHitPoints
                    && anchor.building.canBeDamagedByAttacks,
                    "base domain configures a selectable damageable shared-health anchor");
                HediffDef protection = DirectXmlToObject.ObjectFromXml<HediffDef>(test.SelectSingleNode("/Defs/HediffDef")!, false);
                Check(test.SelectSingleNode("/Defs/QuestEditor_Library.DomainDef/protection")!.InnerText == protection.defName
                    && protection.hediffClass == typeof(Hediff_DomainProtection)
                    && test.SelectSingleNode("/Defs/QuestEditor_Library.DomainDef/maximumHealth")!.InnerText == "100000",
                    "base domain connects caster damage to its explicit 100000 shared health");
                XElement shaders = XDocument.Load(Path.Combine(module, "Defs", "CQF_DomainShaders.xml")).Root!;
                XElement ground = shaders.Elements("ShaderDef").Single(def => def.Element("defName")!.Value == valid.groundShader).Element("shaderProgram")!;
                Check(ground.Element("depthTest")!.Value == "true" && ground.Element("depthWrite")!.Value == "false"
                    && ground.Element("renderQueue")!.Value == "2900", "ground uses depth testing below the pawn transparent queue");
            }
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
