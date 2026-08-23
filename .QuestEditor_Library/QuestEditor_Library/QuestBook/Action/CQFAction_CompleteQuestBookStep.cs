using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_CompleteQuestBookStep : CQFAction_QuestBookStep
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.QuestBook;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            QuestBookInstance instance = FindTargetInstance(quest);
            if (instance == null)
            {
                Log.Error("CQF task book step action could not find a bound task book.");
                return;
            }
            instance.CompleteStepById(stepId, targets);
        }

    }
}
