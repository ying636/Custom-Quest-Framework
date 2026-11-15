using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class DesignatorThingSelection
    {
        public DesignatorThingSelection(ThingDef thing, ThingDef? stuff = null)
        {
            this.Thing = thing;
            this.Stuff = stuff;
        }

        public ThingDef Thing { get; }

        public ThingDef? Stuff { get; }

        public static IEnumerable<DesignatorThingSelection> RestoreRecent(IEnumerable<CQFPaletteSelection> entries)
        {
            foreach (CQFPaletteSelection entry in entries)
            {
                ThingDef? thing = DefDatabase<ThingDef>.GetNamedSilentFail(entry.thingDef);
                if (thing == null)
                {
                    Log.Warning("[CQF] Recent palette ThingDef is unavailable: " + entry.thingDef);
                    continue;
                }
                ThingDef? stuff = entry.stuffDef.NullOrEmpty() ? null : DefDatabase<ThingDef>.GetNamedSilentFail(entry.stuffDef);
                if (!entry.stuffDef.NullOrEmpty() && stuff == null)
                {
                    Log.Warning("[CQF] Recent palette stuff is unavailable: " + entry.stuffDef);
                    continue;
                }
                if (thing.MadeFromStuff && stuff == null)
                {
                    Log.Warning("[CQF] Recent palette selection requires stuff: " + entry.thingDef);
                    continue;
                }
                if (stuff != null && (!thing.MadeFromStuff || !GenStuff.AllowedStuffsFor(thing).Contains(stuff)))
                {
                    Log.Warning("[CQF] Recent palette stuff is not allowed: " + entry.thingDef + "/" + entry.stuffDef);
                    continue;
                }
                yield return new DesignatorThingSelection(thing, stuff);
            }
        }
    }
}
