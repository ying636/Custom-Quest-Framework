namespace QuestEditor_Library
{
    public sealed class CQFSignalReferenceChange
    {
        public CQFSignalReferenceChange(object owner, string previousSignal, string nextSignal, string sourceLabel)
        {
            this.Owner = owner;
            this.PreviousSignal = previousSignal;
            this.NextSignal = nextSignal;
            this.SourceLabel = sourceLabel;
        }

        public object Owner { get; }
        public string PreviousSignal { get; }
        public string NextSignal { get; }
        public string SourceLabel { get; }
        public bool IsCurrent => this.Owner is TrapComp trap ? trap.inSignal == this.PreviousSignal
            : this.Owner is ActionComp comp && comp.signal == this.PreviousSignal;

        public void Apply()
        {
            if (this.Owner is TrapComp trap)
            {
                trap.inSignal = this.NextSignal;
            }
            else if (this.Owner is ActionComp comp)
            {
                comp.signal = this.NextSignal;
            }
        }
    }
}
