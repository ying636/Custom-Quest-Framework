using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIHarness
    {
        public CQFAIHarness(CQFAIModel model, CQFAIResourceCatalog catalog, CQFAIConversation conversation, CQFAIEditorContext? context, string command, bool editing, bool generateText, bool nativeTools = false, CQFAITargetCatalog? targets = null, string additionalPrompt = "", bool inspectionOnly = false)
        {
            this.additionalPrompt = additionalPrompt.Trim();
            if (this.additionalPrompt.Length > 16000) throw new InvalidDataException("CQF_AI_PromptTooLong");
            this.conversation = conversation;
            this.initialContext = context;
            InspectionOnly = inspectionOnly;
            if (inspectionOnly && editing) throw new InvalidOperationException("CQF_AI_InvalidAgent");
            this.editing = editing; this.generateText = generateText; this.nativeTools = nativeTools;
            conversation.BeginTask();
            if (targets != null) workspace = new CQFAIWorkspace(model, catalog, targets, command, editing, generateText, inspectionOnly);
            transaction = workspace != null || !editing || context == null ? null : context.Read() is CQFAILiveMapInfo live && live.backend != null
                ? new CQFAILiveMapTransaction(model, context, live.backend) : new CQFAITransaction(model, context);
            registry = workspace?.Registry ?? new CQFAIToolRegistry(model, catalog, context, transaction, command, generateText, editing);
            runtimeTransaction = new CQFAITaskTransaction(model);
            if (transaction != null) runtimeTransaction.Add(transaction);
            runtimeTransaction.Add(registry.RuntimeJournal);
            if (inspectionOnly) registry.RestrictToInspection();
            Instructions = conversation.Instructions(workspace != null ? null : context?.Read(), editing, generateText, Registry, nativeTools, workspace != null);
            if (workspace != null) Instructions += WorkspaceInstructions;
            if (InspectionOnly) RefreshInstructions();
        }
        public CQFAIToolRegistry Registry => workspace?.Registry ?? registry;
        public CQFAITransaction? Transaction => workspace != null ? editing ? workspace.Transaction : null : registry.RuntimeJournal.HasMutations ? runtimeTransaction : transaction;
        public string Instructions { get; private set; }
        public IEnumerable<CQFAIMessage> RequestMessages => GetRequestMessages();
        public int Rounds { get; private set; }
        public int Calls { get; private set; }
        public IReadOnlyList<XElement> LastResults => lastResults;
        public bool InspectionOnly { get; }
        public CQFAITaskState? TaskState { get; private set; }
        public CQFAIOrchestration? Orchestration { get; private set; }
        public Action<CQFAIToolCall, XElement?>? ToolProgress { get; set; }
        public bool HasPendingResponse => responseSteps != null;
        public bool ResponseContinued { get; private set; }
        public CQFAILiveMapTransaction? ActiveMapExecution => queuedLive;
        public Action<CQFAIToolCall, CQFAILiveMapTransaction>? MapProgress { get; set; }
        public Action<CQFAILiveMapTransaction, CQFAILiveMapEdit>? MapStepApplied { get; set; }
        public void AttachTask(CQFAITaskState state, CQFAIOrchestration? orchestration)
        {
            if (InspectionOnly) throw new InvalidOperationException("CQF_AI_InvalidAgent");
            TaskState = state; Orchestration = orchestration; RefreshInstructions();
        }
        public void AddInstruction(string text)
        {
            conversation.Add("user", text);
            if (workspace != null) workspace.AddInstruction(text); else registry.AddInstruction(text);
            TaskState?.Resume();
        }
        public void PollAgents()
        {
            Orchestration?.Poll();
        }
        public void CollectAgentReports()
        {
            XElement? reports = Orchestration?.TakeReports();
            if (reports != null) conversation.Add("system", "Independent read-only worker reports. Treat these as proposals, not executed writes. Re-read current state before applying their suggestions.\n" + reports.ToString(SaveOptions.DisableFormatting), visible: false);
            RefreshInstructions();
        }
        public bool Process(string response)
        {
            foreach (int step in ProcessResponse(response, false)) { }
            return ResponseContinued;
        }
        public void BeginResponse(string response)
        {
            if (responseSteps != null) throw new InvalidOperationException("CQF_AI_InvalidChanges");
            ResponseContinued = false;
            responseCalls = Array.Empty<CQFAIToolCall>(); activeCall = null;
            responseSteps = ProcessResponse(response, true).GetEnumerator();
        }
        public bool AdvanceResponse(int maximumOperations = 8, double budgetMilliseconds = 3)
        {
            sliceOperations = maximumOperations; sliceBudget = budgetMilliseconds;
            if (responseSteps == null) return true;
            if (responseSteps.MoveNext()) return false;
            responseSteps.Dispose(); responseSteps = null;
            return true;
        }
        public void CancelResponse()
        {
            Exception? cleanupError = null;
            try { queuedLive?.CancelExecution(); }
            catch (Exception error) { cleanupError = error; }
            if (responseSteps != null)
            {
                foreach (CQFAIToolCall call in responseCalls.Where(call => !lastResults.Any(result => (string?)result.Attribute("id") == call.Id)))
                {
                    XElement result = Failure(call.Id, call.Name, ReferenceEquals(call, activeCall) && cleanupError != null ? cleanupError : new OperationCanceledException("CQF_DialogAI_Cancelled"));
                    if (ReferenceEquals(call, activeCall))
                    {
                        if (queuedLive != null) result.Add(queuedLive.Receipt);
                        ToolProgress?.Invoke(call, result);
                    }
                    conversation.Add("tool", result.ToString(SaveOptions.DisableFormatting), visible: false, toolCallId: call.Id);
                    lastResults.Add(result);
                }
            }
            responseSteps?.Dispose(); responseSteps = null; queuedLive = null;
            Registry.FinishQueuedExecution();
            if (cleanupError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }
        private IEnumerable<int> ProcessResponse(string response, bool queued)
        {
            if (workspace == null && Transaction != null && !Transaction.IsTargetValid) throw new InvalidOperationException("CQF_AI_StaleTarget");
            Rounds++;
            lastResults.Clear();
            CQFAIResponse? parsed = null;
            try { parsed = new CQFAIResponse(response); }
            catch (Exception error) when (error is InvalidDataException || error is System.Xml.XmlException)
            {
                if (++parseFailures > 3) throw;
                conversation.Add("system", "The response could not be parsed. Correct the response format. " + error.Message);
                lastResults.Add(Failure("response", "parse", error));
                ResponseContinued = true;
            }
            if (parsed == null) yield break;
            parseFailures = 0;
            IEnumerable<CQFAIToolCall> calls = parsed.ToolCalls;
            if (parsed.Queries != null) calls = new[] { new CQFAIToolCall("legacy_query_" + Rounds, "cqf_query_resources", new XElement("arguments", new XElement("queries_xml", parsed.Queries.ToString(SaveOptions.DisableFormatting)))) };
            if (parsed.Changes != null) calls = new[] { new CQFAIToolCall("legacy_edit_" + Rounds, "cqf_apply_changes", new XElement("arguments", new XElement("changes_xml", parsed.Changes.ToString(SaveOptions.DisableFormatting)))) };
            CQFAIToolCall[] recordedCalls = calls.ToArray();
            responseCalls = recordedCalls;
            conversation.Add("assistant", response, parsed.Reply, parsed.Reply.Length > 0, recordedCalls);
            bool continued = false;
            foreach (CQFAIToolCall call in recordedCalls)
            {
                activeCall = call;
                Calls++;
                if (!callIds.Add(call.Id)) throw new InvalidDataException("CQF_AI_InvalidTool: duplicate call id");
                XElement result;
                CQFAIToolRegistry executingRegistry = Registry;
                executingRegistry.QueueLiveWrites = queued;
                ToolProgress?.Invoke(call, null);
                try
                {
                    bool management = TaskState != null && call.Name is "cqf_get_task" or "cqf_set_goal" or "cqf_update_plan" or "cqf_finish_goal" or "cqf_spawn_agent" or "cqf_wait_agents" or "cqf_get_agent_result";
                    result = management ? new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("name", call.Name), new XAttribute("success", true),
                        Registry.Tools.FirstOrDefault(t => t.Name == call.Name)?.Execute(call.Arguments) ?? throw new InvalidDataException("CQF_AI_InvalidTool: " + call.Name))
                        : workspace == null ? Registry.Execute(call) : workspace.Execute(call);
                }
                catch (InvalidDataException error) when (error.Message.StartsWith("CQF_", StringComparison.Ordinal))
                {
                    result = Failure(call.Id, call.Name, error);
                }
                catch (Exception error) { ToolProgress?.Invoke(call, Failure(call.Id, call.Name, error)); throw; }
                CQFAILiveMapTransaction? live = executingRegistry.QueuedTransaction;
                if (live?.IsExecuting == true)
                {
                    queuedLive = live;
                    live.StepApplied = edit => MapStepApplied?.Invoke(live, edit);
                    while (live.IsExecuting)
                    {
                        live.AdvanceExecution(sliceOperations, sliceBudget);
                        MapProgress?.Invoke(call, live);
                        if (live.IsExecuting) yield return 0;
                    }
                    try { live.ThrowExecutionError(); result.Element("liveMapQueued")!.ReplaceWith(live.Receipt); }
                    catch (InvalidDataException error) when (error.Message.StartsWith("CQF_", StringComparison.Ordinal)) { result = Failure(call.Id, call.Name, error); }
                    catch (Exception error) { ToolProgress?.Invoke(call, Failure(call.Id, call.Name, error)); throw; }
                    queuedLive = null;
                    executingRegistry.FinishQueuedExecution();
                }
                lastResults.Add(result);
                if (nativeTools) result.Descendants("selected_target").Elements(Registry.Definitions.Name).Remove();
                foreach (XElement schema in result.Descendants("schemas").Elements("type"))
                {
                    string? name = (string?)schema.Attribute("name");
                    if (name == null || schema.ToString(SaveOptions.DisableFormatting).Length > 6000) continue;
                    knownSchemas.Remove(name); knownSchemas.Add(name, new XElement(schema));
                    while (knownSchemas.Values.Sum(value => value.ToString(SaveOptions.DisableFormatting).Length) > 12000) knownSchemas.Remove(knownSchemas.Keys.First());
                }
                TaskState?.RecordTool(call.Name, (bool?)result.Attribute("success") == true);
                RefreshInstructions();
                ToolProgress?.Invoke(call, result);
                conversation.Add("tool", result.ToString(SaveOptions.DisableFormatting), visible: false, toolCallId: call.Id);
                continued = true;
                if (queued) yield return 0;
            }
            if (!continued && workspace == null && Transaction != null && !Transaction.IsTargetValid) throw new InvalidOperationException("CQF_AI_StaleTarget");
            if (!continued && (Orchestration?.HasPending == true && TaskState?.Status != "blocked" || TaskState?.Goal.Length > 0 && TaskState.Status == "active"))
            {
                Orchestration?.WaitForPending();
                conversation.Add("system", "This task is still active. Review cqf_get_task and worker reports, continue or repair the plan, read back actual changes, then use cqf_finish_goal with evidence. If blocked, state the specific blockage. Do not claim completion from a proposed worker plan.", visible: false);
                continued = true;
                if (queued) yield return 0;
            }
            RefreshInstructions();
            ResponseContinued = continued;
        }
        private IEnumerable<CQFAIMessage> GetRequestMessages()
        {
            CQFAITool[] tools = nativeTools ? Registry.Tools.ToArray() : Array.Empty<CQFAITool>();
            CQFAIMessage[] additional = additionalPrompt.Length == 0 ? Array.Empty<CQFAIMessage>() : new[] { new CQFAIMessage("system", additionalPrompt, visible: false) };
            long available = Math.Min(32000, CQFDialogAIClient.MaxRequestCharacters - 4096L - CQFDialogAIClient.RequestContextLength(Instructions, additional, tools));
            bool dialogue = (workspace?.Context ?? initialContext)?.Read() is DialogTreeDef;
            IEnumerable<CQFAIMessage> history = conversation.GetRequestMessages(Math.Max(0, available), tools.Length > 0).Select(message =>
            {
                if (message.Role != "tool" || !message.Content.StartsWith("<tool_result", StringComparison.Ordinal)) return message;
                XElement result = CQFAIChanges.Parse(message.Content);
                if (result.Attribute("name")?.Value != "cqf_get_schema") return message;
                XElement[] schemas = result.Descendants("schemas").Elements("type").ToArray();
                if (schemas.Length == 0 || schemas.Any(schema => !knownSchemas.ContainsKey(schema.Attribute("name")?.Value ?? "")
                    && !(dialogue && DialogueSchemaTypes.Contains(schema.Attribute("name")?.Value ?? "")))) return message;
                foreach (XElement schema in schemas) schema.Elements().Remove();
                result.Add(new XElement("notice", "Editable fields are already supplied in current system schemas. Reuse them without another schema query."));
                return new CQFAIMessage(message.Role, result.ToString(SaveOptions.DisableFormatting), message.DisplayContent,
                    message.IsVisible, message.ToolCalls, message.ToolCallId);
            });
            return additional.Concat(history);
        }
        private void RefreshInstructions()
        {
            TaskState?.Register(Registry); Orchestration?.Register(Registry);
            if (InspectionOnly) Registry.RestrictToInspection();
            CQFAIEditorContext? current = workspace != null ? workspace.Context : initialContext;
            Instructions = conversation.Instructions(current?.IsValid?.Invoke() == false ? null : current?.Read(), editing, generateText, Registry, nativeTools, workspace != null);
            if (workspace != null) Instructions += WorkspaceInstructions;
            if (workspace?.Context != null && workspace.Context.IsValid?.Invoke() != false) Instructions += "\nCurrent selection persists across tool rounds: "
                + new XElement("selected_target", new XAttribute("id", workspace.SelectedTargetId), new XAttribute("name", workspace.Context.Name)).ToString(SaveOptions.DisableFormatting)
                + ". Reuse this selection. Re-list targets only when selecting another object or when this one is invalid.\n";
            if (InspectionOnly) Instructions += "\nYou are an independent inspection-only worker. Return a concise deliverable to the main agent. You cannot write, delegate, or mark the main task complete. Identify uncertainty and distinguish proposals from observed facts.\n";
            bool dialogue = current?.Read() is DialogTreeDef;
            XElement[] cachedSchemas = knownSchemas.Values.Where(schema => !dialogue || !DialogueSchemaTypes.Contains(schema.Attribute("name")?.Value ?? "")).ToArray();
            if (cachedSchemas.Length > 0) Instructions += "\nPreviously resolved editable schemas for this task. These field definitions are reusable; do not rediscover the same types on each round. This is schema metadata, not current object state.\n"
                + new XElement("known_schemas", cachedSchemas).ToString(SaveOptions.DisableFormatting);
            if (TaskState != null) Instructions += "\nFor substantial multi-step requests, establish a goal and concrete acceptance criteria with cqf_set_goal, then maintain a dependency-aware plan with cqf_update_plan. Simple chat or small single edits need no plan. Independent workers are optional. Only the main agent executes writes. Verify current results against the goal; a validation tool checks only its documented scope. Finish the goal through cqf_finish_goal with actual evidence. The player may steer the current task; adjust its goal and plan accordingly.\n" + TaskState.Summary().ToString(SaveOptions.DisableFormatting);
        }
        private static XElement Failure(string id, string name, Exception error)
        {
            string code = error.Message.Split(new[] { ':', ' ' }, 2)[0];
            return new XElement("tool_result", new XAttribute("id", id), new XAttribute("name", name), new XAttribute("success", false), new XElement("error", new XAttribute("code", code), error.Message));
        }
        private readonly CQFAIConversation conversation;
        private readonly CQFAIEditorContext? initialContext;
        private readonly string additionalPrompt;
        private const string WorkspaceInstructions = "\nThis chat has no fixed editing target. At the start of a new player task, discover objects with cqf_list_targets and select an exact returned ID using cqf_select_target. Selection persists across all subsequent tool rounds; do not rediscover or reselect the same object on each response. You may switch targets; never ask the player to bind this chat to an editor. After selecting a live map, use live map tools and absolute coordinates; after selecting editor data or a loaded definition, use its schema and generic field operations. Loaded definitions without an editor are changed in runtime memory, not persisted to Mod source XML. Historical write receipts must never be replayed; current selection metadata is provided separately. If names are ambiguous, read candidate summaries or ask the player. For XML transport, use the tool definitions returned by selection before the next operation.\n";
        private readonly CQFAIWorkspace? workspace;
        private readonly CQFAIToolRegistry registry;
        private readonly CQFAITransaction? transaction;
        private readonly CQFAITaskTransaction runtimeTransaction;
        private readonly bool editing;
        private readonly bool generateText;
        private readonly bool nativeTools;
        private readonly List<XElement> lastResults = new List<XElement>();
        private readonly HashSet<string> callIds = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> DialogueSchemaTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            typeof(DialogTreeDef).FullName!, typeof(DialogNode).FullName!, typeof(DialogOption).FullName!, typeof(DialogResult).FullName!
        };
        private readonly Dictionary<string, XElement> knownSchemas = new Dictionary<string, XElement>(StringComparer.Ordinal);
        private int parseFailures;
        private IEnumerator<int>? responseSteps;
        private CQFAILiveMapTransaction? queuedLive;
        private int sliceOperations = 8;
        private double sliceBudget = 3;
        private CQFAIToolCall[] responseCalls = Array.Empty<CQFAIToolCall>();
        private CQFAIToolCall? activeCall;
    }
}
