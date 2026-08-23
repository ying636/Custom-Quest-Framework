using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookObjective_Building : QuestBookObjective_ThingTarget
    {
        public override bool RequiresCheck => true;

        public override System.Collections.Generic.IEnumerable<ThingDef> GetThingTargets()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.building != null);
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
            int count = Find.Maps.Where(map => map.IsPlayerHome)
                .Sum(map => map.listerBuildings.AllBuildingsColonistOfDef(targetThingDef).Count);
            progress.currentCount = count;
            progress.completed = count >= TargetCount;
            return true;
        }
    }
}
