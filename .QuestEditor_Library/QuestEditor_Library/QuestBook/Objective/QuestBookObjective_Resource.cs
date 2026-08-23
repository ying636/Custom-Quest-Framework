using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjective_Resource : QuestBookObjective_ThingTarget
    {
        public override bool RequiresCheck => true;

        public override System.Collections.Generic.IEnumerable<ThingDef> GetThingTargets()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.CountAsResource);
        }

        public override bool Process(QuestBookObjectiveProgress progress, Signal signal)
        {
            return false;
        }

        public override bool Check(QuestBookObjectiveProgress progress)
        {
            if (targetThingDef == null || progress == null)
            {
                return false;
            }
            int count = Find.Maps.Where(map => map.IsPlayerHome).Sum(map => map.resourceCounter.GetCount(targetThingDef));
            progress.currentCount = count;
            progress.completed = count >= TargetCount;
            return true;
        }
    }
}
