using System.Xml.Linq;

namespace QuestEditor_Library
{
    public sealed class CQFAILiveMapEdit
    {
        public CQFAILiveMapEdit(string key, Action apply, Action undo, Func<string> read, Func<XElement> receipt)
        {
            Key = key;
            this.apply = apply;
            this.undo = undo;
            this.read = read;
            this.receipt = receipt;
        }
        public string Key { get; }
        public string? Before { get; private set; }
        public string? After { get; private set; }
        public bool IsCurrent => After != null && read() == After;
        public XElement Receipt => receipt();
        public void Apply()
        {
            Before = read();
            apply();
            After = read();
        }
        public void Undo()
        {
            if (Before == null || read() == Before) return;
            undo();
            if (read() != Before) throw new InvalidOperationException("CQF_AI_RollbackMismatch: " + Key);
        }
        public void Refresh() { After = read(); }
        private readonly Action apply;
        private readonly Action undo;
        private readonly Func<string> read;
        private readonly Func<XElement> receipt;
    }
}
