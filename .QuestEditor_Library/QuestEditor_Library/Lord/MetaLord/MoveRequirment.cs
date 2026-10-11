using Verse;

namespace QuestEditor_Library
{
    public sealed class MoveRequirment : IExposable
    {
        public void ExposeData()
        {
            Scribe_References.Look(ref this.pawn, "pawn");
            Scribe_References.Look(ref this.targetMap, "targetMap");
            Scribe_Values.Look(ref this.targetGroup, "targetGroup");
            Scribe_Values.Look(ref this.status, "status");
            Scribe_Values.Look(ref this.reason, "reason");
            Scribe_Values.Look(ref this.createdTick, "createdTick");
            Scribe_Values.Look(ref this.nextCheckTick, "nextCheckTick");
            Scribe_Values.Look(ref this.timeoutTicks, "timeoutTicks", 60000);
        }

        public Pawn pawn;
        public Map targetMap;
        [NoTranslate] public string targetGroup;
        public MetaLordMoveStatus status;
        [NoTranslate] public string reason;
        public int createdTick;
        public int nextCheckTick;
        public int timeoutTicks = 60000;
    }
}
