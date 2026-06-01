using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
namespace QuestEditor_Library
{
public class DutyData
    {
    }
    public class HediffInformation : ISaveable, IExposable
    {
        public HediffInformation() { }
        public HediffInformation(HediffDef hediff, BodyPartDef part, float severity, string partLabel)
        {
            this.partLabel = partLabel;
            this.part = part;
            this.hediff = hediff;
            this.severity = severity;
            this.buffer = "";
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("partLabel", this.partLabel));
            result.Add(new XElement("hediff", this.hediff.defName));
            result.Add(new XElement("part", this.part?.defName));
            result.Add(new XElement("severity", this.severity));
            result.Add(new XElement("partLabelForSeeing", this.partLabelForSeeing));
            return result;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.partLabelForSeeing, "HediffInformation_partLabelForSeeing");
            Scribe_Values.Look(ref this.severity, "HediffInformation_severity");
            Scribe_Values.Look(ref this.partLabel, "HediffInformation_partLabel");
            Scribe_Defs.Look(ref this.part, "HediffInformation_part");
            Scribe_Defs.Look(ref this.hediff, "HediffInformation_hediff");
        }
        public string buffer;
        public float severity;
        public string partLabelForSeeing;
        public string partLabel;
        public BodyPartDef part;
        public HediffDef hediff;
    }
    public class ArrivingWay : IExposable,IDrawable
    {     
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
        }
        public virtual void SpawnPnaw(List<Pawn> pawns, IntVec3 position, Map map)
        {
            pawns.ForEach(pawn => GenSpawn.Spawn(pawn, position, map));  
        }
        public virtual void ExposeData()
        {
        }
    }
    public class ArrivingWay_DropPod : ArrivingWay 
    {
        public override void SpawnPnaw(List<Pawn> pawns, IntVec3 position, Map map)
        {
            ActiveTransporterInfo activeDropPodInfo = new ActiveTransporterInfo();
            foreach (Thing item in pawns)
            {
                activeDropPodInfo.innerContainer.TryAdd(item, true);
            }
            DropPodUtility.MakeDropPodAt(position,map, activeDropPodInfo);
        }
    }
}
