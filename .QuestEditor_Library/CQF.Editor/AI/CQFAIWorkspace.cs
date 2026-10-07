using System.Globalization;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIWorkspace
    {
        public CQFAIWorkspace(CQFAIModel model, CQFAIResourceCatalog resources, CQFAITargetCatalog targets, string command, bool editing, bool text, bool inspectionOnly = false)
        {
            this.model = model; this.resources = resources; this.targets = targets; this.command = command; this.editing = editing; this.text = text;
            this.inspectionOnly = inspectionOnly;
            Transaction = new CQFAITaskTransaction(model);
            runtimeJournal = new CQFAIRuntimeJournal(model);
            Transaction.Add(runtimeJournal);
            Registry = CreateRegistry(null, null);
        }
        public CQFAIToolRegistry Registry { get; private set; }
        public CQFAIEditorContext? Context { get; private set; }
        public string SelectedTargetId { get; private set; } = string.Empty;
        public CQFAITaskTransaction Transaction { get; }
        public void AddInstruction(string text)
        {
            command += "\n" + text;
            Registry.AddInstruction(text);
            foreach (CQFAIToolRegistry registry in registries.Values.Where(r => !ReferenceEquals(r, Registry))) registry.AddInstruction(text);
        }
        public XElement Execute(CQFAIToolCall call)
        {
            if (call.Name is "cqf_list_targets" or "cqf_select_target")
            {
                CQFAITool tool = Registry.Tools.Single(tool => tool.Name == call.Name);
                return new XElement("tool_result", new XAttribute("id", call.Id), new XAttribute("name", call.Name), new XAttribute("success", true), tool.Execute(call.Arguments));
            }
            try { return Registry.Execute(call); }
            catch (InvalidOperationException error) when (error.Message == "CQF_AI_StaleTarget") { throw new InvalidDataException("CQF_AI_MissingTarget", error); }
        }
        private CQFAIToolRegistry CreateRegistry(CQFAIEditorContext? context, CQFAITransaction? transaction)
        {
            CQFAIToolRegistry registry = new CQFAIToolRegistry(model, resources, context, transaction, command, text, editing, runtimeJournal);
            registry.Register(new CQFAITool("cqf_list_targets", "Discover actual open editor data, loaded game maps and supported loaded CQF definitions. List first, then select an exact returned ID. Definitions without an editor are changed in runtime memory; saving source XML is separate.",
                arguments => targets.Query(arguments.Element("search")?.Value ?? "", int.TryParse(arguments.Element("offset")?.Value ?? "0", NumberStyles.None, CultureInfo.InvariantCulture, out int offset) ? offset : -1),
                ("search", "Name, type or kind substring; at most 100 characters.", false), ("offset", "Page offset from 0 to 100000.", false)));
            registry.Register(new CQFAITool("cqf_select_target", "Select an exact target ID returned by cqf_list_targets. Reads its current state and exposes the appropriate tools for the next round. Selection is internal to this task; it never binds the chat or replays historical writes.", Select,
                ("target_id", "Exact current target ID returned by discovery, never a historical ID.", true)));
            if (inspectionOnly) registry.RestrictToInspection();
            return registry;
        }
        private XElement Select(XElement arguments)
        {
            CQFAITarget target = targets.Resolve(arguments.Element("target_id")!.Value);
            if (!registries.TryGetValue(target.Id, out CQFAIToolRegistry registry)
                || checkpoints.TryGetValue(target.Id, out CQFAITransaction checkpoint) && checkpoint is not CQFAILiveMapTransaction && !checkpoint.IsCurrent)
            {
                CQFAIEditorContext context = target.Resolve();
                CQFAITransaction? transaction = !editing ? null : context.Read() is CQFAILiveMapInfo live && live.backend != null
                    ? new CQFAILiveMapTransaction(model, context, live.backend) : new CQFAITransaction(model, context);
                registry = CreateRegistry(context, transaction);
                registries[target.Id] = registry;
                contexts[target.Id] = context;
                if (transaction != null) { Transaction.Add(transaction); checkpoints[target.Id] = transaction; }
            }
            Registry = registry;
            Context = contexts[target.Id];
            SelectedTargetId = target.Id;
            return new XElement("selected_target", target.Summary, Registry.Tools.Single(tool => tool.Name == "cqf_get_context").Execute(new XElement("arguments")), Registry.Definitions);
        }
        private readonly CQFAIModel model;
        private readonly CQFAIRuntimeJournal runtimeJournal;
        private readonly CQFAIResourceCatalog resources;
        private readonly CQFAITargetCatalog targets;
        private string command;
        private readonly bool editing;
        private readonly bool text;
        private readonly bool inspectionOnly;
        private readonly Dictionary<string, CQFAIToolRegistry> registries = new Dictionary<string, CQFAIToolRegistry>(StringComparer.Ordinal);
        private readonly Dictionary<string, CQFAIEditorContext> contexts = new Dictionary<string, CQFAIEditorContext>(StringComparer.Ordinal);
        private readonly Dictionary<string, CQFAITransaction> checkpoints = new Dictionary<string, CQFAITransaction>(StringComparer.Ordinal);
    }
}
