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
                result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            }
            return result;
        }

        public void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawLabelAndText_Line(y,"LordName".Translate(),ref this.name,x,100f);
            y += 30f;
            this.Data.Draw(ref y,inRect,x);
            CQFEditorTools.DrawSelectableText(y, "MapDataFaction".Translate(), ref this.faction, () => CQFEditorTools.DrawFloatMenu<FactionDef>(DefDatabase<FactionDef>.AllDefs.ToList().FindAll((f) => !f.isPlayer), (f) => this.faction = f.defName, (f) => f.label, new List<FloatMenuOption>()
            {
                new FloatMenuOption("RandomHostile".Translate(),() => this.faction = "RandomHostile"),
                new FloatMenuOption("RandomAlly".Translate(),() => this.faction = "RandomAlly"),
                new FloatMenuOption("RandomNeutral".Translate(),() => this.faction = "RandomNeutral"),
                new FloatMenuOption("PawnDataMapFaction".Translate(),() => this.faction = "MapFaction")
            }), x, 120f);
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow_UseIcon(ref y,x,this.actions,inRect,"ActionsAfterGeneration".Translate(),a => a.GetType().Name.Translate());
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
            return (LordJob)Activator.CreateInstance(this.lordJob);
        }
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            this.DrawName(ref y,inRect,x);
            y += 30f;
            if (this.JobSelectable)
            {
                if (Widgets.ButtonText(new Rect(x, y, 350f, 25f), "CQF_LordJob".Translate(this.lordJob.Name.CanTranslate() ? this.lordJob.Name.Translate().ToString() : this.lordJob.Name), false))
                {
                    Find.WindowStack.Add(new Dialog_Select<Type>(typeof(LordJob).AllSubclassesNonAbstract(), null, (t) => t.Name.CanTranslate() ? t.Name.Translate().ToString() : t.Name, "Select".Translate()
                        , t => this.lordJob = t, null, null, (t) => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : ""));
                }
            }
            else 
            {
                Widgets.Label(new Rect(x, y, 350f, 25f), "CQF_LordJob".Translate(this.LordJob.Name.CanTranslate() ? this.LordJob.Name.Translate().ToString() : this.LordJob.Name));
            }
            y += 30f;
        }
        public virtual void DrawName(ref float y, Rect inRect, float x)
        {      
            Rect rect = new Rect(x, y, 250f, 25f);
            if (Widgets.ButtonText(rect, this.GetType().Name.Translate(), false))
            {
                List<Type> types = typeof(LordJobData).AllSubclassesNonAbstract().ListFullCopy();
                types.Add(typeof(LordJobData));
                Find.WindowStack.Add(new Dialog_Select<Type>(types, null, (t) => t.Name.CanTranslate() ? t.Name.Translate().ToString() : t.Name, "Select".Translate()
        , t =>
        {
            this.lordData.lordJobData = (LordJobData)Activator.CreateInstance(t);
            this.lordData.lordJobData.lordData = this.lordData;
        }, null, null, (t) => (t.Name + "_Tip").CanTranslate() ? (t.Name + "_Tip").Translate().ToString() : ""));
            }
            if ((this.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (this.GetType().Name + "_Tip").Translate());
            }
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            if (this.lordJob != typeof(LordJob_Custom)) 
            {
                result.Add(new XElement("lordJob", this.lordJob.FullName));
            }
            return result;
        }

        public virtual void ExposeData()
        {      
            Scribe_Values.Look(ref this.lordJob, "lordJob", typeof(LordJob_Custom),true);
        }

        public Type lordJob = typeof(LordJob_Custom);
        public LordData lordData;
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
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "TargetPositionName".Translate(),ref this.targetPositionName,x,150);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "PawnDataFaction".Translate(), ref this.faction, x,150);
            y += 30f;
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
