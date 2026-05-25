using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData_InteractableThing : CustomThingData
    {
        public CustomThingData_InteractableThing() { }
        public CustomThingData_InteractableThing(InteractableThing thing, IntVec3 pos) : base(thing, pos) 
        {
            this.operations = thing.operations;
            this.operationDefs = thing.operationDefs;
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName); 
            if (this.operations.Any())
            {
                XElement operations = new XElement("operations");
                this.operations.ForEach((x) => operations.Add(x.SaveToXElement("li")));
                result.Add(operations);
            }
            if (this.operationDefs.Any())
            {
                XElement operations = new XElement("operationDefs");
                this.operationDefs.ForEach((x) => operations.Add(new XElement("li",x.defName)));
                result.Add(operations);
            }
            return result;
        }

public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things,
    IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, Func<ThingDef,bool, ThingDef> getStuff = null,Rot4? rot = null)
        {
            InteractableThing interactableThing = (InteractableThing)base.SpawnThing(map, quest,out List<Thing> ts, centre, load, def, getStuff);
            if (interactableThing == null)
            {
                things = ts;
                return null;
            }
            if (!load)
            {
                this.operations.ForEach(c => interactableThing.operations.Add(c.Copy()));
                this.operationDefs.ForEach(d => d.interactions.ForEach(c => interactableThing.operations.Add(c.Copy())));
                interactableThing.operations.ForEach(op =>
                {
                    op.results.ForEach(r =>
                    {
                        r.actions.ForEach(a =>
                        {
                            if (!load && a is CQFAction_SentSignal signal)
                            {
                                List<string> signalParts2 = new List<string>()
                                {
                                }; 
                                signalParts2.Add(signal.signal);
                                if (signal.signalIsOnlyValidInPart)
                                {
                                    signalParts2.Add(def.defName + GenStep_CustomMap.generatedCount[def.Origin].ToString());
                                }
                                signal.signal = string.Concat(signalParts2);
                            }
                        });
                    });
                });
            }
            else 
            {
                this.operations.ForEach(c => interactableThing.operations.Add(c.Copy()));
                this.operationDefs.ForEach(d => interactableThing.operationDefs.Add(d));
            }
            things = ts;
            return interactableThing;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.operations, "operations", LookMode.Deep);
            Scribe_Collections.Look(ref this.operationDefs, "operationDefs", LookMode.Deep);
        }

        public List<InteractionOperation> operations = new List<InteractionOperation>();
        public List<InteractionDataDef> operationDefs = new List<InteractionDataDef>();
    }
}
