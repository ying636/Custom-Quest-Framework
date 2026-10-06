using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class LordData :ISaveable, IDrawable,IExposable
    {  
        public LordJobData Data 
        {
            get 
            {
                if (this.lordJobData == null) 
                {
                    this.lordJobData = new LordJobData() {lordData = this};
                }
                return lordJobData;
            }
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("name",this.name));
            result.Add(this.Data.SaveToXElement("lordJobData"));
            result.Add(new XElement("faction", this.faction));
            if (this.actions.Any()) 
            {
                result.Add(CQFSerialization.SaveList_Saveable(this.actions, "actions"));
            }
            return result;
        }

        public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LordData.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref this.name,"name");
            Scribe_Values.Look(ref this.faction, "faction");
            Scribe_Deep.Look(ref this.lordJobData, "lordJobData");
            Scribe_Collections.Look(ref this.actions,"actions",LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.lordJobData != null) 
            {
                this.lordJobData.lordData = this;
            }
        }

        public string name = "default";
        public string faction;
        public LordJobData lordJobData;
        public List<CQFAction_Lord> actions = new List<CQFAction_Lord>();
    }
    public class LordJobData : ISaveable, IDrawable, IExposable
    {
        public virtual bool JobSelectable => true;
        public virtual Type LordJob => this.lordJob;
        public virtual LordJob CreateJob(Map map,Quest quest) 
        {
            LordJob result = (LordJob)Activator.CreateInstance(this.lordJob);
            if (result is LordJob_ComplexCustom complexJob)
            {
                complexJob.defaultDutyMap = this.dutyMap;
                complexJob.defaultStartNodeId = this.dutyMapStartNodeId;
            }
            return result;
        }
        public virtual void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LordJobData.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual void DrawName(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LordJobData.DrawName(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            if (this.lordJob != typeof(LordJob_Custom)) 
            {
                result.Add(new XElement("lordJob", this.lordJob.FullName));
            }
            if (this.dutyMap != null)
            {
                result.Add(new XElement("dutyMap", this.dutyMap.defName));
            }
            if (!this.dutyMapStartNodeId.NullOrEmpty())
            {
                result.Add(new XElement("dutyMapStartNodeId", this.dutyMapStartNodeId));
            }
            return result;
        }

        public virtual void ExposeData()
        {      
            Scribe_Values.Look(ref this.lordJob, "lordJob", typeof(LordJob_Custom),true);
            Scribe_Defs.Look(ref this.dutyMap, "dutyMap");
            Scribe_Values.Look(ref this.dutyMapStartNodeId, "dutyMapStartNodeId");
        }

        public Type lordJob = typeof(LordJob_Custom);
        public LordData lordData;
        public DutyMapDef dutyMap;
        public string dutyMapStartNodeId;
        internal void DrawComplexDutyMap(ref float y, float x)

        {
            object[] arguments = new object[]
            {
                y,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LordJobData.DrawComplexDutyMap(Ref:float,None:float)", this, arguments);
            y = (float)arguments[0];
        }
}
    public class LordJobData_DefendBase : LordJobData 
    {
        public override bool JobSelectable => false;
        public override Type LordJob => typeof(LordJob_DefendBase);
        public override LordJob CreateJob(Map map, Quest quest)
        {
            return new LordJob_DefendBase(GameTools.GetFaction(this.faction,map),
                GameTools.GetTarget(null,quest,this.targetPositionName).Cell,10);
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.LordJobData_DefendBase.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.targetPositionName, "targetPositionName");
            Scribe_Values.Look(ref this.faction, "faction");
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.targetPositionName != null)
            {
                result.Add(new XElement("targetPositionName", this.targetPositionName));
            }
            if (this.faction != null)
            {
                result.Add(new XElement("faction", this.faction));
            }
            return result;
        }

        public string targetPositionName;
        public string faction;
    }
}
