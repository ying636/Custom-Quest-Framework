using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData_CustomMapExit : CustomThingData
    {
        public CustomThingData_CustomMapExit() { }
        public CustomThingData_CustomMapExit(CustomMapExit thing, IntVec3 pos) : base(thing, pos) 
        {
            this.exitName = thing.exitName;
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("exitName", this.exitName));
            return result;
        }
        public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things
            , IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, Func<ThingDef, bool, ThingDef> getStuff = null, Rot4? rot = null)
        {
            CustomMapExit customMapExit = (CustomMapExit)base.SpawnThing(map, quest,out List<Thing> ts, centre, load, def, getStuff);
            if (customMapExit == null)
            {
                things = ts;
                return null;
            }
            customMapExit.exitName = this.exitName;
            if (map.Parent is MapParent_Custom parent && parent.entrance is CustomMapEntrance entrance && entrance.exitName == this.exitName)
            {
                entrance.exit = customMapExit;
                customMapExit.entrance = entrance;
                parent.exit = customMapExit;
                parent.enterSpot = this.position + map.Center;
            }
            things = ts;
            return customMapExit;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.exitName, "exitName");
        }


        [NoTranslate]
        public string exitName;
    }
}
