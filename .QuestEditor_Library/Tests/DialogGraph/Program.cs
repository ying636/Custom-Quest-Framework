using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QuestEditor_Library;
using Verse;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

internal static class Program
{
    private static void Main(string[] args)
    {
        string gameDirectory = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", ".."));
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            string name = new AssemblyName(args.Name).Name + ".dll";
            string managed = Path.Combine(Path.Combine(gameDirectory, "RimWorldWin64_Data", "Managed"), name);
            if (File.Exists(managed)) return Assembly.LoadFrom(managed);
            string mod = Path.Combine(Path.Combine(gameDirectory, "Mods", "CQF", "1.6", "Assemblies", "net48"), name);
            if (File.Exists(mod)) return Assembly.LoadFrom(mod);
            return null;
        };
        try { Run(); } catch (Exception error) { Console.WriteLine(error); Environment.Exit(1); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        UnityEngine.Debug.unityLogger.logHandler = new ChecksLogHandler();
        FieldInfo prefsData = typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)!;
        prefsData.SetValue(null, RuntimeHelpers.GetUninitializedObject(prefsData.FieldType));
        DialogTreeDef tree = new DialogTreeDef { defName = "CQF_Check", title = "CQF_Check_Title" };
        DialogNode a = tree.CreateNewNode(null);
        DialogNode b = tree.CreateNewNode(null);
        tree.nodeMoulds[0].options.Add(new DialogOption());
        tree.nodeMoulds[0].options[0].results[0].nextIndex = a.index;
        a.options.Add(new DialogOption());
        a.options[0].results[0].nextIndex = b.index;
        b.options.Add(new DialogOption());
        b.options[0].results[0].nextIndex = 0;
        tree.Update();
        Check(tree.idleNodes.Count == 0, "cycle reachability");
        DialogNode idle = tree.CreateNewNode(null);
        idle.options.Add(new DialogOption());
        idle.options[0].results[0].nextIndex = idle.index;
        tree.Update();
        Check(tree.idleNodes.Count == 1 && tree.idleNodes[0] == idle, "unreachable self loop");
        CQFDialogEditSession session = new CQFDialogEditSession();
        session.Reset(tree);
        DialogTreeDef copy = session.Copy(tree);
        copy.nodeMoulds[0].options[0].text = "CQF_Changed";
        Check(tree.nodeMoulds[0].options[0].text != "CQF_Changed", "deep copy isolation");
        tree.nodeMoulds[0].editorX = 123.25f;
        tree.nodeMoulds[0].editorY = -42f;
        tree.nodeMoulds[0].editorPositionSet = true;
        session.Observe(tree);
        Check(session.CanUndo, "undo available");
        DialogTreeDef undone = session.Undo(tree);
        Check(!undone.nodeMoulds[0].editorPositionSet, "undo position");
        DialogTreeDef redone = session.Redo(undone);
        Check(redone.nodeMoulds[0].editorX == 123.25f && redone.nodeMoulds[0].editorY == -42f, "redo position");
        Check(redone.SaveToXElement("tree").Descendants("editorX").First().Value == "123.25", "position XML");
        Console.WriteLine("Graph/history checks passed.");
        GenTypes.AllTypes.AddRange(new[] { typeof(CQFThingDefCount), typeof(CQFAction_Loop), typeof(DialogCondition_Chance), typeof(DialogOption), typeof(DialogTreeDef) });
        CQFDialogPatch patch = new CQFDialogPatch(session);
        string before = tree.SaveToXElement("tree").ToString();
        DialogTreeDef draft = patch.Build(tree, "<patch><addNode node='10'><text>CQF_Check_New</text></addNode><addOption node='10'><text>CQF_Check_Choice</text></addOption><connect node='10' option='0' result='0' next='0'/><connect node='0' option='0' result='0' next='10'/></patch>");
        Check(draft.nodeMoulds[10].options[0].results[0].nextIndex == 0, "patch back link");
        Check(before == tree.SaveToXElement("tree").ToString(), "patch source isolation");
        Reject(() => patch.Build(tree, "<patch><connect node='0' option='0' result='0' next='999'/></patch>"), "dangling reference");
        Reject(() => patch.Build(tree, "<patch><removeNode node='0'/></patch>"), "entry deletion");
        Reject(() => patch.Build(tree, "<patch><addNode node='1'/></patch>"), "duplicate node");
        Reject(() => patch.Build(tree, "<patch><setNode node='0'><unknown>1</unknown></setNode></patch>"), "unknown field");
        Reject(() => patch.Build(tree, "<patch><setResult node='0' option='0' result='0'><actions><li Class='System.Diagnostics.Process'/></actions></setResult></patch>"), "invalid type");
        Reject(() => patch.Build(tree, "<!DOCTYPE patch [<!ENTITY x SYSTEM 'file:///C:/Windows/win.ini'>]><patch><setNode node='0'><text>&x;</text></setNode></patch>"), "DTD rejection");
        Console.WriteLine("Patch checks passed.");
        ThingDef item = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); item.defName = "CQF_Check_Item"; item.label = "CQF_Check_Item_Label";
        DefDatabase<ThingDef>.Add(item);
        string range = new IntRange(2, 4).ToString();
        DialogTreeDef resourceDraft = patch.Build(tree, "<patch><setOption node='0' option='0'><requiredThings><li Class='QuestEditor_Library.CQFThingDefCount'><thing>CQF_Check_Item</thing><count>" + range + "</count></li></requiredThings></setOption></patch>");
        Check(((CQFThingDefCount)resourceDraft.nodeMoulds[0].options[0].requiredThings[0]).thing == item, "loaded Def resolution");
        DialogTreeDef copiedResource = session.Copy(resourceDraft);
        Check(((CQFThingDefCount)copiedResource.nodeMoulds[0].options[0].requiredThings[0]).thing == item, "snapshot preserves Def identity");
        ((CQFThingDefCount)copiedResource.nodeMoulds[0].options[0].requiredThings[0]).count = new IntRange(9, 9);
        Check(((CQFThingDefCount)resourceDraft.nodeMoulds[0].options[0].requiredThings[0]).count.min == 2, "snapshot isolates item counts");
        Reject(() => patch.Build(tree, "<patch><setOption node='0' option='0'><requiredThings><li Class='QuestEditor_Library.CQFThingDefCount'><thing>CQF_Missing_Item</thing></li></requiredThings></setOption></patch>"), "unknown Def");
        CQFDialogAIResourceCatalog catalog = new CQFDialogAIResourceCatalog(patch);
        Check(catalog.TryQuery("<queries><def type='Verse.ThingDef' search='CQF_Check_Item'/></queries>", out string? resources) && resources!.Contains("CQF_Check_Item"), "resource query results");
        Reject(() => catalog.TryQuery("<queries><def type='System.String' search='CQF'/></queries>", out _), "unknown Def query type");
        Reject(() => patch.Build(tree, "<patch>unwrapped text<setNode node='0'><text>CQF_Check</text></setNode></patch>"), "unwrapped patch text");
        Reject(() => patch.Build(tree, "<patch><setResult node='0' option='0' result='0'><nextIndex IsNull='true'>5</nextIndex></setResult></patch>"), "invalid null representation");
        DialogTreeDef partial = patch.SelectChanges(tree, draft, new[] { 10 });
        Check(partial.nodeMoulds.ContainsKey(10) && partial.nodeMoulds[0].options[0].results[0].nextIndex == a.index, "partial draft application");
        Reject(() => patch.SelectChanges(tree, draft, new[] { 0 }), "partial draft dangling link");
        DialogTreeDef accepted = patch.SelectChanges(tree, draft, tree.nodeMoulds.Keys.Union(draft.nodeMoulds.Keys));
        Check(accepted.nodeMoulds[0].options[0].results[0].nextIndex == 10, "full draft application");
        System.Xml.Linq.XElement serialized = redone.SaveToXElement("tree"); foreach (System.Xml.Linq.XAttribute classAttribute in serialized.Descendants().Attributes("Class").Where(attribute => attribute.Value == typeof(DialogOption).FullName).ToArray()) classAttribute.Remove();
        System.Xml.XmlNode xmlNode = new System.Xml.XmlDocument().ReadNode(serialized.CreateReader())!;
        DialogTreeDef reloaded = DirectXmlToObject.ObjectFromXml<DialogTreeDef>(xmlNode, false);
        Check(reloaded.nodeMoulds[0].editorX == 123.25f && reloaded.nodeMoulds[0].editorY == -42f && reloaded.nodeMoulds[0].editorPositionSet, "RimWorld XML position roundtrip");
        Check(reloaded.nodeMoulds[0].options[0].results[0].nextIndex == a.index, "RimWorld XML link roundtrip");
        Check(ChecksLogHandler.ErrorCount == 0, "no game parser errors");
        DialogTreeDef actionDraft = patch.Build(tree, "<patch><setResult node='0' option='0' result='0'><actions><li Class='QuestEditor_Library.CQFAction_Loop'><loopCount>2</loopCount><actions><li Class='QuestEditor_Library.CQFAction_Loop'><loopCount>3</loopCount></li></actions></li></actions></setResult></patch>");
        Check(((CQFAction_Loop)actionDraft.nodeMoulds[0].options[0].results[0].actions[0]).loopCount == 2, "polymorphic action fields");
        Check(((CQFAction_Loop)((CQFAction_Loop)actionDraft.nodeMoulds[0].options[0].results[0].actions[0]).actions[0]).loopCount == 3, "nested polymorphic actions");
        DialogTreeDef removed = patch.Build(tree, "<patch><removeNode node='1'/></patch>");
        Check(!removed.nodeMoulds.ContainsKey(1) && removed.nodeMoulds[0].options[0].results[0].nextIndex == null, "node deletion disconnects incoming links");
        Reject(() => patch.Build(tree, "<patch><setNode node='0'><editorX>NaN</editorX></setNode></patch>"), "nonfinite position");
        Console.WriteLine("Resource checks passed.");
        RunNetwork().GetAwaiter().GetResult();
    }

    private static async Task RunNetwork()
    {
        TcpListener portProbe = new TcpListener(IPAddress.Loopback, 0);
        portProbe.Start();
        int port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
        portProbe.Stop();
        string address = "http://127.0.0.1:" + port + "/v1";
        using HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
        listener.Start();
        string command = "CQF_命令 \"quoted\"\nnext line";
        Task server = Task.Run(async () =>
        {
            HttpListenerContext context = await listener.GetContextAsync();
            Check(context.Request.RawUrl == "/v1/chat/completions", "API base URL normalization");
            Check(context.Request.Headers["Authorization"] == "Bearer CQF_Check_Key", "bearer authentication");
            string received = await new StreamReader(context.Request.InputStream, Encoding.UTF8).ReadToEndAsync();
            using JsonDocument document = JsonDocument.Parse(received);
            Check(document.RootElement.GetProperty("model").GetString() == "CQF_Check_Model", "request model");
            Check(document.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() == command, "UTF-8 command and escaping");
            byte[] response = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content = "<patch><addNode node='20'/></patch>" } } } }));
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(response);
            context.Response.Close();
        });
        CQFDialogAIClient client = new CQFDialogAIClient(address, "CQF_Check_Model", "CQF_Check_Key", 10);
        string reply = await client.CompleteAsync("CQF_Instructions", command, CancellationToken.None);
        await server;
        Check(reply == "<patch><addNode node='20'/></patch>", "response JSON parsing");
        Task errorServer = Task.Run(async () =>
        {
            HttpListenerContext context = await listener.GetContextAsync();
            context.Response.StatusCode = 401;
            context.Response.Close();
        });
        try { await client.CompleteAsync("CQF_Check", "CQF_Check", CancellationToken.None); throw new Exception("Expected HTTP error"); }
        catch (InvalidOperationException error) { Check(error.Message.Contains("401"), "HTTP errors are reported"); }
        await errorServer;
        Task incompleteServer = Task.Run(async () =>
        {
            HttpListenerContext context = await listener.GetContextAsync();
            byte[] response = Encoding.UTF8.GetBytes("{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"content\":\"<patch>\"}}]}");
            await context.Response.OutputStream.WriteAsync(response);
            context.Response.Close();
        });
        try { await client.CompleteAsync("CQF_Check", "CQF_Check", CancellationToken.None); throw new Exception("Expected incomplete rejection"); }
        catch (InvalidOperationException error) { Check(error.Message == "CQF_DialogAI_Incomplete", "truncated response rejection"); }
        await incompleteServer;
        Task cancelServer = Task.Run(async () =>
        {
            HttpListenerContext context = await listener.GetContextAsync();
            await Task.Delay(250);
            context.Response.Close();
        });
        using CancellationTokenSource cancellation = new CancellationTokenSource(100);
        try { await client.CompleteAsync("CQF_Check", "CQF_Check", cancellation.Token); throw new Exception("Expected cancellation"); }
        catch (OperationCanceledException) { Console.WriteLine("PASS cancellation"); }
        await cancelServer;
        listener.Stop();
        Reject(() => new CQFDialogAIClient("not a URL", "CQF_Check", "", 10), "invalid endpoint");
        Reject(() => new CQFDialogAIClient(address, "", "", 10), "missing model");
        Console.WriteLine("Network checks passed.");
    }
    private static void Check(bool result, string name)
    {
        if (!result) throw new Exception("Failed: " + name);
        Console.WriteLine("PASS " + name);
    }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (Exception error) when (error is InvalidDataException || error is System.Xml.XmlException || error is ArgumentException || error is InvalidOperationException) { Console.WriteLine("PASS reject " + name); return; }
        throw new Exception("Expected rejection: " + name);
    }
}