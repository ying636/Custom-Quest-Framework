using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData_CustomTrap : CustomThingData
    {
        public CustomThingData_CustomTrap() { }
        public CustomThingData_CustomTrap(CustomTrap thing, IntVec3 pos) : base(thing, pos) 
        {
            this.trapName = thing.trapName;
            this.trapComps = thing.trapComps;
            if (thing is CustomTrap_Capture trap) 
            {
                this.tickToDisarm = trap.tickToDisarm;
                this.disarmReport = trap.disarmReport; 
                this.disarmActions = trap.disarmActions;
            }
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName);
            XElement actions = new XElement("trapComps");
            this.trapComps.ForEach((x) => actions.Add(x.SaveToXElement("li")));
            result.Add(actions); 
            result.Add(new XElement("trapName", this.trapName));
            if (this.tickToDisarm != 100) 
            {
                result.Add(new XElement("tickToDisarm", this.tickToDisarm));
            }
            if (this.disarmReport != "DisarmTrap")
            {
                result.Add(new XElement("disarmReport", this.disarmReport));
            }
            if (this.disarmActions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.disarmActions, "disarmActions"));
            }
            return result;
        }

        public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things
            , IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, Func<ThingDef,bool, ThingDef> getStuff = null, Rot4? rot = null)
        {
            CustomTrap customTrap = (CustomTrap)base.SpawnThing(map, quest,out List<Thing> ts, centre, load, def, getStuff);
            if (customTrap == null)
            {
                things = ts;
                return null;
            }
            customTrap.trapName = this.trapName;
            this.trapComps.ForEach(c =>
            {
                TrapComp comp = new TrapComp();
                comp.mode = c.mode;
                comp.signalIsOnlyValidInPart = c.signalIsOnlyValidInPart;
                comp.triggerWhenDamaged = c.triggerWhenDamaged;
                comp.tick = c.tick;
                comp.buffer = c.tick.ToString();
                List<string> signalParts = new List<string>(){};
                if (quest != null) 
                {
                    signalParts.Add("Quest");
                    signalParts.Add(quest?.id.ToString());
                    signalParts.Add(".");
                }
                signalParts.Add(c.inSignal);
                if (c.signalIsOnlyValidInPart)
                {
                    signalParts.Add(def.defName + GenStep_CustomMap.generatedCount[def.Origin].ToString());
                }
                comp.inSignal = load ? c.inSignal : string.Concat(signalParts);
                c.actions.ForEach(a => comp.actions.Add(a.Copy()));
                comp.actions.ForEach(a =>
                {
                    if (a is CQFAction_SentSignal signal && !load)
                    {
                        List<string> signalParts2 = new List<string>()
                        { 
                        };
                        if (trapName != "undefined")
                        {
                            signalParts2.Add(trapName+".");
                        }
                        signalParts2.Add(signal.signal);
                        if (signal.signalIsOnlyValidInPart)
                        {
                            if (GenStep_CustomMap.generatedCount.ContainsKey(def.Origin))
                            {
                                signalParts2.Add(def.defName + GenStep_CustomMap.generatedCount[def.Origin].ToString());
                            }
                        }
                        signal.signal = string.Concat(signalParts2);
                    }
                });
                customTrap.trapComps.Add(comp);
            });
            if (customTrap is CustomTrap_Capture trap)
            {
                trap.tickToDisarm = this.tickToDisarm;
                trap.disarmReport = this.disarmReport;

                this.disarmActions.ForEach(a =>
                {
                    CQFAction a2 = a.Copy();
                    if (a2 is CQFAction_SentSignal signal && !load)
                    {
                        List<string> signalParts2 = new List<string>()
                        {  
                        }; 
                        if (trapName != "undefined")
                        {
                            signalParts2.Add(trapName+".");
                        }
                        signalParts2.Add(signal.signal);
                        if (signal.signalIsOnlyValidInPart)
                        {
                            if (GenStep_CustomMap.generatedCount.ContainsKey(def.Origin))
                            {
                                signalParts2.Add(def.defName + GenStep_CustomMap.generatedCount[def.Origin].ToString());
                            }
                        }
                        signal.signal = string.Concat(signalParts2);
                    }  
                    trap.disarmActions.Add(a2);
                });
            }
            things = ts;
            return customTrap;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.trapName, "trapName");
            Scribe_Collections.Look(ref this.trapComps, "textComp", LookMode.Deep); 
            Scribe_Collections.Look(ref this.disarmActions, "disarmActions", LookMode.Deep);
            Scribe_Values.Look(ref this.disarmReport, "disarmReport");
            Scribe_Values.Look(ref this.tickToDisarm, "tickToDisarm");
        }

        [NoTranslate]
        public string trapName = "undefined";
        [NoTranslate]
        public List<TrapComp> trapComps = new List<TrapComp>();

        public string disarmReport = "DisarmTrap";
        public int tickToDisarm = 100;
        public List<CQFAction> disarmActions = new List<CQFAction>();
    }
}
