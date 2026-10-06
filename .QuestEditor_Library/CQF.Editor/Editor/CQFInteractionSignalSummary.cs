using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    internal sealed class CQFInteractionSignalSummary
    {
        public Map? Map { get; set; }
        public Thing? SourceThing { get; set; }
        public string? Name { get; set; }
        public bool Dirty { get; set; } = true;
        public List<string> Signals { get; set; } = new List<string>();
    }
}
