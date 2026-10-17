using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using QuestEditor_Library;
using RimWorld;
using Verse;
using UnityEngine;

internal static class Program
{
    private static void Main(string[] args)
    {
        string game = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", ".."));
        AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
        {
            string file = Path.Combine(game, new AssemblyName(request.Name).Name + ".dll");
            if (new AssemblyName(request.Name).Name == "0Harmony")
                file = Path.GetFullPath(Path.Combine(game, "..", "..", "workshop", "content", "294100", "2009463077", "1.7", "Assemblies", "0Harmony.dll"));
            return File.Exists(file) ? Assembly.LoadFrom(file) : null;
        };
        try
        {
            if (args.Any(arg => arg.StartsWith("modules-", StringComparison.Ordinal))) OptionalModuleChecks.Run(game, args.Single());
            else if (args.Contains("callbacks")) NativeCallbackChecks.Run();
            else if (args.Contains("serialization")) SerializationChecks.Run();
            else if (args.Contains("hibernation")) HibernationSchedulingChecks.Run();
            else if (args.Contains("patches")) Version17Checks.Run(game);
            else Run(game, args.Contains("enabled"));
        }
        catch (Exception error) { Console.WriteLine(error); Environment.Exit(1); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string game, bool enabled)
    {
        Debug.unityLogger.logHandler = new ChecksLogHandler();
        new Harmony("CQF.Checks.ErrorLog").Patch(typeof(Log).GetMethod(nameof(Log.Error)),
            prefix: new HarmonyMethod(typeof(ChecksLogHandler).GetMethod(nameof(ChecksLogHandler.RecordError))));
        FieldInfo prefs = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
        prefs.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefs.FieldType));
        Assembly framework = typeof(DialogTreeDef).Assembly;
        Type[] types = framework.GetTypes();
        ModContentPack moduleContent = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        string modRoot = Path.Combine(game, "Mods", "CQF");
        typeof(ModContentPack).GetField("rootDirInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(moduleContent, new DirectoryInfo(modRoot));
        XmlDocument loadFoldersXml = new XmlDocument();
        loadFoldersXml.LoadXml(File.ReadAllText(Path.Combine(modRoot, "LoadFolders.xml"), System.Text.Encoding.UTF8));
        ModLoadFolders nativeFolders = DirectXmlToObject.ObjectFromXml<ModLoadFolders>(loadFoldersXml.DocumentElement!, false);
        moduleContent.foldersToLoadDescendingOrder = nativeFolders.FoldersForVersion(VersionControl.CurrentVersionStringWithoutBuild).Where(folder => folder.requiredAnyOfPackageIds == null && folder.requiredAllOfPackageIds == null && folder.disallowedAnyOfPackageIds == null && folder.ShouldLoad)
            .Reverse().Select(folder => Path.Combine(modRoot, folder.folderName)).ToList();
        Check(!ModContentPack.GetAllFilesForMod(moduleContent, "Assemblies", extension => extension == ".dll").Values.Any(file => file.Name == "CQF.Editor.dll"), "initial native assembly scan cannot load editor before settings");
        List<string> initialFolders = moduleContent.foldersToLoadDescendingOrder.ToList();
        CQFConditionalLoadFolders conditionalFolders = new CQFConditionalLoadFolders(moduleContent, new CustomQuestFramework_ModSetting());
        Check(conditionalFolders.GetFolders("enableEditor").Count == 0, "LoadFolders custom condition excludes disabled editor");
        moduleContent.assemblies = new ModAssemblyHandler(moduleContent);
        moduleContent.assemblies.loadedAssemblies.Add(framework);
        LoadedModManager.RunningModsListForReading.Add(moduleContent);
        GenTypes.ClearCache();
        MapManagerInstanceChecks.Run();
        Check(!framework.GetReferencedAssemblies().Any(reference => reference.Name == "CQF.Editor"), "framework has no editor assembly reference");
        Check(framework.GetType("QuestEditor_Library.PatchOperation_CQFEditorDesignators") == null, "editor registration no longer requires a custom patch operation");
        Check(!new CustomQuestFramework_ModSetting().enableEditor, "editor defaults off");
        CQFEditorLoader.Load(null!, Array.Empty<string>());
        Check(!CQFEditorBridge.IsLoaded && !AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.Editor"), "disabled loader does not touch editor assembly");
        Check(!CQFAIBridge.IsLoaded && CQFAIBridge.GenerateMap == null, "disabled editor leaves AI entry unavailable");
        CustomMapGenerationChecks.Run();
        foreach (string name in new[] { "Page_QuestEditor", "QuestEditor_Dialog", "CQFDialogAIClient", "CQFDialogAIWindow", "Designator_CQFTools" })
            Check(framework.GetType("QuestEditor_Library." + name) == null, "editor type absent: " + name);
        Check(new[] { typeof(CQFEditorTools), typeof(CQFSignalBook), typeof(CQFTargetKeyBook), typeof(CQFActionListEditor), typeof(CQFConditionListEditor), typeof(CQFContentTextExport) }
            .All(type => type.Assembly == framework), "shared drawing and selectors belong to framework");
        Check(!types.Any(type => type.Name == "CQFAction_FinishResearchEditor" || type.Name == "CQFActionEditor"), "object drawer forwarding classes removed");
        Check(typeof(CQFEditorBridge).GetMethod("Invoke") == null && typeof(CQFEditorBridge).GetMethod("Register") == null, "reflection drawer dispatch removed");
        DynamicMethod standaloneDraw = new DynamicMethod("CQF_Check_DirectBaseDrawer", typeof(void),
            new[] { typeof(QuestBookObjective), typeof(float).MakeByRefType(), typeof(Rect), typeof(float) }, typeof(Program).Module, true);
        ILGenerator standaloneIL = standaloneDraw.GetILGenerator();
        standaloneIL.Emit(OpCodes.Ldarg_0);
        standaloneIL.Emit(OpCodes.Ldarg_1);
        standaloneIL.Emit(OpCodes.Ldarg_2);
        standaloneIL.Emit(OpCodes.Ldarg_3);
        standaloneIL.Emit(OpCodes.Call, typeof(QuestBookObjective).GetMethod(nameof(QuestBookObjective.DrawSpecial))!);
        standaloneIL.Emit(OpCodes.Ret);
        object[] standaloneArguments = { RuntimeHelpers.GetUninitializedObject(typeof(QuestBookObjective_Resource)), 7f, new Rect(), 0f };
        standaloneDraw.Invoke(null, standaloneArguments);
        Check((float)standaloneArguments[1] == 7f && !CQFEditorBridge.IsLoaded, "base object drawing does not require editor initialization");
        Check(new[] { typeof(DialogTreeDef), typeof(CustomMapDataDef), typeof(ComplexPawnDef), typeof(DutyMapDef), typeof(QuestBookDef), typeof(GameComponent_Editor), typeof(CQFComponentOverride), typeof(MapComponent_CQFComponentOverrides) }.All(type => type.Assembly == framework), "runtime types and component save overrides remain in framework");
        Check(CQFSerialization.SaveList(new List<string> { "CQF_Check" }, "values").Element("li")!.Value == "CQF_Check", "serialization works without editor");
        CQFContentPaths.Initialize(Path.Combine(game, "Mods", "CQF"));
        Check(CQFContentPaths.Quests == Path.Combine(game, "Mods", "CQF", "Quests"), "content path independent of editor");
        DialogTreeDef tree = new DialogTreeDef { defName = "CQF_Check_Module" };
        tree.nodeMoulds[0].options.Add(new DialogOption());
        tree.nodeMoulds[0].options[0].results[0].nextIndex = 0;
        tree.Update();
        EditorDesignatorPatchChecks.Run(game, null, moduleContent);
        ConditionalLoadFolderChecks.Run();
        XmlDocument document = new XmlDocument();
        string xml = tree.SaveToXElement("tree").ToString().Replace(" Class=\"QuestEditor_Library.DialogOption\"", "");
        document.LoadXml(xml);
        DialogTreeDef restored = DirectXmlToObject.ObjectFromXml<DialogTreeDef>(document.DocumentElement!, false);
        restored.ResolveReferences();
        Check(restored.nodeMoulds[0].options[0].results[0].nextIndex == 0, "framework XML loads without editor");
        string[] runtimeMethods = { "Work", "Generate", "GenerateZone", "Spawn", "SpawnThing", "Process", "Check", "SaveToXElement", "ExposeData", "PostSpawnSetup" };
        MethodInfo[] roots = types.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => runtimeMethods.Contains(method.Name) && !method.IsAbstract).ToArray();
        foreach (MethodInfo method in roots) Check(!ReachesEditor(method, framework, new HashSet<MethodBase>()), "runtime method independent: " + method.DeclaringType!.Name + "." + method.Name, false);
        Console.WriteLine("PASS runtime method independence: " + roots.Length);
        if (!enabled)
        {
            Check(!CQFEditorBridge.IsLoaded, "framework checks leave editor unloaded");
            Console.WriteLine("Framework-only checks passed.");
            return;
        }
        moduleContent.foldersToLoadDescendingOrder = initialFolders.ToList();
        conditionalFolders = new CQFConditionalLoadFolders(moduleContent, new CustomQuestFramework_ModSetting { enableEditor = true });
        Check(conditionalFolders.GetFolders("enableEditor").Count == 1, "LoadFolders custom condition includes enabled editor");
        string editorPath = Path.Combine(conditionalFolders.GetFolders("enableEditor")[0], "CQF.Editor.dll");
        Assembly editor = Assembly.LoadFrom(editorPath);
        Type[] editorTypes = editor.GetTypes();
        Check(editor.GetType("QuestEditor_Library.Page_QuestEditor") != null, "editor types available after explicit load");
        Check(!Directory.GetFiles(Path.Combine(game, "Mods", "CQF", VersionControl.CurrentVersionStringWithoutBuild, "Assemblies"), "CQF.Editor.dll", SearchOption.AllDirectories).Any(), "editor outside automatic assembly directory");
        object entry = Activator.CreateInstance(editor.GetType("QuestEditor_Library.CQFEditorModule")!)!;
        CQFEditorBridge.Attach(new CheckEditorModule(entry));
        Check(CQFEditorBridge.IsLoaded, "editor registration succeeds");
        Check(CQFAIBridge.IsLoaded && CQFAIBridge.GenerateMap != null, "AI is registered with editor initialization");
        Check(editor.GetType("QuestEditor_Library.CQFAIWindow") != null && editor.GetType("QuestEditor_Library.CQFDialogAIClient") != null, "AI code is packaged inside editor DLL");
        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.AI"), "editor does not load a separate AI assembly");
        moduleContent.assemblies.loadedAssemblies.Add(editor);
        GenTypes.ClearCache();
        EditorDesignatorPatchChecks.Run(game, editor, moduleContent);
        ThingTransformChecks.Run(game, editor);
        PlacementRotationChecks.Run(editor);
        conditionalFolders.Exclude("enableEditor");
        EditorDesignatorPatchChecks.Run(game, null, moduleContent);
        Check(!moduleContent.foldersToLoadDescendingOrder.Contains(Path.GetDirectoryName(editorPath)!), "failed module exclusion removes its XML resources");
        Check(ReferenceEquals(CQFEditorBridge.Editor, CQFEditorBridge.Module), "typed editor entry available after attach");
        Check(!editorTypes.Any(type => type.Name == "CQFAction_FinishResearchEditor" || type.Name == "CQFActionEditor"), "editor assembly contains no object forwarding drawers");
        Console.WriteLine("Editor module checks passed.");
    }

    private static bool ReachesEditor(MethodBase method, Assembly framework, HashSet<MethodBase> visited)
    {
        if (!visited.Add(method)) return false;
        if (method.DeclaringType == typeof(CQFEditorBridge) && method.Name == "get_Editor") return true;
        if (method.DeclaringType?.Assembly != framework) return false;
        IteratorStateMachineAttribute? iterator = method.GetCustomAttribute<IteratorStateMachineAttribute>();
        if (iterator != null && ReachesEditor(iterator.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!, framework, visited)) return true;
        return CalledMethods(method).Any(called => ReachesEditor(called, framework, visited));
    }

    private static IEnumerable<MethodBase> CalledMethods(MethodBase method)
    {
        foreach (var instruction in Instructions(method))
            if (instruction.code.OperandType == OperandType.InlineMethod)
            {
                MethodBase? called = method.Module.ResolveMethod(instruction.token, method.DeclaringType?.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null);
                if (called != null) yield return called;
            }
    }

    private static IEnumerable<(OpCode code, int token)> Instructions(MethodBase method)
    {
        byte[]? bytes = method.GetMethodBody()?.GetILAsByteArray();
        if (bytes == null) yield break;
        int position = 0;
        while (position < bytes.Length)
        {
            short value = bytes[position++];
            if (value == 0xfe) value = (short)(0xfe00 | bytes[position++]);
            OpCode code = codes[value];
            int token = code.OperandType is OperandType.InlineMethod or OperandType.InlineString ? BitConverter.ToInt32(bytes, position) : 0;
            int length = code.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + BitConverter.ToInt32(bytes, position) * 4,
                _ => 4
            };
            position += length;
            yield return (code, token);
        }
    }

    private static void Check(bool passed, string name, bool print = true)
    {
        if (!passed) throw new InvalidOperationException(name);
        if (print) Console.WriteLine("PASS " + name);
    }

    private static readonly Dictionary<short, OpCode> codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!).ToDictionary(code => code.Value);
}
