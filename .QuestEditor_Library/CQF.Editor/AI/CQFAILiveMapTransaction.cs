using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAILiveMapTransaction : CQFAITransaction
    {
        public CQFAILiveMapTransaction(CQFAIModel model, CQFAIEditorContext context, ICQFAILiveMap backend) : base(model, context)
        {
            this.model = model; this.context = context; this.backend = backend;
        }
        public override bool CanUndo => edits.Count > 0;
        public override bool IsTargetValid => backend.IsValid && context.IsValid?.Invoke() != false && ReferenceEquals(context.Identity, backend.Identity);
        public override bool IsCurrent => CheckCurrent();
        public bool DeferExecution { get; set; }
        public bool IsExecuting { get; private set; }
        public int CompletedOperations => applied.Count;
        public int TotalOperations => pending?.Count ?? last.Count;
        public string ExecutionPhase => failure != null ? "CQF_AI_MapRollingBack" : "CQF_AI_Executing";
        public XElement? CurrentOperation { get; private set; }
        public Exception? ExecutionError => failure;
        public ICQFAILiveMap Backend => backend;
        public XElement Receipt => new XElement("liveMapApplied", new XAttribute("operations", last.Count), new XAttribute("undoAvailable", CanUndo && !undoConflict && IsTargetValid),
            last.Take(40).Select(edit => edit.Receipt), new XElement("validation", new XAttribute("passed", failure == null), new XAttribute("scope", "applied_operations"), new XAttribute("checkedOperations", last.Count)));
        public Action<CQFAILiveMapEdit>? StepApplied { get; set; }
        public override void Build(XElement changes, string command, bool generateText)
        {
            if (!IsTargetValid || IsExecuting) throw new InvalidOperationException("CQF_AI_StaleTarget");
            if (!DeferExecution && CanUndo) _ = IsCurrent;
            if (changes.Name != "changes" || changes.HasAttributes || changes.Elements().Count() is < 1 or > 500
                || changes.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))) throw new InvalidDataException("CQF_AI_InvalidChanges");
            pending = backend.Prepare(changes, model, command, generateText);
            if (pending.Count + edits.Count > 20000) { pending = null; throw new InvalidDataException("CQF_AI_ToolLimit"); }
        }
        public override void Apply()
        {
            BeginExecution();
            if (DeferExecution) return;
            while (IsExecuting) AdvanceExecution(int.MaxValue, double.MaxValue);
            ThrowExecutionError();
        }
        public void AdvanceExecution(int maximumOperations, double budgetMilliseconds)
        {
            if (maximumOperations < 1 || budgetMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(maximumOperations));
            Stopwatch budget = Stopwatch.StartNew();
            int performed = 0;
            while (IsExecuting && performed++ < maximumOperations && budget.Elapsed.TotalMilliseconds < budgetMilliseconds)
            {
                if (failure != null)
                {
                    if (cursor >= 0)
                    {
                        CQFAILiveMapEdit edit = applied[cursor--];
                        try
                        {
                            foreach (CQFAILiveMapEdit related in Related(edit)) if (!related.IsCurrent) undoConflict = true;
                            if (edit.After != null && !edit.IsCurrent) { undoConflict = true; throw new InvalidOperationException("CQF_AI_StaleTarget"); }
                            edit.Undo();
                            ReplaceCheckpoint(edit.Key, previous[edit]);
                            foreach (CQFAILiveMapEdit related in Related(edit)) related.Refresh();
                        }
                        catch (Exception rollback) { rollbackFailures.Add(rollback); }
                        continue;
                    }
                    if (rollbackFailures.Count > 0)
                    {
                        failure = new AggregateException(new[] { failure }.Concat(rollbackFailures));
                        CommitApplied();
                    }
                    else { applied.Clear(); last.Clear(); }
                    IsExecuting = false; pending = null;
                    continue;
                }
                if (!IsTargetValid) { Fail(new InvalidOperationException("CQF_AI_StaleTarget")); continue; }
                if (cursor >= pending!.Count) { CommitApplied(); IsExecuting = false; pending = null; continue; }
                CQFAILiveMapEdit next = pending[cursor++];
                CQFAILiveMapEdit[] affected = Related(next).ToArray();
                foreach (CQFAILiveMapEdit related in affected) if (!related.IsCurrent) undoConflict = true;
                previous[next] = checkpoints.TryGetValue(next.Key, out CQFAILiveMapEdit prior) ? prior : null;
                applied.Add(next);
                try
                {
                    next.Apply();
                    foreach (CQFAILiveMapEdit related in affected) related.Refresh();
                    ReplaceCheckpoint(next.Key, next);
                    CurrentOperation = next.Receipt; StepApplied?.Invoke(next);
                }
                catch (Exception error)
                {
                    try
                    {
                        next.Refresh();
                        foreach (CQFAILiveMapEdit related in affected) related.Refresh();
                        ReplaceCheckpoint(next.Key, next);
                    }
                    catch (Exception snapshotError) { error = new AggregateException(error, snapshotError); }
                    Fail(error);
                }
            }
        }
        public void CancelExecution()
        {
            if (!IsExecuting) return;
            if (failure != null)
            {
                while (IsExecuting) AdvanceExecution(int.MaxValue, double.MaxValue);
                ThrowExecutionError(); return;
            }
            CommitApplied(); IsExecuting = false; pending = null;
        }
        public void ThrowExecutionError()
        {
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
        public override void Undo()
        {
            if (!CanUndo || !IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            for (int index = edits.Count - 1; index >= 0; index--) { edits[index].Undo(); edits.RemoveAt(index); }
            checkpoints.Clear(); cellKeys.Clear(); last.Clear(); pending = null;
        }
        private bool CheckCurrent()
        {
            if (IsExecuting || undoConflict || !IsTargetValid) return false;
            foreach (CQFAILiveMapEdit edit in checkpoints.Values)
                if (!edit.IsCurrent) { undoConflict = true; return false; }
            return true;
        }
        private void BeginExecution()
        {
            if (pending == null || !IsTargetValid || IsExecuting) throw new InvalidOperationException("CQF_AI_StaleTarget");
            applied.Clear(); previous.Clear(); rollbackFailures.Clear(); failure = null; CurrentOperation = null;
            cursor = 0; IsExecuting = true;
        }
        private IEnumerable<CQFAILiveMapEdit> Related(CQFAILiveMapEdit edit)
        {
            HashSet<string> keys = new HashSet<string> { edit.Key };
            foreach (IntVec3 cell in edit.AffectedCells) if (cellKeys.TryGetValue(cell, out HashSet<string> entries)) keys.UnionWith(entries);
            foreach (string key in keys) if (checkpoints.TryGetValue(key, out CQFAILiveMapEdit value)) yield return value;
        }
        private void ReplaceCheckpoint(string key, CQFAILiveMapEdit? edit)
        {
            if (checkpoints.TryGetValue(key, out CQFAILiveMapEdit old))
                foreach (IntVec3 cell in old.AffectedCells)
                    if (cellKeys.TryGetValue(cell, out HashSet<string> keys)) { keys.Remove(key); if (keys.Count == 0) cellKeys.Remove(cell); }
            if (edit == null) { checkpoints.Remove(key); return; }
            checkpoints[key] = edit;
            foreach (IntVec3 cell in edit.AffectedCells)
            {
                if (!cellKeys.TryGetValue(cell, out HashSet<string> keys)) { keys = new HashSet<string>(); cellKeys.Add(cell, keys); }
                keys.Add(key);
            }
        }
        private void Fail(Exception error) { failure = error; cursor = applied.Count - 1; }
        private void CommitApplied() { last = applied.ToList(); edits.AddRange(applied); }
        private readonly CQFAIModel model;
        private readonly CQFAIEditorContext context;
        private readonly ICQFAILiveMap backend;
        private readonly List<CQFAILiveMapEdit> edits = new List<CQFAILiveMapEdit>();
        private readonly Dictionary<string, CQFAILiveMapEdit> checkpoints = new Dictionary<string, CQFAILiveMapEdit>();
        private readonly Dictionary<IntVec3, HashSet<string>> cellKeys = new Dictionary<IntVec3, HashSet<string>>();
        private readonly Dictionary<CQFAILiveMapEdit, CQFAILiveMapEdit?> previous = new Dictionary<CQFAILiveMapEdit, CQFAILiveMapEdit?>();
        private readonly List<CQFAILiveMapEdit> applied = new List<CQFAILiveMapEdit>();
        private readonly List<Exception> rollbackFailures = new List<Exception>();
        private List<CQFAILiveMapEdit> last = new List<CQFAILiveMapEdit>();
        private IReadOnlyList<CQFAILiveMapEdit>? pending;
        private Exception? failure;
        private int cursor;
        private bool undoConflict;
    }
}
