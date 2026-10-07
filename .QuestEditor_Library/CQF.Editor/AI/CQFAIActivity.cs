using System.Diagnostics;
using System.Net;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIActivity
    {
        public CQFAIActivity(int messageIndex) { MessageIndex = messageIndex; }
        public int MessageIndex { get; }
        public bool Expanded { get; set; }
        public bool Finished { get; private set; }
        public string StageKey => state;
        public double ElapsedSeconds => elapsed + timer.Elapsed.TotalSeconds;
        public string Notice { get; set; } = string.Empty;
        public CQFAITaskState? TaskState { get; set; }
        public string ExecutionProgress { get; set; } = string.Empty;
        public IReadOnlyList<CQFAIOperation> Operations => operations;
        public string Reasoning => previousReasoning + reasoning;
        public string Preview
        {
            get
            {
                string text = content.TrimStart();
                if (text.StartsWith("<", StringComparison.Ordinal) || text.StartsWith("```", StringComparison.Ordinal))
                {
                    int start = text.IndexOf("<reply>", StringComparison.Ordinal);
                    if (start < 0) return string.Empty;
                    text = text.Substring(start + 7);
                    int end = text.IndexOf('<');
                    if (end >= 0) text = text.Substring(0, end);
                    text = WebUtility.HtmlDecode(text);
                }
                return text.Length <= 16384 ? text : text.Substring(0, 16384) + "\n" + "CQF_AI_PreviewExcerpt".Translate();
            }
        }
        public string Header => "CQF_AI_ActivityHeader".Translate(state.Translate(), ElapsedSeconds.ToString("0"), operations.Count);
        public string CurrentStep => ExecutionProgress.Length > 0 ? ExecutionProgress : pending.Length > 0 ? "CQF_AI_PreparingTool".Translate(string.Join(", ", pending.Select(CQFAIOperation.ToolLabel))) : operations.LastOrDefault()?.Label
            ?? (Reasoning.Length > 0 ? "CQF_AI_ReasoningAvailable" : "CQF_AI_ReasoningUnavailable").Translate().ToString();
        public void Update(CQFAIStreamUpdate? progress)
        {
            if (progress == null || Finished) return;
            reasoning = progress.Reasoning.Length <= 32768 ? progress.Reasoning : progress.Reasoning.Substring(0, 32768) + "…";
            content = progress.Text;
            pending = progress.Tools.ToArray();
            state = pending.Length > 0 ? "CQF_AI_Preparing" : content.Length > 0 ? "CQF_AI_Responding" : reasoning.Length > 0 ? "CQF_AI_Thinking" : "CQF_AI_Waiting";
        }
        public void NextRound()
        {
            if (reasoning.Length > 0 && previousReasoning.Length < 32768)
            {
                previousReasoning += (previousReasoning.Length > 0 ? "\n\n" : string.Empty) + reasoning;
                if (previousReasoning.Length > 32768) previousReasoning = previousReasoning.Substring(0, 32768) + "…";
            }
            reasoning = string.Empty; content = string.Empty; pending = Array.Empty<string>(); ExecutionProgress = string.Empty; state = "CQF_AI_Waiting";
        }
        public void Add(CQFAIOperation operation) { operations.Add(operation); pending = Array.Empty<string>(); state = "CQF_AI_Executing"; }
        public void WaitForAgents() { state = "CQF_AI_WaitingAgents"; }
        public void Complete(string status = "CQF_AI_ActivityDone", bool keepPreview = false)
        {
            if (Finished) return;
            timer.Stop(); Finished = true; state = status; pending = Array.Empty<string>();
            if (!keepPreview) content = string.Empty;
        }
        public XElement Save() => new XElement("activity", new XAttribute("index", MessageIndex), new XAttribute("state", Finished ? state : "CQF_AI_ActivityStopped"),
            new XAttribute("elapsed", ElapsedSeconds), new XElement("reasoning", Reasoning), new XElement("preview", Preview),
            new XElement("notice", Notice), TaskState?.Save(), operations.Select(operation => operation.Save()));
        public static CQFAIActivity Restore(XElement value)
        {
            int index = (int)value.Attribute("index")!;
            double seconds = (double)value.Attribute("elapsed")!;
            string state = (string?)value.Attribute("state") ?? "";
            if (index < 0 || seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)
                || state is not ("CQF_AI_ActivityDone" or "CQF_AI_ActivityStopped" or "CQF_AI_ActivityFailed" or "CQF_AI_ActivityBlocked")) throw new InvalidDataException("CQF_AI_InvalidHistory");
            CQFAIActivity activity = new CQFAIActivity(index) { elapsed = seconds, state = state, reasoning = value.Element("reasoning")?.Value ?? "", content = value.Element("preview")?.Value ?? "", Notice = value.Element("notice")?.Value ?? "" };
            if (value.Element("task") is XElement task) activity.TaskState = CQFAITaskState.Restore(task);
            if (activity.reasoning.Length > 65538 || activity.content.Length > 17000) throw new InvalidDataException("CQF_AI_InvalidHistory");
            activity.operations.AddRange(value.Elements("operation").Select(CQFAIOperation.Restore));
            activity.Finished = true; activity.timer.Stop(); activity.timer.Reset();
            return activity;
        }
        private readonly List<CQFAIOperation> operations = new List<CQFAIOperation>();
        private readonly Stopwatch timer = Stopwatch.StartNew();
        private string state = "CQF_AI_Waiting";
        private string previousReasoning = string.Empty;
        private string reasoning = string.Empty;
        private string content = string.Empty;
        private string[] pending = Array.Empty<string>();
        private double elapsed;
    }
}
