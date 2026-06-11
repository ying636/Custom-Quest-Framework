using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public enum CQFActionCategory
    {
        FlowControl,
        SignalState,
        DataWrite,
        SpawnThing,
        MapAction,
        ThingChange,
        Pawn,
        Faction,
        VisualEffect,
        DialogEvent,
        MainMap,
        EventArea,
        Misc
    }

    public abstract class CQFAction : ISaveable, IDrawable, IExposable
    {
        public virtual CQFActionCategory ActionCategory => CQFActionCategory.Misc;

        public virtual CQFAction Copy()
        {
            XElement x = this.SaveToXElement("CQFAction");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            CQFAction result = DirectXmlToObject.ObjectFromXml<CQFAction>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }
        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.Label(rect, this.GetType().Name.Translate().Colorize(ColorLibrary.SkyBlue));
            if ((this.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (this.GetType().Name + "_Tip").Translate());
            }
            y += 30f;
        }
        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            return result;
        }
        public abstract void ExposeData();
        public abstract void Work(Dictionary<string, TargetInfo> targets, Quest quest);

    }
    public class CQFAction_Loop : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.FlowControl;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "LoopCount".Translate(), ref this.loopCount, ref this.buffer, x, 150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("loopCount", this.loopCount));
            result.Add(new XElement("buffer", this.buffer));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            for (int i = 0; i < this.loopCount;i++) 
            {
                this.actions.ForEach(a => a.Work(targets,quest));
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.loopCount, "loopCount");
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public int loopCount = 1;
        public string buffer;
        public List<CQFAction> actions = new List<CQFAction>();
    }
    public class CQFAction_DelayExecute : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.FlowControl;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y,"DelayTime".Translate(),ref this.delayTime,ref this.buffer,x,150f);
            y += 30f;
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("delayTime",this.delayTime));
            result.Add(new XElement("buffer", this.buffer));
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.actions.ForEach(a => GameComponent_Editor.Component.AddExecutiveRequest(delayTime,a,quest,targets));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.delayTime, "delayTime");
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public int delayTime = 0;
        public string buffer;
        public List<CQFAction> actions = new List<CQFAction>();
    }
    public class CQFAction_PostGenerationExecute : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.FlowControl;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.actions.ForEach(a => 
            GenStep_CustomMap.requests.Add(new ExecutiveRequest(a,quest,targets, 0)));
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }
        public List<CQFAction> actions = new List<CQFAction>();
    }
    public class CQFAction_Sequence : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.FlowControl;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.actions.ForEach(a => a.Work(targets, quest));
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public List<CQFAction> actions = new List<CQFAction>();
    }
    public class CQFAction_Random : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.FlowControl;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            this.actions.RandomElement().Work(targets, quest);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
        }

        public List<CQFAction> actions = new List<CQFAction>();
    }
    public class CQFAction_Condition : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.FlowControl;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawActionList_UseWindow(ref y, x, this.actions, inRect, "TriggerActions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
            CQFEditorTools.DrawIDrawList_UseWindow(ref y, x, this.conditions, inRect, "TriggerConditions".Translate(), a => a.GetType().Name.Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList_Saveable(this.actions, "actions"));
            result.Add(CQFEditorTools.SaveList_Saveable(this.conditions, "conditions"));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (!this.conditions.Exists(c => !c.Satisfied(targets,out string r,quest))) 
            {
                this.actions.ForEach(a =>a.Work(targets, quest));
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref this.actions, "actions", LookMode.Deep);
            Scribe_Collections.Look(ref this.conditions, "conditions", LookMode.Deep);
        }

        public List<CQFAction> actions = new List<CQFAction>();
        public List<DialogCondition> conditions = new List<DialogCondition>();
    }
    public class CQFAction_Chance : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.FlowControl;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (this.action != null)
            {
                Widgets.DrawLine(new Vector2(x, y), new Vector2(inRect.width, y), ColorLibrary.SkyBlue, 1f);
                this.action.Draw(ref y, inRect, x);
                Widgets.DrawLine(new Vector2(x, y), new Vector2(inRect.width, y), ColorLibrary.SkyBlue, 1f);
                y += 5f;
            }
            if (Widgets.ButtonText(new Rect(x, y, 150f, 25f), "SelectAction".Translate(), false))
            {
                CQFEditorTools.OpenCQFActionSelect(a => this.action = (CQFAction)Activator.CreateInstance(a));
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "LootChance".Translate(), ref this.chance, ref this.buffer, x, 150f);
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.action.SaveToXElement("action"));
            result.Add(new XElement("chance", this.chance));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (Rand.Chance(this.chance))
            {
                this.action.Work(targets, quest);
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.buffer, "buffer");
            Scribe_Values.Look(ref this.chance, "chance");
            Scribe_Deep.Look(ref this.action, "action");
        }

        public string buffer;
        public float chance;
        public CQFAction action;
    }
    public class CQFAction_SentSignal : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SignalState;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "OutSignal".Translate(), ref this.signal, x, 350f);
            y += 30f;
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.CheckboxLabeled(rect, "SignalOnlyIsValidInPart".Translate(), ref this.signalIsOnlyValidInPart);
            TooltipHandler.TipRegion(rect, "SignalOnlyIsValidInPartTip".Translate());
            y += 30f;
            rect.y += 30f;
            Widgets.CheckboxLabeled(rect, "AddQuestPrefix".Translate(), ref this.addQuestPrefix);
            TooltipHandler.TipRegion(rect, "AddQuestPrefixTip".Translate());
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("signal", this.signal));
            if (!this.addQuestPrefix)
            {
                result.Add(new XElement("addQuestPrefix", this.addQuestPrefix));
            }
            if (this.signalIsOnlyValidInPart)
            {
                result.Add(new XElement("signalIsOnlyValidInPart", this.signalIsOnlyValidInPart));
            }
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            List<string> signalParts = new List<string>();
            if (quest != null)
            {
                if (DebugSettings.godMode)
                {
                    Log.Message(quest.name);
                } 
            }

            string s = this.signal;
            if (this.addQuestPrefix && quest != null)
            {
                s = $"Quest{quest.id}." + s;
            }
            Find.SignalManager.SendSignal(new Signal(s));
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.signal, "CQFAction_SentSignal_signal");
            Scribe_Values.Look(ref this.addQuestPrefix, "CQFAction_SentSignal_addQuestPrefix");
            Scribe_Values.Look(ref this.signalIsOnlyValidInPart, "CQFAction_SentSignal_signalIsOnlyValidInPart");
        }
        [NoTranslate]
        public string signal;
        public bool signalIsOnlyValidInPart = false;
        public bool addQuestPrefix = true;
    }
    public class CQFAction_SetBool : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SignalState;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "keyOfBoolValue".Translate(), ref this.keyOfBool, x, 350f);
            y += 30f;
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.CheckboxLabeled(rect, "valueOfBool".Translate(), ref this.valueOfBool);
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("keyOfBool", this.keyOfBool));
            result.Add(new XElement("valueOfBool", this.valueOfBool));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            GameComponent_Editor.Component.GetQuestData(quest)?.SetBool(this.keyOfBool, this.valueOfBool);
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.keyOfBool, "CQFAction_Bool_keyOfBool");
            Scribe_Values.Look(ref this.valueOfBool, "CQFAction_Bool_valueOfBool");
        }

        [NoTranslate]
        public string keyOfBool;
        public bool valueOfBool;
    }
    public class CQFAction_SetGlobalBool : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.SignalState;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "keyOfBoolValue".Translate(), ref this.keyOfBool, x, 350f);
            y += 30f;
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.CheckboxLabeled(rect, "valueOfBool".Translate(), ref this.valueOfBool);
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("keyOfBool", this.keyOfBool));
            result.Add(new XElement("valueOfBool", this.valueOfBool));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            GameComponent_Editor.Component.SetBool(this.keyOfBool, this.valueOfBool);
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.keyOfBool, "CQFAction_Bool_keyOfBool");
            Scribe_Values.Look(ref this.valueOfBool, "CQFAction_Bool_valueOfBool");
        }

        [NoTranslate]
        public string keyOfBool;
        public bool valueOfBool;
    }
    public class CQFAction_Message : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFMessage".Translate(), ref this.message, x, 240f);
            y += 30f;
            if (Widgets.ButtonText(new Rect(x, y, 450f, 25f), "CQFMessageType".Translate(this.type?.defName.Translate().ToString()), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<MessageTypeDef>.AllDefsListForReading, (d) => this.type = d, (d) => d.defName.Translate());
            }
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("message", this.message));
            result.Add(new XElement("type", this.type?.defName));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (targets.TryGetValue("Trigger", out TargetInfo t1) && t1.Thing is Thing thing
                                                                  && thing.Faction != Faction.OfPlayer)
            {
                return;
            }
            var message = ResolveMessage(targets); 
            Messages.Message(message, new LookTargets(targets.Values), this.type);
        }

        public string ResolveMessage(Dictionary<string, TargetInfo> targets)
        {
            string message = this.message.Translate();
            List<NamedArgument> names = new List<NamedArgument>();
            targets.ToList().ForEach(t =>
            {
                if (t.Value.HasThing)
                {
                    names.Add(t.Value.Thing.Named(t.Key));
                }
            });
            message = message.Formatted(names);
            targets.ToList().ForEach(t =>
            {
                if (t.Value.Thing is Pawn pawn)
                {
                    message = message.AdjustedFor(pawn, t.Key, true);
                }
            });
            return message;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.message, "CQFAction_Message_message");
            Scribe_Defs.Look(ref this.type, "CQFAction_Message_type");
        }

        public string message;
        public MessageTypeDef type = MessageTypeDefOf.PositiveEvent;
    }
    public class CQFAction_Quest : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 450f, 25f), "CQFQuestDef".Translate(this.quest?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<QuestScriptDef>.AllDefsListForReading, (d) => this.quest = d, (d) => d.defName);
            }
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("quest", this.quest.defName));
            return result;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.GiveQuest, Find.RandomPlayerHomeMap);
            Quest quest2 = QuestUtility.GenerateQuestAndMakeAvailable(this.quest, incidentParms.points);
            if (!quest2.hidden && this.quest.sendAvailableLetter)
            {
                QuestUtility.SendLetterQuestAvailable(quest2);
            }
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref this.quest, "CQFAction_Quest_quest");
        }

        public QuestScriptDef quest;
    }
    public class CQFAction_Incident : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 450f, 25f), "CQFIncidentDef".Translate(this.incident?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<IncidentDef>.AllDefsListForReading, (d) => this.incident = d, (d) => d.label);
            }
            y += 30f;
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("incident", this.incident.defName));
            return result;
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            foreach (KeyValuePair<string,TargetInfo> item in targets)
            {
                if (item.Value.Map is Map map) 
                {
                    IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(this.incident.category, map);
                    this.incident.Worker.TryExecute(incidentParms);
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref this.incident, "CQFAction_Incident_incident");
        }

        public IncidentDef incident;
    }
    public class CQFAction_StartDialog : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 320f, 25f), "DialogManagerForSpawner".Translate(this.dialog?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<DialogManagerDef>.AllDefsListForReading, m => this.dialog = m, m => m.defName);
            }
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "Interviewer".Translate(), ref this.interviewerText, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.interviewerText = t, t => t.Translate()), x, 150f);
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "Interviewee".Translate(), ref this.intervieeText, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.intervieeText = t, t => t.Translate()), x, 150f);
            y += 30f;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Thing interviewer = null;
            Thing interviewee = null;
            targets.ToList().ForEach(t =>
            {
                if (t.Key == this.interviewerText)
                {
                    interviewer = t.Value.Thing;
                }
                if (t.Key == this.intervieeText)
                {
                    interviewee = t.Value.Thing;
                }
            });
            if (interviewer == null && GameTools.GetTargetFromQuestDatabase(quest, this.interviewerText) is TargetInfo target)
            {
                interviewer = target.Thing;
            }
            if (interviewee == null && GameTools.GetTargetFromQuestDatabase(quest, this.intervieeText) is TargetInfo target2)
            {
                interviewee = target2.Thing;
            }
            Find.WindowStack.Add(this.dialog.GetTree(interviewer, interviewee)?.CreateCQFDialog(
                interviewee, interviewer, quest));
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("dialog", this.dialog.defName));
            result.Add(new XElement("interviewerText", this.interviewerText));
            result.Add(new XElement("intervieeText", this.intervieeText));
            return result;
        }
        public override void ExposeData()
        {
            Scribe_Defs.Look(ref this.dialog, "dialog");
            Scribe_Values.Look(ref this.interviewerText, "interviewerText");
            Scribe_Values.Look(ref this.intervieeText, "intervieeText");
        }

        public DialogManagerDef dialog;
        [NoTranslate]
        public string interviewerText;
        [NoTranslate]
        public string intervieeText;
    }
    public class CQFAction_SetRelation : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.Pawn;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x, y, 320f, 25f), "CQF_RelationDef".Translate(this.relation?.label), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<PawnRelationDef>.AllDefsListForReading, m => this.relation = m, m => m.label);
            }
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "TargetA".Translate(), ref this.targetA, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.targetA = t, t => t.Translate()), x, 150f);
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "TargetB".Translate(), ref this.targetB, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.targetB = t, t => t.Translate()), x, 150f);
            y += 30f;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Thing targetA = null;
            Thing targetB = null;
            targets.ToList().ForEach(t =>
            {
                if (t.Key == this.targetA)
                {
                    targetA = t.Value.Thing;
                }
                if (t.Key == this.targetB)
                {
                    targetB = t.Value.Thing;
                }
            });
            if (targetA == null && GameTools.GetTargetFromQuestDatabase(quest, this.targetA) is TargetInfo target)
            {
                targetA = target.Thing;
            }
            if (targetB == null && GameTools.GetTargetFromQuestDatabase(quest, this.targetB) is TargetInfo target2)
            {
                targetB = target2.Thing;
            }
            if (targetA is Pawn pawnA && targetB is Pawn pawnB) 
            {
                pawnA.relations.AddDirectRelation(this.relation,pawnB);
            }
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("relation", this.relation.defName));
            result.Add(new XElement("targetA", this.targetA));
            result.Add(new XElement("targetB", this.targetB));
            return result;
        }
        public override void ExposeData()
        {
            Scribe_Defs.Look(ref this.relation, "relation");
            Scribe_Values.Look(ref this.targetA, "targetA");
            Scribe_Values.Look(ref this.targetB, "targetB");
        }

        public PawnRelationDef relation;
        [NoTranslate]
        public string targetA;
        [NoTranslate]
        public string targetB;
    }

    public class CQFAction_LinkEntranceAndExit : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "EntranceKey".Translate(), ref this.entranceText,
                () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.entranceText = t,
                    t => t.Translate()), x, 150f);
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "ExitKey".Translate(), ref this.exitText,
                () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.exitText = t,
                    t => t.Translate()), x, 150f);
            y += 30f;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            CustomMapEntrance entrance = null;
            CustomMapExit exit = null;
            targets.ToList().ForEach(t =>
            {
                if (t.Key == this.entranceText)
                {
                    entrance = (CustomMapEntrance)t.Value.Thing;
                }

                if (t.Key == this.exitText)
                {
                    exit = (CustomMapExit)t.Value.Thing;
                }
            });
            if (entrance == null &&
                GameTools.GetTargetFromQuestDatabase(quest, this.entranceText) is var target)
            {
                entrance = (CustomMapEntrance)target.Thing;
            }

            if (exit == null &&
                GameTools.GetTargetFromQuestDatabase(quest, this.exitText) is var target2)
            {
                exit = (CustomMapExit)target2;
            }

            if (entrance != null && exit != null)
            {
                entrance.exit = exit;
                entrance.CustomMap = exit.Map;
                exit.entrance = entrance;
            }
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("entranceText", this.entranceText));
            result.Add(new XElement("exitText", this.exitText));
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.entranceText, "entranceText");
            Scribe_Values.Look(ref this.exitText, "exitText");
        }

        [NoTranslate] public string entranceText;
        [NoTranslate] public string exitText;
    }

    public class CQFAction_Skip : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "skipedTargetText".Translate(), ref this.skipedTargetText, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.skipedTargetText = t, t => t.Translate()), x, 150f);
            y += 30f;
            CQFEditorTools.DrawSelectableText(y, "targetLocationText".Translate(), ref this.targetLocationText, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.targetLocationText = t, t => t.Translate()), x, 150f);
            y += 30f;
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Thing skipedTarget = null;
            TargetInfo targetLocation = TargetInfo.Invalid;
            targets.ToList().ForEach(t =>
            {
                if (t.Key == this.skipedTargetText)
                {
                    skipedTarget = t.Value.Thing;
                }
                if (t.Key == this.targetLocationText)
                {
                    targetLocation = t.Value;
                }
            });
            if (skipedTarget == null && GameTools.GetTargetFromQuestDatabase(quest, this.skipedTargetText) is TargetInfo target)
            {
                skipedTarget = target.Thing;
            }
            if (!targetLocation.IsValid && GameTools.GetTargetFromQuestDatabase(quest, this.targetLocationText) is TargetInfo target2)
            {
                targetLocation = target2;
            }
            if (Prefs.DevMode)
            {
                Log.Message("SkipTest");
                Log.Message("目标:"  + (skipedTarget == null));
                Log.Message(targetLocation.IsValid);
            }
            if (skipedTarget != null && targetLocation.IsValid)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("Skip");
                    Log.Message(skipedTarget.ToString());
                    Log.Message(targetLocation.ToString());
                }
                if (skipedTarget.Spawned)
                {
                    skipedTarget.DeSpawn();
                }
                GenSpawn.Spawn(skipedTarget, targetLocation.Cell, targetLocation.Map);
                if (targetLocation.Cell.Fogged(targetLocation.Map))
                {
                    FloodFillerFog.FloodUnfog(targetLocation.Cell, targetLocation.Map);
                }
            }
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("skipedTargetText", this.skipedTargetText));
            result.Add(new XElement("targetLocationText", this.targetLocationText));
            return result;
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.skipedTargetText, "skipedTargetText");
            Scribe_Values.Look(ref this.targetLocationText, "targetLocationText");
        }

        [NoTranslate]
        public string skipedTargetText;
        [NoTranslate]
        public string targetLocationText;
    }
    public class CQFAction_SkipToPlayerMap : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "skipedTargetText".Translate(), ref this.skipedTargetText, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.skipedTargetText = t, t => t.Translate()), x, 150f);
            y += 30f; 
        }
        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            Thing skipedTarget = null; 
            targets.ToList().ForEach(t =>
            {
                if (t.Key == this.skipedTargetText)
                {
                    skipedTarget = t.Value.Thing;
                } 
            });
            if (skipedTarget == null && GameTools.GetTargetFromQuestDatabase(quest, this.skipedTargetText) is TargetInfo target)
            {
                skipedTarget = target.Thing;
            } 
            if (skipedTarget != null && Find.AnyPlayerHomeMap is Map map &&
                map.AllCells.ToList().Find(c => c.Walkable(map) && c.Standable(map)
                && !c.Fogged(map)) is IntVec3 targetLocation)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("SkipToRandomPlayerMap");
                    Log.Message(skipedTarget.ToString());
                }
                if (skipedTarget.Spawned)
                {
                    skipedTarget.DeSpawn();
                } 
                GenSpawn.Spawn(skipedTarget, targetLocation,map); 
            }
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("skipedTargetText", this.skipedTargetText)); 
            return result;
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.skipedTargetText, "skipedTargetText"); 
        }

        [NoTranslate]
        public string skipedTargetText; 
    }
    
    public class CQFAction_ClearMainPawnCache : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MainMap;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.pawnName.NullOrEmpty())
            {
                return;
            }
            MainMapWorldComponent comp = MainMapWorldComponent.Component;
            if (comp == null)
            {
                return;
            }
            List<MainSite> sites = this.clearAllMainMaps ? comp.GetAllMainSites() : comp.GetMainSites(this.mainMapDef);
            foreach (MainSite site in sites)
            {
                site.RemoveMainPawnCache(this.pawnName);
            }
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.pawnName.NullOrEmpty())
            {
                result.Add(new XElement("pawnName", this.pawnName));
            }
            if (this.mainMapDef != null)
            {
                result.Add(new XElement("mainMapDef", this.mainMapDef.defName));
            }
            if (this.clearAllMainMaps)
            {
                result.Add(new XElement("clearAllMainMaps", this.clearAllMainMaps));
            }
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.pawnName, "pawnName");
            Scribe_Defs.Look(ref this.mainMapDef, "mainMapDef");
            Scribe_Values.Look(ref this.clearAllMainMaps, "clearAllMainMaps");
        }

        public string pawnName;
        public MainMapDef mainMapDef;
        public bool clearAllMainMaps;
    }

    public class CQFAction_DestroyMainSite : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MainMap;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "MainSiteKey".Translate(), ref this.key,
                () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.key = t, t => t.Translate()), x, 150f);
            y += 30f;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            MainMapWorldComponent.Component?.TryDestroyMainSiteByKey(this.key, quest, targets);
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.key.NullOrEmpty())
            {
                result.Add(new XElement("key", this.key));
            }
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.key, "key");
        }

        public string key;
    }

    public class CQFAction_RecordMainSiteVisitCount : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MainMap;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "MainSiteKey".Translate(), ref this.key,
                () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts, t => this.key = t, t => t.Translate()), x, 150f);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "RecordKeyOfData".Translate(), ref this.recordKey, x, 150f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "RecordToTemporaryBase".Translate(), ref this.recordToTemporaryBase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "RecordToQuestBase".Translate(), ref this.recordToQuestBase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 350f, 25f), "RecordToGlobalBase".Translate(), ref this.recordToGlobalBase);
            y += 30f;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (MainMapWorldComponent.Component == null ||
                !MainMapWorldComponent.Component.TryGetMainSiteByKey(this.key, quest, targets, out MainSite site))
            {
                return;
            }
            if (this.recordToTemporaryBase)
            {
                GameComponent_Editor.Component.TemporaryDatabase.SetValue(this.recordKey, site.visitCount);
            }
            if (this.recordToQuestBase)
            {
                GameComponent_Editor.Component.GetQuestData(quest)?.SetValue(this.recordKey, site.visitCount);
            }
            if (this.recordToGlobalBase)
            {
                GameComponent_Editor.Component.GlobalDatabase.SetValue(this.recordKey, site.visitCount);
            }
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (!this.key.NullOrEmpty())
            {
                result.Add(new XElement("key", this.key));
            }
            if (!this.recordKey.NullOrEmpty())
            {
                result.Add(new XElement("recordKey", this.recordKey));
            }
            if (this.recordToTemporaryBase)
            {
                result.Add(new XElement("recordToTemporaryBase", this.recordToTemporaryBase));
            }
            if (this.recordToQuestBase)
            {
                result.Add(new XElement("recordToQuestBase", this.recordToQuestBase));
            }
            if (this.recordToGlobalBase)
            {
                result.Add(new XElement("recordToGlobalBase", this.recordToGlobalBase));
            }
            return result;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.key, "key");
            Scribe_Values.Look(ref this.recordKey, "recordKey");
            Scribe_Values.Look(ref this.recordToTemporaryBase, "recordToTemporaryBase");
            Scribe_Values.Look(ref this.recordToQuestBase, "recordToQuestBase", true);
            Scribe_Values.Look(ref this.recordToGlobalBase, "recordToGlobalBase");
        }

        public string key;
        public string recordKey;
        public bool recordToQuestBase = true;
        public bool recordToTemporaryBase;
        public bool recordToGlobalBase;
    }
}
