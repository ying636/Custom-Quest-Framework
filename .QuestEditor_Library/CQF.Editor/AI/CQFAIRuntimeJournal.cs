using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAIRuntimeJournal : CQFAITransaction
    {
        public CQFAIRuntimeJournal(CQFAIModel model) : base(model, new CQFAIEditorContext("CQF_AI_Runtime", () => new CQFAILiveMapInfo(), _ => throw new InvalidDataException("CQF_AI_InvalidChanges"))) { }
        public bool HasMutations => edits.Count > 0 || irreversible;
        public override bool UndoSupported => !irreversible;
        public override bool CanUndo => !irreversible && edits.Count > 0;
        public override bool IsTargetValid => !captured || ReferenceEquals(game, Current.Game);
        public override bool IsCurrent => CanUndo && IsTargetValid && edits.GroupBy(edit => edit.Key).All(group => group.Last().IsCurrent);
        public XElement ApplyEdit(CQFAILiveMapEdit edit)
        {
            if (!IsTargetValid || edits.Count >= 2048) throw new InvalidOperationException("CQF_AI_StaleTarget");
            if (edits.Count > 0 && !IsCurrent) irreversible = true;
            if (!captured) { game = Current.Game; captured = true; }
            try { edit.Apply(); }
            catch (Exception error)
            {
                try { edit.Undo(); } catch (Exception rollback) { irreversible = true; throw new AggregateException(error, rollback); }
                ExceptionDispatchInfo.Capture(error).Throw(); throw;
            }
            edits.Add(edit);
            foreach (CQFAILiveMapEdit checkpoint in edits) checkpoint.Refresh();
            XElement receipt = edit.Receipt;
            string serialized = receipt.ToString(SaveOptions.DisableFormatting);
            if (serialized.Length > 16000) receipt = new XElement("receipt", new XAttribute("truncated", true), new XAttribute("totalCharacters", serialized.Length), new XElement("preview", serialized.Substring(0, 16000)), new XElement("readBackRequired", true));
            return new XElement("runtimeEdited", new XAttribute("undoSupported", UndoSupported), new XAttribute("undoAvailable", IsCurrent), receipt);
        }
        public XElement Edit(string key, Action apply, Action undo, Func<XElement> read)
            => ApplyEdit(new CQFAILiveMapEdit(key, apply, undo, () => read().ToString(SaveOptions.DisableFormatting), read));
        public void MarkIrreversible()
        {
            if (!IsTargetValid) throw new InvalidDataException("CQF_AI_StaleTarget");
            if (!captured) { game = Current.Game; captured = true; }
            irreversible = true;
        }
        public override void Undo()
        {
            if (!IsCurrent) throw new InvalidOperationException("CQF_AI_StaleTarget");
            for (int index = edits.Count - 1; index >= 0; index--) { edits[index].Undo(); edits.RemoveAt(index); }
        }
        private readonly List<CQFAILiveMapEdit> edits = new List<CQFAILiveMapEdit>();
        private Game? game;
        private bool irreversible;
        private bool captured;
    }
}
