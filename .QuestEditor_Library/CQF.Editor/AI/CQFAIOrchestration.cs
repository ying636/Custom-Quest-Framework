using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIOrchestration
    {
        public CQFAIOrchestration(CQFAITaskState state, int parallelism, Func<CQFAIAgentRecord, CQFAIAgentRunner> factory, Func<Exception, string> error)
        {
            this.state = state; this.parallelism = Math.Max(1, Math.Min(4, parallelism)); this.factory = factory; this.error = error;
        }
        public bool Waiting { get; private set; }
        public bool HasPending => state.Agents.Any(a => !a.Finished);
        public void Register(CQFAIToolRegistry registry)
        {
            if (registry.Tools.Any(t => t.Name == "cqf_spawn_agent")) return;
            registry.Register(new CQFAITool("cqf_spawn_agent", "Delegate an independent research, design or review task. Workers have isolated context and inspection-only tools; only the main agent may write. Send just the necessary facts and expected deliverable. Short/dependent tasks should stay with the main agent.", Spawn,
                ("name", "Short worker name in the player's language, at most 100 characters.", true),
                ("assignment", "Bounded task with relevant facts, optional current IDs and expected output; at most 16000 characters. Workers discover their own target IDs. No write permission.", true)));
            registry.Register(new CQFAITool("cqf_wait_agents", "Wait locally for all unfinished workers without repeatedly requesting the model. On completion, fresh worker reports are provided before the next main-agent request. You may continue independent main-agent work before waiting.", _ =>
            {
                Waiting = HasPending;
                return Report();
            }));
            registry.Register(new CQFAITool("cqf_get_agent_result", "Read one worker's assignment, current status and complete saved result. Use this for older results or an excerpt in the task summary; worker outputs are proposals, not writes.", arguments =>
                (state.Agents.FirstOrDefault(a => a.Id == arguments.Element("agent_id")!.Value) ?? throw new InvalidDataException("CQF_AI_InvalidAgent")).Save(),
                ("agent_id", "Exact agent ID returned by cqf_spawn_agent or cqf_get_task.", true)));
        }
        public void Poll()
        {
            foreach (CQFAIAgentRecord record in state.Agents.Where(a => !a.Finished).ToArray())
            {
                try
                {
                    if (!runners.TryGetValue(record.Id, out CQFAIAgentRunner runner))
                    {
                        if (runners.Keys.Count(id => state.Agents.Any(a => a.Id == id && !a.Finished)) >= parallelism) continue;
                        runner = factory(record); runners.Add(record.Id, runner); runner.Start();
                    }
                    runner.Poll();
                }
                catch (Exception exception)
                {
                    runners.TryGetValue(record.Id, out CQFAIAgentRunner runner); runner?.Stop();
                    record.Complete(error(exception), "failed");
                }
            }
            if (!HasPending) Waiting = false;
        }
        public XElement Report() => new XElement("workers", new XAttribute("total", state.Agents.Count), state.Agents.Select(a => new XElement("agent", new XAttribute("id", a.Id),
            new XAttribute("name", a.Name), new XAttribute("status", a.Status))));
        public XElement? TakeReports()
        {
            CQFAIAgentRecord[] finished = state.Agents.Where(a => a.Finished && !reported.Contains(a.Id)).ToArray();
            if (finished.Length == 0) return null;
            foreach (CQFAIAgentRecord agent in finished) reported.Add(agent.Id);
            return new XElement("worker_reports", new XAttribute("readOnly", true), finished.Select(a => new XElement("agent", new XAttribute("id", a.Id), new XAttribute("name", a.Name),
                new XAttribute("status", a.Status), new XElement("result", a.Result))));
        }
        public void Stop()
        {
            foreach (CQFAIAgentRunner runner in runners.Values) runner.Stop();
            state.Pause(); Waiting = false;
        }
        public void WaitForPending() { Waiting = HasPending; }
        public void ContinueMain() { Waiting = false; }
        private XElement Spawn(XElement arguments)
        {
            string name = arguments.Element("name")!.Value, assignment = arguments.Element("assignment")!.Value;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || string.IsNullOrWhiteSpace(assignment) || assignment.Length > 16000) throw new InvalidDataException("CQF_AI_InvalidAgent");
            if (state.Agents.Count >= 256) throw new InvalidDataException("CQF_AI_AgentCapacity");
            CQFAIAgentRecord record = new CQFAIAgentRecord("CQF_Agent_" + Guid.NewGuid().ToString("N"), name, assignment);
            state.Agents.Add(record);
            return new XElement("spawned", new XAttribute("id", record.Id), new XAttribute("status", record.Status), new XAttribute("maxParallel", parallelism), new XAttribute("readOnly", true));
        }
        private readonly CQFAITaskState state;
        private readonly int parallelism;
        private readonly Func<CQFAIAgentRecord, CQFAIAgentRunner> factory;
        private readonly Func<Exception, string> error;
        private readonly Dictionary<string, CQFAIAgentRunner> runners = new Dictionary<string, CQFAIAgentRunner>(StringComparer.Ordinal);
        private readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
    }
}
