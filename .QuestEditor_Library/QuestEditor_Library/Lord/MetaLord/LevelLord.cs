using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public sealed class LevelLord : IExposable
    {
        public IReadOnlyList<Lord> Lords => this.ownLords.AsReadOnly();

        public void ExposeData()
        {
            Scribe_References.Look(ref this.level, "level");
            Scribe_Collections.Look(ref this.ownLords, "ownLords", LookMode.Reference);
            Scribe_Collections.Look(ref this.tags, "tags", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.ownLords ??= new List<Lord>();
                this.tags ??= new List<string>();
            }
        }

        public Map level;
        public List<string> tags = new List<string>();
        internal List<Lord> ownLords = new List<Lord>();
    }
}
