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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public abstract bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest);

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref this.failReason, "DialogCondition_Bool_failReason");
        }

        [NoTranslate]
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition_WithSubConditions.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
            else if (GameComponent_Editor.Instance.GetQuestData(quest) is QuestData data && data.GetBool(this.boolName))
            {
                reason = null;
                return true;
            }
            else
            {
                reason = this.failReason.Translate();
                return false;
            }
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition_Bool.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
                reason = this.failReason.Translate();
                return false;
            }
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition_Chance.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
            if (this.checkGlobalDatabase && GameComponent_Editor.Instance.GlobalDatabase.TargetDatas.Exists(
                d => d.key == this.targetKey && 
                (!this.needSpawned || (d.target.HasThing && d.target.Thing.Spawned)))) 
            {
                reason = null;
                return true;
            }
            if (this.checkTemporaryDatabase && GameComponent_Editor.Instance.TemporaryDatabase.TargetExists(this.targetKey, this.needSpawned))
            {
                reason = null;
                return true;
            }
            if (this.checkQuestDatabase && quest != null && GameComponent_Editor.Instance.GetQuestData(quest).TargetDatas.Exists(
    d => d.key == this.targetKey &&
    (!this.needSpawned || (d.target.HasThing && d.target.Thing.Spawned))))
            {
                reason = null;
                return true;
            }
            reason = this.failReason.Translate();
            return false;
        }
        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition_DatabaseExists.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
            if (quest != null && base.GetTarget(targets, out Thing target, out reason, quest)
                              && GameComponent_Editor.Instance.GetQuestData(quest)
                    .GetGroup(this.targetKey) is {} g && g.Exists(
                        d => d == target &&
                             (!this.needSpawned || (d.Spawned))))
            {
                reason = null;
                return true;
            }

            reason = this.failReason.Translate();
            return false;
        }

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition_GroupExists.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
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
            reason = this.failReason.Translate();
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

        public override DialogCondition ChildCondition
        {
            get => this.condition;
            set => this.condition = value;
        }

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
                reason = this.failReason.Translate();
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition_QuestIsGenerated.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (!Find.QuestManager?.QuestsListForReading?.Exists(q => q?.root == quest?.root) ?? true)
            {
                reason = null;
                return true;
            }
            reason = this.failReason.Translate();
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
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.DialogCondition_ColonistCount.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            var count = PawnsFinder.AllMaps_FreeColonistsSpawned.Count;
            if (this.needGreater ? count > this.count : this.count > count)
            {
                reason = null;
                return true;
            }
            reason = this.failReason.Translate();
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
