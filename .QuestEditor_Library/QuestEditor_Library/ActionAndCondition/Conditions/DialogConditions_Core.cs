using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Unity.Collections;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;

namespace QuestEditor_Library
{
    public abstract class DialogCondition : ISaveable, IDrawable, IExposable
    {
        public virtual DialogCondition Copy()
        {
            XElement x = this.SaveToXElement("DialogCondition");
            XmlNode node = new XmlDocument().ReadNode(x.CreateReader()) as XmlNode;
            DialogCondition result = DirectXmlToObject.ObjectFromXml<DialogCondition>(node, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            return result;
        }

        public virtual XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.SetAttributeValue("Class", this.GetType().FullName);
            result.Add(new XElement("failReason", this.failReason));
            return result;
        }

        public virtual void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            Rect rect = new Rect(x, y, 250f, 25f);
            Widgets.Label(rect, this.GetType().Name.Translate().Colorize(CQFUIStyle.Accent));
            if ((this.GetType().Name + "_Tip").CanTranslate())
            {
                TooltipHandler.TipRegion(rect, (this.GetType().Name + "_Tip").Translate());
            }

            y += 30f;
            string source = this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason;
            string edited = source;
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFFailReason".Translate(), ref edited, x, 100f);
            if (edited != source)
                this.failReason = edited;
            y += 30f;
        }

        public abstract bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest);
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref this.failReason, "DialogCondition_Bool_failReason");
        }

        [NoTranslate]
        [CQFLocalizableText]
        public string failReason;
    }

    public abstract class DialogCondition_WithSubConditions : DialogCondition
    {
        public virtual List<DialogCondition> ChildConditions => null;

        public virtual DialogCondition ChildCondition
        {
            get => null;
            set
            {
            }
        }

        public virtual bool AllowMultipleChildConditions => true;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawIDrawList(ref y, x + 5f, this.ChildConditions, inRect, "Conditions".Translate());
            y += 30f;
        }

        protected XElement SaveChildConditions(string nodeName)
        {
            return CQFSerialization.SaveList_Saveable(this.ChildConditions, nodeName);
        }

        protected void ExposeChildConditions(ref List<DialogCondition> conditions, string label)
        {
            Scribe_Collections.Look(ref conditions, label, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && conditions == null)
            {
                conditions = new List<DialogCondition>();
            }
        }
    }

    public class DialogCondition_Bool : DialogCondition
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.boolName, "DialogCondition_Bool_boolName");
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (GameComponent_Editor.Instance.GetBool(this.boolName))
            {
                reason = null;
                return true;
            }
            else if (GameComponent_Editor.Instance.GetQuestData(quest)is QuestData data && data.GetBool(this.boolName))
            {
                reason = null;
                return true;
            }
            else
            {
                reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
                return false;
            }
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "keyOfBoolValue".Translate(), ref this.boolName, x, 100f);
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("boolName", this.boolName));
            return result;
        }

        [NoTranslate]
        public string boolName;
    }

    public class DialogCondition_Chance : DialogCondition
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.chance, "DialogCondition_Chance_chance");
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (Rand.Chance(this.chance))
            {
                reason = null;
                return true;
            }
            else
            {
                reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
                return false;
            }
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "chance".Translate(), ref this.chance, ref this.buffer, x);
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("chance", this.chance));
            return result;
        }

        [NoTranslate]
        public string buffer;
        public float chance;
    }

    public class DialogCondition_DatabaseExists : DialogCondition
    {
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (this.checkGlobalDatabase && GameComponent_Editor.Instance.GlobalDatabase.TargetDatas.Exists(d => d.key == this.targetKey && (!this.needSpawned || (d.target.HasThing && d.target.Thing.Spawned))))
            {
                reason = null;
                return true;
            }

            if (this.checkTemporaryDatabase && GameComponent_Editor.Instance.TemporaryDatabase.TargetExists(this.targetKey, this.needSpawned))
            {
                reason = null;
                return true;
            }

            if (this.checkQuestDatabase && quest != null && GameComponent_Editor.Instance.GetQuestData(quest).TargetDatas.Exists(d => d.key == this.targetKey && (!this.needSpawned || (d.target.HasThing && d.target.Thing.Spawned))))
            {
                reason = null;
                return true;
            }

            reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
            return false;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            CQFTargetKeyEditor.DrawBookField(y, "TargetKey".Translate(), this.targetKey, value => this.targetKey = value, x, 100f, inRect.width - x - 20f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "NeedSpawned".Translate(), ref this.needSpawned);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "CheckGlobalDatabase".Translate(), ref this.checkGlobalDatabase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "CheckTemporaryDatabase".Translate(), ref this.checkTemporaryDatabase);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "CheckQuestDatabase".Translate(), ref this.checkQuestDatabase);
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("targetKey", this.targetKey));
            if (this.checkGlobalDatabase)
            {
                result.Add(new XElement("checkGlobalDatabase", this.checkGlobalDatabase));
            }

            if (this.checkTemporaryDatabase)
            {
                result.Add(new XElement("checkTemporaryDatabase", this.checkTemporaryDatabase));
            }

            if (this.checkQuestDatabase)
            {
                result.Add(new XElement("checkQuestDatabase", this.checkQuestDatabase));
            }

            if (!this.needSpawned)
            {
                result.Add(new XElement("needSpawned", this.needSpawned));
            }

            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.targetKey, "targetKey");
            Scribe_Values.Look(ref this.needSpawned, "needSpawned");
            Scribe_Values.Look(ref this.checkGlobalDatabase, "checkGlobalDatabase");
            Scribe_Values.Look(ref this.checkTemporaryDatabase, "checkTemporaryDatabase");
            Scribe_Values.Look(ref this.checkQuestDatabase, "checkQuestDatabase");
        }

        [NoTranslate]
        public string targetKey;
        public bool needSpawned = true;
        public bool checkGlobalDatabase = false;
        public bool checkTemporaryDatabase = false;
        public bool checkQuestDatabase = false;
    }

    public class DialogCondition_GroupExists : DialogCondition_Target
    {
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (quest != null && base.GetTarget(targets, out Thing target, out reason, quest) && GameComponent_Editor.Instance.GetQuestData(quest).GetGroup(this.targetKey)is { } g && g.Exists(d => d == target && (!this.needSpawned || (d.Spawned))))
            {
                reason = null;
                return true;
            }

            reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
            return false;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            CQFTargetKeyEditor.DrawBookField(y, "TargetKey".Translate(), this.targetKey, value => this.targetKey = value, x, 100f, inRect.width - x - 20f);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 200f, 25f), "NeedSpawned".Translate(), ref this.needSpawned);
            y += 30f;
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("targetKey", this.targetKey));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.targetKey, "targetKey");
            Scribe_Values.Look(ref this.needSpawned, "needSpawned");
        }

        [NoTranslate]
        public string targetKey;
        public bool needSpawned = true;
    }

    public class DialogCondition_And : DialogCondition_WithSubConditions
    {
        public override List<DialogCondition> ChildConditions => this.condition;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.SaveChildConditions("condition"));
            return result;
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            string r = null;
            if (this.ChildConditions.Exists(c => !c.Satisfied(targets, out r, quest)))
            {
                reason = r;
                return false;
            }

            reason = null;
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            this.ExposeChildConditions(ref this.condition, "DialogCondition_condition");
        }

        public List<DialogCondition> condition = new List<DialogCondition>();
    }

    public class DialogCondition_Or : DialogCondition_WithSubConditions
    {
        public override List<DialogCondition> ChildConditions => this.condition;

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(this.SaveChildConditions("condition"));
            return result;
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            string r = null;
            if (this.ChildConditions.Exists(c => c.Satisfied(targets, out r, quest)))
            {
                reason = null;
                return true;
            }

            reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            this.ExposeChildConditions(ref this.condition, "DialogCondition_condition");
        }

        public List<DialogCondition> condition = new List<DialogCondition>();
    }

    public class DialogCondition_Reversal : DialogCondition_WithSubConditions
    {
        public override bool AllowMultipleChildConditions => false;
        public override DialogCondition ChildCondition { get => this.condition; set => this.condition = value; }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.condition != null)
            {
                result.Add(this.condition.SaveToXElement("condition"));
            }

            return result;
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (this.condition == null)
            {
                reason = null;
                return true;
            }

            string r = null;
            if (this.condition.Satisfied(targets, out r, quest))
            {
                reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
                return false;
            }

            reason = null;
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.condition, "condition");
        }

        public DialogCondition condition;
    }

    public class DialogCondition_QuestIsGenerated : DialogCondition
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("quest", this.quest.defName));
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            if (CQFUIStyle.ButtonText(new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 25f), "CQFQuestDef".Translate(this.quest?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<QuestScriptDef>.AllDefsListForReading, (d) => this.quest = d, (d) => d.defName);
            }

            y += 30f;
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (!Find.QuestManager?.QuestsListForReading?.Exists(q => q?.root == quest?.root) ?? true)
            {
                reason = null;
                return true;
            }

            reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.quest, "quest");
        }

        public QuestScriptDef quest;
    }

    public class DialogCondition_ColonistCount : DialogCondition
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("count", this.count));
            result.Add(new XElement("needGreater", this.needGreater));
            return result;
        }

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width);
            base.Draw(ref y, inRect, x);
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 20f), "NeedToBeGreater".Translate(), ref this.needGreater);
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "NeedCount".Translate(), ref this.count, ref this.buffer, x, 100f);
            y += 30f;
        }

        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            var count = PawnsFinder.AllMaps_FreeColonistsSpawned.Count;
            if (this.needGreater ? count > this.count : this.count > count)
            {
                reason = null;
                return true;
            }

            reason = (this.failReason.CanTranslate() ? this.failReason.Translate().ToString() : this.failReason);
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.count, "count");
            Scribe_Values.Look(ref this.needGreater, "needGreater");
        }

        internal string buffer;
        public int count;
        public bool needGreater = true;
    }
}
