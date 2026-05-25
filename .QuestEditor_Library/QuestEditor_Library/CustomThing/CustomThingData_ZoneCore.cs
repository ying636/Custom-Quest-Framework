using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class CustomThingData_ZoneCore : CustomThingData
    {
        public CustomThingData_ZoneCore() { }
        public CustomThingData_ZoneCore(ZoneCore thing, IntVec3 pos) : base(thing, pos) 
        {
            this.coreRotation = thing.coreRotation;
            this.conditions = thing.conditions;
            this.isCenter = thing.isCenter;
            this.reserveThing = thing.reserveThing;
            this.coreTags = thing.coreTags;
            this.prohibitRotatingDocking = thing.prohibitRotatingDocking;
            this.prohibitFlippingDocking = thing.prohibitFlippingDocking;
            this.generationKey = thing.generationKey;
            this.destroyThings = thing.destroyThings;
        }
        public override XElement SaveToXElement(string nodeName) 
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("coreRotation", this.coreRotation));
            result.Add(new XElement("isCenter", this.isCenter));
            if (!this.size.IsEmpty)
            {
                result.Add(new XElement("size", this.size));
            }
            if (this.generationKey != null && this.generationKey != "")
            {
                result.Add(new XElement("generationKey", this.generationKey));
            }
            if (this.prohibitRotatingDocking)
            {
                result.Add(new XElement("prohibitRotatingDocking", this.prohibitRotatingDocking));
            }
            if (this.prohibitFlippingDocking)
            {
                result.Add(new XElement("prohibitFlippingDocking", this.prohibitFlippingDocking));
            }
            if (this.destroyThings)
            {
                result.Add(new XElement("destroyThings", this.destroyThings));
            }
            if (this.reserveThing != null) 
            {
                result.Add(this.reserveThing.SaveToXElement("reserveThing"));
            }
            if (this.conditions != null && this.conditions.Any())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(this.conditions, "conditions"));
            }
            if (this.coreTags != null && this.coreTags.Any())
            {
                result.Add(CQFEditorTools.SaveList(this.coreTags, "coreTags"));
            }
            return result;
        }

        public override Thing SpawnThing(Map map, Quest quest, out List<Thing> things,
            IntVec3? centre = null, bool load = false, CustomMapDataDef def = null, Func<ThingDef,bool, ThingDef> getStuff = null, Rot4? rot = null)
        {
            ZoneCore core = (ZoneCore)base.SpawnThing(map, quest,out List<Thing> ts, centre, load, def, getStuff);
            core.generationKey = this.generationKey;
            core.prohibitRotatingDocking = this.prohibitRotatingDocking; 
            core.prohibitFlippingDocking = this.prohibitFlippingDocking;
            core.coreRotation = this.coreRotation;
            core.conditions = this.conditions;
            core.isCenter = this.isCenter;
            core.reserveThing = this.reserveThing;
            core.coreTags = this.coreTags;
            core.size = this.size;
            core.destroyThings = this.destroyThings;
            if (!load && !CQFEditorTools.disgenerateByCore)
            {
                var ts2 = core.GenerateZone(getStuff, quest, this.generationKey != null
                    && GenStep_CustomMap.generatedCount_Key.TryGetValue(this.generationKey, out int count) && GenStep_CustomMap.generatedLimit_Key.TryGetValue(this.generationKey, out int limit)
                    && count >= limit);
                if (ts2 != null)
                {
                    ts.AddRange(ts2);
                }
            }
            things = ts;
            return null;
        }
        public override string ToString()
        {
            return $"方向：{this.coreRotation.ToStringHuman()},位置：{this.position}";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.generationKey, "generationKey");
            Scribe_Values.Look(ref this.prohibitRotatingDocking, "prohibitRotatingDocking");
            Scribe_Values.Look(ref this.prohibitFlippingDocking, "prohibitFlippingDocking");
            Scribe_Values.Look(ref this.destroyThings, "destroyThings");
            Scribe_Values.Look(ref this.size, "size");
            Scribe_Values.Look(ref this.isCenter, "isCenter");
            Scribe_Values.Look(ref this.coreRotation, "coreRotation");
            Scribe_Deep.Look(ref this.reserveThing, "reserveThing");
            Scribe_Collections.Look(ref this.conditions, "conditions", LookMode.Deep);
            Scribe_Collections.Look(ref this.coreTags, "coreTags", LookMode.Value);
        }

        public CoreSize size = CoreSize.Empty;
        public bool isCenter = false;
        public Rot4 coreRotation = Rot4.Invalid;
        public ThingData reserveThing = null;
        public List<ZoneCondition> conditions = new List<ZoneCondition>();
        public List<string> coreTags = new List<string>();
        public string generationKey; 
        public bool destroyThings = false;
        public bool prohibitRotatingDocking = false;
        public bool prohibitFlippingDocking = false;
    }

    public class CoreSize : IExposable
    {
        public CoreSize()
        {
        }
        public CoreSize(int minX,int minZ,int maxX,int maxZ) 
        {
            this.minX = minX;
            this.minZ = minZ;
            this.maxX = maxX;
            this.maxZ = maxZ;
        }
        public static CoreSize Empty => new CoreSize(0,0,0,0);
        public bool IsEmpty => this.minX == this.maxX && this.minX == 0 && this.maxZ == this.minZ && this.minZ == 0;
        public override string ToString()
        {
            return $"{minX},{minZ},{maxX},{maxZ}";
        }
        public string ToStringHuman()
        {
            return $"MinX:{minX},MinZ:{minZ},MaxX:{maxX},MaxZ:{maxZ}";
        }
        public CoreSize GetCopy() 
        {
            return new CoreSize(this.minX,this.minZ,this.maxX,this.maxZ);
        }
        public void Rotate(Rot4 coreRotation,RotationDirection rotDir) 
        {
            switch (rotDir)
            {
                case RotationDirection.Opposite:
                    if (coreRotation.AsVector2.x != 0)
                    {
                        int var = this.minX;
                        this.minX = this.maxX;
                        this.maxX = var;
                    }
                    else 
                    {
                        int var2 = this.minZ;
                        this.minZ = this.maxZ;
                        this.maxZ = var2;
                    }
                ; break;
                case RotationDirection.Counterclockwise:
                    int var3 = this.maxZ;
                    this.maxZ = this.maxX;
                    this.maxX = this.minZ;
                    this.minZ = this.minX;
                    this.minX = var3;
                    ; break;
                case RotationDirection.Clockwise:
                    int var4 = this.maxZ;
                    this.maxZ = this.minX;
                    this.minX = this.minZ;
                    this.minZ = this.maxX;
                    this.maxX = var4;
                    ; break;
                default:; break;
            }
        }
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            string[] array = xmlRoot.InnerText.Split(new char[]
            {
                ','
            });
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            this.minX = Convert.ToInt32(array[0], invariantCulture);
            this.minZ = Convert.ToInt32(array[1], invariantCulture);
            this.maxX = Convert.ToInt32(array[2], invariantCulture);
            this.maxZ = Convert.ToInt32(array[3], invariantCulture);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.minX, "minX");
            Scribe_Values.Look(ref this.minZ, "minZ"); 
            Scribe_Values.Look(ref this.maxX, "maxX"); 
            Scribe_Values.Look(ref this.maxZ, "maxZ");
        }

        public int minX;
        public int minZ;
        public int maxX;
        public int maxZ;
    }
}
