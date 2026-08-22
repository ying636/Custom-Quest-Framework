using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace QuestEditor_Library
{
    public class CQFAction_StartQuestBook : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.QuestBook;

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (questDef == null)
            {
                Log.Error("CQF task book start action has no QuestScriptDef.");
                return;
            }
            QuestUtility.GenerateQuestAndMakeAvailable(questDef, new Slate());
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref questDef, "questDef");
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("questDef", questDef?.defName));
            return result;
        }

        public QuestScriptDef questDef;
    }
}
