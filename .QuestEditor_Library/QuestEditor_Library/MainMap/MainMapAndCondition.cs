using RimWorld;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class MainMapAndCondition : IExposable, IDrawable, ISaveable
    {
        public bool Satisfied(Quest quest)
        {
            if (this.conditions.NullOrEmpty())
            {
                return true;
            }
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            foreach (DialogCondition condition in this.conditions)
            {
                if (condition != null && !condition.Satisfied(targets, out string reason, quest))
                {
                    return false;
                }
            }
            return true;
        }

        public void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.MainMapAndCondition.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            if (!this.name.NullOrEmpty())
            {
                result.Add(new XElement("name", this.name));
            }
            if (this.set != null)
            {
                result.Add(this.set.SaveToXElement("set"));
            }
            if (!this.conditions.NullOrEmpty())
            {
                result.Add(CQFSerialization.SaveList_Saveable(this.conditions, "conditions"));
            }
            return result;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.name, "name");
            Scribe_Deep.Look(ref this.set, "set");
            Scribe_Collections.Look(ref this.conditions, "conditions", LookMode.Deep);
        }

        public string name;
        public CustomMapGenerationSet set;
        public List<DialogCondition> conditions = new List<DialogCondition>();
    }
}
