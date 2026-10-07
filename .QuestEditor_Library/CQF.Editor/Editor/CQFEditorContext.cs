using System;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFEditorContext : IDisposable
    {
        public CQFEditorContext(Thing source, PawnSpawnData? fixedPawnHeader = null)
        {
            this.previous = SourceThing;
            SourceThing = source;
            this.previousHeader = FixedPawnHeader;
            FixedPawnHeader = fixedPawnHeader;
        }

        public static Thing SourceThing { get; private set; }
        public static PawnSpawnData? FixedPawnHeader { get; private set; }
        public static Map Map => SourceThing?.Map ?? Find.CurrentMap;

        public void Dispose()
        {
            SourceThing = this.previous;
            FixedPawnHeader = this.previousHeader;
        }

        private readonly Thing previous;
        private readonly PawnSpawnData? previousHeader;
    }
}
