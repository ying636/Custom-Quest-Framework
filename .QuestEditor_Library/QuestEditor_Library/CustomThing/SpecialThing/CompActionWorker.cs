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

namespace QuestEditor_Library
{
    public class CompActionWorker : ThingComp
    {
        public Quest Quest 
        {
            get 
            {
                return GameTools.GetQuestFromThing(this.parent);
            }
        }
        public void PasteSingleComp() 
        {
            if (CQFEditorTools.actionComp != null)
            {
                this.comps.Add(CQFEditorTools.actionComp.Copy());
            }
        }
        public Dictionary<string, TargetInfo> GetTargetThis() 
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            result.Add("CustomThing",new TargetInfo(this.parent));
            return result;
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad) 
            {
                this.comps?.ForEach(s =>
                {
                    if (s.mode == ActionTriggerMode.Spawn)
                    {
                        s.actions.ForEach(a => a.Work(this.GetTargetThis(), this.Quest));
                    }
                });
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            this.comps?.ForEach(s =>
            {
                if (s.mode == ActionTriggerMode.Tick && this.parent.IsHashIntervalTick(s.tick))
                {
                    s.actions.ForEach(a => a.Work(this.GetTargetThis(),this.Quest));
                }
            });
        }
        public override void Notify_SignalReceived(Signal signal)
        {
            base.Notify_SignalReceived(signal);
            this.comps?.ForEach(s =>
            {
                if (s.mode == ActionTriggerMode.Signal && signal.tag == s.signal)                 
                {
                    s.actions.ForEach(a => a.Work(this.GetTargetThis(), this.Quest));
                }
            });
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            this.comps?.ForEach(s =>
            {
                if (s.mode == ActionTriggerMode.Damaged)
                {
                    s.actions.ForEach(a => a.Work(this.GetTargetThis(), this.Quest));
                }
            });
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            this.comps?.ForEach(s =>
            {
                if (s.mode == ActionTriggerMode.Destroy)
                {
                    Dictionary<string, TargetInfo> target = this.GetTargetThis();
                    target["CustomThing"] = new TargetInfo(this.parent.Position, previousMap);
                    s.actions.ForEach(a =>  a.Work(target,GameTools.GetQuestFromMap(previousMap)));
                }
            });
            base.PostDestroy(mode, previousMap);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref this.comps, "CQFComps", LookMode.Deep);
        }

        public List<ActionComp> comps = new List<ActionComp>();
    }

    public class ActionComp : IExposable, ISaveable, IDrawable
    {
        public ActionComp Copy()
        {
            XElement x = this.SaveToXElement("ActionComp");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            ActionComp result = DirectXmlToObject.ObjectFromXml<ActionComp>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public void Draw(ref float y, Rect inRect, float x)
        {
            CQFEditorTools.DrawLabelAndText_Line(y,"CompName".Translate(),ref this.compName,x,100f);
            Rect rectCP = new Rect(380f, y, 25f, 25f);
            if (Widgets.ButtonImage(rectCP, TexButton.Copy))
            {
                CQFEditorTools.actionComp = this.Copy();
            }
            TooltipHandler.TipRegion(rectCP, "Copy".Translate());
            y += 30f;
            if (Widgets.ButtonText(new Rect(x,y,600f,25f),
                    "CQFActionTriggerMode".Translate(("ActionTriggerMode_" + this.mode.ToString()).Translate().ToString()),false)) 
            {
                var actions = new List<ActionTriggerMode>()
                { ActionTriggerMode.Signal, ActionTriggerMode.Tick,
                    ActionTriggerMode.Damaged,ActionTriggerMode.Destroy
                    ,ActionTriggerMode.MapGeneration,ActionTriggerMode.Open};
                if (this.allowedActions != null)
                {
                    actions = this.allowedActions;
                }
                CQFEditorTools.DrawFloatMenu(actions,
                    m => this.mode = m,m => ("ActionTriggerMode_" + m.ToString()).Translate().ToString());
            }
            y += 30f;
            if (this.mode == ActionTriggerMode.Signal) 
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "InSignal".Translate(), ref this.signal, x,150f);
                y += 30f;
                Rect rect = new Rect(x, y, 350f, 25f);
                Widgets.CheckboxLabeled(rect, "SignalOnlyIsValidInPart".Translate(),ref this.signalIsOnlyValidInPart);
                TooltipHandler.TipRegion(rect, "SignalOnlyIsValidInPartTip".Translate());
                y += 30f;
            }
            if (this.mode == ActionTriggerMode.Tick)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "TickToTrigger".Translate(), ref this.tick,ref this.buffer, x);
                TooltipHandler.TipRegion(new Rect(x,y,150f,25f), "TickToTriggerTip".Translate());
                y += 30f;
            }
            CQFEditorTools.DrawActionList(ref y,x,this.actions,inRect, "InteractionActions".Translate());
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.compName, "compName");
            Scribe_Values.Look(ref this.mode, "mode");
            Scribe_Values.Look(ref this.signal, "signal");
            Scribe_Values.Look(ref this.signalIsOnlyValidInPart, "signalIsOnlyValidInPart");
            Scribe_Values.Look(ref this.tick, "tick");
            Scribe_Collections.Look(ref this.actions, "actions",LookMode.Deep);
        }

        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("compName",this.compName));
            result.Add(new XElement("mode", this.mode));
            if (this.mode == ActionTriggerMode.Signal && !this.signal.NullOrEmpty()) 
            {
                result.Add(new XElement("signal", this.signal));
                result.Add(new XElement("signalIsOnlyValidInPart", this.signalIsOnlyValidInPart));
            }
            if (this.mode == ActionTriggerMode.Tick)
            {
                result.Add(new XElement("tick", this.tick));
            }
            XElement actions = new XElement("actions");
            this.actions.ForEach(a => actions.Add(a.SaveToXElement("li")));
            result.Add(actions);
            return result;
        }

        public string buffer;
        [NoTranslate]
        public string compName = "Undefined";
        public ActionTriggerMode mode = ActionTriggerMode.None;
        [NoTranslate]
        public string signal = "";
        public bool signalIsOnlyValidInPart = false;
        public int tick = 0;
        public List<CQFAction> actions = new List<CQFAction>();

        public List<ActionTriggerMode> allowedActions;
    }

    public enum ActionTriggerMode : byte
    {
        None = 0,
        Signal = 1,
        Damaged = 2,
        StepOn = 3,
        Tick = 4,
        Destroy = 5,
        Spawn = 6,
        MapGeneration = 7,
        Open = 8,
        Down = 9,
        Kill = 10
    }
}
