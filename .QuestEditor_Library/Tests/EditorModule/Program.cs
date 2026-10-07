using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;
using QuestEditor_Library;
using Verse;
using UnityEngine;

internal static class Program
{
    private static void Main(string[] args)
    {
        string game = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", ".."));
        AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
        {
            string file = Path.Combine(game, "RimWorldWin64_Data", "Managed", new AssemblyName(request.Name).Name + ".dll");
            return File.Exists(file) ? Assembly.LoadFrom(file) : null;
        };
        try { Run(game, args.Contains("enabled")); }
        catch (Exception error) { Console.WriteLine(error); Environment.Exit(1); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string game, bool enabled)
    {
        Debug.unityLogger.logHandler = new ChecksLogHandler();
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
        moduleContent.foldersToLoadDescendingOrder = nativeFolders.FoldersForVersion("1.6").Where(folder => folder.requiredAnyOfPackageIds == null && folder.requiredAllOfPackageIds == null && folder.disallowedAnyOfPackageIds == null && folder.ShouldLoad)
            .Reverse().Select(folder => Path.Combine(modRoot, folder.folderName)).ToList();
        Check(!ModContentPack.GetAllFilesForMod(moduleContent, "Assemblies", extension => extension == ".dll").Values.Any(file => file.Name == "CQF.Editor.dll"), "initial native assembly scan cannot load editor before settings");
        List<string> initialFolders = moduleContent.foldersToLoadDescendingOrder.ToList();
        CQFConditionalLoadFolders conditionalFolders = new CQFConditionalLoadFolders(moduleContent, new CustomQuestFramework_ModSetting());
        Check(conditionalFolders.GetFolders("enableEditor").Count == 0, "LoadFolders custom condition excludes disabled editor");
        moduleContent.assemblies = new ModAssemblyHandler(moduleContent);
        moduleContent.assemblies.loadedAssemblies.Add(framework);
        LoadedModManager.RunningModsListForReading.Add(moduleContent);
        GenTypes.ClearCache();
        Check(!framework.GetReferencedAssemblies().Any(reference => reference.Name == "CQF.Editor"), "framework has no editor assembly reference");
        Check(framework.GetType("QuestEditor_Library.PatchOperation_CQFEditorDesignators") == null, "editor registration no longer requires a custom patch operation");
        Check(!new CustomQuestFramework_ModSetting().enableEditor, "editor defaults off");
        CQFEditorLoader.Load(null!, Array.Empty<string>());
        Check(!CQFEditorBridge.IsLoaded && !AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.Editor"), "disabled loader does not touch editor assembly");
        Check(!CQFAIBridge.IsLoaded && CQFAIBridge.GenerateMap == null, "disabled editor leaves AI entry unavailable");
        foreach (string name in new[] { "Page_QuestEditor", "QuestEditor_Dialog", "CQFEditorTools", "CQFDialogAIClient", "CQFDialogAIWindow", "CQFSignalBook", "Designator_CQFTools" })
            Check(framework.GetType("QuestEditor_Library." + name) == null, "editor type absent: " + name);
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
        MethodInfo invoke = typeof(CQFEditorBridge).GetMethod(nameof(CQFEditorBridge.Invoke))!;
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
        Check(!Directory.GetFiles(Path.Combine(game, "Mods", "CQF", "1.6", "Assemblies"), "CQF.Editor.dll", SearchOption.AllDirectories).Any(), "editor outside automatic assembly directory");
        object entry = Activator.CreateInstance(editor.GetType("QuestEditor_Library.CQFEditorModule")!)!;
        CQFEditorBridge.Attach(new CheckEditorModule(entry));
        Check(CQFEditorBridge.IsLoaded, "editor registration succeeds");
        Check(CQFAIBridge.IsLoaded && CQFAIBridge.GenerateMap != null, "AI is registered with editor initialization");
        Check(editor.GetType("QuestEditor_Library.CQFAIWindow") != null && editor.GetType("QuestEditor_Library.CQFDialogAIClient") != null, "AI code is packaged inside editor DLL");
        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.AI"), "editor does not load a separate AI assembly");
        moduleContent.assemblies.loadedAssemblies.Add(editor);
        GenTypes.ClearCache();
        EditorDesignatorPatchChecks.Run(game, editor, moduleContent);
        conditionalFolders.Exclude("enableEditor");
        EditorDesignatorPatchChecks.Run(game, null, moduleContent);
        Check(!moduleContent.foldersToLoadDescendingOrder.Contains(Path.GetDirectoryName(editorPath)!), "failed module exclusion removes its XML resources");
        IDictionary methods = (IDictionary)typeof(CQFEditorBridge).GetField("methods", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check(methods.Count > 250, "editor drawers registered: " + methods.Count);
        foreach (MethodInfo method in types.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)))
        {
            if (CalledMethods(method).Any(called => called.DeclaringType == invoke.DeclaringType && called.Name == invoke.Name))
            {
                foreach (string key in StringConstants(method).Where(value => value.StartsWith("QuestEditor_Library.")))
                    Check(methods.Contains(key), "registered framework entry: " + key, false);
            }
        }
        Console.WriteLine("PASS all framework editor entries registered");
        object[] values = { 2f };
        Check((int)CQFEditorBridge.Invoke("CQF_Check_Ref", new object(), values)! == 9 && (float)values[0] == 9f, "return and ref arguments preserved");
        Check((string)CQFEditorBridge.Invoke("CQF_Check_Generic", null, new object[] { 3 }, new[] { typeof(int) })! == "Int32:3", "generic arguments preserved");
        try { CQFEditorBridge.Invoke("CQF_Check_Error", null, Array.Empty<object>()); throw new Exception("Error was swallowed."); }
        catch (InvalidDataException error) when (error.Message == "CQF_Check_Error") { Console.WriteLine("PASS original exception preserved"); }
        object objective = RuntimeHelpers.GetUninitializedObject(typeof(QuestBookObjective_Resource));
        object[] drawerArguments = { 7f, new Rect(), 0f };
        CQFEditorBridge.Invoke("QuestEditor_Library.QuestBookObjective.DrawSpecial(Ref:float,None:UnityEngine.Rect,None:float)", objective, drawerArguments);
        Check((float)drawerArguments[0] == 7f, "base drawer dispatch without UI side effects");
        Console.WriteLine("Editor module checks passed.");
    }

    private static bool ReachesEditor(MethodBase method, Assembly framework, HashSet<MethodBase> visited)
    {
        if (!visited.Add(method)) return false;
        if (method.DeclaringType == typeof(CQFEditorBridge) && method.Name == nameof(CQFEditorBridge.Invoke)) return true;
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

    private static IEnumerable<string> StringConstants(MethodInfo method)
    {
        foreach (var instruction in Instructions(method))
            if (instruction.code.OperandType == OperandType.InlineString) yield return method.Module.ResolveString(instruction.token);
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
