using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using QuestEditor_Library;
using UnityEngine;
using Verse;

namespace CQF.RuntimeChecks;

public sealed class RuntimeChecksDriver : MonoBehaviour
{
    private void Update()
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > 420f) throw new TimeoutException("CQF runtime checks exceeded 420 seconds at stage " + stage);
            if (server != null && server.IsFaulted) server.GetAwaiter().GetResult();
            if (LongEventHandler.AnyEventNowOrWaiting) return;
            if (stage == 0)
            {
                root = GenFilePaths.SaveDataFolderPath;
                Directory.CreateDirectory(Path.Combine(root, "Screenshots"));
                bool disabled = File.Exists(Path.Combine(root, "disabled"));
                bool readOnly = File.Exists(Path.Combine(root, "readonly"));
                if (!disabled && Current.Game?.CurrentMap == null) return;
                Check(CQFEditorBridge.IsLoaded == !disabled, "editor startup setting");
                Check(CQFAIBridge.IsLoaded == !disabled, "AI startup setting");
                if (LanguageDatabase.activeLanguage.loadErrors.Count != 0)
                    File.WriteAllLines(Path.Combine(root, "TranslationErrors.txt"), LanguageDatabase.activeLanguage.loadErrors, Encoding.UTF8);
                if (disabled)
                {
                    Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name is "CQF.Editor" or "CQF.AI"), "disabled modules absent from runtime assemblies");
                    Check(GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.Designator_DestroyThing") == null, "disabled editor type absent");
                    Finish(true);
                    return;
                }
                Check(GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.Designator_DestroyThing") != null, "enabled editor type registered before XML");
                Check(CQFAIBridge.GenerateMap != null, "map generation bridge attached");
                Check(GenTypes.GetTypeInAnyAssembly("QuestEditor_Library.CQFAIWindow").Assembly.GetName().Name == "CQF.Editor", "AI window belongs to editor assembly");
                Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "CQF.AI"), "AI has no separate assembly");
                Check(!CustomQuestFramework_ModSetting.setting.dialogAIEnabled, "AI requests default off after editor load");
                object model = New("CQFAIModel");
                object catalog = New("CQFAIResourceCatalog", model);
                XElement overview = (XElement)Call(catalog, "Overview")!;
                Check(overview.Descendants("mod").Any(mod => mod.Attribute("id")?.Value == "hailuan.customquestframework"), "catalog contains actual loaded CQF package");
                XElement resources = (XElement)Call(catalog, "Query", XElement.Parse("<queries><defs type='Verse.ThingDef' search='Wall'/></queries>"))!;
                Check(resources.Descendants("def").Any(def => def.Attribute("name")?.Value == "Wall" && def.Attribute("mod")?.Value == "ludeon.rimworld"), "resource query resolves actual Wall and Core origin");
                StartServer(readOnly);
                CustomQuestFramework_ModSetting setting = CustomQuestFramework_ModSetting.setting;
                setting.dialogAIEnabled = true;
                setting.dialogAIEndpoint = "http://127.0.0.1:" + port + "/v1";
                setting.dialogAIModel = "CQF_Runtime_Model";
                setting.dialogAIKey = "CQF_Runtime_Key";
                setting.dialogAITimeout = 60;
                if (readOnly)
                {
                    setting.dialogAIAllowEditing = false;
                    setting.dialogAIAllowTextGeneration = true;
                    CQFAIBridge.Open();
                    window = Find.WindowStack.Windows.Single(item => item.GetType().Name == "CQFAIWindow");
                    Set(window, "command", "CQF_Runtime_Question"); Call(window, "Start");
                    Check(!(bool)Get(window, "requestEditing")!, "AI without a target starts in chat mode");
                    Check((bool)Get(window, "requestGenerateText")!, "requests read the text permission from AI settings");
                    stage = 4;
                    return;
                }
                target = new CustomMapDataDef { defName = "CQF_Runtime_Map", size = new IntVec3(64, 1, 64) };
                CQFAIBridge.Open(new CQFAIEditorContext(target.defName, () => target, value => target = (CustomMapDataDef)value));
                window = Find.WindowStack.Windows.Single(item => item.GetType().Name == "CQFAIWindow");
                Check(window.draggable && window.resizeable && !window.absorbInputAroundWindow && !window.forcePause && !window.preventCameraMotion, "AI is a movable nonblocking companion window");
                Check(window.InitialSize.x <= 420f && window.InitialSize.y <= 510f, "AI opens as a small window");
                CQFAIBridge.Open();
                Check(Find.WindowStack.Windows.Count(item => item.GetType().Name == "CQFAIWindow") == 1, "opening AI again reuses the chat window");
                Set(window, "command", "CQF_Runtime_Request");
                Call(window, "Start");
                Call(window, "ToggleCollapsed");
                stage = 1;
            }
            else if (stage == 1)
            {
                transaction = Get(window!, "transaction");
                if (Get(window!, "task") == null && (transaction == null || Property(transaction, "Draft") == null))
                    throw new InvalidOperationException("Window request failed: " + Get(window!, "status"));
                if (transaction == null || Property(transaction, "Draft") == null || Get(window!, "task") != null) return;
                Check(((CustomMapDataDef)Property(transaction, "Draft")!).thingDatas.Count == 1, "window creates map draft through real HTTP and resource query");
                Check(target!.thingDatas.Count == 1, "AI changes are immediately visible in the editing target");
                Check((bool)Get(window!, "collapsed")!, "collapsed chat processes completed requests");
                Call(window!, "ToggleCollapsed");
                ScreenCapture.CaptureScreenshot(Path.Combine(root, "Screenshots", "CQF_MapApplied.png"));
                stage = 2; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 2 && Time.realtimeSinceStartup >= nextTime)
            {
                Check(target!.thingDatas.Count == 1, "live map draft apply");
                Call(window!, "Undo");
                Check(target.thingDatas.Count == 0, "live map draft undo");
                Call(transaction!, "Build", XElement.Parse(MapChanges), "CQF_Runtime_Request", false);
                Call(transaction!, "Apply");
                previousMap = Current.Game.CurrentMap;
                CQFAIBridge.GenerateMap!(target);
                stage = 3;
            }
            else if (stage == 3)
            {
                Map map = Current.Game.CurrentMap;
                if (map == previousMap) return;
                Check(map.Size == new IntVec3(64, 1, 64), "AI draft generates actual editor map");
                Check(map.terrainGrid.TerrainAt(new IntVec3(10, 0, 10)).defName == "Concrete", "generated map contains requested terrain");
                Thing wall = map.thingGrid.ThingsListAt(new IntVec3(12, 0, 12)).Single(thing => thing.def.defName == "Wall");
                Check(wall.Stuff.defName == "BlocksGranite", "generated map contains requested wall and material");
                CustomMapDataDef captured = new CustomMapDataDef { defName = "CQF_Runtime_Captured" };
                captured.LoadData(map);
                Check(captured.thingDatas.Any(data => data.def.defName == "Wall"), "capture reads actual existing map objects");
                object changes = New("CQFAIChanges", New("CQFAIModel"));
                CustomMapDataDef edited = (CustomMapDataDef)Call(changes, "Build", captured, XElement.Parse("<changes><erase x='12' z='12'/></changes>"), "CQF_Runtime_Erase", false)!;
                Check(!edited.thingDatas.Any(data => data.def.defName == "Wall") && captured.thingDatas.Any(data => data.def.defName == "Wall"), "existing map edits preserve captured source");
                previousMap = map;
                CQFAIBridge.GenerateMap!(edited);
                stage = 11;
            }
            else if (stage == 11)
            {
                Map map = Current.Game.CurrentMap;
                if (map == previousMap) return;
                Check(!map.thingGrid.ThingsListAt(new IntVec3(12, 0, 12)).Any(thing => thing.def.defName == "Wall"), "edited existing map removes requested wall in game");
                Check(map.terrainGrid.TerrainAt(new IntVec3(10, 0, 10)).defName == "Concrete", "edited existing map preserves original terrain");
                CheckEditorCompanion();
                stage = 12; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 12 && Time.realtimeSinceStartup >= nextTime)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(root, "Screenshots", "CQF_EditorCompanion.png"));
                stage = 13; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 13 && Time.realtimeSinceStartup >= nextTime)
            {
                Find.WindowStack.TryRemove(editorHost!);
                Call(window!, "SyncContext");
                Check(Get(window!, "context") == null, "closing the editor detaches the editing target");
                Check(CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing, "closing an editor preserves the editing preference");
                Set(window!, "command", "CQF_Runtime_Question"); Call(window!, "Start");
                stage = 4;
            }
            else if (stage == 4)
            {
                if (Get(window!, "task") != null) return;
                Check(Get(window!, "status")!.ToString() == "CQF_AI_Replied".Translate().ToString(), "live player chat reply");
                CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing = true;
                CustomQuestFramework_ModSetting.setting.dialogAIAllowTextGeneration = false;
                Set(window!, "command", "CQF_Runtime_Followup"); Call(window!, "Start");
                Check(!(bool)Get(window!, "requestEditing")!, "editing permission alone cannot edit without a target");
                Check(!(bool)Get(window!, "requestGenerateText")!, "the next request uses updated text settings");
                stage = 5;
            }
            else if (stage == 5)
            {
                if (Get(window!, "task") != null) return;
                Check(Get(window!, "status")!.ToString() == "CQF_AI_Replied".Translate().ToString(), "live continuous chat followup");
                Check(server!.IsCompleted && !server.IsFaulted, "all provider requests completed");
                ScreenCapture.CaptureScreenshot(Path.Combine(root, "Screenshots", "CQF_Chat.png"));
                stage = 6; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 6 && Time.realtimeSinceStartup >= nextTime)
            {
                CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing = false;
                CustomQuestFramework_ModSetting.setting.dialogAIAllowTextGeneration = true;
                editorHost = (Window)New("QuestEditor_Dialog");
                editorHost.GetType().GetProperty("CurTree")!.SetValue(editorHost, new DialogTreeDef { defName = "CQF_Runtime_SettingsTarget" });
                Find.WindowStack.Add(editorHost);
                Call(window!, "SyncContext");
                Check(Get(window!, "context") != null, "selecting dialogue content attaches the editing target");
                Check(!CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing && CustomQuestFramework_ModSetting.setting.dialogAIAllowTextGeneration,
                    "switching to an editor preserves both AI settings");
                stage = 7; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 7 && Time.realtimeSinceStartup >= nextTime)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(root, "Screenshots", "CQF_ToolIcons.png"));
                stage = 8; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 8 && Time.realtimeSinceStartup >= nextTime)
            {
                Call(window!, "SyncContext");
                Check(!CustomQuestFramework_ModSetting.setting.dialogAIAllowEditing, "repeated context updates do not reset the editing preference");
                stage = 9; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 9 && Time.realtimeSinceStartup >= nextTime)
            {
                CQFAIEditorContext active = (CQFAIEditorContext)Get(window!, "context")!;
                Call(window!, "ClearChat");
                Check(((System.Collections.ICollection)Property(Get(window!, "conversation")!, "Messages")!).Count == 0, "clear chat removes conversation history");
                Check(ReferenceEquals(Get(window!, "context"), active), "clear chat preserves the active editing target");
                stage = 10; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 10 && Time.realtimeSinceStartup >= nextTime)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(root, "Screenshots", "CQF_EmptyChat.png"));
                Find.WindowStack.TryRemove(editorHost!);
                settingsWindow = (Window)New("CQFAISettingsWindow");
                Find.WindowStack.Add(settingsWindow);
                Call(window!, "SyncContext");
                Check(Get(window!, "context") == null, "settings can open without an editing target");
                Check(Find.WindowStack.GetsInput(settingsWindow), "AI settings receive input without an editing target");
                stage = 14; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 14 && Time.realtimeSinceStartup >= nextTime)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(root, "Screenshots", "CQF_Settings.png"));
                stage = 15; nextTime = Time.realtimeSinceStartup + 2f;
            }
            else if (stage == 15 && Time.realtimeSinceStartup >= nextTime)
            {
                Find.WindowStack.TryRemove(settingsWindow!);
                XElement saved = XElement.Load(Path.Combine(root, "Config", "Mod_CQF_CustomQuestFramework_Mod.xml"));
                Check(saved.Descendants("dialogAIAllowEditing").Single().Value.Equals("false", StringComparison.OrdinalIgnoreCase), "editing preference is saved when AI settings close");
                Check(saved.Descendants("dialogAIAllowTextGeneration").Single().Value.Equals("true", StringComparison.OrdinalIgnoreCase), "text permission is saved when AI settings close");
                Finish(true);
            }
        }
        catch (Exception error)
        {
            if (root.Length == 0) root = GenFilePaths.SaveDataFolderPath;
            File.AppendAllText(Path.Combine(root, "RuntimeChecks.txt"), "FAIL stage " + stage + "\n" + error + "\n", Encoding.UTF8);
            Log.Error("CQF runtime checks: " + error);
            Finish(false);
        }
    }

    private void CheckEditorCompanion()
    {
        int messageCount = ((System.Collections.ICollection)Property(Get(window!, "conversation")!, "Messages")!).Count;
        editorHost = (Window)New("QuestEditor_Dialog");
        DialogTreeDef tree = new DialogTreeDef { defName = "CQF_Runtime_Companion" };
        editorHost.GetType().GetProperty("CurTree")!.SetValue(editorHost, tree);
        Find.WindowStack.Add(editorHost);
        Call(window!, "SyncContext");
        CQFAIEditorContext context = (CQFAIEditorContext)Get(window!, "context")!;
        Check(ReferenceEquals(context.Owner, editorHost) && ReferenceEquals(context.Read(), tree), "chat follows the actual dialogue editor");
        Check(Find.WindowStack.GetsInput(editorHost), "editor receives input while the AI window is open");
        CQFAIBridge.Open(context);
        Check(ReferenceEquals(window, Find.WindowStack.Windows.Single(item => item.GetType().Name == "CQFAIWindow")), "editor AI icon reuses the existing conversation");
        Check(((System.Collections.ICollection)Property(Get(window!, "conversation")!, "Messages")!).Count >= messageCount, "switching editor preserves conversation history");
        object model = Get(window!, "model")!;
        Set(window!, "requestTransaction", New("CQFAITransaction", model, context));
        Set(window!, "requestEditing", true);
        Set(window!, "requestCommand", "CQF_Runtime_Editor");
        Set(window!, "requestGenerateText", false);
        Set(window!, "task", Task.FromResult("<assistant><reply>CQF_Runtime_Editor</reply><changes><set path='/nodeMoulds/@0/text'><value>CQF_Runtime_LiveEdit</value></set></changes></assistant>"));
        Call(window!, "Poll");
        Check(((DialogTreeDef)Property(editorHost, "CurTree")!).nodeMoulds[0].text == "CQF_Runtime_LiveEdit", "AI result refreshes the real dialogue editor immediately");
        Call(window!, "Undo");
        Check(((DialogTreeDef)Property(editorHost, "CurTree")!).nodeMoulds[0].text == tree.nodeMoulds[0].text, "chat undo restores the real dialogue editor");
        context = (CQFAIEditorContext)Get(window!, "context")!;
        Set(window!, "requestTransaction", New("CQFAITransaction", model, context));
        ((DialogTreeDef)context.Read()).nodeMoulds[0].text = "CQF_Runtime_ManualEdit";
        Set(window!, "task", Task.FromResult("<assistant><changes><set path='/nodeMoulds/@0/text'><value>CQF_Runtime_StaleEdit</value></set></changes></assistant>"));
        Call(window!, "Poll");
        Check(((DialogTreeDef)context.Read()).nodeMoulds[0].text == "CQF_Runtime_ManualEdit", "pending AI reply preserves manual editor changes");
        Check(Get(window!, "status")!.ToString()!.Contains("changed") || Get(window!, "status")!.ToString()!.Contains("CQF_AI_StaleTarget"), "conflicting edit reports a visible error");
        Window dutyHost = (Window)New("QuestEditor_DutyMap");
        Find.WindowStack.Add(dutyHost);
        Set(window!, "task", new TaskCompletionSource<string>().Task);
        Call(window!, "SyncContext");
        Check(Get(window!, "task") == null && ReferenceEquals(((CQFAIEditorContext)Get(window!, "context")!).Owner, dutyHost), "switching to duty editor cancels the old target request");
        Check(Find.WindowStack.GetsInput(dutyHost), "duty editor remains interactive beside AI");
        Find.WindowStack.TryRemove(dutyHost);
        Call(window!, "SyncContext");
        Check(ReferenceEquals(((CQFAIEditorContext)Get(window!, "context")!).Owner, editorHost), "closing duty editor restores dialogue target");
    }

    private void StartServer(bool readOnly)
    {
        TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        listener = new HttpListener(); listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
        server = Task.Run(async () =>
        {
            for (int index = 0; index < (readOnly ? 2 : 4); index++)
            {
                HttpListenerContext request = await listener.GetContextAsync();
                using StreamReader reader = new StreamReader(request.Request.InputStream, Encoding.UTF8);
                string body = await reader.ReadToEndAsync();
                File.WriteAllText(Path.Combine(root, "Request" + index + ".json"), body, Encoding.UTF8);
                if (request.Request.Headers["Authorization"] != "Bearer CQF_Runtime_Key") throw new InvalidDataException("Missing configured authorization");
                if (!readOnly && index == 1 && !body.Contains("BlocksGranite")) throw new InvalidDataException("Actual resource query result missing from followup");
                if (index == (readOnly ? 1 : 3) && !body.Contains("CQF_Runtime_Question")) throw new InvalidDataException("Previous player message missing from continuous chat");
                string content = readOnly ? "<assistant><reply>CQF_Runtime_Reply</reply></assistant>"
                    : index == 0 ? "<assistant><queries><defs type='Verse.ThingDef' search='BlocksGranite'/></queries></assistant>"
                    : index == 1 ? "<assistant><reply>CQF_Runtime_MapReply</reply>" + MapChanges + "</assistant>"
                    : "<assistant><reply>CQF_Runtime_Reply</reply></assistant>";
                string json = "{\"choices\":[{\"message\":{\"content\":\"" + content + "\"},\"finish_reason\":\"stop\"}]}";
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                request.Response.ContentType = "application/json";
                request.Response.ContentLength64 = bytes.Length;
                await request.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                request.Response.Close();
            }
        });
    }

    private void Check(bool result, string name)
    {
        if (!result) throw new InvalidOperationException(name);
        File.AppendAllText(Path.Combine(root, "RuntimeChecks.txt"), "PASS " + name + "\n", Encoding.UTF8);
    }

    private void Finish(bool success)
    {
        finished = true;
        listener?.Close();
        File.AppendAllText(Path.Combine(root, "RuntimeChecks.txt"), success ? "COMPLETE\n" : "FAILED\n", Encoding.UTF8);
        Application.Quit(success ? 0 : 1);
    }

    private static object New(string name, params object[] args) => Activator.CreateInstance(GenTypes.GetTypeInAnyAssembly("QuestEditor_Library." + name), args)!;
    private static object? Get(object instance, string name) => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);
    private static void Set(object instance, string name, object value) => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
    private static object? Property(object instance, string name) => instance.GetType().GetProperty(name)!.GetValue(instance);
    private static object? Call(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(instance, args);

    private const string MapChanges = "<changes><terrain def='Concrete' x='0' z='0' width='64' height='64'/><place def='Wall' stuff='BlocksGranite' x='12' z='12'/></changes>";
    private string root = string.Empty;
    private int stage;
    private int port;
    private float nextTime;
    private bool finished;
    private Window? window;
    private Window? editorHost;
    private Window? settingsWindow;
    private object? transaction;
    private CustomMapDataDef? target;
    private Map? previousMap;
    private HttpListener? listener;
    private Task? server;
}
