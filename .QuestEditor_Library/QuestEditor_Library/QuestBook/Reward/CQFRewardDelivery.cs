using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFRewardDelivery
    {
        public static bool TryDrop(IEnumerable<CQFThingData> rewards, Quest quest)
        {
            Map map = Find.RandomPlayerHomeMap ?? Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Error("CQF task book reward could not find a player home map.");
                return false;
            }

            List<Thing> things = new List<Thing>();
            foreach (CQFThingData reward in rewards ?? Enumerable.Empty<CQFThingData>())
            {
                AddRewardThings(things, reward);
            }
            if (!things.Any())
            {
                Log.Error("CQF task book reward did not produce any valid things.");
                return false;
            }

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            if (!dropSpot.IsValid)
            {
                Log.Error("CQF task book reward could not find a valid trade drop spot.");
                return false;
            }
            DropPodUtility.DropThingsNear(dropSpot, map, things, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: false, forbid: false, allowFogged: true);
            TaggedString label = "LetterLabelQuestDropPodsArrived".Translate();
            TaggedString text = "LetterQuestDropPodsArrived".Translate(GenLabel.ThingsLabel(things));
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new TargetInfo(dropSpot, map), null, quest);
            return true;
        }

        private static void AddRewardThings(List<Thing> things, CQFThingData reward)
        {
            if (reward == null)
            {
                Log.Error("CQF task book reward contains an invalid reward entry.");
                return;
            }
            if (reward is CQFThingDefCount thingReward)
            {
                if (thingReward.thing == null || thingReward.count.max <= 0)
                {
                    Log.Error("CQF task book reward contains an invalid thing reward entry.");
                    return;
                }
                if (thingReward.thing.category == ThingCategory.Building)
                {
                    if (!thingReward.thing.Minifiable)
                    {
                        Log.Error("CQF task book reward building is not minifiable: " + thingReward.thing.defName);
                        return;
                    }
                    for (int index = 0; index < thingReward.count.RandomInRange; index++)
                    {
                        Thing building = ThingMaker.MakeThing(thingReward.thing, thingReward.stuff ?? GenStuff.DefaultStuffFor(thingReward.thing));
                        MinifiedThing minified = MinifyUtility.MakeMinified(building);
                        if (minified == null)
                        {
                            Log.Error("CQF task book reward building could not be minified: " + thingReward.thing.defName);
                            continue;
                        }
                        things.Add(minified);
                    }
                    return;
                }
                if (thingReward.thing.category != ThingCategory.Item)
                {
                    Log.Error("CQF task book reward only supports items and minifiable buildings: " + thingReward.thing.defName);
                    return;
                }
                int remaining = thingReward.count.RandomInRange;
                while (remaining > 0)
                {
                    Thing item = ThingMaker.MakeThing(thingReward.thing, thingReward.thing.MadeFromStuff ? thingReward.stuff ?? GenStuff.DefaultStuffFor(thingReward.thing) : null);
                    int stackCount = remaining > item.def.stackLimit ? item.def.stackLimit : remaining;
                    item.stackCount = stackCount;
                    things.Add(item);
                    remaining -= stackCount;
                }
                return;
            }
            try
            {
                List<Thing> spawned = reward.Spawn();
                if (spawned.NullOrEmpty())
                {
                    Log.Error("CQF task book reward did not produce any valid things: " + reward.GetType().Name);
                    return;
                }
                foreach (Thing thing in spawned.Where(thing => thing != null))
                {
                    things.Add(thing);
                }
            }
            catch (System.Exception exception)
            {
                Log.Error("CQF task book reward spawn failed for " + reward.GetType().Name + ": " + exception);
            }
        }
    }
}
