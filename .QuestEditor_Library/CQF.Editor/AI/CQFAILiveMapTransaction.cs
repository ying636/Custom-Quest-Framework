using System.Runtime.ExceptionServices;
using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAILiveMapTransaction : CQFAITransaction
    {
        public CQFAILiveMapTransaction(CQFAIModel model, CQFAIEditorContext context, ICQFAILiveMap backend) : base(model, context)
        {
            this.model = model;
            this.context = context;
            this.backend = backend;
        }
        public override bool CanUndo => edits.Count > 0;
        public override bool IsCurrent => backend.IsValid && context.IsValid?.Invoke() != false && ReferenceEquals(context.Identity, backend.Identity)
            && edits.GroupBy(edit => edit.Key).All(group => group.Last().IsCurrent);
        public XElement Receipt => new XElement("liveMapApplied", new XAttribute("operations", last.Count), new XAttribute("undoAvailable", CanUndo),
            last.Take(40).Select(edit => edit.Receipt), backend.Validate());
        public override void Build(XElement changes, string command, bool generateText)
        {
            if (!IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            if (changes.Name != "changes" || changes.HasAttributes || changes.Elements().Count() is < 1 or > 500
                || changes.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value))) throw new InvalidDataException("CQF_AI_InvalidChanges");
            pending = backend.Prepare(changes, model, command, generateText);
            if (pending.Count + edits.Count > 20000) { pending = null; throw new InvalidDataException("CQF_AI_ToolLimit"); }
        }
        public override void Apply()
        {
            if (pending == null || !IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            List<CQFAILiveMapEdit> applied = new List<CQFAILiveMapEdit>();
            try
            {
                foreach (CQFAILiveMapEdit edit in pending) { applied.Add(edit); edit.Apply(); }
            }
            catch (Exception error)
            {
                List<Exception> failures = new List<Exception>();
                foreach (CQFAILiveMapEdit edit in applied.AsEnumerable().Reverse())
                    try { edit.Undo(); } catch (Exception rollback) { failures.Add(rollback); }
                pending = null;
                if (failures.Count > 0)
                {
                    edits.AddRange(applied);
                    last = applied;
                    foreach (var group in edits.GroupBy(edit => edit.Key))
                        try { group.Last().Refresh(); } catch (Exception checkpoint) { failures.Add(checkpoint); }
                    throw new AggregateException(new[] { error }.Concat(failures));
                }
                ExceptionDispatchInfo.Capture(error).Throw();
            }
            last = applied;
            edits.AddRange(applied);
            foreach (var group in edits.GroupBy(edit => edit.Key)) group.Last().Refresh();
            pending = null;
        }
        public override void Undo()
        {
            if (!CanUndo || !IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            for (int index = edits.Count - 1; index >= 0; index--) { edits[index].Undo(); edits.RemoveAt(index); }
            last.Clear();
            pending = null;
        }
        private readonly CQFAIModel model;
        private readonly CQFAIEditorContext context;
        private readonly ICQFAILiveMap backend;
        private readonly List<CQFAILiveMapEdit> edits = new List<CQFAILiveMapEdit>();
        private List<CQFAILiveMapEdit> last = new List<CQFAILiveMapEdit>();
        private IReadOnlyList<CQFAILiveMapEdit>? pending;
    }
}
