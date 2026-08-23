using System.Collections.Generic;
using Verse;

namespace QuestEditor_Library
{
    public abstract class QuestBookObjective_ThingTarget : QuestBookObjective_TargetCount
    {
        public ThingDef targetThingDef;

        public override bool UsesThingTarget => true;

        public override ThingDef TargetThingDef
        {
            get => targetThingDef;
            set => targetThingDef = value;
        }

        public override IEnumerable<ThingDef> GetThingTargets()
        {
            yield break;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref targetThingDef, "targetThingDef");
        }
    }
}
