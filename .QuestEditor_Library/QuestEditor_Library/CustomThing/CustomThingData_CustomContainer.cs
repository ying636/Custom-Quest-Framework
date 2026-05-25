using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData_CustomContainer : CustomThingData
    {
        public CustomThingData_CustomContainer() { }
        public CustomThingData_CustomContainer(CustomContainer thing, IntVec3 pos) : base(thing, pos) 
        {
            this.tickToOpen = thing.tickToOpen;
            this.openingActions = thing.openingActions;
            this.openingConditions = thing.openingConditions;
            this.innerThings = thing.innerThings;
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.tickToOpen != 100) 
            {
                result.Add(new XElement("tickToOpen", this.tickToOpen));
            }
            if (this.openingConditions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.openingConditions, "openingConditions"));
            }
            if (this.openingActions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.openingActions, "openingActions"));
            }
            if (this.innerThings.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.innerThings, "innerThings"));
            }
            return result;
        }

        public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things,
            IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, 
            Func<ThingDef, bool, ThingDef> getStuff = null, Rot4? rot = null)
        {
            CustomContainer customContainer = (CustomContainer)base.SpawnThing(map, quest, out List<Thing> ts,
                centre, load, def, getStuff);
            if (customContainer == null)
            {
                things = ts;
                return null;
            }
            if (!load && this.innerThings.Any()) 
            {
                this.innerThings.RandomElementByWeight(t => t.chance).SpawnLoots(map,customContainer.InteractionCell,null,customContainer).ForEach(t =>
                {
                    t.Rotation = this.def.rotatable ? customContainer.Rotation : Rot4.South;
                    t.DeSpawn();
                    customContainer.TryAcceptThing(t);
                    t.Rotation = this.def.rotatable ? customContainer.Rotation : Rot4.South;
                });
            }
            customContainer.tickToOpen = this.tickToOpen;
            this.openingActions.ForEach(a =>
            {
                if (load || a as CQFAction_SentSignal == null)
                {
                    customContainer.openingActions.Add(a.Copy());
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
                        customContainer.openingActions.Add(signal);
                    }
                }
            });
            this.openingConditions.ForEach(c =>
            {
                customContainer.openingConditions.Add(c.Copy());
            });
            things = ts;
            return customContainer;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.tickToOpen, "tickToOpen");
            Scribe_Collections.Look(ref this.innerThings, "innerThings", LookMode.Deep);
            Scribe_Collections.Look(ref this.openingConditions, "openingConditions", LookMode.Deep); 
            Scribe_Collections.Look(ref this.openingActions, "openingActions", LookMode.Deep);
        }

        public int tickToOpen = 100;
        public List<LootData> innerThings = new List<LootData>();
        public List<DialogCondition> openingConditions = new List<DialogCondition>();
        public List<CQFAction> openingActions = new List<CQFAction>();
    }
}
