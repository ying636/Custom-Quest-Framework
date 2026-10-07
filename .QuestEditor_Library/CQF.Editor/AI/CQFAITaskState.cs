using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAITaskState
    {
        public string Goal { get; private set; } = string.Empty;
        public string Status { get; private set; } = "active";
        public string Evidence { get; private set; } = string.Empty;
        public IReadOnlyList<string> Criteria => criteria;
        public IReadOnlyList<CQFAIPlanStep> Steps => steps;
        public List<CQFAIAgentRecord> Agents { get; } = new List<CQFAIAgentRecord>();
        public bool RequiresVerification { get; private set; }
        public void Register(CQFAIToolRegistry registry)
        {
            if (registry.Tools.Any(t => t.Name == "cqf_get_task")) return;
            registry.Register(new CQFAITool("cqf_get_task", "Read the persisted goal, acceptance criteria, plan and recent worker summaries. State is task bookkeeping, not an editing target.", _ => Summary()));
            registry.Register(new CQFAITool("cqf_set_goal", "Create or revise the goal and acceptance criteria from the player's request. Preserve completed work when requirements change. No specific map/layout preferences are implied.", SetGoal,
                ("goal", "Concise goal in the player's language; at most 4000 characters.", true), ("criteria_xml", "<criteria><item>Concrete acceptance condition</item></criteria>, 1 to 32 items.", true)));
            registry.Register(new CQFAITool("cqf_update_plan", "Replace the plan atomically. Steps may depend on other steps. Only mark completed with evidence from actual work; identify limited validation honestly.", UpdatePlan,
                ("plan_xml", "<plan><step id='unique_id' status='pending|running|completed|blocked' depends='optional,step,ids'><text>Step</text><evidence>Required for completed steps</evidence></step></plan>, at most 64 steps; no cycles.", true)));
            registry.Register(new CQFAITool("cqf_finish_goal", "Finish or report an actual blockage. Completion requires every planned step completed, no unfinished workers, and a current read/validation after the last write. Summarize checks actually made against the criteria.", FinishGoal,
                ("outcome", "completed or blocked", true), ("evidence", "Actual checks/results or the specific blockage; at most 4000 characters.", true)));
        }
        public void RecordTool(string name, bool success)
        {
            if (!success && name is not ("cqf_operate_runtime" or "cqf_manage_definition")) return;
            if (name is "cqf_apply_changes" or "cqf_edit_map_thing" or "cqf_edit_map_configuration" or "cqf_add_dialogue_branch" or "cqf_add_interaction" or "cqf_configure_entrance" or "cqf_configure_exit" or "cqf_operate_runtime" or "cqf_manage_definition")
            {
                RequiresVerification = true;
                if (Goal.Length > 0 && Status == "completed") Resume();
            }
            else if (name is "cqf_read_target" or "cqf_read_map_region" or "cqf_read_map_thing" or "cqf_read_map_configuration" or "cqf_validate_target" or "cqf_read_runtime" or "cqf_inspect_map" or "cqf_check_conditions" or "cqf_read_resource") RequiresVerification = false;
        }
        public void Pause()
        {
            if (Status == "active") Status = "paused";
            foreach (CQFAIAgentRecord agent in Agents.Where(a => !a.Finished)) agent.Complete("", "paused");
        }
        public void Resume() { Status = "active"; Evidence = string.Empty; }
        public XElement Summary() => new XElement("task", new XAttribute("status", Status), new XAttribute("requiresVerification", RequiresVerification), new XElement("goal", Goal),
            new XElement("criteria", criteria.Select(c => new XElement("item", c))), new XElement("plan", steps.Select(s => s.Save())), new XElement("evidence", Evidence),
            new XElement("agents", new XAttribute("total", Agents.Count), Agents.Skip(Math.Max(0, Agents.Count - 8)).Select(a => new XElement("agent", new XAttribute("id", a.Id),
                new XAttribute("name", a.Name), new XAttribute("status", a.Status), new XElement("resultPreview", a.Result.Length > 500 ? a.Result.Substring(0, 500) : a.Result)))));
        public XElement Save() => new XElement("task", new XAttribute("status", Status), new XAttribute("requiresVerification", RequiresVerification), new XElement("goal", Goal),
            new XElement("criteria", criteria.Select(c => new XElement("item", c))), new XElement("plan", steps.Select(s => s.Save())), new XElement("evidence", Evidence),
            new XElement("agents", Agents.Select(a => a.Save())));
        public static CQFAITaskState Restore(XElement value)
        {
            CQFAITaskState state = new CQFAITaskState();
            string goal = value.Element("goal")?.Value ?? "", status = (string?)value.Attribute("status") ?? "";
            if (goal.Length > 4000 || status is not ("active" or "paused" or "completed" or "blocked")) throw new InvalidDataException("CQF_AI_InvalidHistory");
            if (goal.Length > 0) state.SetGoal(new XElement("arguments", new XElement("goal", goal), new XElement("criteria_xml", (value.Element("criteria") ?? new XElement("criteria")).ToString(SaveOptions.DisableFormatting))));
            XElement plan = value.Element("plan") ?? new XElement("plan");
            if (plan.HasElements) state.UpdatePlan(new XElement("arguments", new XElement("plan_xml", plan.ToString(SaveOptions.DisableFormatting))));
            state.Status = status == "active" ? "paused" : status;
            state.Evidence = value.Element("evidence")?.Value ?? "";
            state.RequiresVerification = (bool?)value.Attribute("requiresVerification") ?? false;
            state.Agents.AddRange((value.Element("agents")?.Elements("agent") ?? Enumerable.Empty<XElement>()).Select(CQFAIAgentRecord.Restore));
            if (state.Evidence.Length > 4000 || state.Agents.Count > 256 || state.Agents.Select(a => a.Id).Distinct().Count() != state.Agents.Count) throw new InvalidDataException("CQF_AI_InvalidHistory");
            return state;
        }
        private XElement SetGoal(XElement arguments)
        {
            string goal = arguments.Element("goal")!.Value;
            XElement root = CQFAIChanges.Parse(arguments.Element("criteria_xml")!.Value);
            string[] values = root.Elements().Select(e => e.Value).ToArray();
            if (string.IsNullOrWhiteSpace(goal) || goal.Length > 4000 || root.Name != "criteria" || root.HasAttributes || values.Length < 1 || values.Length > 32
                || root.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value))
                || values.Sum(c => c.Length) > 16000 || root.Elements().Any(e => e.Name != "item" || e.HasAttributes || e.HasElements || string.IsNullOrWhiteSpace(e.Value) || e.Value.Length > 2000)) throw new InvalidDataException("CQF_AI_InvalidPlan");
            Goal = goal; criteria.Clear(); criteria.AddRange(values); Resume();
            return Summary();
        }
        private XElement UpdatePlan(XElement arguments)
        {
            XElement root = CQFAIChanges.Parse(arguments.Element("plan_xml")!.Value);
            if (root.Name != "plan" || root.HasAttributes || !root.HasElements || root.Elements().Count() > 64 || root.Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value))) throw new InvalidDataException("CQF_AI_InvalidPlan");
            CQFAIPlanStep[] proposed = root.Elements().Select(e => new CQFAIPlanStep(e)).ToArray();
            if (proposed.Sum(s => s.Text.Length + s.Evidence.Length) > 32000) throw new InvalidDataException("CQF_AI_InvalidPlan");
            Dictionary<string, CQFAIPlanStep> indexed = new Dictionary<string, CQFAIPlanStep>(StringComparer.Ordinal);
            foreach (CQFAIPlanStep step in proposed) if (indexed.ContainsKey(step.Id)) throw new InvalidDataException("CQF_AI_InvalidPlan"); else indexed.Add(step.Id, step);
            HashSet<string> complete = new HashSet<string>(StringComparer.Ordinal);
            while (complete.Count < proposed.Length)
            {
                CQFAIPlanStep[] available = proposed.Where(s => !complete.Contains(s.Id) && s.Dependencies.All(complete.Contains)).ToArray();
                if (available.Length == 0) throw new InvalidDataException("CQF_AI_InvalidPlan");
                foreach (CQFAIPlanStep step in available)
                {
                    if (step.Status is "running" or "completed" && step.Dependencies.Any(id => indexed[id].Status != "completed")) throw new InvalidDataException("CQF_AI_InvalidPlan");
                    complete.Add(step.Id);
                }
            }
            steps.Clear(); steps.AddRange(proposed);
            if (Status == "completed") Resume();
            return Summary();
        }
        private XElement FinishGoal(XElement arguments)
        {
            string outcome = arguments.Element("outcome")!.Value, evidence = arguments.Element("evidence")!.Value;
            if (Goal.Length == 0 || outcome is not ("completed" or "blocked") || string.IsNullOrWhiteSpace(evidence) || evidence.Length > 4000) throw new InvalidDataException("CQF_AI_InvalidPlan");
            if (outcome == "completed" && (RequiresVerification || steps.Any(s => s.Status != "completed") || Agents.Any(a => !a.Finished))) throw new InvalidDataException("CQF_AI_TaskUnfinished");
            Status = outcome; Evidence = evidence;
            return Summary();
        }
        private readonly List<string> criteria = new List<string>();
        private readonly List<CQFAIPlanStep> steps = new List<CQFAIPlanStep>();
    }
}
