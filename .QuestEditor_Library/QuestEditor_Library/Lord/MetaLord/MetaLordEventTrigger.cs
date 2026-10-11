using System.Linq;
using Verse;

namespace QuestEditor_Library
{
    public class MetaLordEventTrigger
    {
        public virtual bool Active(MetaLord lord, LordEventSignal signal)
        {
            if (signal.type != this.type || (!this.signalTag.NullOrEmpty() && signal.tag != this.signalTag)) return false;
            int count = lord.Members.Count;
            if (this.minimumMembers >= 0 && count < this.minimumMembers) return false;
            if (this.maximumMembers >= 0 && count > this.maximumMembers) return false;
            if (!this.groupTag.NullOrEmpty())
            {
                MultLevelLordJobBase job = signal.group?.LordJob as MultLevelLordJobBase;
                if (job == null || (job.groupName != this.groupTag && !job.groupTags.Contains(this.groupTag))) return false;
            }
            return true;
        }

        public LordEventSignalType type;
        [NoTranslate] public string signalTag;
        [NoTranslate] public string groupTag;
        public int minimumMembers = -1;
        public int maximumMembers = -1;
    }
}
