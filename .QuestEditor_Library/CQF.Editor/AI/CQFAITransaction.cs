using System.Xml.Linq;
using System.Runtime.ExceptionServices;

namespace QuestEditor_Library
{
    public class CQFAITransaction
    {
        public CQFAITransaction(CQFAIModel model, CQFAIEditorContext context)
        {
            this.model = model;
            this.context = context;
            identity = context.Identity;
            Source = model.Copy(context.Read());
            sourceXml = Snapshot(Source);
        }
        public object Source { get; }
        public virtual bool UndoSupported => true;
        public object? Draft { get; private set; }
        public virtual bool CanUndo => undo != null;
        public virtual bool IsTargetValid => context.IsValid?.Invoke() != false && ReferenceEquals(context.Identity, identity);
        public virtual bool IsCurrent => context.IsValid?.Invoke() != false && ReferenceEquals(context.Identity, identity) && Snapshot(context.Read()) == (CanUndo ? appliedXml : sourceXml);
        public virtual void Build(XElement changes, string command, bool generateText)
        {
            if (!IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            object draft = new CQFAIChanges(model).Build(CanUndo ? context.Read() : Draft ?? Source, changes, command, generateText);
            context.Validate?.Invoke(draft);
            Draft = draft;
        }
        public virtual void Apply()
        {
            if (Draft == null || !IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            object before = model.Copy(context.Read());
            context.Validate?.Invoke(Draft);
            ApplyWithRollback(model.Copy(Draft), before);
            undo ??= before;
            appliedXml = Snapshot(context.Read());
            identity = context.Identity;
        }
        public virtual void Undo()
        {
            if (undo == null || !IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            ApplyWithRollback(model.Copy(undo), model.Copy(context.Read()));
            undo = null;
            Draft = null;
            identity = context.Identity;
        }
        private void ApplyWithRollback(object value, object before)
        {
            try
            {
                string expected = Snapshot(value);
                context.Apply(value);
                if (Snapshot(context.Read()) != expected) throw new InvalidDataException("CQF_AI_ApplyMismatch");
            }
            catch (Exception error)
            {
                try
                {
                    context.Apply(before);
                    if (Snapshot(context.Read()) != Snapshot(before)) throw new InvalidOperationException("CQF_AI_RollbackMismatch");
                }
                catch (Exception rollback) { throw new AggregateException(error, rollback); }
                identity = context.Identity;
                ExceptionDispatchInfo.Capture(error).Throw();
            }
        }
        private string Snapshot(object value) => model.Write(value, root: true).ToString(SaveOptions.DisableFormatting);
        private readonly CQFAIModel model;
        private readonly CQFAIEditorContext context;
        private object identity;
        private readonly string sourceXml;
        private object? undo;
        private string? appliedXml;
    }
}
