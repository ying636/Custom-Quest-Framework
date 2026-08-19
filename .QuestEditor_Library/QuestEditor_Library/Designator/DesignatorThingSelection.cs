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
    }
}
