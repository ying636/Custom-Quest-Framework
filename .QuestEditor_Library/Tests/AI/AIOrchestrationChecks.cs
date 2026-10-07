using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QuestEditor_Library;

internal static class AIOrchestrationChecks
{
    public static void Run(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        object data = new DialogTreeDef { defName = "CQF_Check_PlannedDialogue", label = "CQF_Check_Original" };
        CQFAIEditorContext context = new("CQF_Check_PlannedDialogue", () => data, value => data = value);
        CQFAIConversation conversation = new(model, catalog);
        CQFAIHarness main = new(model, catalog, conversation, context, "CQF_Check_Command", true, true, true);
        CQFAITaskState state = new(); main.AttachTask(state, null);
        Check(main.Registry.Tools.Any(t => t.Name == "cqf_update_plan") && !main.Registry.Tools.Any(t => t.Name == "cqf_spawn_agent"), "planning works independently of worker delegation");
        int call = 0;
        void Execute(string tool, params (string name, string value)[] arguments) => main.Process(AIToolChecks.Response("CQF_Check_Plan_" + call++, tool, arguments));
        Execute("cqf_set_goal", ("goal", "CQF_Check_Goal"), ("criteria_xml", "<criteria><item>CQF_Check_Readback</item></criteria>"));
        Execute("cqf_update_plan", ("plan_xml", "<plan><step id='edit' status='pending'><text>CQF_Check_Edit</text></step><step id='check' depends='edit' status='pending'><text>CQF_Check_Check</text></step></plan>"));
        Check(state.Goal == "CQF_Check_Goal" && state.Criteria.Count == 1 && state.Steps.Count == 2 && main.Instructions.Contains("CQF_Check_Goal"), "goal and dependency plan are stored and supplied to the model");
        string prior = state.Save().ToString();
        foreach (string invalid in new[]
        {
            "<plan><step id='a' depends='b'><text>A</text></step><step id='b' depends='a'><text>B</text></step></plan>",
            "<plan><step id='a' depends='missing'><text>A</text></step></plan>",
            "<plan><step id='a'><text>A</text></step><step id='a'><text>B</text></step></plan>",
            "<plan><step id='a' status='completed'><text>A</text></step></plan>",
            "<plan><step id='a'><text>A</text></step><step id='b' status='running' depends='a'><text>B</text></step></plan>"
        })
        {
            Execute("cqf_update_plan", ("plan_xml", invalid));
            Check(main.LastResults.Single().Element("error") != null && state.Save().ToString() == prior, "invalid or impossible plans preserve the prior plan atomically");
        }
        Execute("cqf_finish_goal", ("outcome", "completed"), ("evidence", "CQF_Check_NotDone"));
        Check(state.Status == "active" && main.LastResults.Single().Element("error") != null, "pending steps prevent premature goal completion");
        Execute("cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_PlannedLabel</value></set></changes>"));
        Check(state.RequiresVerification && ((DialogTreeDef)data).label == "CQF_Check_PlannedLabel" && main.Transaction!.CanUndo, "planned changes still apply live and keep undo");
        Execute("cqf_update_plan", ("plan_xml", "<plan><step id='edit' status='completed'><text>CQF_Check_Edit</text><evidence>CQF_Check_Applied</evidence></step><step id='check' depends='edit' status='completed'><text>CQF_Check_Check</text><evidence>CQF_Check_Claim</evidence></step></plan>"));
        Execute("cqf_finish_goal", ("outcome", "completed"), ("evidence", "CQF_Check_Claim"));
        Check(state.Status == "active" && main.LastResults.Single().Element("error") != null, "claimed plan evidence cannot bypass the required post-write read");
        Execute("cqf_read_target");
        Execute("cqf_finish_goal", ("outcome", "completed"), ("evidence", "CQF_Check_ReadActualLabel"));
        Check(state.Status == "completed" && !main.Process("<assistant><reply>CQF_Check_Done</reply></assistant>"), "checked goals can complete and finish the outer conversation");
        Execute("cqf_apply_changes", ("changes_xml", "<changes><set path='/label'><value>CQF_Check_ExtraEdit</value></set></changes>"));
        Check(state.Status == "active" && state.RequiresVerification, "additional writes reopen completed goals and require a new readback");
        main.Transaction!.Undo(); Check(((DialogTreeDef)data).label == "CQF_Check_Original", "orchestration leaves actual editor undo intact");
        main.AddInstruction("CQF_Check_AdditionalRequirement");
        Check(state.Status == "active" && conversation.VisibleMessages.Last().DisplayContent == "CQF_Check_AdditionalRequirement", "mid-task requirements update the same task and visible chat");
        Check(main.Process("<assistant><reply>CQF_Check_UnfinishedReply</reply></assistant>"), "a final-looking reply cannot silently finish an active goal");
        WorkerChecks(model, catalog, context);
        PersistenceChecks(state);
        SteeringChecks(model, catalog);
    }

    private static void WorkerChecks(CQFAIModel model, CQFAIResourceCatalog catalog, CQFAIEditorContext context)
    {
        using CancellationTokenSource cancellation = new();
        int thread = Environment.CurrentManagedThreadId, requestCount = 0;
        Dictionary<string, TaskCompletionSource<string>> requests = new();
        Dictionary<string, CQFAIHarness> workers = new();
        CQFAITaskState state = new();
        Func<IEnumerable<CQFAITarget>> targets = () => new[] { new CQFAITarget(context, "CQF_Check_WorkerTarget", typeof(DialogTreeDef).FullName!, "editor", () => context, () => true) };
        CQFAIOrchestration coordinator = new(state, 2, record =>
        {
            CQFAIConversation conversation = new(model, catalog);
            CQFAIHarness worker = new(model, catalog, conversation, null, record.Assignment, false, false, true, new CQFAITargetCatalog(targets), inspectionOnly: true);
            conversation.Add("user", record.Assignment); workers.Add(record.Id, worker);
            return new CQFAIAgentRunner(record, worker, (harness, token) =>
            {
                token.ThrowIfCancellationRequested(); requestCount++;
                Check(Environment.CurrentManagedThreadId == thread, "worker requests and game-state tool dispatch remain on the host thread");
                TaskCompletionSource<string> completion = new(); requests[record.Id] = completion;
                return completion.Task;
            }, cancellation.Token);
        }, error => "CQF_Check_ReportedError: " + error.Message);
        CQFAIHarness main = new(model, catalog, new CQFAIConversation(model, catalog), null, "CQF_Check_ParentOnly", true, true, true);
        main.AttachTask(state, coordinator);
        for (int index = 0; index < 3; index++) main.Process(AIToolChecks.Response("CQF_Check_Spawn_" + index, "cqf_spawn_agent", ("name", "CQF_Check_Worker" + index), ("assignment", "CQF_Check_Assignment" + index)));
        main.Process(AIToolChecks.Response("CQF_Check_Wait", "cqf_wait_agents"));
        coordinator.Poll();
        Check(coordinator.Waiting && state.Agents.Count(a => a.Status == "running") == 2 && state.Agents.Count(a => a.Status == "pending") == 1, "worker scheduler enforces two concurrent requests and queues the third");
        int count = requestCount;
        for (int index = 0; index < 20; index++) coordinator.Poll();
        Check(requestCount == count && coordinator.TakeReports() == null, "waiting pumps locally without repeated model requests or fabricated reports");
        string first = state.Agents[0].Id;
        Check(workers[first].InspectionOnly && workers[first].Transaction == null && workers[first].RequestMessages.Single(m => m.Role == "user").Content == "CQF_Check_Assignment0",
            "workers receive independent bounded context rather than parent conversation history");
        requests[first].SetResult(AIToolChecks.Response("CQF_Check_WorkerList", "cqf_list_targets")); coordinator.Poll();
        string id = workers[first].LastResults.Single().Descendants("target").Single().Attribute("id")!.Value;
        requests[first].SetResult(AIToolChecks.Response("CQF_Check_WorkerSelect", "cqf_select_target", ("target_id", id))); coordinator.Poll();
        Check(workers[first].Registry.Tools.Any(t => t.Name == "cqf_read_target") && !workers[first].Registry.Tools.Any(t => t.Name == "cqf_apply_changes" || t.Name == "cqf_spawn_agent")
            && !workers[first].LastResults.Single().Descendants("tool").Any(t => t.Attribute("name")?.Value == "cqf_apply_changes"), "worker target selection exposes actual read tools without write or recursive delegation tools");
        requests[first].SetResult(AIToolChecks.Response("CQF_Check_WorkerDenied", "cqf_apply_changes", ("changes_xml", "<changes/>"))); coordinator.Poll();
        Check(workers[first].LastResults.Single().Element("error")?.Attribute("code")?.Value == "CQF_AI_ReadOnly", "worker writes are rejected by executable permission checks");
        requests[first].SetResult("<assistant><reply>CQF_Check_WorkerProposal</reply></assistant>"); coordinator.Poll();
        Check(state.Agents[0].Status == "completed" && state.Agents[2].Status == "running" && state.Agents.Count(a => a.Status == "running") == 2, "finishing one worker immediately admits the queued worker within the concurrency cap");
        XElement report = coordinator.TakeReports()!;
        Check(report.Descendants("result").Single().Value == "CQF_Check_WorkerProposal" && coordinator.TakeReports() == null, "worker results are returned exactly once and identified as read-only proposals");
        main.Process(AIToolChecks.Response("CQF_Check_OlderWorkerResult", "cqf_get_agent_result", ("agent_id", first)));
        Check(main.LastResults.Single().Descendants("result").Single().Value == "CQF_Check_WorkerProposal", "older complete worker results remain available through targeted reads");
        requests[state.Agents[1].Id].SetException(new InvalidDataException("CQF_Check_WorkerFailure")); coordinator.Poll();
        Check(state.Agents[1].Status == "failed" && state.Agents[1].Result.Contains("CQF_Check_WorkerFailure") && coordinator.Waiting, "one worker failure is recorded while independent workers keep running");
        requests[state.Agents[2].Id].SetResult("<assistant><reply>CQF_Check_FinalWorker</reply></assistant>"); coordinator.Poll();
        Check(!coordinator.HasPending && !coordinator.Waiting, "worker wait is released only after outstanding workers finish");
        main.CollectAgentReports();
        Check(main.RequestMessages.Any(m => m.Content.Contains("CQF_Check_FinalWorker")) && main.Instructions.Contains("CQF_Check_Worker0"), "main requests receive current worker reports and task summaries");
        main.Process(AIToolChecks.Response("CQF_Check_SetBlockedGoal", "cqf_set_goal", ("goal", "CQF_Check_BlockedGoal"), ("criteria_xml", "<criteria><item>CQF_Check_Condition</item></criteria>")));
        main.Process(AIToolChecks.Response("CQF_Check_StopSpawn", "cqf_spawn_agent", ("name", "CQF_Check_StopWorker"), ("assignment", "CQF_Check_StopAssignment"))); coordinator.Poll();
        main.Process(AIToolChecks.Response("CQF_Check_BlockedGoal", "cqf_finish_goal", ("outcome", "blocked"), ("evidence", "CQF_Check_MissingRequirement")));
        Check(!main.Process("<assistant><reply>CQF_Check_BlockageReported</reply></assistant>"), "a reported blockage can finish without waiting for irrelevant unfinished workers");
        cancellation.Cancel(); coordinator.Stop();
        Check(state.Agents.Last().Status == "paused" && !coordinator.HasPending && state.Status == "blocked", "stopping pauses remaining workers and preserves a recorded blockage");
        CQFAIToolRegistry registry = new(model, catalog, context, null, "", false);
        registry.Register(new CQFAITool("cqf_check_untrusted_extension", "CQF_Check_UnknownExtension", _ => throw new Exception("Must not execute")));
        registry.RestrictToInspection(); Check(!registry.Tools.Any(t => t.Name == "cqf_check_untrusted_extension"), "worker allowlist excludes unknown extension tools");
    }

    private static void PersistenceChecks(CQFAITaskState state)
    {
        state.Agents.Add(new CQFAIAgentRecord("CQF_Check_SavedWorker", "CQF_Check_Name", "CQF_Check_Assignment")); state.Agents.Last().Start();
        CQFAISession session = new(); session.Messages.Add(new CQFAIMessage("user", "CQF_Check_SavedTask"));
        session.Activities.Add(new CQFAIActivity(1) { TaskState = state });
        CQFAISession restored = CQFAISession.Restore(session.Save());
        CQFAITaskState saved = restored.Activities.Single().TaskState!;
        Check(saved.Goal == state.Goal && saved.Criteria.SequenceEqual(state.Criteria) && saved.Steps.Count == state.Steps.Count && saved.Agents.Single().Assignment == "CQF_Check_Assignment",
            "conversation archives retain goals, criteria, plans and worker records");
        Check(saved.Status == "paused" && saved.Agents.Single().Status == "paused" && restored.Activities.Single().Finished, "loading an unfinished task does not restart network requests or replay edits");
        saved.Resume(); Check(saved.Status == "active" && state.Status == "active", "restored task state can continue independently after explicit new input");
        XElement invalid = state.Save(); invalid.SetAttributeValue("status", "unknown"); Reject(() => CQFAITaskState.Restore(invalid), "invalid saved task statuses are reported");
        invalid = state.Save(); invalid.Element("agents")!.Element("agent")!.SetAttributeValue("seconds", "NaN"); Reject(() => CQFAITaskState.Restore(invalid), "invalid worker durations cannot corrupt history");
    }

    private static void SteeringChecks(CQFAIModel model, CQFAIResourceCatalog catalog)
    {
        CQFAIWindow window = (CQFAIWindow)RuntimeHelpers.GetUninitializedObject(typeof(CQFAIWindow));
        CQFAIHarness harness = new(model, catalog, new CQFAIConversation(model, catalog), null, "CQF_Check_Steering", false, false);
        TaskCompletionSource<string> pending = new();
        void Set(string name, object value) => typeof(CQFAIWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(window, value);
        Set("harness", harness); Set("task", pending.Task); Set("pendingSteering", true);
        bool enabled = CustomQuestFramework_ModSetting.setting.dialogAIEnabled;
        CustomQuestFramework_ModSetting.setting.dialogAIEnabled = true;
        try
        {
            typeof(CQFAIWindow).GetMethod("Poll", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null);
            Check(ReferenceEquals(typeof(CQFAIWindow).GetField("task", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window), pending.Task) && window.IsWorking,
                "steering waits for the in-flight response before starting another main request");
        }
        finally { CustomQuestFramework_ModSetting.setting.dialogAIEnabled = enabled; }
    }
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidDataException) { Console.WriteLine("PASS " + name); return; }
        throw new InvalidOperationException("Expected rejection: " + name);
    }
}
