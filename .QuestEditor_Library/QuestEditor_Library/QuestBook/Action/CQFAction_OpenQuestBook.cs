using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_OpenQuestBook : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.QuestBook;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            QuestBookInstance instance = GameComponent_QuestBook.Instance?.FindByQuest(quest);
            if (instance == null)
            {
                Log.Error("CQF task book open action could not find a book bound to the quest.");
                return;
            }
            GameComponent_QuestBook.Instance.OpenBook(instance);
        }

        public override void ExposeData()
        {
        }
    }
}
