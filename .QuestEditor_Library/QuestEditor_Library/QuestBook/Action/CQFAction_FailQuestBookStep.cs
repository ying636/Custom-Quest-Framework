using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_FailQuestBookStep : CQFAction_QuestBookStep
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.QuestBook;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            QuestBookInstance instance = FindTargetInstance(quest);
            if (instance == null)
            {
                Log.Error("CQF task book step fail action could not find a bound task book.");
                return;
            }
            instance.FailStepById(stepId, quest);
        }

    }
}
