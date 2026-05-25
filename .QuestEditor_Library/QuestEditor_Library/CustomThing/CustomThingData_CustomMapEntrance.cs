using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData_CustomMapEntrance : CustomThingData
    {
        public CustomThingData_CustomMapEntrance() { }
        public CustomThingData_CustomMapEntrance(CustomMapEntrance thing, IntVec3 pos) : base(thing, pos) 
        {
            this.data = thing.MapDef;
            this.exitName = thing.exitName;
            this.opended = thing.opended;
            if (thing is CustomMapEntrance_Chance entrance)
            {
                this.tagWithChance = entrance.tagWithChance;
                this.mapDefWithChance = entrance.mapDefWithChance;
            }
        }
        public override CustomThingData Copy()
        {
            CustomThingData_CustomMapEntrance result = (CustomThingData_CustomMapEntrance)base.Copy();
            result.data = this.data;
            result.exitName = this.exitName;
            result.tagWithChance = this.tagWithChance;
            result.mapDefWithChance = this.mapDefWithChance;
            result.opended = this.opended;
            return result;
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.data != null) 
            {
                result.Add(new XElement("data", this.data.defName));
            }
            result.Add(new XElement("exitName", this.exitName));
            if (!this.opended)
            {
                result.Add(new XElement("opended", this.opended));
            }
            if (this.tagWithChance.Any())
            {
                XElement tagWithChance = new XElement("tagWithChance");
                this.tagWithChance.ForEach(t =>
                {
                    XElement li = new XElement("li");
                    li.Add(new XElement("tag", t.tag));
                    li.Add(new XElement("chance", t.chance));
                    tagWithChance.Add(li);
                });
                result.Add(tagWithChance);
            }
            if (this.mapDefWithChance.Any())
            {
                XElement mapDefWithChance = new XElement("mapDefWithChance");
                this.mapDefWithChance.ForEach(t =>
                {
                    XElement li = new XElement("li");
                    li.Add(new XElement("def", t.def.defName));
                    li.Add(new XElement("chance", t.chance));
                    mapDefWithChance.Add(li);
                });
                result.Add(mapDefWithChance);
            }

            return result;
        }

        public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things,
            IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, Func<ThingDef,bool, ThingDef> getStuff = null, Rot4? rot = null)
        {
            CustomMapEntrance customMapEntrance = (CustomMapEntrance)base.SpawnThing(map, quest,out List<Thing> ts, centre, load, def, getStuff);
            if (!this.opended) 
            {
                customMapEntrance.Swtich(this.opended);
            }
            if (customMapEntrance == null)
            {
                things = ts;
                return null;
            }
            if (customMapEntrance is CustomMapEntrance_Chance entrance)
            {
                entrance.mapDefWithChance = this.mapDefWithChance;
                entrance.tagWithChance = this.tagWithChance;
            }
            customMapEntrance.SetMapDef(this.data);
            customMapEntrance.exitName = this.exitName;
            customMapEntrance.questID = quest?.id.ToString();
            things = ts;
            return customMapEntrance;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.exitName, "exitName");
            Scribe_Values.Look(ref this.opended, "opended");
            Scribe_Defs.Look(ref this.data, "data");
            Scribe_Collections.Look(ref this.tagWithChance, "tagWithChance",LookMode.Deep);
            Scribe_Collections.Look(ref this.mapDefWithChance, "mapDefWithChance", LookMode.Deep);
        }

        [NoTranslate]
        public string exitName;
        public bool opended = true;
        public CustomMapDataDef data;
        public List<TagWithChance> tagWithChance = new List<TagWithChance>();
        public List<MapDefWithChance> mapDefWithChance = new List<MapDefWithChance>();
    }
}
