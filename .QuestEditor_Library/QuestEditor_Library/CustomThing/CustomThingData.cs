using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData : ISaveable,IExposable
    {
        public CustomThingData() { }
        public CustomThingData(Thing thing,IntVec3 pos) 
        {
            this.def = thing.def;
            this.stuff = thing.Stuff;
            this.style = thing.StyleDef;
            this.position = pos;
            this.count = thing.stackCount;
            this.rotation = thing.Rotation;
            if (thing.def.CanHaveFaction)
            {
                this.faction = thing.Faction?.def;
            }
            if (thing.TryGetComp<CompPowerBattery>() is CompPowerBattery compB)
            {
                this.storedEnergy = compB.StoredEnergy;
            }
            if (thing.TryGetComp<CompActionWorker>() is CompActionWorker comp && comp.comps != null) 
            {
                this.comps = comp.comps;
            }
            if (thing.TryGetComp<CompCustomText>() is CompCustomText compText)
            {
                if (compText.useCustomName)
                {
                     this.customName = compText.customName.Translate();
                }
                if (compText.useCustomDescription)
                {
                    this.customDescription = compText.customDescription;
                }
                if (compText.useCustomInspectText)
                {
                    this.customInspectText = compText.customInspectText;
                }
            }
            if (thing.TryGetComp<CompColorable>() is CompColorable color)
            {
                this.color = color.Color;
            }
        }
        public virtual CustomThingData Copy()
        {
            XElement x = this.SaveToXElement("CustomThingData");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            CustomThingData result = DirectXmlToObject.ObjectFromXml<CustomThingData>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public virtual Thing SpawnThing(Map map, Quest quest, out List<Thing> things,
            IntVec3? centre = null, bool load = false, CustomMapDataDef def = null
            , Func<ThingDef,bool, ThingDef> getDef = null,Rot4? rot = null)
        {
            IntVec3 pos = this.position;
            if (centre != null)
            {
                pos += centre.Value;
            }
            else if (map != null)
            {
                pos += map.Center;
            } 
            if (map != null && !pos.InBounds(map))
            {
                Log.Error("Spawn CustomThing Error:Out of bounds" + this.ToString());
                things = null;
                return null;
            }
            Thing result = Current.Game == null ? 
                GameTools.MakeThingWithoutID(this.def,this.stuff) : 
                ThingMaker.MakeThing(getDef == null ? this.def : getDef(this.def,false), getDef == null ? this.stuff : getDef(this.stuff,true));
            if (map != null)
            {
                GenSpawn.Spawn(result, pos, map, rot != null ? rot.Value : this.rotation);
            }
            result.StyleDef = this.style;
            if (this.faction != null)
            {
                result.SetFaction(GameTools.GetFaction(this.faction));
            }
            if (result.TryGetComp<CompPowerBattery>() is CompPowerBattery compB)
            {
                compB.SetStoredEnergyPct(0f);
                compB.AddEnergy(this.storedEnergy);
            }

            if (result.TryGetComp<CompActionWorker>() is CompActionWorker comp)
            {
                this.comps.ForEach(c => comp.comps.Add(c.Copy()));
                if (!load)
                {
                    comp.comps.ForEach(c =>
                    {
                        if (c.mode == ActionTriggerMode.Signal)
                        {
                            List<string> signalParts = new List<string>()
                            {
                            };
                            if (quest != null)
                            {
                                signalParts.Add("Quest");
                                signalParts.Add(quest?.id.ToString());
                                signalParts.Add(".");
                            }
                            signalParts.Add(c.signal);
                            if (c.signalIsOnlyValidInPart)
                            {
                                if (def != null && GenStep_CustomMap.generatedCount.TryGetValue(def.Origin,out int value))
                                {
                                    signalParts.Add(def.defName + value.ToString());
                                }
                            }
                            c.signal = string.Concat(signalParts);
                        }
                        c.actions.ForEach(a =>
                        {
                            if (a is CQFAction_SentSignal signal)
                            {
                                List<string> signalParts = new List<string>()
                                {
                                    signal.signal
                                };  
                                if (def != null && c.signalIsOnlyValidInPart)
                                {
                                    signalParts.Add(def.defName + GenStep_CustomMap.generatedCount[def.Origin].ToString());
                                }
                                signal.signal = string.Concat(signalParts);
                            }
                        });
                    });

                    comp.comps?.ForEach(s =>
                    {
                        if (s.mode == ActionTriggerMode.MapGeneration)
                        {
                            s.actions.ForEach(a => a.Work(comp.GetTargetThis(),quest));
                        }
                    });
                }
            }
            if (result.TryGetComp<CompCustomText>() is CompCustomText compText)
            {
                if (this.customName != null)
                {
                    compText.useCustomName = true;

                    compText.customName = load ? this.customName : this.customName.Translate().ToString();
                }
                if (this.customDescription != null)
                {
                    compText.useCustomDescription = true;

                    compText.customDescription = load ? this.customDescription : this.customDescription.Translate().ToString();

                }
                if (this.customInspectText != null)
                {
                    compText.useCustomInspectText = true;
                    compText.customInspectText = load ? this.customInspectText : this.customInspectText.Translate().ToString();
                }
            }
            if (result.TryGetComp<CompColorable>() is CompColorable color)
            {
                color.SetColor(this.color);
            }
            if (quest != null)
            {
                List<string> signalParts = new List<string>()
                        {
                "Quest",
                quest.id.ToString(),
                ""
                        };
                result.questTags = new List<string>() { string.Concat(signalParts) };
                if (def != null && GenStep_CustomMap.generatedCount.TryGetValue(def.Origin, out int value))
                {
                    result.questTags.Add(def.defName + value.ToString());
                }
            };
            things = new List<Thing>() {result};
            return result;
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            if (this.def == null) 
            {
                return null;
            }
            result.SetAttributeValue("Class", this.GetType().FullName);
            result.Add(new XElement("def", this.def.defName));
            if (this.rotation != Rot4.North)
            {
                result.Add(new XElement("rotation", this.rotation));
            }
            if (this.count != 1)
            {
                result.Add(new XElement("count", this.count));
            }
            if (this.storedEnergy != 0)
            {
                result.Add(new XElement("storedEnergy", this.storedEnergy));
            }
            if (this.stuff != null)
            {
                result.Add(new XElement("stuff", this.stuff.defName));
            }
            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction.defName));
            }
            if (this.style != null)
            {
                result.Add(new XElement("style", this.style.defName));
            }
            if (this.customName != null)
            {
                result.Add(new XElement("customName", this.customName));
            }
            if (this.customDescription != null)
            {
                result.Add(new XElement("customDescription", this.customDescription));
            }
            if (this.customInspectText != null)
            {
                result.Add(new XElement("customInspectText", this.customInspectText));
            }
            if (this.color != Color.white)
            {
                result.Add(new XElement("color", this.color.ToString()));
            }
            XElement pos = new XElement("position",$"({this.position.x},{this.position.y},{this.position.z})");
            if (!this.comps.NullOrEmpty()) 
            {
                XElement comps = new XElement("comps");
                this.comps.ForEach(c => comps.Add(c.SaveToXElement("li")));
                result.Add(comps);
            }
            result.Add(pos);
            return result;
        }

        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref this.def, "def");
            Scribe_Defs.Look(ref this.style, "style");
            Scribe_Defs.Look(ref this.stuff, "stuff");
            Scribe_Defs.Look(ref this.faction, "faction");
            Scribe_Values.Look(ref this.position, "position"); 
            Scribe_Values.Look(ref this.rotation, "rotation");
            Scribe_Values.Look(ref this.color, "color");
            Scribe_Values.Look(ref this.count, "count"); 
            Scribe_Values.Look(ref this.storedEnergy, "storedEnergy"); 
            Scribe_Values.Look(ref this.customName, "customName"); 
            Scribe_Values.Look(ref this.customDescription, "customDescription");
            Scribe_Values.Look(ref this.customInspectText, "customInspectText");
            Scribe_Collections.Look(ref this.comps, "comps",LookMode.Deep);
        }

        public ThingDef def;
        public ThingStyleDef style = null;
        public ThingDef stuff = null;
        public FactionDef faction = null;
        public IntVec3 position;
        public Rot4 rotation = Rot4.North;
        public Color color = Color.white;
        public int count = 1;
        public float storedEnergy = 0;

        public string customName = null;
        public string customDescription = null;
        public string customInspectText = null;
        public List<ActionComp> comps = new List<ActionComp>();
    }
}
