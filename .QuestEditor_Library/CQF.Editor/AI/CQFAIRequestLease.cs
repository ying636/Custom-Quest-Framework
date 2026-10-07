using System.Threading;

namespace QuestEditor_Library
{
    public sealed class CQFAIRequestLease : IDisposable
    {
        public CQFAIRequestLease(CQFAIRequestBudget budget, long allocation)
        {
            this.budget = budget; this.allocation = allocation;
        }
        public CQFAITokenUsage? Usage { get; set; }
        public bool Rejected { get; set; }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref completed, 1) == 0) budget.Complete(allocation, Usage, Rejected);
        }
        private readonly CQFAIRequestBudget budget;
        private readonly long allocation;
        private int completed;
    }
}
