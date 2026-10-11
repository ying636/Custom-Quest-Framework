using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public sealed class MetaLordEvent
    {
        [NoTranslate] public string key;
        public bool once;
        public int cooldownTicks;
        public MetaLordEventTrigger trigger;
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
