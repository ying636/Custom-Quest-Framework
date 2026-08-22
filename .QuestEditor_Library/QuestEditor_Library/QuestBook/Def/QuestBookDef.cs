using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class QuestBookDef : Def
    {
        public XElement SaveToXElement(string nodeName)
        {
            XElement result = new XElement(nodeName);
            result.Add(new XElement("defName", defName));
            result.Add(new XElement("label", label));
            if (!description.NullOrEmpty())
            {
                result.Add(new XElement("description", description));
            }
            if (questDef != null)
            {
                result.Add(new XElement("questDef", questDef.defName));
            }
            result.Add(new XElement("questVisibility", questVisibility));
            result.Add(new XElement("completionAuthority", completionAuthority));
            result.Add(new XElement("autoStart", autoStart));
            result.Add(new XElement("allowSkip", allowSkip));
            if (!chapters.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(chapters, "chapters"));
            }
            if (!onStartActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onStartActions, "onStartActions"));
            }
            if (!onCompleteActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onCompleteActions, "onCompleteActions"));
            }
            if (!onFailActions.NullOrEmpty())
            {
                result.Add(CQFEditorTools.SaveList_Saveable(onFailActions, "onFailActions"));
            }
            return result;
        }

        public QuestScriptDef questDef;
        public QuestBookQuestVisibility questVisibility = QuestBookQuestVisibility.QuestAndBook;
        public QuestBookCompletionAuthority completionAuthority = QuestBookCompletionAuthority.Quest;
        public bool autoStart = true;
        public bool allowSkip;
        public List<QuestBookChapter> chapters = new List<QuestBookChapter>();
        public List<CQFAction> onStartActions = new List<CQFAction>();
        public List<CQFAction> onCompleteActions = new List<CQFAction>();
        public List<CQFAction> onFailActions = new List<CQFAction>();

        public QuestBookChapter FirstChapter => chapters.Count == 0 ? null : chapters[0];
    }
}
