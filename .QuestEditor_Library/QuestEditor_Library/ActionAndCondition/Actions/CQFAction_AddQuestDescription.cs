using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_AddQuestDescription : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.DialogEvent;

        public override void Draw(ref float y, Rect inRect, float x)

        {
            object[] arguments = new object[]
            {
                y,
                inRect,
                x
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.CQFAction_AddQuestDescription.Draw(Ref:float,None:UnityEngine.Rect,None:float)", this, arguments);
            y = (float)arguments[0];
        }
        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (quest == null)
            {
                Log.Error("[CQF] CQFAction_AddQuestDescription cannot add a description because the quest is null.");
                return;
            }
            if (this.description.NullOrEmpty())
            {
                Log.Error("[CQF] CQFAction_AddQuestDescription cannot add an empty description.");
                return;
            }

            SignalArgs receivedArgs = default;
            foreach (KeyValuePair<string, TargetInfo> target in targets)
            {
                if (target.Value.HasThing)
                {
                    receivedArgs.Add(target.Value.Thing.Named(target.Key));
                }
            }

            QuestPart_DescriptionPart descriptionPart = quest.AddPart<QuestPart_DescriptionPart>();
            descriptionPart.descriptionPart = this.description.Translate();
            descriptionPart.inSignalEnable = $"Quest{quest.id}.Part{descriptionPart.Index}.CQFAddQuestDescription";
            descriptionPart.signalListenMode = QuestPart.SignalListenMode.Always;
            Find.SignalManager.SendSignal(new Signal(descriptionPart.inSignalEnable, receivedArgs));
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("description", this.description));
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.description, "description");
        }

        public string description = string.Empty;
    }
}
