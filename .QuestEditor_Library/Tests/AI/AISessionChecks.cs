using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;

internal static class AISessionChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CQF_AI_HistoryChecks_" + Guid.NewGuid().ToString("N")));
        try
        {
            CQFAISessionStore store = new(root);
            Check(store.Recent() == null && store.List(_ => throw new InvalidOperationException()).Count == 0, "empty history works without creating files");
            CQFAISession session = new() { Title = "CQF_Check_中文会话", Draft = "CQF_Check_输入\n草稿" };
            session.Messages.Add(new CQFAIMessage("user", "CQF_Check_请求"));
            session.Messages.Add(new CQFAIMessage("assistant", "CQF_Check_RawToolSecret", "CQF_Check_回答", true,
                new[] { new CQFAIToolCall("CQF_Check_OldCall", "cqf_apply_changes", new XElement("arguments", new XElement("changes_xml", "<changes/>"))) }));
            session.Messages.Add(new CQFAIMessage("tool", "CQF_Check_HiddenResult", visible: false));
            CQFAIActivity activity = new(1);
            activity.Update(new CQFAIStreamUpdate("CQF_Check_Preview", "CQF_Check_Thought", Array.Empty<string>()));
            CQFAIOperation operation = new(new CQFAIToolCall("CQF_Check_Record", "cqf_apply_changes", new XElement("arguments", new XElement("changes_xml", "CQF_Check_XmlSecret"))));
            activity.Add(operation);
            operation.Complete(XElement.Parse("<tool_result success='true'><liveMapApplied operations='2'/></tool_result>"), "");
            activity.Complete(); session.Activities.Add(activity);
            session.TaskUsage.Add(new CQFAITokenUsage(99, 11, 110)); session.TaskUsage.Add(null);
            session.SessionUsage.Restore(session.TaskUsage.Save("usage"));
            store.Save(session);
            CQFAISession restored = new CQFAISessionStore(root).Recent()!;
            Check(restored.Id == session.Id && restored.Title == session.Title && restored.Draft == session.Draft, "new store restores recent session metadata and Unicode input draft");
            Check(restored.Messages.Count == 2 && restored.Messages[1].Content == "CQF_Check_回答" && restored.Messages.All(message => message.ToolCalls.Count == 0), "history restores visible text without executable calls or hidden tool messages");
            string saved = File.ReadAllText(Path.Combine(root, "CQF_" + session.Id + ".xml"), System.Text.Encoding.UTF8);
            Check(!saved.Contains("CQF_Check_RawToolSecret") && !saved.Contains("CQF_Check_HiddenResult") && !saved.Contains("CQF_Check_XmlSecret"), "history excludes raw tool payloads and hidden request context");
            Check(restored.Activities.Single().Reasoning == "CQF_Check_Thought" && restored.Activities[0].Operations.Single().Succeeded == true && restored.Activities[0].Operations[0].Count == 2,
                "history preserves provider reasoning and actual operation receipts");
            Check(restored.TaskUsage.Total == 110 && restored.SessionUsage.Input == 99 && restored.SessionUsage.Unavailable == 1, "history preserves reported and unavailable usage totals");
            CQFAISession extendedSession = new() { Title = "CQF_Check_ExtendedHistory" };
            extendedSession.Messages.Add(new CQFAIMessage("user", "CQF_Check_ExtendedRequest"));
            CQFAIActivity extendedActivity = new(1);
            for (int index = 0; index < 160; index++)
            {
                CQFAIOperation extendedOperation = new(new CQFAIToolCall("CQF_Check_History_" + index, "cqf_read_map_region", new XElement("arguments")));
                extendedActivity.Add(extendedOperation);
                extendedOperation.Complete(new XElement("tool_result", new XAttribute("success", true)), "");
            }
            extendedActivity.Complete(); extendedSession.Activities.Add(extendedActivity);
            store.Save(extendedSession, false);
            Check(store.Load(extendedSession.Id).Activities.Single().Operations.Count == 160 && store.Recent()!.Id == session.Id,
                "extended task history saves to disk and reloads more than 96 tool receipts without changing the recent session");
            store.Delete(extendedSession.Id);
            CQFAIActivity interrupted = new(1); interrupted.Update(new CQFAIStreamUpdate("CQF_Check_Partial", "CQF_Check_PartialThought", Array.Empty<string>()));
            CQFAIActivity stopped = CQFAIActivity.Restore(interrupted.Save());
            Check(stopped.Finished && stopped.StageKey == "CQF_AI_ActivityStopped" && stopped.Preview == "CQF_Check_Partial", "unfinished saved requests restore as stopped previews and cannot resume execution");
            CQFAISession second = new() { Title = "CQF_Check_Second" }; second.Messages.Add(new CQFAIMessage("user", "CQF_Check_SecondRequest")); store.Save(second);
            session.Title = "CQF_Check_Renamed"; store.Save(session, false);
            Check(store.Recent()!.Id == second.Id && store.Load(session.Id).Title == session.Title, "renaming another session leaves the active recent session unchanged");
            List<Exception> errors = new();
            string damagedId = Guid.NewGuid().ToString("N"), damagedPath = Path.Combine(root, "CQF_" + damagedId + ".xml");
            File.WriteAllText(damagedPath, "<broken", System.Text.Encoding.UTF8);
            Check(store.List(errors.Add).Count == 2 && errors.Count == 1 && File.ReadAllText(damagedPath) == "<broken", "damaged history is reported while healthy entries remain available and original files are preserved");
            Check(store.List(_ => { }).Any(entry => entry.Title == "CQF_Check_Renamed" && entry.Preview == "CQF_Check_回答"), "history list provides compact titles and previews");
            string priorDraft = session.Draft; session.Draft = new string('x', 600001);
            Reject(() => store.Save(session), "oversized drafts cannot replace a readable history file");
            session.Draft = priorDraft;
            Check(store.Load(session.Id).Draft == priorDraft, "rejected saves preserve the last valid session content");
            Reject(() => store.Load("../../outside"), "history filenames cannot escape their directory");
            string mismatch = Guid.NewGuid().ToString("N"); File.Copy(Path.Combine(root, "CQF_" + session.Id + ".xml"), Path.Combine(root, "CQF_" + mismatch + ".xml"));
            Reject(() => store.Load(mismatch), "session identifiers must match the requested file");
            XElement badVersion = session.Save(); badVersion.SetAttributeValue("version", 2); Reject(() => CQFAISession.Restore(badVersion), "unsupported history versions are reported");
            XElement executable = session.Save(); executable.Element("messages")!.Add(new XElement("message", new XAttribute("role", "tool"), "CQF_Check_Tool"));
            Reject(() => CQFAISession.Restore(executable), "history cannot restore tool-role messages for replay");
            XElement invalidUsage = session.Save(); invalidUsage.Element("taskUsage")!.SetAttributeValue("total", 109); Reject(() => CQFAISession.Restore(invalidUsage), "invalid saved usage cannot fabricate token totals");
            File.WriteAllText(damagedPath, "<!DOCTYPE CQF_AI_Session [<!ENTITY x SYSTEM 'file:///CQF_Check_Outside'>]><CQF_AI_Session>&x;</CQF_AI_Session>", System.Text.Encoding.UTF8);
            Reject(() => store.Load(damagedId), "history XML forbids DTD and external entities");
            CQFAIWindow window = (CQFAIWindow)RuntimeHelpers.GetUninitializedObject(typeof(CQFAIWindow));
            Set(window, "model", model); Set(window, "catalog", catalog); Set(window, "conversation", new CQFAIConversation(model, catalog)); Set(window, "store", store);
            Set(window, "activities", new List<CQFAIActivity>()); Set(window, "taskUsage", new CQFAITokenTotals()); Set(window, "sessionUsage", new CQFAITokenTotals());
            Set(window, "session", second); Set(window, "command", "");
            typeof(CQFAIWindow).GetMethod("RestoreSession", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, new object[] { restored });
            window.NewChat(); string fresh = window.SessionId;
            Check(fresh != restored.Id && store.Load(restored.Id).Messages.Count == 2, "new conversation preserves the original archive and starts an independent session");
            window.SwitchChat(restored.Id);
            Check(window.SessionId == restored.Id && (string)Field(window, "command")! == restored.Draft && Field(window, "transaction") == null, "switching conversations restores drafts without target binding or undo objects");
            CQFAIConversation loadedConversation = (CQFAIConversation)Field(window, "conversation")!;
            loadedConversation.BeginTask();
            Check(loadedConversation.RequestMessages.All(message => message.Role != "tool" && message.ToolCalls.Count == 0), "continuing a saved conversation supplies historical prose only");
            window.RenameChat(restored.Id, "CQF_Check_WindowRename"); Check(store.Load(restored.Id).Title == "CQF_Check_WindowRename", "window rename persists the active title");
            window.DeleteChat(restored.Id); Check(!File.Exists(Path.Combine(root, "CQF_" + restored.Id + ".xml")) && window.SessionId != restored.Id, "deleting the active session starts a fresh chat without resurrecting its file");
            store.Delete(second.Id); Check(store.Recent()!.Id == window.SessionId, "deleting another archive does not clear the active recent pointer");
            store.Delete(window.SessionId); Check(store.Recent() == null, "deleting the recent session clears its pointer");
        }
        finally
        {
            if (Directory.Exists(root) && Path.GetFileName(root).StartsWith("CQF_AI_HistoryChecks_", StringComparison.Ordinal)
                && Path.GetDirectoryName(root) == Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)) Directory.Delete(root, true);
        }
    }
    private static void Set(CQFAIWindow window, string name, object value) => typeof(CQFAIWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, value);
    private static object? Field(CQFAIWindow window, string name) => typeof(CQFAIWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window);
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (Exception error) when (error is InvalidDataException || error is System.Xml.XmlException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
