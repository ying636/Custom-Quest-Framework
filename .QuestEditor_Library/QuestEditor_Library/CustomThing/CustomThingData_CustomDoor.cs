using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData_CustomDoor : CustomThingData
    {
        public CustomThingData_CustomDoor() { }
        public CustomThingData_CustomDoor(CustomDoor thing, IntVec3 pos) : base(thing, pos) 
        {
            this.openingConditions = thing.openingConditions.ListFullCopy();
            this.openingActions = thing.openingActions.ListFullCopy();
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.openingConditions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.openingConditions, "openingConditions"));
            }
            if (this.openingActions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.openingActions, "openingActions"));
            }
            return result;
        }

        public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things,
            IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, Func<ThingDef, bool, ThingDef> getStuff = null, Rot4? rot = null)
        {
            CustomDoor door = (CustomDoor)base.SpawnThing(map, quest,out List<Thing> ts, centre, load, def, getStuff);
            if (door == null)
            {
                things = ts;
                return null;
            }
            this.openingConditions.ForEach(c =>
            {
                door.openingConditions.Add(c.Copy());
            });
            this.openingActions.ForEach(a =>
            {
                if (load || a as CQFAction_SentSignal == null)
                {
                    door.openingActions.Add(a.Copy());
                }
                else
                {
                    if (a.Copy() is CQFAction_SentSignal signal)
                    {
                        List<string> signalParts2 = new List<string>()
                        {
                        }; 
                        signalParts2.Add(signal.signal);
                        if (signal.signalIsOnlyValidInPart)
                        {
                            if (GenStep_CustomMap.generatedCount.ContainsKey(def.Origin))
                            {
                                signalParts2.Add(def.defName + GenStep_CustomMap.generatedCount[def.Origin].ToString());
                            }
                        }
                        signal.signal = string.Concat(signalParts2);
                        door.openingActions.Add(signal);
                    }
                }
            });
            things = ts;
            return door;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.openingConditions, "openingConditions", LookMode.Deep);
            Scribe_Collections.Look(ref this.openingActions, "openingActions", LookMode.Deep);
        }

        public List<CQFAction> openingActions = new List<CQFAction>();
        public List<DialogCondition> openingConditions = new List<DialogCondition>();
    }
}
