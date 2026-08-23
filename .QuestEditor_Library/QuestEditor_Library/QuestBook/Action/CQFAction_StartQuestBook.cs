using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_StartQuestBook : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.QuestBook;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            string selected = bookDef == null ? "CQF_QuestBook_None".Translate().ToString() : (bookDef.label.NullOrEmpty() ? bookDef.defName : bookDef.label);
            Rect selectRect = new Rect(x, y, Mathf.Max(280f, inRect.width - x - 12f), 28f);
            if (Widgets.ButtonText(selectRect, "CQF_QuestBook_ActionStartQuestBook".Translate(selected), false))
            {
                CQFEditorTools.DrawFloatMenu(
                    DefDatabase<QuestBookDef>.AllDefsListForReading,
                    definition => bookDef = definition,
                    definition => definition.label.NullOrEmpty() ? definition.defName : definition.label);
            }
            y += selectRect.height + 8f;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (bookDef == null)
            {
                Log.Error("CQF task book start action has no QuestBookDef.");
                return;
            }
            if (GameComponent_QuestBook.Instance == null)
            {
                Log.Error("CQF task book start action could not find GameComponent_QuestBook.");
                return;
            }
            if (quest != null)
            {
                if (GameComponent_QuestBook.Instance.CreateInstance(bookDef, quest) == null)
                {
                    Log.Error("CQF task book start action failed to create a quest-bound instance.");
                }
            }
            else
            {
                if (GameComponent_QuestBook.Instance.CreateAutoInstance(bookDef) == null)
                {
                    Log.Error("CQF task book start action failed to create an automatic instance.");
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref bookDef, "bookDef");
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("bookDef", bookDef?.defName));
            return result;
        }

        public QuestBookDef bookDef;
    }
}
