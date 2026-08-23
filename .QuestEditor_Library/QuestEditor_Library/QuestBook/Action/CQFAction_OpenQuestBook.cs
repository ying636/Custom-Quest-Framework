using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_OpenQuestBook : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.QuestBook;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect descriptionRect = new Rect(x, y, Mathf.Max(280f, inRect.width - x - 12f), 38f);
            Widgets.DrawMenuSection(descriptionRect);
            Widgets.Label(new Rect(descriptionRect.x + 12f, descriptionRect.y + 9f, descriptionRect.width - 24f, 22f), "CQF_QuestBook_ActionOpenDescription".Translate().Colorize(ColorLibrary.PaleBlue));
            y += descriptionRect.height + 8f;
        }

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
