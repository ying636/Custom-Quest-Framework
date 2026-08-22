using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_CompleteQuestBookStep : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.QuestBook;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            QuestBookInstance instance = GameComponent_QuestBook.Instance?.FindByQuest(quest);
            if (instance == null)
            {
                Log.Error("CQF task book step action could not find a bound task book.");
                return;
            }
            instance.CompleteStepById(stepId, targets);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref stepId, "stepId");
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("stepId", stepId));
            return result;
        }

        [NoTranslate]
        public string stepId;
    }
}
