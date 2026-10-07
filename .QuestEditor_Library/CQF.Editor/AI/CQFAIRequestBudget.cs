namespace QuestEditor_Library
{
    public sealed class CQFAIRequestBudget
    {
        public CQFAIRequestBudget(long tokenLimit, int requestLimit)
        {
            if (tokenLimit < 1 || requestLimit < 1) throw new ArgumentOutOfRangeException(nameof(tokenLimit));
            TokenLimit = tokenLimit; RequestLimit = requestLimit;
        }
        public long TokenLimit { get; }
        public int RequestLimit { get; }
        public long Used { get { lock (gate) return used; } }
        public long Reserved { get { lock (gate) return reserved; } }
        public int Requests { get { lock (gate) return requests; } }
        public bool HasEstimates { get { lock (gate) return estimates; } }
        public bool IsBlocked { get { lock (gate) return blocked; } }
        public CQFAIRequestLease Reserve(long estimatedInput)
        {
            long allocation = checked(Math.Max(1, estimatedInput) + 4096);
            lock (gate)
            {
                if (blocked || requests >= RequestLimit || allocation > TokenLimit - used - reserved)
                {
                    blocked = true;
                    throw new InvalidOperationException("CQF_AI_BudgetPaused");
                }
                requests++; reserved += allocation;
                return new CQFAIRequestLease(this, allocation);
            }
        }
        public void Complete(long allocation, CQFAITokenUsage? usage, bool rejected)
        {
            lock (gate)
            {
                reserved -= allocation;
                used = checked(used + (rejected ? 0 : usage?.Total ?? allocation));
                estimates |= !rejected && usage == null;
            }
        }
        private readonly object gate = new object();
        private long used;
        private long reserved;
        private int requests;
        private bool estimates;
        private bool blocked;
    }
}
