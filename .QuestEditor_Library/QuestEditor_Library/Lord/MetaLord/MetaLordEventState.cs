using Verse;

namespace QuestEditor_Library
{
    public sealed class MetaLordEventState : IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.executed, "executed");
            Scribe_Values.Look(ref this.faulted, "faulted");
            Scribe_Values.Look(ref this.nextTick, "nextTick");
        }

        public bool executed;
        public bool faulted;
        public int nextTick;
    }
}
