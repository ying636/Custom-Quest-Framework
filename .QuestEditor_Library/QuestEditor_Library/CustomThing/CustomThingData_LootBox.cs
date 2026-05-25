using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using Verse;
using static UnityEngine.Networking.UnityWebRequest;

namespace QuestEditor_Library
{
    public class CustomThingData_LootBox : CustomThingData
    {
        public CustomThingData_LootBox() { }
        public CustomThingData_LootBox(LootBox lootBox, IntVec3 pos) : base(lootBox, pos) 
        {
            this.loots = lootBox.loots;
            this.tickToOpen = lootBox.tickToOpen;
            this.openReport = lootBox.openReport;
            this.destroyAfterOpening = lootBox.destroyAfterOpening;
            this.lootBoxName = lootBox.lootBoxName;
            this.lootDef = lootBox.lootDef;
            this.openWhenDestroyed = lootBox.openWhenDestroyed;
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.lootDef != null) 
            {
                result.Add(new XElement("lootDef", this.lootDef.defName));
            }
            result.Add(new XElement("lootBoxName", this.lootBoxName));
            result.Add(new XElement("tickToOpen", this.tickToOpen));
            result.Add(new XElement("openReport", this.openReport));
            result.Add(new XElement("destroyAfterOpening", this.destroyAfterOpening));
            if (!this.openWhenDestroyed)
            {
                result.Add(new XElement("openWhenDestroyed", this.openWhenDestroyed));
            }
            if (this.loots.Any()) 
            {
                XElement loots = new XElement("loots");
                this.loots.ForEach((x) => loots.Add(x.SaveToXElement("li")));
                result.Add(loots);
            }
            return result;
        }

public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things,
    IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, Func<ThingDef,bool, ThingDef> getStuff = null,Rot4? rot = null)
        {
            LootBox lootBox = (LootBox)base.SpawnThing(map, quest,out List<Thing> ts, centre,load,def, getStuff);
            lootBox.lootBoxName = this.lootBoxName;
            if (lootBox == null) 
            {
                things = ts;
                return null;
            }
            lootBox.loots.AddRange(this.loots);
            if (this.lootDef != null)
            {
                if (load)
                {
                    lootBox.useLootDef = true;
                    lootBox.lootDef = this.lootDef;
                }
                else
                {
                    lootBox.loots.AddRange(this.lootDef.loots);
                }
            }
            if (quest != null)
            {
                List<string> signalParts = new List<string>()
                        {
                "Quest",
                quest.id.ToString(),
                    "." + lootBox.lootBoxName
                        };
                lootBox.questTags = new List<string>() { string.Concat(signalParts) };
                if (def != null && GenStep_CustomMap.generatedCount.TryGetValue(def.Origin, out int value))
                {
                    lootBox.questTags.Add(def.defName + value.ToString() + "." + lootBox.lootBoxName);
                }
            };
            lootBox.tickToOpen = this.tickToOpen;
            lootBox.openReport = this.openReport;
            lootBox.destroyAfterOpening = this.destroyAfterOpening;
            lootBox.openWhenDestroyed = this.openWhenDestroyed;
            things = ts;
            return lootBox;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.lootBoxName, "lootBoxName");
            Scribe_Values.Look(ref this.tickToOpen, "tickToOpen");
            Scribe_Values.Look(ref this.openReport, "openReport");
            Scribe_Values.Look(ref this.destroyAfterOpening, "destroyAfterOpening");
            Scribe_Values.Look(ref this.openWhenDestroyed, "openWhenDestroyed");
            Scribe_Collections.Look(ref this.loots, "loots", LookMode.Deep);
            Scribe_Defs.Look(ref this.lootDef, "lootDef");
        }

        [NoTranslate]
        public string lootBoxName;
        public int tickToOpen = 100;
        public string openReport = "OpenLoot";
        public bool destroyAfterOpening = false;
        public List<LootData> loots = new List<LootData>();
        public LootDataDef lootDef;
        public bool openWhenDestroyed = true;
    }
}
