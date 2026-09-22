using System;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFEditorContext : IDisposable
    {
        public CQFEditorContext(Thing source)
        {
            this.previous = SourceThing;
            SourceThing = source;
        }

        public static Thing SourceThing { get; private set; }
        public static Map Map => SourceThing?.Map ?? Find.CurrentMap;

        public void Dispose()
        {
            SourceThing = this.previous;
        }

        private readonly Thing previous;
    }
}
