using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CustomTrap : ThingWithComps,IDrawable, ICustomThing,IPastableData
    {
        public override string Label => this.TextComp == null || !this.textComp.useCustomName ? base.Label : this.textComp.customName;
        public override string DescriptionFlavor => this.TextComp == null || !this.textComp.useCustomDescription ? base.DescriptionFlavor : this.textComp.customDescription;
        public CompCustomText TextComp
        {
            get
            {
                if (this.textComp == null)
                {
                    this.textComp = this.TryGetComp<CompCustomText>();
                }
                return this.textComp;
            }
        }
        public List<TrapComp> TrapComps 
        {
            get
            {
                if (this.trapComps == null)
                {
                    this.trapComps = new List<TrapComp>();
                }
                return this.trapComps;
            }
        }
        public Dictionary<string, TargetInfo> GetTargetThis()
        {
            Dictionary<string, TargetInfo> result = new Dictionary<string, TargetInfo>();
            result.Add("CustomThing", this);
            return result;
        }
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            Text.Font = GameFont.Small;
            CQFEditorTools.DrawLabelAndText_Line(y, "TrapName".Translate(), ref this.trapName, x, 350f);
            Rect rect = new Rect(inRect.xMax - 70f, y + 30f, 30f,30f);
            if (Widgets.ButtonImage(rect,TexButton.Copy))
            {
                CQFEditorTools.copyTrapComps.Clear();
                foreach (var trapComp in this.trapComps)
                {
                    CQFEditorTools.copyTrapComps.Add(trapComp.Copy());
                }
            } 
            rect.x += 35f;
            if (Widgets.ButtonImage(rect,TexButton.Paste))
            {
                foreach (var trapComp in CQFEditorTools.copyTrapComps)
                {
                    this.trapComps.Add(trapComp.Copy());
                }
            }
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y,x,this.TrapComps,
                inRect,"TrapComps".Translate().Colorize(ColorLibrary.LightBlue),() => 
                {
                    CQFEditorTools.DrawFloatMenu(new List<ActionTriggerMode>() { ActionTriggerMode.Signal, ActionTriggerMode.StepOn, ActionTriggerMode.Tick }, m => this.TrapComps.Add(new TrapComp() {mode = m}), m => ("ActionTriggerMode_" + m.ToString()).Translate());

                }, c => ("ActionTriggerMode_" + c.mode.ToString()).Translate());
        }
        protected override void Tick()
        {
            base.Tick();
            if (this.Spawned) 
            {
                if (this.IsHashIntervalTick(5))
                {
                    if (this.Position.GetFirstPawn(this.Map) is Pawn pawn)
                    {
                        this.Notify_PawnStepOn(pawn);
                    }
                }
                this.TrapComps.ForEach(c =>
                {
                    if (c.mode == ActionTriggerMode.Tick && c.tick != 0 &&
                    this.IsHashIntervalTick(c.tick))
                    {
                        c.actions.ForEach(a => a.Work(this.GetTargetThis(),
                            GameTools.GetQuestFromThing(this)));
                    }
                }); 
            }
        }
 
        public virtual void Notify_PawnStepOn(Pawn pawn)
        {
            this.TrapComps.ForEach(c =>
            {
                if (c.mode == ActionTriggerMode.StepOn && this.Map != null)
                {
                    c.Trigger(this, pawn);
                }
            });
        }
        
        public override void Notify_SignalReceived(Signal signal)
        {
            base.Notify_SignalReceived(signal); 
            this.TrapComps.ForEach(c =>
            {
                if (signal.tag == c.inSignal)
                {
                    c.Trigger(this);
                };
            });
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            this.TrapComps.ForEach(c =>
            {
                if (c.triggerWhenDamaged)
                {
                    c.Trigger(this);
                };
            });
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.trapName, "CustomTrap_trapName");
            Scribe_Collections.Look(ref this.trapComps, "textComp",LookMode.Deep);
        }
        public void PasteData()
        {
            this.trapComps.Clear();
            foreach (var actionComp in CQFEditorTools.copyTrapComps)
            {
                this.trapComps.Add(actionComp.Copy());
            }
        }
        public CustomThingData GetData(IntVec3 pos)
        {
            return new CustomThingData_CustomTrap(this,pos);
        }

        public string trapName = "undefined";
        public List<TrapComp> trapComps = new List<TrapComp>();
        private CompCustomText textComp = null;
    }

    public class TrapComp : IDrawable,IExposable, ISaveable
    {
        public TrapComp Copy()
        {
            XElement x = this.SaveToXElement("TrapComp");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader());
            TrapComp result = DirectXmlToObject.ObjectFromXml<TrapComp>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public void Trigger(CustomTrap trap,Pawn pawn = null)
        {
            if (Prefs.DevMode)
            {
                Log.Message("CQF Trap Trigger:" + trap.trapName);
            }
            this.actions.ForEach(a =>
            {
                if (pawn == null)
                {
                    a.Work(trap.GetTargetThis(), GameTools.GetQuestFromThing(trap));
                    return;
                }
                Dictionary<string, TargetInfo> targets = trap.GetTargetThis();
                targets.Add("Trigger", pawn);
                a.Work(targets, GameTools.GetQuestFromThing(trap));
            });
        }
        public void Draw(ref float y, Rect inRect, float x)
        {
            if (Widgets.ButtonText(new Rect(x, y, 325f, 25f), "CustomTrapMode".Translate(("ActionTriggerMode_" + this.mode.ToString()).Translate()), false))
            {
                CQFEditorTools.DrawFloatMenu(new List<ActionTriggerMode>() { ActionTriggerMode.Signal, ActionTriggerMode.StepOn, ActionTriggerMode.Tick }, m => this.mode = m, m => ("ActionTriggerMode_" + m.ToString()).Translate());
            }
            y += 30f;
            if (this.mode == ActionTriggerMode.Signal)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "TrapInSignal".Translate(), ref this.inSignal, x, 200f);
                y += 30f;
                Rect rect = new Rect(x, y, 350f, 25f);
                Widgets.CheckboxLabeled(rect, "SignalOnlyIsValidInPart".Translate(), ref this.signalIsOnlyValidInPart);
                TooltipHandler.TipRegion(rect, "SignalOnlyIsValidInPartTip".Translate());
                y += 30f;
            }
            if (this.mode == ActionTriggerMode.Tick)
            {
                CQFEditorTools.DrawLabelAndText_Line(y, "TickToTrigger".Translate(), ref this.tick, ref this.buffer, x);
                TooltipHandler.TipRegion(new Rect(x, y, 150f, 25f), "TickToTriggerTip".Translate());
                y += 30f;
            }
            Widgets.CheckboxLabeled(new Rect(x, y, 250f, 25f), "TriggerWhenDamaged".Translate(), ref this.triggerWhenDamaged);  
            TooltipHandler.TipRegion(new Rect(x, y, 125f, 30f), "CustomTrapTip".Translate());
            y += 30f;
            CQFEditorTools.DrawActionList(ref y, x, this.actions, inRect, "TrapActions".Translate());
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref this.triggerWhenDamaged, "triggerWhenDamaged");
            Scribe_Values.Look(ref this.inSignal, "CustomTrap_inSignal");
            Scribe_Values.Look(ref this.mode, "CustomTrap_mode");
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.tick, "CustomTrap_tick");
            Scribe_Values.Look(ref this.signalIsOnlyValidInPart, "signalIsOnlyValidInPart");
            Scribe_Collections.Look(ref this.actions, "CustomTrap_actions", LookMode.Deep);
        }

        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);   
            result.Add(new XElement("mode", this.mode));
            result.Add(new XElement("triggerWhenDamaged", this.triggerWhenDamaged));
            if (this.mode == ActionTriggerMode.Signal)
            {
                result.Add(new XElement("inSignal", this.inSignal));     
                result.Add(new XElement("signalIsOnlyValidInPart", this.signalIsOnlyValidInPart));
            }
            if (this.mode == ActionTriggerMode.Tick)
            {
                result.Add(new XElement("tick", this.tick));
            }
            XElement actions = new XElement("actions");
            this.actions.ForEach((x) => actions.Add(x.SaveToXElement("li")));
            result.Add(actions);
            return result;
        }

        public bool triggerWhenDamaged = true;
        public string buffer;
        public string inSignal = null;
        public int tick = 0;
        public ActionTriggerMode mode = ActionTriggerMode.None;
        public bool signalIsOnlyValidInPart = false;
        public List<CQFAction> actions = new List<CQFAction>();
    }
}
