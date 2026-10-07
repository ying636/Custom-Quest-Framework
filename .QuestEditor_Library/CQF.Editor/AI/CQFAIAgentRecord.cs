using System.Diagnostics;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAIAgentRecord
    {
        public CQFAIAgentRecord(string id, string name, string assignment)
        {
            Id = id; Name = name; Assignment = assignment;
        }
        public string Id { get; }
        public string Name { get; }
        public string Assignment { get; }
        public string Status { get; private set; } = "pending";
        public string Result { get; private set; } = string.Empty;
        public string Progress { get; set; } = string.Empty;
        public bool Expanded { get; set; }
        public double Seconds => elapsed + timer.Elapsed.TotalSeconds;
        public bool Finished => Status is "completed" or "failed" or "paused";
        public void Start() { Status = "running"; timer.Start(); }
        public void Complete(string result, string status = "completed")
        {
            if (Finished) return;
            if (status is not ("completed" or "failed" or "paused")) throw new InvalidDataException("CQF_AI_InvalidAgent");
            Status = status; Result = result; timer.Stop();
        }
        public XElement Save() => new XElement("agent", new XAttribute("id", Id), new XAttribute("name", Name), new XAttribute("status", Status), new XAttribute("seconds", Seconds), new XAttribute("expanded", Expanded),
            new XElement("assignment", Assignment), new XElement("result", Result), new XElement("progress", Progress));
        public static CQFAIAgentRecord Restore(XElement value)
        {
            string id = (string?)value.Attribute("id") ?? "", name = (string?)value.Attribute("name") ?? "", assignment = value.Element("assignment")?.Value ?? "";
            string status = (string?)value.Attribute("status") ?? "";
            double seconds = (double?)value.Attribute("seconds") ?? -1;
            string result = value.Element("result")?.Value ?? "", progress = value.Element("progress")?.Value ?? "";
            if (id.Length == 0 || id.Length > 80 || name.Length == 0 || name.Length > 100 || assignment.Length > 16000 || result.Length > 32768 || progress.Length > 2000
                || status is not ("pending" or "running" or "completed" or "failed" or "paused") || seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new InvalidDataException("CQF_AI_InvalidHistory");
            return new CQFAIAgentRecord(id, name, assignment) { Status = status is "pending" or "running" ? "paused" : status, Result = result, Progress = progress, elapsed = seconds,
                Expanded = (bool?)value.Attribute("expanded") ?? false };
        }
        private double elapsed;
        private readonly Stopwatch timer = new Stopwatch();
    }
}
