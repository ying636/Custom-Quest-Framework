using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public class MetaLordEventDef : Def
    {
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (this.tickInterval < 60) yield return "tickInterval must be at least 60.";
            HashSet<string> keys = new HashSet<string>();
            if (this.events == null) { yield return "Missing events list."; yield break; }
            foreach (MetaLordEvent entry in this.events)
            {
                if (entry == null) { yield return "Null MetaLord event."; continue; }
                if (string.IsNullOrWhiteSpace(entry.key) || !keys.Add(entry.key)) yield return "Missing or duplicate event key: " + entry.key;
                if (entry.trigger == null) yield return "Missing event trigger: " + entry.key;
                else if (entry.trigger.minimumMembers < -1 || entry.trigger.maximumMembers < -1 ||
                    (entry.trigger.minimumMembers >= 0 && entry.trigger.maximumMembers >= 0 && entry.trigger.minimumMembers > entry.trigger.maximumMembers))
                    yield return "Invalid event member range: " + entry.key;
                if (entry.cooldownTicks < 0) yield return "Negative event cooldown: " + entry.key;
                if (entry.actions == null || entry.actions.Count == 0 || entry.actions.Contains(null)) yield return "Missing event actions: " + entry.key;
            }
        }

        public int tickInterval = 2500;
        public List<MetaLordEvent> events = new List<MetaLordEvent>();
    }
}
