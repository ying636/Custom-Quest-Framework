namespace QuestEditor_Library
{
    public sealed class CQFAITaskTransaction : CQFAITransaction
    {
        public CQFAITaskTransaction(CQFAIModel model) : base(model, new CQFAIEditorContext("CQF_AI_Task", () => new CQFAILiveMapInfo(), _ => throw new InvalidOperationException("CQF_AI_InvalidChanges"))) { }
        public override bool UndoSupported => transactions.All(transaction => transaction.UndoSupported);
        public override bool CanUndo => UndoSupported && transactions.Any(transaction => transaction.CanUndo);
        public override bool IsTargetValid => transactions.Where(transaction => transaction.CanUndo).All(transaction => transaction.IsTargetValid);
        public override bool IsCurrent => CanUndo && transactions.Where(transaction => transaction.CanUndo).All(transaction => transaction.IsCurrent);
        public void Add(CQFAITransaction transaction) { transactions.Add(transaction); }
        public override void Undo()
        {
            if (!IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            foreach (CQFAITransaction transaction in transactions.AsEnumerable().Reverse().Where(transaction => transaction.CanUndo)) transaction.Undo();
        }
        private readonly List<CQFAITransaction> transactions = new List<CQFAITransaction>();
    }
}
