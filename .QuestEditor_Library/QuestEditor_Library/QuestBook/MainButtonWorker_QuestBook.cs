using System.Linq;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public class MainButtonWorker_QuestBook : MainButtonWorker_ToggleTab
    {
        public override bool Visible => base.Visible && HasAvailableQuestBook();

        private static bool HasAvailableQuestBook()
        {
            return GameComponent_QuestBook.Instance?.Instances?.Any(instance =>
                instance?.bookDef != null) == true;
        }
    }
}
