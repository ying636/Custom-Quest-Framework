using System.Threading;
using System.Threading.Tasks;

namespace QuestEditor_Library
{
    public sealed class CQFAIAgentRunner
    {
        public CQFAIAgentRunner(CQFAIAgentRecord record, CQFAIHarness harness, Func<CQFAIHarness, CancellationToken, Task<string>> request, CancellationToken cancellation,
            Func<CQFAIStreamUpdate?>? progress = null, Action? accountUsage = null)
        {
            this.record = record; this.harness = harness; this.request = request; this.progress = progress; this.accountUsage = accountUsage;
            if (harness.Registry.EditingAllowed || !harness.InspectionOnly) throw new InvalidOperationException("CQF_AI_InvalidAgent");
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        }
        public void Start() { record.Start(); pending = request(harness, lifetime.Token); }
        public void Poll()
        {
            if (record.Finished) return;
            lifetime.Token.ThrowIfCancellationRequested();
            CQFAIStreamUpdate? update = progress?.Invoke();
            if (update != null)
            {
                string text = update.Tools.Count > 0 ? string.Join(", ", update.Tools) : update.Reasoning.Length > 0 ? update.Reasoning : update.Text;
                record.Progress = text.Length > 2000 ? text.Substring(text.Length - 2000) : text;
            }
            if (pending == null || !pending.IsCompleted) return;
            Task<string> completed = pending; pending = null;
            string response;
            try { response = completed.GetAwaiter().GetResult(); }
            finally { accountUsage?.Invoke(); }
            if (harness.Process(response)) pending = request(harness, lifetime.Token);
            else
            {
                string reply = new CQFAIResponse(response).Reply;
                if (reply.Length > 32768) throw new InvalidDataException("CQF_AI_AgentResultTooLarge");
                record.Complete(reply);
                Stop();
            }
        }
        public void Stop()
        {
            if (stopped) return;
            stopped = true; lifetime.Cancel();
            if (pending?.IsCompleted == true) accountUsage?.Invoke();
            pending?.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            pending = null;
            lifetime.Dispose();
        }
        private readonly CQFAIAgentRecord record;
        private readonly CQFAIHarness harness;
        private readonly Func<CQFAIHarness, CancellationToken, Task<string>> request;
        private readonly CancellationTokenSource lifetime;
        private readonly Func<CQFAIStreamUpdate?>? progress;
        private readonly Action? accountUsage;
        private Task<string>? pending;
        private bool stopped;
    }
}
