using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIExecutionRuntime
    {
        public CQFAIExecutionRuntime(CQFAIModel model, CQFAIRuntimeJournal journal, bool text = true, string command = "") { this.model = model; this.journal = journal; this.text = text; this.command = command; }
        public XElement Help() => new XElement("execution",
            new XElement("query", "kind=signal_receivers,offset,limit,optional search. Lists actual registered receiver types; a registration is not proof that it accepts a particular signal."),
            new XElement("targets", "<targets><target><key>exact argument/target name</key><thing_id>spawned Thing ID</thing_id></target><target><key>Position</key><map_id>id</map_id><x>cellX</x><z>cellZ</z></target></targets>. Maximum 32; mutually exclusive Thing or cell per target."),
            new XElement("operation", new XAttribute("kind", "send_signal"), "tag=exact runtime signal name,global=false,targets optional. Uses actual SignalManager and NamedArgument targets. Does not add Quest prefixes. Not undoable; native receiver exceptions are logged by the game and delivery must be verified by readback."),
            new XElement("operation", new XAttribute("kind", "run_actions"), "actions_xml=<value><li Class='exact CQFAction subtype'>model fields...</li></value>,optional quest_id,targets. Discovers and executes supported loaded CQF action types and submod subclasses through Work. CustomThing actions use their serializable data field, not raw customThing instances. Maximum 32 root actions and 2048 estimated nested invocations per call. Validates data before execution; receipt distinguishes invoked actions from proven effects, reports partial failure. Not fully undoable."),
            new XElement("conditions", "cqf_check_conditions request fields: conditions_xml=<value><li Class='exact DialogCondition subtype'>model fields...</li></value>,quest_id optional,targets optional. Maximum 40 root conditions. Returns satisfied and actual reason, including target/database failures. Query schema and exact subclasses with cqf_find_types first."));
        public XElement Read(XElement request)
        {
            CQFAIRuntimeRequest.Fields(request, "kind", "search", "offset", "limit");
            string search = CQFAIRuntimeRequest.Text(request, "search");
            if (search.Length > 100) throw new InvalidDataException("CQF_AI_InvalidTool: search");
            SignalManager manager = Find.SignalManager ?? throw new InvalidDataException("CQF_AI_MissingResource: signal manager");
            return CQFAIRuntimeRequest.Page("signalReceivers", manager.receivers.Select((receiver, index) => new { receiver, index })
                .Where(entry => entry.receiver.GetType().FullName!.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).Select(entry => new XElement("receiver", new XAttribute("index", entry.index),
                    new XAttribute("type", entry.receiver.GetType().FullName!), entry.receiver is Quest quest ? new XAttribute("questId", quest.id) : null)), request);
        }
        public XElement Check(XElement request)
        {
            if (Current.Game == null) throw new InvalidDataException("CQF_AI_MissingResource: no active game");
            CQFAIRuntimeRequest.Fields(request, "conditions_xml", "quest_id", "targets");
            List<DialogCondition> conditions = (List<DialogCondition>)(model.Read(CQFAIChanges.Parse(CQFAIRuntimeRequest.Text(request, "conditions_xml")), typeof(List<DialogCondition>)) ?? throw new InvalidDataException("CQF_AI_InvalidValue: conditions"));
            if (conditions.Count > 40 || conditions.Any(condition => condition == null)) throw new InvalidDataException("CQF_AI_InvalidValue: condition count");
            CQFAIChanges.Validate(conditions);
            Dictionary<string, TargetInfo> targets = CQFAIRuntimeRequest.Targets(request); Quest? quest = CQFAIRuntimeRequest.Quest(request);
            XElement[] results = conditions.Select((condition, index) => { bool satisfied = condition.Satisfied(targets, out string reason, quest!); reason ??= ""; return new XElement("condition", new XAttribute("index", index), new XAttribute("type", condition.GetType().FullName!), new XAttribute("satisfied", satisfied), new XAttribute("reasonTruncated", reason.Length > 2000), new XElement("reason", reason.Length > 2000 ? reason.Substring(0, 2000) : reason)); }).ToArray();
            return new XElement("conditionResults", new XAttribute("allSatisfied", results.All(result => (bool)result.Attribute("satisfied")!)), results);
        }
        public void AddInstruction(string text) { command += "\n" + text; }
        public XElement Operate(XElement request)
        {
            string kind = CQFAIRuntimeRequest.Text(request, "kind");
            CQFAIRuntimeRequest.Fields(request, kind switch
            {
                "send_signal" => new[] { "kind", "tag", "global", "targets" },
                "run_actions" => new[] { "kind", "targets", "actions_xml", "quest_id" },
                _ => throw new InvalidDataException("CQF_AI_InvalidTool: execution operation")
            });
            Dictionary<string, TargetInfo> targets = CQFAIRuntimeRequest.Targets(request);
            if (kind == "send_signal")
            {
                string tag = CQFAIRuntimeRequest.Text(request, "tag"); bool global = CQFAIRuntimeRequest.Bool(request, "global");
                SignalManager manager = Find.SignalManager ?? throw new InvalidDataException("CQF_AI_MissingResource: signal manager");
                if (string.IsNullOrWhiteSpace(tag) || tag.Length > 200) throw new InvalidDataException("CQF_AI_InvalidValue: signal tag");
                if ((int)typeof(SignalManager).GetField("signalsThisFrame", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)! >= 3000) throw new InvalidDataException("CQF_AI_InvalidValue: signal frame limit reached");
                SignalArgs args = new SignalArgs(targets.Select(pair => new NamedArgument(pair.Value.HasThing ? (object)pair.Value.Thing : pair.Value, pair.Key)).ToArray());
                int receivers = manager.receivers.Count;
                journal.MarkIrreversible(); manager.SendSignal(new Signal(tag, args, global));
                return new XElement("signalDispatched", new XAttribute("tag", tag), new XAttribute("registeredReceivers", receivers), new XAttribute("undoSupported", false), new XAttribute("deliveryVerified", false));
            }
            if (kind != "run_actions") throw new InvalidDataException("CQF_AI_InvalidTool: execution operation");
            List<CQFAction> actions = (List<CQFAction>)(model.Read(CQFAIChanges.Parse(CQFAIRuntimeRequest.Text(request, "actions_xml")), typeof(List<CQFAction>)) ?? throw new InvalidDataException("CQF_AI_InvalidValue: actions"));
            if (actions.Count < 1 || actions.Count > 32 || actions.Any(action => action == null)) throw new InvalidDataException("CQF_AI_InvalidValue: action count");
            CQFAIChanges.Validate(actions);
            XElement serialized = model.Write(actions);
            new CQFAIChanges(model).Build(new CQFAIGenerationActionsConfiguration(), new XElement("changes", new XElement("set", new XAttribute("path", "/actions"), serialized)), command, text);
            if (serialized.Descendants().Count() > 4096 || serialized.Descendants("loopCount").Any(value => !int.TryParse(value.Value, out int count) || count < 0 || count > 128)) throw new InvalidDataException("CQF_AI_InvalidValue: action execution budget");
            long budget = 0;
            foreach (XElement action in serialized.Descendants().Where(element => element.Attribute("Class") != null && typeof(CQFAction).IsAssignableFrom(model.Resolve(element.Attribute("Class")!.Value))))
            {
                long cost = 1;
                foreach (XElement ancestor in action.Ancestors().Where(element => element.Attribute("Class") != null && typeof(CQFAction_Loop).IsAssignableFrom(model.Resolve(element.Attribute("Class")!.Value))))
                {
                    cost *= (int?)ancestor.Element("loopCount") ?? 1;
                    if (cost > 2048) throw new InvalidDataException("CQF_AI_InvalidValue: nested action execution budget");
                }
                budget += cost;
                if (budget > 2048 || action.Elements("actions").Any(element => element.Attribute("null")?.Value == "true" || element.Elements().Any(child => child.Attribute("null")?.Value == "true")))
                    throw new InvalidDataException("CQF_AI_InvalidValue: action execution budget/empty action");
            }
            Quest? quest = CQFAIRuntimeRequest.Quest(request);
            XElement result = new XElement("actionsInvoked", new XAttribute("undoSupported", false), new XAttribute("effectsVerified", false));
            journal.MarkIrreversible();
            for (int index = 0; index < actions.Count; index++)
            {
                try { actions[index].Work(targets, quest!); result.Add(new XElement("action", new XAttribute("index", index), new XAttribute("type", actions[index].GetType().FullName!), new XAttribute("invoked", true))); }
                catch (Exception error)
                {
                    Log.Error("CQF AI runtime action: " + error);
                    result.SetAttributeValue("success", false); result.SetAttributeValue("partial", index > 0);
                    string detail = error.ToString(); result.Add(new XElement("error", new XAttribute("index", index), detail.Length > 4000 ? detail.Substring(0, 4000) : detail)); break;
                }
            }
            return result;
        }
        private readonly CQFAIModel model;
        private readonly CQFAIRuntimeJournal journal;
        private readonly bool text;
        private string command;
    }
}
